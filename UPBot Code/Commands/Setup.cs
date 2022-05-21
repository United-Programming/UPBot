using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;

/// <summary>
/// This command is used to configure the bot, so roles and messages can be set for other servers.
/// author: CPU
/// </summary>
public class SlashSetup : ApplicationCommandModule {

  readonly DiscordComponentEmoji ey = new DiscordComponentEmoji(DiscordEmoji.FromUnicode("✅"));
  readonly DiscordComponentEmoji en = new DiscordComponentEmoji(DiscordEmoji.FromUnicode("❎"));
  readonly DiscordComponentEmoji el = new DiscordComponentEmoji(DiscordEmoji.FromUnicode("↖️"));
  readonly DiscordComponentEmoji er = new DiscordComponentEmoji(DiscordEmoji.FromUnicode("↘️"));
  readonly DiscordComponentEmoji ec = new DiscordComponentEmoji(DiscordEmoji.FromUnicode("❌"));
  DiscordComponentEmoji ok = null;
  DiscordComponentEmoji ko = null;



  /*
   
  /setup -> show interation
  /setup list -> dump config
  /setup tracking #channel what
  /setup spamprotection what

   
   
   */

  [SlashCommand("setup", "Configuration of the features")]
  public async Task SetupCommand(InteractionContext ctx) {
    if (ctx.Guild == null) {
      await ctx.CreateResponseAsync("I cannot be used in Direct Messages.", true);
      return;
    }
    Utils.LogUserCommand(ctx);
    ulong gid = ctx.Guild.Id;

    if (!Configs.HasAdminRole(gid, ctx.Member.Roles, false)) {
      await ctx.CreateResponseAsync("Only admins can setup the bot.", true);
      return;
    }

    var interact = ctx.Client.GetInteractivity();
    if (ok == null) {
      ok = new DiscordComponentEmoji(Utils.GetEmoji(EmojiEnum.OK));
      ko = new DiscordComponentEmoji(Utils.GetEmoji(EmojiEnum.KO));
    }

    // Basic intro message
    var msg = CreateMainConfigPage(ctx, null);
    var result = await interact.WaitForButtonAsync(msg, TimeSpan.FromMinutes(2));
    var interRes = result.Result;

    while (interRes != null && interRes.Id != "idexitconfig") {
      interRes.Handled = true;
      string cmdId = interRes.Id;

      // ******************************************************************** Back *************************************************************************
      if (cmdId == "idback") {
        msg = CreateMainConfigPage(ctx, msg);
      }

      // ***************************************************** DefAdmins ***********************************************************************************
      else if (cmdId == "iddefineadmins") {
        msg = CreateAdminsInteraction(ctx, msg);
      }

      // *********************************************************** DefAdmins.AddRole *******************************************************************************
      else if (cmdId == "idroleadd") {
        await ctx.Channel.DeleteMessageAsync(msg);
        DiscordMessage prompt = await ctx.Channel.SendMessageAsync(ctx.Member.Mention + ", please mention the roles to add (_type anything else to close_)");
        var answer = await interact.WaitForMessageAsync((dm) => {
          return (dm.Channel == ctx.Channel && dm.Author.Id == ctx.Member.Id);
        }, TimeSpan.FromMinutes(2));
        if (answer.Result != null && answer.Result.MentionedRoles.Count > 0) {
          foreach (var dr in answer.Result.MentionedRoles) {
            if (!Configs.AdminRoles[gid].Contains(dr.Id)) {
              Configs.AdminRoles[gid].Add(dr.Id);
              Database.Add(new AdminRole(gid, dr.Id));
            }
          }
        }

        await ctx.Channel.DeleteMessageAsync(prompt);
        msg = CreateAdminsInteraction(ctx, null);
      }

      // *********************************************************** DefAdmins.RemRole *******************************************************************************
      else if (cmdId.Length > 8 && cmdId[0..9] == "idrolerem") {
        await ctx.Channel.DeleteMessageAsync(msg);
        if (int.TryParse(cmdId[9..], out int rpos)) {
          ulong rid = Configs.AdminRoles[ctx.Guild.Id][rpos]; ;
          Database.DeleteByKeys<AdminRole>(gid, rid);
          Configs.AdminRoles[ctx.Guild.Id].RemoveAt(rpos);
        }

        msg = CreateAdminsInteraction(ctx, null);
      }

      // ************************************************************ DefTracking **************************************************************************
      else if (cmdId == "iddefinetracking") {
        msg = CreateTrackingInteraction(ctx, msg);
      }

      // ************************************************************ DefTracking.Change Channel ************************************************************************
      else if (cmdId == "idchangetrackch") {
        await ctx.Channel.DeleteMessageAsync(msg);
        DiscordMessage prompt = await ctx.Channel.SendMessageAsync(ctx.Member.Mention + ", please mention the channel (_use: **#**_) as tracking channel\nType _remove_ to remove the tracking channel");
        var answer = await interact.WaitForMessageAsync((dm) => {
          return (dm.Channel == ctx.Channel && dm.Author.Id == ctx.Member.Id && (dm.MentionedChannels.Count > 0 || dm.Content.Contains("remove", StringComparison.InvariantCultureIgnoreCase)));
        }, TimeSpan.FromMinutes(2));
        if (answer.Result == null || (answer.Result.MentionedChannels.Count == 0 && !answer.Result.Content.Contains("remove", StringComparison.InvariantCultureIgnoreCase))) {
          await interRes.Interaction.CreateResponseAsync(DSharpPlus.InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent("Config timed out"));
          return;
        }

        if (answer.Result.MentionedChannels.Count > 0) {
          if (Configs.TrackChannels[gid] == null) {
            TrackChannel tc = new TrackChannel();
            Configs.TrackChannels[gid] = tc;
            tc.trackJoin = true;
            tc.trackLeave = true;
            tc.trackRoles = true;
            tc.channel = answer.Result.MentionedChannels[0];
            tc.Guild = gid;
            tc.ChannelId = tc.channel.Id;
          }
          else {
            Database.Delete(Configs.TrackChannels[gid]);
            Configs.TrackChannels[gid].channel = answer.Result.MentionedChannels[0];
            Configs.TrackChannels[gid].ChannelId = Configs.TrackChannels[gid].channel.Id;
          }
          Database.Add(Configs.TrackChannels[gid]);

        }
        else if (answer.Result.Content.Contains("remove", StringComparison.InvariantCultureIgnoreCase)) {
          if (Configs.TrackChannels[gid] != null) {
            Database.Delete(Configs.TrackChannels[gid]);
            Configs.TrackChannels[gid] = null;
          }
        }

        await ctx.Channel.DeleteMessageAsync(prompt);
        msg = CreateTrackingInteraction(ctx, null);
      }

      // ************************************************************ DefTracking.Remove Tracking ************************************************************************
      else if (cmdId == "idremtrackch") {
        if (Configs.TrackChannels[gid] != null) {
          Database.Delete(Configs.TrackChannels[gid]);
          Configs.TrackChannels[gid] = null;
        }

        msg = CreateTrackingInteraction(ctx, msg);
      }

      // ************************************************************ Alter Tracking Join ************************************************************************
      else if (cmdId == "idaltertrackjoin") {
        AlterTracking(gid, true, false, false);
        msg = CreateTrackingInteraction(ctx, msg);
      }

      // ************************************************************ Alter Tracking Leave ************************************************************************
      else if (cmdId == "idaltertrackleave") {
        AlterTracking(gid, false, true, false);
        msg = CreateTrackingInteraction(ctx, msg);
      }

      // ************************************************************ Alter Tracking Roles ************************************************************************
      else if (cmdId == "idaltertrackroles") {
        AlterTracking(gid, false, false, true);
        msg = CreateTrackingInteraction(ctx, msg);
      }


      // ********* Config Spam Protection ***********************************************************************
      else if (cmdId == "idfeatrespamprotect" || cmdId == "idfeatrespamprotect0" || cmdId == "idfeatrespamprotect1" || cmdId == "idfeatrespamprotect2") {
        Config c = Configs.GetConfig(gid, Config.ParamType.SpamProtection);
        ulong val = 0;
        if (c != null) val = c.IdVal;
        ulong old = val;
        if (cmdId == "idfeatrespamprotect0") val ^= 1ul;
        if (cmdId == "idfeatrespamprotect1") val ^= 2ul;
        if (cmdId == "idfeatrespamprotect2") val ^= 4ul;
        if (val != old) {
          if (c == null) {
            c = new Config(gid, Config.ParamType.SpamProtection, val);
            Configs.TheConfigs[gid].Add(c);
          }
          else c.IdVal = val;
          Database.Add(c);
          Configs.SpamProtection[gid] = val;
        }
        msg = CreateSpamProtectInteraction(ctx, msg);
      }


      // ***************************************************** UNKNOWN ***********************************************************************************
      else {
        Utils.Log("Unknown interaction result: " + cmdId, ctx.Guild.Name);
      }
      result = await interact.WaitForButtonAsync(msg, TimeSpan.FromMinutes(2));
      interRes = result.Result;
    }
    if (interRes == null) await ctx.Channel.DeleteMessageAsync(msg); // Expired
    else await interRes.Interaction.CreateResponseAsync(DSharpPlus.InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent("Config completed"));

  }

