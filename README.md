# Rocket

![rocket](https://github.com/user-attachments/assets/5ade6138-6f09-4f08-9939-f0dce5e35b4c)

A simple Discord utility bot with config file based slash command creation.

# Features

## Dynamic Voice Channels (DVC)

> [!IMPORTANT]  
> Dynamic Voice Channels can only be setup for whole categories

### Activating DVC
DVC are not activated by default and must be setup on a category basis. <br>
To do so, set the `Manage Channels` permission override in the category for the bot user.

> [!IMPORTANT]  
> Please make sure you are creating the permission override for the bot **user** and not the role

**1. Navigate to the category settings:**

![edit_category](https://github.com/user-attachments/assets/9cd90d9e-d8db-4154-a438-b815e2d6717a)

**2. Set up the permission override:**

> [!IMPORTANT]  
> If the category is private, make sure the bot has access to it through the `Add members or roles` button above

![set_permissions](https://github.com/user-attachments/assets/54e6a223-541a-490f-b6fe-729b80c47f0c)

**3. Expected outcome:**

![showcase](https://github.com/user-attachments/assets/3441a46b-e080-4823-adbb-f103fb0b6dea)

## Role Assignment Slash Command
Users can use the `/assign` or `/zuweisen` (for German discord clients) slash commands to assign themselves a role.

> [!WARNING]  
> Please be careful when adding roles to the bot since it could lead to users gaining permissions which they are not supposed to have
> 
> In order to find out how you are supposed to align your roles, please read this guide: [Role Managment 101](https://support.discord.com/hc/en-us/articles/214836687-Role-Management-101)

**Following roles are excluded automatically:**
- `@everyone`
- Managed roles (such as automatically created bot roles)
- Roles which have the `Administrator` permission
- Roles which are positioned above the highest role of the bot
- The bot's own role

**Example:**

All the roles within the green lines are self-assignable by users, whereas all the roles within the red lines are not.

> [!NOTE]
> At the moment of the screenshot the bot's highest (and only) assigned role was `Bot` (excluding the default managed bot role `Rocket` at the bottom)

![assignment](https://github.com/user-attachments/assets/9ba7da9d-497e-41df-8d62-7ac2411b3af6)
