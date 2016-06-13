﻿using Discord.Commands;
using Discord.Modules;
using NadekoBot.Classes.JSONModels;
using NadekoBot.Extensions;
using NadekoBot.Modules.Games.Commands;
using NadekoBot.Modules.Permissions.Classes;
using NadekoBot.Modules.Permissions.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Permissions
{
    internal class PermissionModule : DiscordModule
    {
        public override string Prefix { get; } = NadekoBot.Config.CommandPrefixes.Permissions;

        public PermissionModule()
        {
            commands.Add(new FilterInvitesCommand(this));
            commands.Add(new FilterWords(this));
        }

        public override void Install(ModuleManager manager)
        {
            manager.CreateCommands("", cgb =>
            {

                cgb.AddCheck(PermissionChecker.Instance);

                commands.ForEach(cmd => cmd.Init(cgb));

                cgb.CreateCommand(Prefix + "permrole")
                    .Alias(Prefix + "pr")
                    .Description("Sets a role which can change permissions. Or supply no parameters to find out the current one. Default one is 'Nadeko'.")
                    .Parameter("role", ParameterType.Unparsed)
                     .Do(async e =>
                     {
                         if (string.IsNullOrWhiteSpace(e.GetArg("role")))
                         {
                             await e.Channel.SendMessage($"Current permissions role is `{PermissionsHandler.GetServerPermissionsRoleName(e.Server)}`").ConfigureAwait(false);
                             return;
                         }

                         var arg = e.GetArg("role");
                         Discord.Role role = null;
                         try
                         {
                             role = PermissionHelper.ValidateRole(e.Server, arg);
                         }
                         catch (Exception ex)
                         {
                             Console.WriteLine(ex.Message);
                             await e.Channel.SendMessage($"Role `{arg}` probably doesn't exist. Create the role with that name first.").ConfigureAwait(false);
                             return;
                         }
                         PermissionsHandler.SetPermissionsRole(e.Server, role.Name);
                         await e.Channel.SendMessage($"Role `{role.Name}` is now required in order to change permissions.").ConfigureAwait(false);
                     });

                cgb.CreateCommand(Prefix + "rpc")
                    .Alias(Prefix + "rolepermissionscopy")
                    .Description($"Copies BOT PERMISSIONS (not discord permissions) from one role to another.\n**Usage**:`{Prefix}rpc Some Role ~ Some other role`")
                    .Parameter("from_to", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var arg = e.GetArg("from_to")?.Trim();
                        if (string.IsNullOrWhiteSpace(arg) || !arg.Contains('~'))
                            return;
                        var args = arg.Split('~').Select(a => a.Trim()).ToArray();
                        if (args.Length > 2)
                        {
                            await e.Channel.SendMessage("💢Invalid number of '~'s in the argument.");
                            return;
                        }
                        try
                        {
                            var fromRole = PermissionHelper.ValidateRole(e.Server, args[0]);
                            var toRole = PermissionHelper.ValidateRole(e.Server, args[1]);

                            PermissionsHandler.CopyRolePermissions(fromRole, toRole);
                            await e.Channel.SendMessage($"Copied permission settings from **{fromRole.Name}** to **{toRole.Name}**.");
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessage($"💢{ex.Message}");
                        }
                    });
                cgb.CreateCommand(Prefix + "cpc")
                    .Alias(Prefix + "channelpermissionscopy")
                    .Description($"Copies BOT PERMISSIONS (not discord permissions) from one channel to another.\n**Usage**:`{Prefix}cpc Some Channel ~ Some other channel`")
                    .Parameter("from_to", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var arg = e.GetArg("from_to")?.Trim();
                        if (string.IsNullOrWhiteSpace(arg) || !arg.Contains('~'))
                            return;
                        var args = arg.Split('~').Select(a => a.Trim()).ToArray();
                        if (args.Length > 2)
                        {
                            await e.Channel.SendMessage("💢Invalid number of '~'s in the argument.");
                            return;
                        }
                        try
                        {
                            var fromChannel = PermissionHelper.ValidateChannel(e.Server, args[0]);
                            var toChannel = PermissionHelper.ValidateChannel(e.Server, args[1]);

                            PermissionsHandler.CopyChannelPermissions(fromChannel, toChannel);
                            await e.Channel.SendMessage($"Copied permission settings from **{fromChannel.Name}** to **{toChannel.Name}**.");
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessage($"💢{ex.Message}");
                        }
                    });
                cgb.CreateCommand(Prefix + "upc")
                    .Alias(Prefix + "userpermissionscopy")
                    .Description($"Copies BOT PERMISSIONS (not discord permissions) from one role to another.\n**Usage**:`{Prefix}upc @SomeUser ~ @SomeOtherUser`")
                    .Parameter("from_to", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var arg = e.GetArg("from_to")?.Trim();
                        if (string.IsNullOrWhiteSpace(arg) || !arg.Contains('~'))
                            return;
                        var args = arg.Split('~').Select(a => a.Trim()).ToArray();
                        if (args.Length > 2)
                        {
                            await e.Channel.SendMessage("💢Invalid number of '~'s in the argument.");
                            return;
                        }
                        try
                        {
                            var fromUser = PermissionHelper.ValidateUser(e.Server, args[0]);
                            var toUser = PermissionHelper.ValidateUser(e.Server, args[1]);

                            PermissionsHandler.CopyUserPermissions(fromUser, toUser);
                            await e.Channel.SendMessage($"Copied permission settings from **{fromUser.ToString()}**to * *{toUser.ToString()}**.");
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessage($"💢{ex.Message}");
                        }
                    });

                cgb.CreateCommand(Prefix + "verbose")
                    .Alias(Prefix + "v")
                    .Description("Sets whether to show when a command/module is blocked.\n**Usage**: ;verbose true")
                    .Parameter("arg", ParameterType.Required)
                    .Do(async e =>
                    {
                        var arg = e.GetArg("arg");
                        var val = PermissionHelper.ValidateBool(arg);
                        PermissionsHandler.SetVerbosity(e.Server, val);
                        await e.Channel.SendMessage($"Verbosity set to {val}.").ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "serverperms")
                    .Alias(Prefix + "sp")
                    .Description("Shows banned permissions for this server.")
                    .Do(async e =>
                    {
                        var perms = PermissionsHandler.GetServerPermissions(e.Server);
                        if (string.IsNullOrWhiteSpace(perms?.ToString()))
                            await e.Channel.SendMessage("No permissions set for this server.").ConfigureAwait(false);
                        await e.Channel.SendMessage(perms.ToString()).ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "roleperms")
                    .Alias(Prefix + "rp")
                    .Description("Shows banned permissions for a certain role. No argument means for everyone.\n**Usage**: ;rp AwesomeRole")
                    .Parameter("role", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var arg = e.GetArg("role");
                        var role = e.Server.EveryoneRole;
                        if (!string.IsNullOrWhiteSpace(arg))
                            try
                            {
                                role = PermissionHelper.ValidateRole(e.Server, arg);
                            }
                            catch (Exception ex)
                            {
                                await e.Channel.SendMessage("💢 Error: " + ex.Message).ConfigureAwait(false);
                                return;
                            }

                        var perms = PermissionsHandler.GetRolePermissionsById(e.Server, role.Id);

                        if (string.IsNullOrWhiteSpace(perms?.ToString()))
                            await e.Channel.SendMessage($"No permissions set for **{role.Name}** role.").ConfigureAwait(false);
                        await e.Channel.SendMessage(perms.ToString()).ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "channelperms")
                    .Alias(Prefix + "cp")
                    .Description("Shows banned permissions for a certain channel. No argument means for this channel.\n**Usage**: ;cp #dev")
                    .Parameter("channel", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var arg = e.GetArg("channel");
                        var channel = e.Channel;
                        if (!string.IsNullOrWhiteSpace(arg))
                            try
                            {
                                channel = PermissionHelper.ValidateChannel(e.Server, arg);
                            }
                            catch (Exception ex)
                            {
                                await e.Channel.SendMessage("💢 Error: " + ex.Message).ConfigureAwait(false);
                                return;
                            }

                        var perms = PermissionsHandler.GetChannelPermissionsById(e.Server, channel.Id);
                        if (string.IsNullOrWhiteSpace(perms?.ToString()))
                            await e.Channel.SendMessage($"No permissions set for **{channel.Name}** channel.").ConfigureAwait(false);
                        await e.Channel.SendMessage(perms.ToString()).ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "userperms")
                    .Alias(Prefix + "up")
                    .Description("Shows banned permissions for a certain user. No argument means for yourself.\n**Usage**: ;up Kwoth")
                    .Parameter("user", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var user = e.User;
                        if (!string.IsNullOrWhiteSpace(e.GetArg("user")))
                            try
                            {
                                user = PermissionHelper.ValidateUser(e.Server, e.GetArg("user"));
                            }
                            catch (Exception ex)
                            {
                                await e.Channel.SendMessage("💢 Error: " + ex.Message).ConfigureAwait(false);
                                return;
                            }

                        var perms = PermissionsHandler.GetUserPermissionsById(e.Server, user.Id);
                        if (string.IsNullOrWhiteSpace(perms?.ToString()))
                            await e.Channel.SendMessage($"No permissions set for user **{user.Name}**.").ConfigureAwait(false);
                        await e.Channel.SendMessage(perms.ToString()).ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "sm").Alias(Prefix + "servermodule")
                    .Parameter("module", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Description("Sets a module's permission at the server level.\n**Usage**: ;sm [module_name] enable")
                    .Do(async e =>
                    {
                        try
                        {
                            var module = PermissionHelper.ValidateModule(e.GetArg("module"));
                            var state = PermissionHelper.ValidateBool(e.GetArg("bool"));

                            PermissionsHandler.SetServerModulePermission(e.Server, module, state);
                            await e.Channel.SendMessage($"Module **{module}** has been **{(state ? "enabled" : "disabled")}** on this server.").ConfigureAwait(false);
                        }
                        catch (ArgumentException exArg)
                        {
                            await e.Channel.SendMessage(exArg.Message).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessage("Something went terribly wrong - " + ex.Message).ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "sc").Alias(Prefix + "servercommand")
                    .Parameter("command", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Description("Sets a command's permission at the server level.\n**Usage**: ;sc [command_name] disable")
                    .Do(async e =>
                    {
                        try
                        {
                            var command = PermissionHelper.ValidateCommand(e.GetArg("command"));
                            var state = PermissionHelper.ValidateBool(e.GetArg("bool"));

                            PermissionsHandler.SetServerCommandPermission(e.Server, command, state);
                            await e.Channel.SendMessage($"Command **{command}** has been **{(state ? "enabled" : "disabled")}** on this server.").ConfigureAwait(false);
                        }
                        catch (ArgumentException exArg)
                        {
                            await e.Channel.SendMessage(exArg.Message).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessage("Something went terribly wrong - " + ex.Message).ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "rm").Alias(Prefix + "rolemodule")
                    .Parameter("module", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("role", ParameterType.Unparsed)
                    .Description("Sets a module's permission at the role level.\n**Usage**: ;rm [module_name] enable [role_name]")
                    .Do(async e =>
                    {
                        try
                        {
                            var module = PermissionHelper.ValidateModule(e.GetArg("module"));
                            var state = PermissionHelper.ValidateBool(e.GetArg("bool"));

                            if (e.GetArg("role")?.ToLower() == "all")
                            {
                                foreach (var role in e.Server.Roles)
                                {
                                    PermissionsHandler.SetRoleModulePermission(role, module, state);
                                }
                                await e.Channel.SendMessage($"Module **{module}** has been **{(state ? "enabled" : "disabled")}** for **ALL** roles.").ConfigureAwait(false);
                            }
                            else
                            {
                                var role = PermissionHelper.ValidateRole(e.Server, e.GetArg("role"));

                                PermissionsHandler.SetRoleModulePermission(role, module, state);
                                await e.Channel.SendMessage($"Module **{module}** has been **{(state ? "enabled" : "disabled")}** for **{role.Name}** role.").ConfigureAwait(false);
                            }
                        }
                        catch (ArgumentException exArg)
                        {
                            await e.Channel.SendMessage(exArg.Message).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessage("Something went terribly wrong - " + ex.Message).ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "rc").Alias(Prefix + "rolecommand")
                    .Parameter("command", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("role", ParameterType.Unparsed)
                    .Description("Sets a command's permission at the role level.\n**Usage**: ;rc [command_name] disable [role_name]")
                    .Do(async e =>
                    {
                        try
                        {
                            var command = PermissionHelper.ValidateCommand(e.GetArg("command"));
                            var state = PermissionHelper.ValidateBool(e.GetArg("bool"));

                            if (e.GetArg("role")?.ToLower() == "all")
                            {
                                foreach (var role in e.Server.Roles)
                                {
                                    PermissionsHandler.SetRoleCommandPermission(role, command, state);
                                }
                                await e.Channel.SendMessage($"Command **{command}** has been **{(state ? "enabled" : "disabled")}** for **ALL** roles.").ConfigureAwait(false);
                            }
                            else
                            {
                                var role = PermissionHelper.ValidateRole(e.Server, e.GetArg("role"));

                                PermissionsHandler.SetRoleCommandPermission(role, command, state);
                                await e.Channel.SendMessage($"Command **{command}** has been **{(state ? "enabled" : "disabled")}** for **{role.Name}** role.").ConfigureAwait(false);
                            }
                        }
                        catch (ArgumentException exArg)
                        {
                            await e.Channel.SendMessage(exArg.Message).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessage("Something went terribly wrong - " + ex.Message).ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "cm").Alias(Prefix + "channelmodule")
                    .Parameter("module", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("channel", ParameterType.Unparsed)
                    .Description("Sets a module's permission at the channel level.\n**Usage**: ;cm [module_name] enable [channel_name]")
                    .Do(async e =>
                    {
                        try
                        {
                            var module = PermissionHelper.ValidateModule(e.GetArg("module"));
                            var state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            var channelArg = e.GetArg("channel");
                            if (channelArg?.ToLower() == "all")
                            {
                                foreach (var channel in e.Server.TextChannels)
                                {
                                    PermissionsHandler.SetChannelModulePermission(channel, module, state);
                                }
                                await e.Channel.SendMessage($"Module **{module}** has been **{(state ? "enabled" : "disabled")}** on **ALL** channels.").ConfigureAwait(false);
                            }
                            else if (string.IsNullOrWhiteSpace(channelArg))
                            {
                                PermissionsHandler.SetChannelModulePermission(e.Channel, module, state);
                                await e.Channel.SendMessage($"Module **{module}** has been **{(state ? "enabled" : "disabled")}** for **{e.Channel.Name}** channel.").ConfigureAwait(false);
                            }
                            else
                            {
                                var channel = PermissionHelper.ValidateChannel(e.Server, channelArg);

                                PermissionsHandler.SetChannelModulePermission(channel, module, state);
                                await e.Channel.SendMessage($"Module **{module}** has been **{(state ? "enabled" : "disabled")}** for **{channel.Name}** channel.").ConfigureAwait(false);
                            }
                        }
                        catch (ArgumentException exArg)
                        {
                            await e.Channel.SendMessage(exArg.Message).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessage("Something went terribly wrong - " + ex.Message).ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "cc").Alias(Prefix + "channelcommand")
                    .Parameter("command", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("channel", ParameterType.Unparsed)
                    .Description("Sets a command's permission at the channel level.\n**Usage**: ;cc [command_name] enable [channel_name]")
                    .Do(async e =>
                    {
                        try
                        {
                            var command = PermissionHelper.ValidateCommand(e.GetArg("command"));
                            var state = PermissionHelper.ValidateBool(e.GetArg("bool"));

                            if (e.GetArg("channel")?.ToLower() == "all")
                            {
                                foreach (var channel in e.Server.TextChannels)
                                {
                                    PermissionsHandler.SetChannelCommandPermission(channel, command, state);
                                }
                                await e.Channel.SendMessage($"Command **{command}** has been **{(state ? "enabled" : "disabled")}** on **ALL** channels.").ConfigureAwait(false);
                            }
                            else
                            {
                                var channel = PermissionHelper.ValidateChannel(e.Server, e.GetArg("channel"));

                                PermissionsHandler.SetChannelCommandPermission(channel, command, state);
                                await e.Channel.SendMessage($"Command **{command}** has been **{(state ? "enabled" : "disabled")}** for **{channel.Name}** channel.").ConfigureAwait(false);
                            }
                        }
                        catch (ArgumentException exArg)
                        {
                            await e.Channel.SendMessage(exArg.Message).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessage("Something went terribly wrong - " + ex.Message).ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "um").Alias(Prefix + "usermodule")
                    .Parameter("module", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("user", ParameterType.Unparsed)
                    .Description("Sets a module's permission at the user level.\n**Usage**: ;um [module_name] enable [user_name]")
                    .Do(async e =>
                    {
                        try
                        {
                            var module = PermissionHelper.ValidateModule(e.GetArg("module"));
                            var state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            var user = PermissionHelper.ValidateUser(e.Server, e.GetArg("user"));

                            PermissionsHandler.SetUserModulePermission(user, module, state);
                            await e.Channel.SendMessage($"Module **{module}** has been **{(state ? "enabled" : "disabled")}** for user **{user.Name}**.").ConfigureAwait(false);
                        }
                        catch (ArgumentException exArg)
                        {
                            await e.Channel.SendMessage(exArg.Message).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessage("Something went terribly wrong - " + ex.Message).ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "uc").Alias(Prefix + "usercommand")
                    .Parameter("command", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("user", ParameterType.Unparsed)
                    .Description("Sets a command's permission at the user level.\n**Usage**: ;uc [command_name] enable [user_name]")
                    .Do(async e =>
                    {
                        try
                        {
                            var command = PermissionHelper.ValidateCommand(e.GetArg("command"));
                            var state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            var user = PermissionHelper.ValidateUser(e.Server, e.GetArg("user"));

                            PermissionsHandler.SetUserCommandPermission(user, command, state);
                            await e.Channel.SendMessage($"Command **{command}** has been **{(state ? "enabled" : "disabled")}** for user **{user.Name}**.").ConfigureAwait(false);
                        }
                        catch (ArgumentException exArg)
                        {
                            await e.Channel.SendMessage(exArg.Message).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessage("Something went terribly wrong - " + ex.Message).ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "asm").Alias(Prefix + "allservermodules")
                    .Parameter("bool", ParameterType.Required)
                    .Description("Sets permissions for all modules at the server level.\n**Usage**: ;asm [enable/disable]")
                    .Do(async e =>
                    {
                        try
                        {
                            var state = PermissionHelper.ValidateBool(e.GetArg("bool"));

                            foreach (var module in NadekoBot.Client.GetService<ModuleService>().Modules)
                            {
                                PermissionsHandler.SetServerModulePermission(e.Server, module.Name, state);
                            }
                            await e.Channel.SendMessage($"All modules have been **{(state ? "enabled" : "disabled")}** on this server.").ConfigureAwait(false);
                        }
                        catch (ArgumentException exArg)
                        {
                            await e.Channel.SendMessage(exArg.Message).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessage("Something went terribly wrong - " + ex.Message).ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "asc").Alias(Prefix + "allservercommands")
                    .Parameter("module", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Description("Sets permissions for all commands from a certain module at the server level.\n**Usage**: ;asc [module_name] [enable/disable]")
                    .Do(async e =>
                    {
                        try
                        {
                            var state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            var module = PermissionHelper.ValidateModule(e.GetArg("module"));

                            foreach (var command in NadekoBot.Client.GetService<CommandService>().AllCommands.Where(c => c.Category == module))
                            {
                                PermissionsHandler.SetServerCommandPermission(e.Server, command.Text, state);
                            }
                            await e.Channel.SendMessage($"All commands from the **{module}** module have been **{(state ? "enabled" : "disabled")}** on this server.").ConfigureAwait(false);
                        }
                        catch (ArgumentException exArg)
                        {
                            await e.Channel.SendMessage(exArg.Message).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessage("Something went terribly wrong - " + ex.Message).ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "acm").Alias(Prefix + "allchannelmodules")
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("channel", ParameterType.Unparsed)
                    .Description("Sets permissions for all modules at the channel level.\n**Usage**: ;acm [enable/disable] [channel_name]")
                    .Do(async e =>
                    {
                        try
                        {
                            var state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            var chArg = e.GetArg("channel");
                            var channel = string.IsNullOrWhiteSpace(chArg) ? e.Channel : PermissionHelper.ValidateChannel(e.Server, chArg);
                            foreach (var module in NadekoBot.Client.GetService<ModuleService>().Modules)
                            {
                                PermissionsHandler.SetChannelModulePermission(channel, module.Name, state);
                            }

                            await e.Channel.SendMessage($"All modules have been **{(state ? "enabled" : "disabled")}** for **{channel.Name}** channel.").ConfigureAwait(false);
                        }
                        catch (ArgumentException exArg)
                        {
                            await e.Channel.SendMessage(exArg.Message).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessage("Something went terribly wrong - " + ex.Message).ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "acc").Alias(Prefix + "allchannelcommands")
                    .Parameter("module", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("channel", ParameterType.Unparsed)
                    .Description("Sets permissions for all commands from a certain module at the channel level.\n**Usage**: ;acc [module_name] [enable/disable] [channel_name]")
                    .Do(async e =>
                    {
                        try
                        {
                            var state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            var module = PermissionHelper.ValidateModule(e.GetArg("module"));
                            var channel = PermissionHelper.ValidateChannel(e.Server, e.GetArg("channel"));
                            foreach (var command in NadekoBot.Client.GetService<CommandService>().AllCommands.Where(c => c.Category == module))
                            {
                                PermissionsHandler.SetChannelCommandPermission(channel, command.Text, state);
                            }
                            await e.Channel.SendMessage($"All commands from the **{module}** module have been **{(state ? "enabled" : "disabled")}** for **{channel.Name}** channel.").ConfigureAwait(false);
                        }
                        catch (ArgumentException exArg)
                        {
                            await e.Channel.SendMessage(exArg.Message).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessage("Something went terribly wrong - " + ex.Message).ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "arm").Alias(Prefix + "allrolemodules")
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("role", ParameterType.Unparsed)
                    .Description("Sets permissions for all modules at the role level.\n**Usage**: ;arm [enable/disable] [role_name]")
                    .Do(async e =>
                    {
                        try
                        {
                            var state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            var role = PermissionHelper.ValidateRole(e.Server, e.GetArg("role"));
                            foreach (var module in NadekoBot.Client.GetService<ModuleService>().Modules)
                            {
                                PermissionsHandler.SetRoleModulePermission(role, module.Name, state);
                            }

                            await e.Channel.SendMessage($"All modules have been **{(state ? "enabled" : "disabled")}** for **{role.Name}** role.").ConfigureAwait(false);
                        }
                        catch (ArgumentException exArg)
                        {
                            await e.Channel.SendMessage(exArg.Message).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessage("Something went terribly wrong - " + ex.Message).ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "arc").Alias(Prefix + "allrolecommands")
                    .Parameter("module", ParameterType.Required)
                    .Parameter("bool", ParameterType.Required)
                    .Parameter("channel", ParameterType.Unparsed)
                    .Description("Sets permissions for all commands from a certain module at the role level.\n**Usage**: ;arc [module_name] [enable/disable] [role_name]")
                    .Do(async e =>
                    {
                        try
                        {
                            var state = PermissionHelper.ValidateBool(e.GetArg("bool"));
                            var module = PermissionHelper.ValidateModule(e.GetArg("module"));
                            var role = PermissionHelper.ValidateRole(e.Server, e.GetArg("channel"));
                            foreach (var command in NadekoBot.Client.GetService<CommandService>().AllCommands.Where(c => c.Category == module))
                            {
                                PermissionsHandler.SetRoleCommandPermission(role, command.Text, state);
                            }
                            await e.Channel.SendMessage($"All commands from the **{module}** module have been **{(state ? "enabled" : "disabled")}** for **{role.Name}** role.").ConfigureAwait(false);
                        }
                        catch (ArgumentException exArg)
                        {
                            await e.Channel.SendMessage(exArg.Message).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessage("Something went terribly wrong - " + ex.Message).ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand(Prefix + "ubl")
                    .Description("Blacklists a mentioned user.\n**Usage**: ;ubl [user_mention]")
                    .Parameter("user", ParameterType.Unparsed)
                    .AddCheck(SimpleCheckers.OwnerOnly())
                    .Do(async e =>
                    {
                        await Task.Run(async () =>
                        {
                            if (!e.Message.MentionedUsers.Any()) return;
                            var usr = e.Message.MentionedUsers.First();
                            NadekoBot.Config.UserBlacklist.Add(usr.Id);
                            ConfigHandler.SaveConfig();
                            await e.Channel.SendMessage($"`Sucessfully blacklisted user {usr.Name}`").ConfigureAwait(false);
                        }).ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "uubl")
                   .Description($"Unblacklists a mentioned user.\n**Usage**: {Prefix}uubl [user_mention]")
                   .Parameter("user", ParameterType.Unparsed)
                   .AddCheck(SimpleCheckers.OwnerOnly())
                   .Do(async e =>
                   {
                       await Task.Run(async () =>
                       {
                           if (!e.Message.MentionedUsers.Any()) return;
                           var usr = e.Message.MentionedUsers.First();
                           if (NadekoBot.Config.UserBlacklist.Contains(usr.Id))
                           {
                               NadekoBot.Config.UserBlacklist.Remove(usr.Id);
                               ConfigHandler.SaveConfig();
                               await e.Channel.SendMessage($"`Sucessfully unblacklisted user {usr.Name}`").ConfigureAwait(false);
                           }
                           else
                           {
                               await e.Channel.SendMessage($"`{usr.Name} was not in blacklist`").ConfigureAwait(false);
                           }
                       }).ConfigureAwait(false);
                   });

                cgb.CreateCommand(Prefix + "cbl")
                    .Description("Blacklists a mentioned channel (#general for example).\n**Usage**: ;cbl [channel_mention]")
                    .Parameter("channel", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        await Task.Run(async () =>
                        {
                            if (!e.Message.MentionedChannels.Any()) return;
                            var ch = e.Message.MentionedChannels.First();
                            NadekoBot.Config.UserBlacklist.Add(ch.Id);
                            ConfigHandler.SaveConfig();
                            await e.Channel.SendMessage($"`Sucessfully blacklisted channel {ch.Name}`").ConfigureAwait(false);
                        }).ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "cubl")
                    .Description("Unblacklists a mentioned channel (#general for example).\n**Usage**: ;cubl [channel_mention]")
                    .Parameter("channel", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        await Task.Run(async () =>
                        {
                            if (!e.Message.MentionedChannels.Any()) return;
                            var ch = e.Message.MentionedChannels.First();
                            NadekoBot.Config.UserBlacklist.Remove(ch.Id);
                            ConfigHandler.SaveConfig();
                            await e.Channel.SendMessage($"`Sucessfully blacklisted channel {ch.Name}`").ConfigureAwait(false);
                        }).ConfigureAwait(false);
                    });

                cgb.CreateCommand(Prefix + "sbl")
                    .Description("Blacklists a server by a name or id (#general for example). **BOT OWNER ONLY**\n**Usage**: ;sbl [servername/serverid]")
                    .Parameter("server", ParameterType.Unparsed)
                    .AddCheck(SimpleCheckers.OwnerOnly())
                    .Do(async e =>
                    {
                        await Task.Run(async () =>
                        {
                            var arg = e.GetArg("server")?.Trim();
                            if (string.IsNullOrWhiteSpace(arg))
                                return;
                            var server = NadekoBot.Client.Servers.FirstOrDefault(s => s.Id.ToString() == arg) ??
                                         NadekoBot.Client.FindServers(arg.Trim()).FirstOrDefault();
                            if (server == null)
                            {
                                await e.Channel.SendMessage("Cannot find that server").ConfigureAwait(false);
                                return;
                            }
                            var serverId = server.Id;
                            NadekoBot.Config.ServerBlacklist.Add(serverId);
                            ConfigHandler.SaveConfig();
                            //cleanup trivias and typeracing
                            Modules.Games.Commands.Trivia.TriviaGame trivia;
                            TriviaCommands.RunningTrivias.TryRemove(serverId, out trivia);
                            TypingGame typeracer;
                            SpeedTyping.RunningContests.TryRemove(serverId, out typeracer);

                            await e.Channel.SendMessage($"`Sucessfully blacklisted server {server.Name}`").ConfigureAwait(false);
                        }).ConfigureAwait(false);
                    });
            });
        }
    }
}