  string GenerateSetupList(DiscordGuild g, ulong gid) {
    // list
    Config cfg;

    string msg = "Setup list for Discord Server " + g.Name + "\n";
    string part = "";
    // Admins ******************************************************
    if (Configs.AdminRoles[gid].Count == 0) msg += "**AdminRoles**: _no roles defined. Owner and roles with Admin flag will be considered bot Admins_\n";
    else {
      foreach (var rid in Configs.AdminRoles[gid]) {
        DiscordRole r = g.GetRole(rid);
        if (r != null) part += r.Name + ", ";
      }
      if (part.Length == 0) msg += "**AdminRoles**: _no roles defined. Owner and roles with Admin flag will be considered bot Admins_\n";
      else msg += "**AdminRoles**: " + part[0..^2] + "\n";
    }

    // TrackingChannel ******************************************************
    if (Configs.TrackChannels[gid] == null) msg += "**TrackingChannel**: _no tracking channel defined_\n";
    else {
      msg += "**TrackingChannel**: " + Configs.TrackChannels[gid].channel.Mention + " for ";
      if (Configs.TrackChannels[gid].trackJoin || Configs.TrackChannels[gid].trackLeave || Configs.TrackChannels[gid].trackRoles) {
        if (Configs.TrackChannels[gid].trackJoin) msg += "_Join_ ";
        if (Configs.TrackChannels[gid].trackLeave) msg += "_Leave_ ";
        if (Configs.TrackChannels[gid].trackRoles) msg += "_Roles_ ";
      }
      else msg += "nothing";
      msg += "\n";
    }

    // SpamProtection ******************************************************
    cfg = Configs.GetConfig(gid, Config.ParamType.SpamProtection);
    if (cfg == null) msg += "**Spam Protection**: _not defined (disabled by default)_\n";
    else if (cfg.IdVal == 0) msg += "**Spam Protection**: _disabled_\n";
    else msg += "**Spam Protection**: enabled for" +
        ((cfg.IdVal & 1ul) == 1 ? " _Discord_" : "") +
        ((cfg.IdVal & 2ul) == 2 ? ((cfg.IdVal & 1ul) == 1 ? ", _Steam_" : " _Steam_") : "") +
        ((cfg.IdVal & 4ul) == 4 ? ((cfg.IdVal & 1ul) != 0 ? ",  _Epic Game Store_" : " _Epic Game Store_") : "") +
        "\n";


    return msg;
  }

