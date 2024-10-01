using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using Rocket.Commands.Config;
using Rocket.Commands.Handler;
using Rocket.Utils;
using Serilog;
using Serilog.Core;

namespace Rocket;

internal static class Program
{
    private static DiscordSocketClient _client = null!;
    private static Logger _logger = null!;
    private static DefaultCommandHandler _commandHandler = null!;

    private static async Task Main()
    {
        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            DefaultRetryMode = RetryMode.RetryRatelimit,
            LogLevel = LogSeverity.Info,
            GatewayIntents = GatewayIntents.Guilds
        });
        _logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File(EnvironmentUtils.GetVariable("ROCKET_LOG_FILE", "rocket-.log"),
                rollingInterval: RollingInterval.Day)
            .CreateLogger();
        _commandHandler = new DefaultCommandHandler();
        var token = EnvironmentUtils.GetVariable("ROCKET_DISCORD_TOKEN");

        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.Fatal("Environment variable `ROCKET_DISCORD_TOKEN` is missing");
            return;
        }

        InitializeDiscordClient();

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        await Task.Delay(Timeout.Infinite);
    }

    private static async Task CreateSlashCommands()
    {
        var path = Path.GetFullPath(EnvironmentUtils.GetVariable("ROCKET_COMMAND_PATH", "config"));
        var directoryInfo = new DirectoryInfo(path);

        if (!directoryInfo.Exists)
        {
            _logger.Warning("No commands found under {Path}", path);
            return;
        }

        var existingCommands = await _client.GetGlobalApplicationCommandsAsync();
        var commandDeserializationTasks = directoryInfo.GetFiles().Select(async file =>
        {
            using var reader = file.OpenText();
            var content = await reader.ReadToEndAsync();
            return JsonConvert.DeserializeObject<CommandConfig>(content);
        });
        var commandConfigs = await Task.WhenAll(commandDeserializationTasks);

        _logger.Information("Found {CommandConfigs} command configs under {Path}", commandConfigs.Length, path);

        var commandCreationTasks = commandConfigs
            .Where(commandConfig => commandConfig != null)
            .Where(commandConfig => existingCommands.All(command => command.Name != commandConfig!.Name))
            .Select(commandConfig =>
            {
                var command = commandConfig!.ToSlashCommand();

                if (!commandConfig.IsGuildCommand)
                    return _client.CreateGlobalApplicationCommandAsync(command);

                var guild = _client.GetGuild(commandConfig.GuildId.GetValueOrDefault());
                return guild.CreateApplicationCommandAsync(command);
            });
        var commands = await Task.WhenAll(commandCreationTasks);
        var skipped = commandConfigs.Length - commands.Length;

        _logger.Information("Created {Commands} new and skipped {Skipped} existing commands", commands.Length, skipped);
    }

    private static Task RegisterSlashCommandHandler()
    {
        _commandHandler.Register(new AssignCommandHandler());

        return Task.CompletedTask;
    }

    private static void InitializeDiscordClient()
    {
        _client.Log += message =>
        {
            switch (message.Severity)
            {
                case LogSeverity.Critical:
                    _logger.Fatal(message.Exception, message.Message);
                    break;
                case LogSeverity.Error:
                    _logger.Error(message.Exception, message.Message);
                    break;
                case LogSeverity.Warning:
                    _logger.Warning(message.Exception, message.Message);
                    break;
                case LogSeverity.Info:
                    _logger.Information(message.Exception, message.Message);
                    break;
                case LogSeverity.Verbose:
                    _logger.Verbose(message.Exception, message.Message);
                    break;
                case LogSeverity.Debug:
                    _logger.Debug(message.Exception, message.Message);
                    break;
                default:
                    _logger.Information(message.Exception, message.Message);
                    break;
            }

            return Task.CompletedTask;
        };
        _client.Ready += CreateSlashCommands;
        _client.Ready += RegisterSlashCommandHandler;
        _client.Ready += async () =>
        {
            await _client.SetCustomStatusAsync("Ready for takeoff!");
            await _client.SetStatusAsync(UserStatus.DoNotDisturb);
        };
        _client.SlashCommandExecuted += command => _commandHandler.HandleAsync(command);
    }
}