  [Command("SetupOLD")]
  [Description("Configration of the bot (interactive if without parameters)")]
  public async Task SetupCommand(CommandContext ctx, [RemainingText][Description("The setup command to execute")] string command) {
    if (ctx.Guild == null) {
      await ctx.RespondAsync("I cannot be used in Direct Messages.");
      return;
    }
    Utils.LogUserCommand(ctx);

    try {
      DiscordGuild g = ctx.Guild;
      ulong gid = g.Id;
      if (!Configs.HasAdminRole(gid, ctx.Member.Roles, false)) {
        await ctx.RespondAsync("Only admins can setup the bot.");
        return;
      }

      string[] cmds = command.Trim().ToLowerInvariant().Split(' ');

      // ****************** LIST *********************************************************************************************************************************************
      if (cmds[0].Equals("list") || cmds[0].Equals("dump")) {
        await Utils.DeleteDelayed(60, ctx.RespondAsync(GenerateSetupList(g, gid)));
        return;
      }

      if (cmds[0].Equals("save")) {
        string theList = GenerateSetupList(g, gid);
        string rndName = "SetupList" + DateTime.Now.Second + "Tmp" + DateTime.Now.Millisecond + ".txt";
        File.WriteAllText(rndName, theList);
        using var fs = new FileStream(rndName, FileMode.Open, FileAccess.Read);
        await Utils.DeleteFileDelayed(30, rndName);
        DiscordMessage msg = await ctx.Message.RespondAsync(new DiscordMessageBuilder().WithContent("Setup List in attachment").WithFiles(new Dictionary<string, Stream>() { { rndName, fs } }));
        await Utils.DeleteDelayed(60, msg);
        return;
      }

      // ****************** TRACKINGCHANNEL *********************************************************************************************************************************************
      if (cmds[0].Equals("trackingchannel") || cmds[0].Equals("trackchannel")) {
        // trch [empty|help] -> help
        // trch [rem|remove] -> remove
        // trch channel [j] [l] [r] -> set
        // trch [j] [l] [r] -> set on existing

        if (cmds.Length > 1) {
          TrackChannel tc = null;
          bool j = false;
          bool l = false;
          bool r = false;
          bool atLestOne = false;
          for (int i = 1; i < cmds.Length; i++) {
            string cmd = cmds[i].Trim().ToLowerInvariant();
            if (cmd == "rem") {  // Remove
              if (!Configs.TrackChannels.ContainsKey(gid) || Configs.TrackChannels[gid] == null) {
                await Utils.DeleteDelayed(15, ctx.RespondAsync("No tracking channel was defined for this server"));
              } else {
                Database.Delete(Configs.TrackChannels[gid]);
                Configs.TrackChannels[gid] = null;
              }
              break;
            } else if (cmd == "j") {
              j = true;
              atLestOne = true;
            } else if (cmd == "l") {
              l = true;
              atLestOne = true;
            } else if (cmd == "r") {
              r = true;
              atLestOne = true;
            } else { // Is it a channel?
              DiscordChannel ch = null;
              if (cmd[0] == '#') cmd = cmd[1..];
              Match cm = Configs.chnnelRefRE.Match(cmd);
              if (cm.Success) {
                if (ulong.TryParse(cm.Groups[1].Value, out ulong cid)) {
                  DiscordChannel tch = ctx.Guild.GetChannel(cid);
                  if (tch != null) ch = tch;
                }
              }
              if (ch == null) {
                foreach (DiscordChannel tch in g.Channels.Values) {
                  if (tch.Name.Equals(cmd, StringComparison.InvariantCultureIgnoreCase)) {
                    ch = tch;
                    break;
                  }
                }
              }
              if (ch != null) {
                if (!Configs.TrackChannels.ContainsKey(gid) || Configs.TrackChannels[gid] == null) {
                  tc = new TrackChannel(gid, ch.Id) {  // Create
                    trackJoin = true,
                    trackLeave = true,
                    trackRoles = true,
                    channel = ch
                  };
                  Configs.TrackChannels[gid] = tc;
                } else {
                  tc = Configs.TrackChannels[gid]; // Grab
                  tc.channel = ch;
                }
              }
            }
          }
          if (atLestOne && tc == null && Configs.TrackChannels[gid] != null)
            tc = Configs.TrackChannels[gid];
          if (tc != null) {
            if (atLestOne) {
              tc.trackJoin = j;
              tc.trackLeave = l;
              tc.trackRoles = r;
            }
            Database.Add(tc);
            j = tc.trackJoin;
            l = tc.trackLeave;
            r = tc.trackRoles;

            await Utils.DeleteDelayed(15, ctx.RespondAsync("Tracking Channel updated to " + tc.channel.Mention + " for " +
              ((!j && !l && !r) ? "_no actions_" : "") + (j ? "_Join_ " : "") + (l ? "_Leave_ " : "") + (r ? "_Roles_ " : "")));
          }
          _ = Utils.DeleteDelayed(15, ctx.Message);
        } else
          await Utils.DeleteDelayed(15, ctx.RespondAsync(
            "Use: `setup trackingchannel` _#channel_ to set a channel\n`j` to track who joins the server, `l` to track who leaves the server, 'r' to track changes on roles (_the flags can be used alone or after a channel definition_.)\n'rem' to remove the tracking channel"));

        return;
      }

      // ****************** ADMINROLES *********************************************************************************************************************************************
      if (cmds[0].Equals("adminroles") || cmds[0].Equals("admins")) {
        if (cmds.Length > 1) {
          var errs = "";
          string msg;
          // set them, or remove
          if (cmds[1].Equals("remove", StringComparison.InvariantCultureIgnoreCase)) {
            foreach (var r in Configs.AdminRoles[gid]) {
              Database.DeleteByKeys<AdminRole>(gid, r);
            }
            Configs.AdminRoles[gid].Clear();

            _ = Utils.DeleteDelayed(15, ctx.Message);
            msg = "**AdminRoles** removed";
            await Utils.DeleteDelayed(15, ctx.RespondAsync(msg));
            return;

          }
          else if (cmds[1].Equals("list", StringComparison.InvariantCultureIgnoreCase)) {
            msg = "**AdminRoles** are: ";
            if (!Configs.AdminRoles.ContainsKey(gid) || Configs.AdminRoles[gid].Count == 0) {
              int maxpos = -1;
              foreach (DiscordRole r in ctx.Guild.Roles.Values) { // Find max position numer
                if (maxpos < r.Position) maxpos = r.Position;
              }
              foreach (DiscordRole r in ctx.Guild.Roles.Values) {
                if (r.Permissions.HasFlag(DSharpPlus.Permissions.Administrator) || r.Position == maxpos)
                  msg += "**" + r.Name + "**, ";
              }
              msg += "and **_" + ctx.Guild.Owner.DisplayName + "_** (_roles are not defined, default ones are used_)";
            }
            else {
              foreach (var rid in Configs.AdminRoles[gid]) {
                DiscordRole r = g.GetRole(rid);
                if (r != null) msg += r.Mention + ", ";
              }
              msg = msg[0..^2];
            }
            if (errs.Length > 0) msg += " _Errors:_ " + errs;
            _ = Utils.DeleteDelayed(15, ctx.Message);
            await Utils.DeleteDelayed(15, ctx.RespondAsync(msg));
            return;

          }
          else {
            var guildRoles = g.Roles.Values;
            for (int i = 1; i < cmds.Length; i++) {
              // Find if we have it (and the id)
              ulong id = 0;
              Match rm = Configs.roleSnowflakeRR.Match(cmds[i]);
              if (rm.Success)
                ulong.TryParse(rm.Groups[1].Value, out id);
              else {
                foreach (var r in guildRoles)
                  if (r.Name.Equals(cmds[i], StringComparison.InvariantCultureIgnoreCase)) {
                    id = r.Id;
                    break;
                  }
              }
              if (id == 0) {
                errs += cmds[i] + " ";
                continue;
              }
              else {
                if (!Configs.AdminRoles[gid].Contains(id)) {
                  Configs.AdminRoles[gid].Add(id);
                  Database.Add(new AdminRole(gid, id));
                }
              }
            }
          }
          // And show the result
          if (Configs.AdminRoles[gid].Count == 0) msg = "No valid **AdminRoles** defined";
          else {
            msg = "**AdminRoles** are: ";
            foreach (var rid in Configs.AdminRoles[gid]) {
              DiscordRole r = g.GetRole(rid);
              if (r != null) msg += "**" + r.Name + "**, ";
            }
            msg = msg[0..^2];
          }
          if (errs.Length > 0) msg += " _Errors:_ " + errs;

          _ = Utils.DeleteDelayed(15, ctx.Message);
          await Utils.DeleteDelayed(15, ctx.RespondAsync(msg));
        }
        else
          await Utils.DeleteDelayed(15, ctx.RespondAsync("Use: `setup adminroles` _@role1 @role2 @role3_ ... (adds the roles as admins for the bot.)\nUse: `setup adminroles remove` to remove all admin roles specified\nUse: `setup adminroles list` to show who will be considered an admin (in term of roles or specific users.)"));
        return;
      }

      // ****************** SPAMPROTECTION *********************************************************************************************************************************************
      if ((cmds[0].Equals("spam") || cmds[0].Equals("spamprotection"))) {
        if (cmds.Length == 1 || cmds[1].Equals("?") || cmds[1].Equals("help")) {
          await Utils.DeleteDelayed(15, ctx.RespondAsync("Use: `spamprotection` [d] [s] [e] (checks spam links for _Discrod_, _Steam_, _EpicGameStore_, and removed them.)"));
        }
        else {
          bool edisc = false, esteam = false, eepic = false;
          for (int i = 1; i < cmds.Length; i++) {
            char mode = cmds[i][0];
            if (mode == 'd') edisc = true;
            if (mode == 's') esteam = true;
            if (mode == 'e') eepic = true;
          }
          ulong val = (edisc ? 1ul : 0) | (esteam ? 2ul : 0) | (eepic ? 4ul : 0);
          Config c = Configs.GetConfig(gid, Config.ParamType.SpamProtection);
          if (c == null) {
            c = new Config(gid, Config.ParamType.SpamProtection, val);
            Configs.TheConfigs[gid].Add(c);
          }
          else c.IdVal = val;
          Configs.SpamProtection[gid] = val;

          string msg = "SpamProtection command changed to ";
          if (val == 0) msg += "_disabled_\n";
          else msg += "enabled for" +
              ((val & 1ul) == 1 ? " _Discord_" : "") +
              ((val & 2ul) == 2 ? ((val & 1ul) == 1 ? ", _Steam_" : " _Steam_") : "") +
              ((val & 4ul) == 4 ? ((val & 1ul) != 0 ? ",  _Epic Game Store_" : " _Epic Game Store_") : "");

          Database.Add(c);
          _ = Utils.DeleteDelayed(15, ctx.Message);
          await Utils.DeleteDelayed(15, ctx.RespondAsync(msg));
        }
        return;
      }




      await Utils.DeleteDelayed(15, ctx.RespondAsync("I do not understand the command: " + ctx.Message.Content));

    } catch (Exception ex) {
      Utils.Log("Error in Setup by command line: " + ex.Message, ctx.Guild.Name);
    }
  }

  private void AlterTracking(ulong gid, bool j, bool l, bool r) {
    TrackChannel tc = Configs.TrackChannels[gid];
    if (j) tc.trackJoin = !tc.trackJoin;
    if (l) tc.trackLeave = !tc.trackLeave;
    if (r) tc.trackRoles = !tc.trackRoles;
    Database.Update(tc);
  }

 
  private DiscordMessage CreateMainConfigPage(InteractionContext ctx, DiscordMessage prevMsg) {
    if (prevMsg != null) ctx.Channel.DeleteMessageAsync(prevMsg).Wait();

    DiscordEmbedBuilder eb = new DiscordEmbedBuilder {
      Title = "UPBot Configuration"
    };
    eb.WithThumbnail(ctx.Guild.IconUrl);
    eb.Description = "Configuration of the UP Bot for the Discord Server **" + ctx.Guild.Name + "**";
    eb.WithImageUrl(ctx.Guild.BannerUrl);
    eb.WithFooter("Member that started the configuration is: " + ctx.Member.DisplayName, ctx.Member.AvatarUrl);

    var builder = new DiscordMessageBuilder();
    builder.AddEmbed(eb.Build());

    //- Set tracking
    //- Set Admins
    //- Spam Protection:
    Config sc = Configs.GetConfig(ctx.Guild.Id, Config.ParamType.SpamProtection);
    List<DiscordButtonComponent> actions = new List<DiscordButtonComponent> {
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "iddefineadmins", "Define Admins", false, er),
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "iddefinetracking", "Define Tracking channel", false, er),
      new DiscordButtonComponent((sc == null || sc.IdVal == 0) ? DSharpPlus.ButtonStyle.Secondary : DSharpPlus.ButtonStyle.Primary, "idfeatrespamprotect", "Spam Protection", false, er)
    };
    builder.AddComponents(actions);

    //-Exit
    builder.AddComponents(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Danger, "idexitconfig", "Exit", false, ec));

    return builder.SendAsync(ctx.Channel).Result;
  }

  private DiscordMessage CreateAdminsInteraction(InteractionContext ctx, DiscordMessage prevMsg) {
    if (prevMsg != null) ctx.Channel.DeleteMessageAsync(prevMsg).Wait();

    DiscordEmbedBuilder eb = new DiscordEmbedBuilder {
      Title = "UPBot Configuration - Admin roles"
    };
    eb.WithThumbnail(ctx.Guild.IconUrl);
    string desc = "Configuration of the UP Bot for the Discord Server **" + ctx.Guild.Name + "**\n\n\n" +
      "Current server roles that are considered bot administrators:\n";

    // List admin roles
    if (Configs.AdminRoles[ctx.Guild.Id].Count == 0) desc += "_**No admin roles defined.** Owner and server Admins will be used_";
    else {
      List<ulong> roles = Configs.AdminRoles[ctx.Guild.Id];
      bool one = false;
      foreach (ulong role in roles) {
        DiscordRole dr = ctx.Guild.GetRole(role);
        if (dr != null) {
          desc += dr.Mention + ", ";
          one = true;
        }
      }
      if (one) desc = desc[0..^2];
      else desc += "_**No admin roles defined.** Owner and server Admins will be used_";
    }
    eb.Description = desc;
    eb.WithImageUrl(ctx.Guild.BannerUrl);
    eb.WithFooter("Member that started the configuration is: " + ctx.Member.DisplayName, ctx.Member.AvatarUrl);

    var builder = new DiscordMessageBuilder();
    builder.AddEmbed(eb.Build());

    // - Define roles
    List<DiscordButtonComponent> actions = new List<DiscordButtonComponent>();
    builder.AddComponents(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "idroleadd", "Add roles", false, ok));
    // - Remove roles
    int num = 0;
    int cols = 0;
    foreach(ulong rid in Configs.AdminRoles[ctx.Guild.Id]) {
      DiscordRole role = ctx.Guild.GetRole(rid);
      if (role == null) {
        Database.DeleteByKeys<AdminRole>(ctx.Guild.Id, rid);
        continue;
      }
      actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "idrolerem" + num, "Remove " + role.Name, false, ko));
      num++;
      cols++;
      if (cols == 5) {
        cols = 0;
        builder.AddComponents(actions);
        actions = new List<DiscordButtonComponent>();
      }
    }
    if (cols > 0) builder.AddComponents(actions);

    // - Exit
    // - Back
    actions = new List<DiscordButtonComponent> {
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Danger, "idexitconfig", "Exit", false, ec),
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idback", "Back", false, el)
    };
    builder.AddComponents(actions);

    return builder.SendAsync(ctx.Channel).Result;
  }

  private DiscordMessage CreateTrackingInteraction(InteractionContext ctx, DiscordMessage prevMsg) {
    if (prevMsg != null) ctx.Channel.DeleteMessageAsync(prevMsg).Wait();

    TrackChannel tc = Configs.TrackChannels[ctx.Guild.Id];

    DiscordEmbedBuilder eb = new DiscordEmbedBuilder {
      Title = "UPBot Configuration - Tracking channel"
    };
    eb.WithThumbnail(ctx.Guild.IconUrl);
    string desc = "Configuration of the UP Bot for the Discord Server **" + ctx.Guild.Name + "**\n\n\n";
    if (tc == null) desc += "_**No tracking channel defined.**_";
    else {
      if (tc.channel == null) desc += "_**No tracking channel defined.**_";
      else desc += "_**Tracking channel:** " + tc.channel.Mention + "_";
    }
    eb.Description = desc;
    eb.WithImageUrl(ctx.Guild.BannerUrl);
    eb.WithFooter("Member that started the configuration is: " + ctx.Member.DisplayName, ctx.Member.AvatarUrl);

    List<DiscordButtonComponent> actions;
    var builder = new DiscordMessageBuilder();
    builder.AddEmbed(eb.Build());

    // - Change channel
    actions = new List<DiscordButtonComponent> {
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "idchangetrackch", "Change channel", false, ok)
    };
    if (Configs.TrackChannels[ctx.Guild.Id] != null)
      actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "idremtrackch", "Remove channel", false, ko));
    builder.AddComponents(actions);

    // - Actions to track
    if (tc != null) {
      actions = new List<DiscordButtonComponent>();
      if (tc.trackJoin) actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "idaltertrackjoin", "Track Joint", false, ey));
      else actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idaltertrackjoin", "Track Joint", false, en));
      if (tc.trackLeave) actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "idaltertrackleave", "Track Leave", false, ey));
      else actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idaltertrackleave", "Track Leave", false, en));
      if (tc.trackRoles) actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "idaltertrackroles", "Track Roles", false, ey));
      else actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idaltertrackroles", "Track Roles", false, en));
      builder.AddComponents(actions);
    }

    // - Exit
    // - Back
    actions = new List<DiscordButtonComponent> {
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Danger, "idexitconfig", "Exit", false, ec),
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idback", "Back", false, el)
    };
    builder.AddComponents(actions);

    return builder.SendAsync(ctx.Channel).Result;
  }

  private DiscordMessage CreateSpamProtectInteraction(InteractionContext ctx, DiscordMessage prevMsg) {
    if (prevMsg != null) ctx.Channel.DeleteMessageAsync(prevMsg).Wait();

    DiscordEmbedBuilder eb = new DiscordEmbedBuilder {
      Title = "UPBot Configuration - Spam Protection"
    };
    eb.WithThumbnail(ctx.Guild.IconUrl);
    Config c = Configs.GetConfig(ctx.Guild.Id, Config.ParamType.SpamProtection);
    bool edisc = c != null && (c.IdVal & 1) == 1;
    bool esteam = c != null && (c.IdVal & 2) == 2;
    bool eepic = c != null && (c.IdVal & 4) == 4;
    eb.Description = "Configuration of the UP Bot for the Discord Server **" + ctx.Guild.Name + "**\n\n" +
      "The **Spam Protection** is a feature of the bot used to watch all posts contain links.\n" +
      "If the link is a counterfait Discord (or Steam, or Epic) link (usually a false free nitro,\n" +
      "then the link will be immediately removed.\n\n**Spam Protection** for\n";
    eb.Description += "**Discord Nitro** feature is " + (edisc ? "_Enabled_" : "_Disabled_") + " (_recommended!_)\n";
    eb.Description += "**Steam** feature is " + (esteam ? "_Enabled_" : "_Disabled_") + "\n";
    eb.Description += "**Epic Game Store** feature is " + (eepic ? "_Enabled_" : "_Disabled_") + "\n";
    eb.WithImageUrl(ctx.Guild.BannerUrl);
    eb.WithFooter("Member that started the configuration is: " + ctx.Member.DisplayName, ctx.Member.AvatarUrl);

    List<DiscordButtonComponent> actions;
    var builder = new DiscordMessageBuilder();
    builder.AddEmbed(eb.Build());

    actions = new List<DiscordButtonComponent> {
      new DiscordButtonComponent(edisc ? DSharpPlus.ButtonStyle.Success : DSharpPlus.ButtonStyle.Danger, "idfeatrespamprotect0", "Discord Nitro", false, edisc ? ey : en),
      new DiscordButtonComponent(esteam ? DSharpPlus.ButtonStyle.Success : DSharpPlus.ButtonStyle.Danger, "idfeatrespamprotect1", "Steam", false, esteam ? ey : en),
      new DiscordButtonComponent(eepic ? DSharpPlus.ButtonStyle.Success : DSharpPlus.ButtonStyle.Danger, "idfeatrespamprotect2", "Epic", false, eepic ? ey : en)
    };
    builder.AddComponents(actions);

    // - Exit
    // - Back
    actions = new List<DiscordButtonComponent> {
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Danger, "idexitconfig", "Exit", false, ec),
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idback", "Back to Main", false, el)
    };
    builder.AddComponents(actions);

    return builder.SendAsync(ctx.Channel).Result;
  }
}
