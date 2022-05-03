using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity.Extensions;

/// <summary>
/// This command is used to configure the bot, so roles and messages can be set for other servers.
/// author: CPU
/// </summary>
public class Setup : BaseCommandModule {

  readonly DiscordComponentEmoji ey = new DiscordComponentEmoji(DiscordEmoji.FromUnicode("✅"));
  readonly DiscordComponentEmoji en = new DiscordComponentEmoji(DiscordEmoji.FromUnicode("❎"));
  readonly DiscordComponentEmoji el = new DiscordComponentEmoji(DiscordEmoji.FromUnicode("↖️"));
  readonly DiscordComponentEmoji er = new DiscordComponentEmoji(DiscordEmoji.FromUnicode("↘️"));
  readonly DiscordComponentEmoji ec = new DiscordComponentEmoji(DiscordEmoji.FromUnicode("❌"));
  DiscordComponentEmoji ok = null;
  DiscordComponentEmoji ko = null;


  /**************************** Interaction *********************************/
  [Command("Setup")]
  [Description("Configration of the bot (interactive if without parameters)")]
  public async Task SetupCommand(CommandContext ctx) {
    if (ctx.Guild == null) {
      await ctx.RespondAsync("I cannot be used in Direct Messages.");
      return;
    }
    Utils.LogUserCommand(ctx);
    ulong gid = ctx.Guild.Id;

    if (!Configs.HasAdminRole(gid, ctx.Member.Roles, false)) {
      await ctx.RespondAsync("Only admins can setup the bot.");
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

      // *************************************************************** ConfigFeats ***********************************************************************
      else if (cmdId == "idconfigfeats") {
        msg = CreateFeaturesInteraction(ctx, msg);
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

      // ********* Config Scoring emojis ***********************************************************************
      else if (cmdId == "idfeatscoresere" || cmdId == "idfeatscoresefe") {
        if (msg != null) await ctx.Channel.DeleteMessageAsync(msg);
        msg = null;
        string emjs = "";
        bool missing = true;
        WhatToTrack wtt = cmdId == "idfeatscoresere" ? WhatToTrack.Reputation : WhatToTrack.Fun;
        foreach (var e in Configs.RepEmojis[gid].Values)
          if (e.HasFlag(wtt)) {
            emjs += e.GetEmoji(ctx.Guild) + " ";
            missing = false;
          }
        if (missing) emjs += " (_No emojis defined!_)";

        DiscordMessage prompt = await ctx.Channel.SendMessageAsync(ctx.Member.Mention + ", type all the emojis to be used for _" + (cmdId == "idfeatscoresere" ? "Reputation" : "Fun") + "_, you have 5 minutes before timeout\nCurrent emojis are: " + emjs);
        var answer = await interact.WaitForMessageAsync((dm) => {
          return (dm.Channel == ctx.Channel && dm.Author.Id == ctx.Member.Id);
        }, TimeSpan.FromMinutes(5));
        if (answer.Result == null || answer.Result.Content.Length < 4) {
          _ = Utils.DeleteDelayed(10, ctx.Channel.SendMessageAsync("Config timed out"));
        }
        else {
          Dictionary<ulong, ReputationEmoji> eset = new Dictionary<ulong, ReputationEmoji>();

          // Start by grabbing all values that are snowflakes
          string resp = answer.Result.Content.Trim();
          resp = Configs.emjSnowflakeRE.Replace(resp, (m) => {
            if (ulong.TryParse(m.Groups[1].Value, out ulong id)) {
              var rem = new ReputationEmoji(gid, id, null, wtt);
              eset.Add(rem.GetKeyValue(), rem);
            }

            return "";
          });
          // And then the values of the unicode emojis regex
          resp = Configs.emjUnicodeRE.Replace(resp, (m) => {
            var rem = new ReputationEmoji(gid, 0, m.Value, wtt);
            eset.Add(rem.GetKeyValue(), rem);
            return "";
          });

          /*
            in set not in dic -> add
            in set & in dic for other -> change (add should be enough)
            in set & in dic for same -> do nothing
            !in set & in dic for other -> do nothing
            !in set & in dic for same -> remove
          */


          // Remove all entries that are no more in the list
          List<ulong> toRemove = new List<ulong>();
          foreach (var ek in Configs.RepEmojis[gid].Keys) {
            if (Configs.RepEmojis[gid][ek].HasFlag(wtt) && !eset.ContainsKey(ek)) toRemove.Add(ek);
          }
          foreach (var e in toRemove) {
            ReputationEmoji re = Configs.RepEmojis[gid][e];
            Configs.RepEmojis[gid].Remove(e);
            Database.Delete(re);
          }
          // Add all missing entries
          foreach (var ek in eset.Keys) {
            Configs.RepEmojis[gid][ek] = eset[ek];
            Database.Add(eset[ek]);
          }

          await ctx.Channel.DeleteMessageAsync(prompt);
        }
        msg = CreateScoresInteraction(ctx, msg);
      }

      // ********* Config Scoring ***********************************************************************
      else if (cmdId.Length > 10 && cmdId[0..11] == "idfeatscore") {
        Config c = Configs.GetConfig(gid, Config.ParamType.Scores);
        ulong val = 0;
        if (c != null) val = c.IdVal;
        ulong old = val;
        if (cmdId == "idfeatscorese0") val ^= (ulong)WhatToTrack.Reputation;
        if (cmdId == "idfeatscorese1") val ^= (ulong)WhatToTrack.Fun;
        if (cmdId == "idfeatscorese2") val ^= (ulong)WhatToTrack.Thanks;
        if (cmdId == "idfeatscorese3") val ^= (ulong)WhatToTrack.Rank;
        if (cmdId == "idfeatscorese4") val ^= (ulong)WhatToTrack.Mention;
        if (val != old) {
          if (c == null) {
            c = new Config(gid, Config.ParamType.Scores, val);
            Configs.TheConfigs[gid].Add(c);
          }
          else c.IdVal = val;
          Database.Add(c);
          Configs.WhatToTracks[gid] = (WhatToTrack)val;
        }
        msg = CreateScoresInteraction(ctx, msg);
      }

      // ********* Emoji for roles ***********************************************************************
      else if (cmdId == "idfeatem4r") {
        msg = CreateEmoji4RoleInteraction(ctx, msg);
      }

      // ********* Emoji for roles.EanbleDisable ***********************************************************************
      else if (cmdId == "idfeatem4rendis") {
        if (Configs.GetConfigValue(gid, Config.ParamType.Emoji4Role) == Config.ConfVal.NotAllowed) Configs.SetConfigValue(gid, Config.ParamType.Emoji4Role, Config.ConfVal.Everybody);
        else Configs.SetConfigValue(gid, Config.ParamType.Emoji4Role, Config.ConfVal.NotAllowed);
        msg = CreateEmoji4RoleInteraction(ctx, msg);
      }

      // ********* Emoji for roles.List No ***********************************************************************
      else if (cmdId == "idfeatem4rshow0") {
        Configs.SetConfigValue(gid, Config.ParamType.Emoji4RoleList, Config.ConfVal.NotAllowed);
        msg = CreateEmoji4RoleInteraction(ctx, msg);
      }

      // ********* Emoji for roles.List Admins ***********************************************************************
      else if (cmdId == "idfeatem4rshow1") {
        Configs.SetConfigValue(gid, Config.ParamType.Emoji4RoleList, Config.ConfVal.OnlyAdmins);
        msg = CreateEmoji4RoleInteraction(ctx, msg);
      }

      // ********* Emoji for roles.List All ***********************************************************************
      else if (cmdId == "idfeatem4rshow2") {
        Configs.SetConfigValue(gid, Config.ParamType.Emoji4RoleList, Config.ConfVal.Everybody);
        msg = CreateEmoji4RoleInteraction(ctx, msg);
      }

      // ********* Emoji for roles.Add (select role) ***********************************************************************
      else if (cmdId == "idfeatem4radd") { // Just show the interaction with the list of roles
        msg = CreateEmoji4RoleInteractionRoleSelect(ctx, msg);
      }

      // ********* Emoji for roles.Add (role selected) ***********************************************************************
      else if (cmdId.Length > 13 && cmdId[0..14] == "idfeatem4raddr") { // Role selected, do the message to add the emoji to the post
        Configs.TempRoleSelected[gid] = null;
        int.TryParse(cmdId[14..], out int rnum);
        var roles = ctx.Guild.Roles.Values;
        int num = 0;
        foreach (var r in roles) {
          if (r.IsManaged || r.Permissions.HasFlag(DSharpPlus.Permissions.Administrator) || r.Position == 0) continue;
          if (num == rnum) {
            Configs.TempRoleSelected[gid] = new TempSetRole(ctx.User.Id, r);
            break;
          }
          num++;
        }
        if (Configs.TempRoleSelected[gid] == null) { // Something wrong, show just the default interaction
          Configs.TempRoleSelected[gid] = null;
          msg = CreateEmoji4RoleInteraction(ctx, msg);
        }
        else {
          msg = CreateEmoji4RoleInteractionEmojiSelect(ctx, msg);
          var waitem = await interact.WaitForButtonAsync(msg, Configs.TempRoleSelected[gid].cancel.Token);

          if (Configs.TempRoleSelected[gid].cancel.IsCancellationRequested) { // We should have what we need here
            if (Configs.TempRoleSelected[gid].message != 0) { // We have a result
              EmojiForRoleValue em = new EmojiForRoleValue {
                Guild = gid,
                Role = Configs.TempRoleSelected[gid].role.Id,
                Channel = Configs.TempRoleSelected[gid].channel,
                Message = Configs.TempRoleSelected[gid].message,
                EmojiId = Configs.TempRoleSelected[gid].emojiid,
                EmojiName = Configs.TempRoleSelected[gid].emojiname
              };
              Configs.Em4Roles[gid].Add(em);
              Database.Add(em);
              Configs.TempRoleSelected[gid] = null;
              msg = CreateEmoji4RoleInteraction(ctx, msg);
            }
            else { // Probably a timeout or clicked on cancel button
              if (waitem.Result != null && waitem.Result.Id == "idfeatem4rback")
                msg = CreateEmoji4RoleInteraction(ctx, msg);
            }
          }
          else { // Probably a timeout or clicked on cancel button
            if (waitem.Result != null && waitem.Result.Id == "idfeatem4rback")
              msg = CreateEmoji4RoleInteraction(ctx, msg);
          }
          Configs.TempRoleSelected[gid] = null;
        }
      }

      // ********* Emoji for roles.remove ***********************************************************************
      else if (cmdId.Length > 13 && cmdId[0..14] == "idfeatem4rlist") {
        int.TryParse(cmdId[14..], out int num);
        EmojiForRoleValue em = Configs.Em4Roles[gid][num];
        // Details
        // Do you want to delete it? Yes/No

        msg = CreateEmoji4RoleRemoveInteraction(ctx, msg, em);
        result = await interact.WaitForButtonAsync(msg, TimeSpan.FromMinutes(2));

        if (result.Result != null && result.Result.Id == "idfeatem4rdel") {
          Configs.Em4Roles[gid].Remove(em);
          Database.Delete(em);
          msg = CreateEmoji4RoleInteraction(ctx, msg);
        }
        else {
          msg = CreateEmoji4RoleInteraction(ctx, msg);
        }
      }

      // ********* Config Affiliation ***********************************************************************
      else if (cmdId == "idaffiliation" || cmdId == "idfeatstats0" || cmdId == "idfeatstats1" || cmdId == "idfeatstats2") { // FIXME
        if (cmdId == "idfeatstats0") Configs.SetConfigValue(gid, Config.ParamType.Affiliation, Config.ConfVal.NotAllowed);
        if (cmdId == "idfeatstats1") Configs.SetConfigValue(gid, Config.ParamType.Affiliation, Config.ConfVal.OnlyAdmins);
        if (cmdId == "idfeatstats2") Configs.SetConfigValue(gid, Config.ParamType.Affiliation, Config.ConfVal.Everybody);
        // FIXME msg = CreateStatsInteraction(ctx, msg);
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
    Config cfg, cfg2;

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

    // Scores ******************************************************
    cfg = Configs.GetConfig(gid, Config.ParamType.Scores);
    if (cfg == null) msg += "**Scores**: _not defined (disabled by default)_\n";
    else {
      WhatToTrack wtt = (WhatToTrack)cfg.IdVal;
      if (wtt == WhatToTrack.None) msg += "**Scores** is _Disabled_\n"; else msg += "**Scores** _Enabled_ are";
      if (wtt.HasFlag(WhatToTrack.Reputation)) {
        msg += " **Reputation**";
        bool missing = true;
        foreach (var e in Configs.RepEmojis[gid].Values)
          if (e.HasFlag(WhatToTrack.Reputation)) {
            missing = false;
            break;
          }
        if (missing) msg += " (_No emojis defined!_)  ";
      }
      if (wtt.HasFlag(WhatToTrack.Fun)) {
        msg += " **Fun**";
        bool missing = true;
        foreach (var e in Configs.RepEmojis[gid].Values)
          if (e.HasFlag(WhatToTrack.Fun)) {
            missing = false;
            break;
          }
        if (missing) msg += " (_No emojis defined!_)  ";
      }
      if (wtt.HasFlag(WhatToTrack.Thanks)) msg += " **Thanks**";
      if (wtt.HasFlag(WhatToTrack.Rank)) msg += " **Rank**";
      if (wtt.HasFlag(WhatToTrack.Mention)) msg += " **Mentions**";
      msg += "\n";
    }

    // Emoji for Role ******************************************************
    cfg = Configs.GetConfig(gid, Config.ParamType.Emoji4Role);
    if (cfg == null) msg += "**Emoji for role**: _not defined (disabled by default)_\n";
    else {
      if ((Config.ConfVal)cfg.IdVal == Config.ConfVal.NotAllowed) {
        msg += "**Emoji for role**: _disabled_ ";
      }
      else {
        msg += "**Emoji for role**: _enabled_ ";
      }
      var ems4role = Configs.Em4Roles[gid];
      if (ems4role.Count == 0) {
        msg += "_no emojis and no roles defined_\n";
      }
      else {
        string rs = "";
        foreach (var em in ems4role) {
          DiscordRole r = g.GetRole(em.Role);
          if (r != null) {
            rs += Utils.GetEmojiSnowflakeID(em.EmojiId, em.EmojiName, g) + " " + r.Name + ", ";
          }
        }
        msg += rs[..^2] + "\n";
      }
    }

    // Emoji for Role List ******************************************************
    cfg = Configs.GetConfig(gid, Config.ParamType.Emoji4RoleList);
    if (cfg == null) msg += "**Emoji for role List**: _not defined (disabled by default)_\n";
    else msg += "**Emoji for role List**: " + (Config.ConfVal)cfg.IdVal + "\n";

    // Affiliation ******************************************************
    cfg = Configs.GetConfig(gid, Config.ParamType.Affiliation);
    if (cfg == null) msg += "**Affiliation**: _not defined (disabled by default)_\n";
    else msg += "**Affiliation**: " + (Config.ConfVal)cfg.IdVal + "\n";


    return msg;
  }

  [Command("Setup")]
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

      // ****************** SCORES *********************************************************************************************************************************************
      if (cmds[0].Equals("scores")) {
        if (cmds.Length > 1) {
          // [rep*|fun|thank*|rank*|mention*] (val) -> enable disable
          // [rep*|fun] set -> set emojis
          // [rep*|fun] list -> list emojis

          Config c = Configs.GetConfig(gid, Config.ParamType.Scores);
          string what = (cmds[1].Trim() + "  ")[0..3].ToLowerInvariant();
          if (what == "res") {

            foreach (var em in Configs.RepEmojis[gid].Values) {
              Database.Delete(em);
            }
            int num = Configs.RepEmojis[gid].Count;
            Configs.RepEmojis[gid].Clear();
            await Utils.DeleteDelayed(15, ctx.RespondAsync("All emojis for Scores have been removed (" + num + " items.)"));

          } else if (what == "rep" || what == "fun") {
            WhatToTrack wtt = (what == "rep") ? WhatToTrack.Reputation : WhatToTrack.Fun;
            string wtts = (what == "rep") ? "Reputation" : "Fun";

            if (c == null) { c = new Config(gid, Config.ParamType.Scores, 0); Configs.TheConfigs[gid].Add(c); }
            if (cmds.Length == 2 || (cmds.Length > 2 && cmds[2].Trim().ToLowerInvariant() == "list")) {
              string emjs = "Emojis defined for the _" + wtts + "_ score: ";
              bool missing = true;
              foreach (var e in Configs.RepEmojis[ctx.Guild.Id].Values)
                if (e.HasFlag(wtt)) {
                  emjs += e.GetEmoji(ctx.Guild) + " ";
                  missing = false;
                }
              if (missing) emjs += " (_No emojis defined!_)  ";

              Database.Add(c);
              _ = Utils.DeleteDelayed(15, ctx.Message);
              await Utils.DeleteDelayed(15, ctx.RespondAsync(emjs));

            } else if (cmds.Length > 2 && cmds[2].Trim().ToLowerInvariant() == "del") {
              List<ulong> toRemove = new List<ulong>();
              foreach (var k in Configs.RepEmojis[gid].Keys) {
                var em = Configs.RepEmojis[gid][k];
                if (em.HasFlag(wtt)) {
                  Database.Delete(em);
                  toRemove.Add(k);
                }
              }
              foreach (var k in toRemove)
                Configs.RepEmojis[gid].Remove(k);
              await Utils.DeleteDelayed(15, ctx.RespondAsync("All emojis for " + wtts + " have been removed."));

            } else if (cmds.Length > 2 && cmds[2].Trim().ToLowerInvariant() == "set") {
              string emjs = "";
              bool missing = true;
              foreach (var e in Configs.RepEmojis[gid].Values)
                if (e.HasFlag(wtt)) {
                  emjs += e.GetEmoji(ctx.Guild) + " ";
                  missing = false;
                }
              if (missing) emjs += " (_No emojis defined!_)";

              DiscordMessage prompt = await ctx.Channel.SendMessageAsync(ctx.Member.Mention + ", type all the emojis to be used for _" + wtts + "_, you have 5 minutes before timeout\nCurrent emojis are: " + emjs);
              var interact = ctx.Client.GetInteractivity();
              var answer = await interact.WaitForMessageAsync((dm) => {
                return (dm.Channel == ctx.Channel && dm.Author.Id == ctx.Member.Id);
              }, TimeSpan.FromMinutes(5));
              await prompt.DeleteAsync();
              if (answer.Result == null || answer.Result.Content.Length < 4) {
                _ = Utils.DeleteDelayed(15, ctx.Channel.SendMessageAsync("Config timed out"));
              } else {
                Dictionary<ulong, ReputationEmoji> eset = new Dictionary<ulong, ReputationEmoji>();

                // Start by grabbing all values that are snowflakes
                string resp = answer.Result.Content.Trim();
                resp = Configs.emjSnowflakeRE.Replace(resp, (m) => {
                  if (ulong.TryParse(m.Groups[1].Value, out ulong id)) {
                    var rem = new ReputationEmoji(gid, id, null, wtt);
                    eset.Add(rem.GetKeyValue(), rem);
                  }
                  return "";
                });
                // And then the values of the unicode emojis regex
                resp = Configs.emjUnicodeRE.Replace(resp, (m) => {
                  var rem = new ReputationEmoji(gid, 0, m.Value, wtt);
                  eset.Add(rem.GetKeyValue(), rem);
                  return "";
                });

                // Remove all entries that are no more in the list
                List<ulong> toRemove = new List<ulong>();
                foreach (var ek in Configs.RepEmojis[gid].Keys) {
                  if (Configs.RepEmojis[gid][ek].HasFlag(wtt) && !eset.ContainsKey(ek)) toRemove.Add(ek);
                }
                foreach (var e in toRemove) {
                  ReputationEmoji re = Configs.RepEmojis[gid][e];
                  Configs.RepEmojis[gid].Remove(e);
                  Database.Delete(re);
                }
                // Add all missing entries
                foreach (var ek in eset.Keys) {
                  if (!Configs.RepEmojis[gid].ContainsKey(ek)) {
                    Configs.RepEmojis[gid].Add(ek, eset[ek]);
                    Database.Add(eset[ek]);
                  } else {
                    Database.Update(eset[ek]);
                  }
                }

                // Show the result, just to check
                emjs = "Emojis defined for the _" + wtts + "_ score: ";
                missing = true;
                foreach (var e in Configs.RepEmojis[gid].Values)
                  if (e.HasFlag(wtt)) {
                    emjs += e.GetEmoji(ctx.Guild) + " ";
                    missing = false;
                  }
                if (missing) emjs += " (_No emojis defined!_)  ";
                _ = Utils.DeleteDelayed(15, ctx.Message);
                await Utils.DeleteDelayed(15, ctx.RespondAsync(emjs));
              }

            } else if (cmds.Length > 2) {
              char mode = cmds[2].ToLowerInvariant()[0];
              if (mode == 'n' || mode == 'd' || mode == '-') c.IdVal &= ~(ulong)wtt;
              if (mode == 'y' || mode == 'e' || mode == '+') c.IdVal |= (ulong)wtt;
              Configs.WhatToTracks[gid] = (WhatToTrack)c.IdVal;
              Database.Update(c);
              if (((WhatToTrack)c.IdVal).HasFlag(wtt))
                await Utils.DeleteDelayed(15, ctx.RespondAsync("Scores " + wtts + " changed to _enabled_"));
              else
                await Utils.DeleteDelayed(15, ctx.RespondAsync("Scores " + wtts + " changed to _disabled_"));
            }
          } else if ((what == "tha" || what == "ran" || what == "men") && cmds.Length > 2) {
            if (c == null) { c = new Config(gid, Config.ParamType.Scores, 0); Configs.TheConfigs[gid].Add(c); }
            WhatToTrack wtt = WhatToTrack.Thanks;
            string wtts = "Thanks";
            if (what == "ran") { wtt = WhatToTrack.Rank; wtts = "Rank"; }
            if (what == "men") { wtt = WhatToTrack.Mention; wtts = "Mentions"; }

            char mode = cmds[2].ToLowerInvariant()[0];
            if (mode == 'n' || mode == 'd' || mode == '-') c.IdVal &= ~(ulong)wtt;
            if (mode == 'y' || mode == 'e' || mode == '+') c.IdVal |= (ulong)wtt;
            Database.Update(c);
            if (((WhatToTrack)c.IdVal).HasFlag(wtt))
              await Utils.DeleteDelayed(15, ctx.RespondAsync("Scores " + wtts + " changed to _enabled_"));
            else
              await Utils.DeleteDelayed(15, ctx.RespondAsync("Scores " + wtts + " changed to _disabled_"));
            Configs.WhatToTracks[gid] = (WhatToTrack)c.IdVal;
          }

          _ = Utils.DeleteDelayed(15, ctx.Message);

          return;
        }

        if (cmds.Length == 1 || cmds[1].Equals("?", StringComparison.InvariantCultureIgnoreCase) || cmds[1].Equals("help", StringComparison.InvariantCultureIgnoreCase)) { // HELP *******************************************
          await Utils.DeleteDelayed(15, ctx.RespondAsync("Use: `rep` or `fun` or `thanks` or `rank` or `mention` to enable/disable the features\n`rep` (or `fun`) `list` will show the emojis for the categories\n`rep` (or `fun`) `set` will wait for a set of emojis to be used for the category."));
          return;
        }
      }

      // ****************** EMOJI4ROLE *********************************************************************************************************************************************
      if (cmds[0].Equals("emojiforrole") || cmds[0].Equals("emoji4role")) {
        if (cmds.Length > 1) {
          // +|- -> enable/disable
          // list -> list emojis
          // add <role> -> wait for emoji and completes
          // remove <emoji>|<num> -> removes

          if (cmds[1].Equals("list", StringComparison.InvariantCultureIgnoreCase)) {
            if (cmds.Length == 2) { // Actual list
              if (Configs.Em4Roles[gid].Count == 0) {
                await Utils.DeleteDelayed(15, ctx.RespondAsync("No emoji for roles are defined"));
                return;
              }
              string ems = "Emojis for role: ";
              if (Configs.GetConfigValue(gid, Config.ParamType.Emoji4Role) == Config.ConfVal.NotAllowed) ems += " (_disabled_)";
              else ems += " (_enabled_)";
              int pos = 1;
              foreach (var em4r in Configs.Em4Roles[gid]) {
                DiscordRole r = g.GetRole(em4r.Role);
                DiscordChannel ch = g.GetChannel(em4r.Channel);
                DiscordMessage m = null;
                try {
                  m = ch?.GetMessageAsync(em4r.Message).Result; // This may fail
                } catch (Exception) { }
                string name = "\n(" + pos + ")  " + Utils.GetEmojiSnowflakeID(em4r.EmojiId, em4r.EmojiName, g) + " ";
                if (r == null || ch == null || m == null) name += "..._invalid_...";
                else name += (m.Content.Length > 12 ? m.Content[0..12] + "..." : m.Content) + " (" + ch.Name + ") -> " + r.Name;
                pos++;
                ems += name;
              }
              await Utils.DeleteDelayed(60, ctx.RespondAsync(ems));
            }
            else { // Enable/disable listing
              char mode = cmds[2][0];
              Config c = Configs.GetConfig(gid, Config.ParamType.Emoji4RoleList);
              if (c == null) {
                c = new Config(gid, Config.ParamType.Emoji4RoleList, 1);
                Configs.TheConfigs[gid].Add(c);
              }
              if (mode == 'n' || mode == 'd') c.IdVal = (int)Config.ConfVal.NotAllowed;
              if (mode == 'a' || mode == 'r' || mode == 'o') c.IdVal = (int)Config.ConfVal.OnlyAdmins;
              if (mode == 'e' || mode == 'y') c.IdVal = (int)Config.ConfVal.Everybody;
              Database.Add(c);
              await Utils.DeleteDelayed(15, ctx.RespondAsync("Listing of existing emojis for role command changed to " + (Config.ConfVal)c.IdVal));
            }
          }

          else if (cmds[1].Equals("enable", StringComparison.InvariantCultureIgnoreCase)) { // ENABLE ******************************************************************************************
            Configs.SetConfigValue(ctx.Guild.Id, Config.ParamType.Emoji4Role, Config.ConfVal.Everybody);
            if (Configs.Em4Roles[gid].Count == 0)
              await Utils.DeleteDelayed(15, ctx.RespondAsync("Emoji for Role command changed to _enabled_ (but no roles are deifned)"));
            else
              await Utils.DeleteDelayed(15, ctx.RespondAsync("Emoji for Role command changed to _enabled_"));
          }

          else if (cmds[1].Equals("disable", StringComparison.InvariantCultureIgnoreCase)) { // DISABLE ******************************************************************************************
            Configs.SetConfigValue(ctx.Guild.Id, Config.ParamType.Emoji4Role, Config.ConfVal.NotAllowed);
            await Utils.DeleteDelayed(15, ctx.RespondAsync("Emoji for Role command changed to _disabled_"));
          }

          else if (cmds[1].Equals("add", StringComparison.InvariantCultureIgnoreCase) && cmds.Length > 2) { // ADD ******************************************************************************************
            string rolename = cmds[2].ToLowerInvariant().Trim();

            // Get the role from the guild, as id or name
            DiscordRole role = null;
            Match rm = Configs.roleSnowflakeRR.Match(rolename);
            if (rm.Success && ulong.TryParse(rm.Groups[1].Value, out ulong rid)) {
              role = g.GetRole(rid);
            }
            else {
              foreach (var r in g.Roles.Values)
                if (r.Name.Equals(rolename, StringComparison.InvariantCultureIgnoreCase)) {
                  role = r;
                  break;
                }
            }
            if (role == null) { // If missing show error
              await Utils.DeleteDelayed(15, ctx.RespondAsync("The role specified is invalid"));
            }
            else { // If good show the message and wait for a while
              Configs.TempRoleSelected[gid] = new TempSetRole(ctx.User.Id, role);

              string msg = "Role is selected to **" + Configs.TempRoleSelected[gid].role.Name + "**\nAdd the emoji you want for this role to the message you want to monitor.";
              DiscordMessage toDelete = await ctx.RespondAsync(msg);
              _ = Configs.TempRoleSelected[gid].cancel.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(120));

              await toDelete.DeleteAsync();
              if (Configs.TempRoleSelected[gid].cancel.IsCancellationRequested) { // We should have what we need here
                if (Configs.TempRoleSelected[gid].message != 0) { // We have a result
                  EmojiForRoleValue em = new EmojiForRoleValue {
                    Guild = gid,
                    Role = Configs.TempRoleSelected[gid].role.Id,
                    Channel = Configs.TempRoleSelected[gid].channel,
                    Message = Configs.TempRoleSelected[gid].message,
                    EmojiId = Configs.TempRoleSelected[gid].emojiid,
                    EmojiName = Configs.TempRoleSelected[gid].emojiname
                  };
                  Configs.Em4Roles[gid].Add(em);
                  Database.Add(em);
                  Configs.TempRoleSelected[gid] = null;
                  await Utils.DeleteDelayed(15, ctx.RespondAsync("The emoji for role (" + Utils.GetEmojiSnowflakeID(em.EmojiId, em.EmojiName, g) + ") has been added"));
                }
              }
            }
          }

          else if (cmds[1].Equals("remove", StringComparison.InvariantCultureIgnoreCase) && cmds.Length > 2) { // REMOVE *************************************************************************************************
            // We can have either a number or an emoji
            string identifier = cmds[2].Trim();

            if (int.TryParse(identifier, out int id)) { // Is an index
              if (Configs.Em4Roles[gid].Count >= id && id > 0) {
                Configs.Em4Roles[gid].RemoveAt(id - 1);
                await Utils.DeleteDelayed(15, ctx.RespondAsync("Emoji for role has been removed"));
              }
              else {
                await Utils.DeleteDelayed(15, ctx.RespondAsync("Cannot find the Emoji for role to remove"));
              }
            }
            else { // Check by emoji
              Match m = Configs.emjSnowflakeRE.Match(identifier);
              if (m.Success && ulong.TryParse(m.Groups[1].Value, out ulong eid)) { // Get the id
                EmojiForRoleValue e = null;
                foreach (var em4r in Configs.Em4Roles[gid]) {
                  if (em4r.EmojiId == eid) {
                    e = em4r;
                    break;
                  }
                }
                if (e != null) {
                  Configs.Em4Roles[gid].Remove(e);
                  await Utils.DeleteDelayed(15, ctx.RespondAsync("Emoji for role has been removed"));
                }
                else {
                  await Utils.DeleteDelayed(15, ctx.RespondAsync("Cannot find the Emoji for role to remove"));
                }
              }
              else {
                EmojiForRoleValue e = null;
                foreach (var em4r in Configs.Em4Roles[gid]) {
                  if (em4r.EmojiName.Equals(identifier, StringComparison.InvariantCultureIgnoreCase)) {
                    e = em4r;
                    break;
                  }
                }
                if (e != null) {
                  Configs.Em4Roles[gid].Remove(e);
                  await Utils.DeleteDelayed(15, ctx.RespondAsync("Emoji for role has been removed"));
                }
                else {
                  await Utils.DeleteDelayed(15, ctx.RespondAsync("Cannot find the Emoji for role to remove"));
                }
              }
            }
          }

          _ = Utils.DeleteDelayed(15, ctx.Message);
          return;
        }

        if (cmds.Length == 1 || cmds[1].Equals("?", StringComparison.InvariantCultureIgnoreCase) || cmds[1].Equals("help", StringComparison.InvariantCultureIgnoreCase)) { // HELP *******************************************
          await Utils.DeleteDelayed(15, ctx.RespondAsync(
            "Use: `enable` or `disable` to activate and deactivate the feature\n`add` _role_ will let you to add then an emoji to the message you want to grant the role specified\n" +
            "`list` will show the emojis for role that are defined\n`remove` will remove the emoji (you can also specify the index of the one to remove)"));
          return;
        }
      }

      // ****************** AFFILIATION *********************************************************************************************************************************************
      if (cmds[0].Equals("affiliation")) {
        if (cmds.Length > 1) {
          // [-|d|r|n] -> remove
          // [add] -> add
          char mode = cmds[1][0];
          Config c = Configs.GetConfig(gid, Config.ParamType.Affiliation);
          if (c == null) {
            c = new Config(gid, Config.ParamType.Affiliation, 1);
            Configs.TheConfigs[gid].Add(c);
          }
          if (mode == '-' || mode == 'd' || mode == 'r' || mode == 'n') {
            c.IdVal = (int)Config.ConfVal.NotAllowed;
            Database.Add(c);
            Database.DeleteByKeys<AffiliationLink>(gid);
            _ = Utils.DeleteDelayed(15, ctx.Message);
            await Utils.DeleteDelayed(15, ctx.RespondAsync("Affiliation links removed"));
            return;
          }
          if (cmds[1].Equals("add")) {
            var interact = ctx.Client.GetInteractivity();
            // Paste affiliation id
            DiscordMessage msgAF = ctx.Message.RespondAsync("Provide the affiliation id (something similar to: `1234lkRyz`, it is after `?aid=` in the affiliation Unity Asset link)").Result;
            var answer = await interact.WaitForMessageAsync((dm) => {
              return (dm.Channel == ctx.Channel && dm.Author.Id == ctx.Member.Id);
            }, TimeSpan.FromMinutes(1));
            if (answer.Result == null || answer.Result.Content.Trim().Length < 4 || answer.Result.Content.Trim().Length > 16) {
              await msgAF.DeleteAsync();
              await Utils.DeleteDelayed(15, ctx.RespondAsync("Affiliation id seems to be not valid, or maybe you took too long to type. Aborting"));
              return;
            }
            string affId = answer.Result.Content.Trim();

            // Paste affiliation id
            await msgAF.DeleteAsync();
            msgAF = ctx.Message.RespondAsync("Provide the title for the affiliation message. Use something not too long").Result;
            answer = await interact.WaitForMessageAsync((dm) => {
              return (dm.Channel == ctx.Channel && dm.Author.Id == ctx.Member.Id);
            }, TimeSpan.FromMinutes(1));
            if (answer.Result == null || answer.Result.Content.Trim().Length < 4 || answer.Result.Content.Trim().Length > 40) {
              await msgAF.DeleteAsync();
              await Utils.DeleteDelayed(15, ctx.RespondAsync("Affiliation title seems to be not valid, or maybe you took too long to type. Aborting"));
              return;
            }
            string affTit = answer.Result.Content.Trim();

            // Paste message %l
            await msgAF.DeleteAsync();
            msgAF = ctx.Message.RespondAsync("Provide the message to promote your affiliation link. You can add `%l` inside the text, it will be replaced with your affiliation link").Result;
            answer = await interact.WaitForMessageAsync((dm) => {
              return (dm.Channel == ctx.Channel && dm.Author.Id == ctx.Member.Id);
            }, TimeSpan.FromMinutes(2));
            if (answer.Result == null || answer.Result.Content.Trim().Length < 12 || answer.Result.Content.Trim().Length > 1000) {
              await msgAF.DeleteAsync();
              await Utils.DeleteDelayed(15, ctx.RespondAsync("Message to post seems to be valid, or maybe you took too long to type. Aborting"));
              return;
            }
            string affMsg = answer.Result.Content.Trim();

            // Paste icon url
            msgAF = ctx.Message.RespondAsync("Provide a link to an icon that will be displaied in the affiliation message.").Result;
            answer = await interact.WaitForMessageAsync((dm) => {
              return (dm.Channel == ctx.Channel && dm.Author.Id == ctx.Member.Id);
            }, TimeSpan.FromMinutes(2));
            if (answer.Result == null || answer.Result.Content.Trim().Length < 16 || !Configs.iconURLRE.IsMatch(answer.Result.Content.Trim())) {
              await msgAF.DeleteAsync();
              await Utils.DeleteDelayed(15, ctx.RespondAsync("Icon URL seems to be not valid, or maybe you took too long to type. Aborting"));
              return;
            }
            string affIcon = answer.Result.Content.Trim();

            AffiliationLink afl = new AffiliationLink(gid, affIcon, affId, affMsg, affTit);
            Database.Add(afl);
            c.IdVal = (int)Config.ConfVal.Everybody;
            Database.Add(c);
            Configs.Affiliations[gid] = afl;

            await Utils.DeleteDelayed(15, ctx.RespondAsync("Affiliaiton links are now defined."));
            return;
          }
          else {
            await Utils.DeleteDelayed(15, ctx.RespondAsync("Use: `add` to define the affiliation links\n`disable` to remove it"));
            return;
          }
        }
        else {
          await Utils.DeleteDelayed(15, ctx.RespondAsync("Use: `add` to define the affiliation links\n`disable` to remove it"));
          return;
        }



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

 
  private DiscordMessage CreateMainConfigPage(CommandContext ctx, DiscordMessage prevMsg) {
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
    //- Enable features:
    List<DiscordButtonComponent> actions = new List<DiscordButtonComponent> {
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "iddefineadmins", "Define Admins", false, er),
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "iddefinetracking", "Define Tracking channel", false, er),
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "idconfigfeats", "Configure features", false, er)
    };
    builder.AddComponents(actions);

    //-Exit
    builder.AddComponents(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Danger, "idexitconfig", "Exit", false, ec));

    return builder.SendAsync(ctx.Channel).Result;
  }

  private DiscordMessage CreateAdminsInteraction(CommandContext ctx, DiscordMessage prevMsg) {
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

  private DiscordMessage CreateTrackingInteraction(CommandContext ctx, DiscordMessage prevMsg) {
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

  private DiscordMessage CreateFeaturesInteraction(CommandContext ctx, DiscordMessage prevMsg) {
    if (prevMsg != null) ctx.Channel.DeleteMessageAsync(prevMsg).Wait();

    DiscordEmbedBuilder eb = new DiscordEmbedBuilder {
      Title = "UPBot Configuration - Features"
    };
    eb.WithThumbnail(ctx.Guild.IconUrl);
    eb.Description = "Configuration of the UP Bot for the Discord Server **" + ctx.Guild.Name + "**\n\n\n" +
      "Select the feature to configure _(red ones are disabled, blue ones are enabled)_";
    eb.WithImageUrl(ctx.Guild.BannerUrl);
    eb.WithFooter("Member that started the configuration is: " + ctx.Member.DisplayName, ctx.Member.AvatarUrl);

    List<DiscordButtonComponent> actions;
    var builder = new DiscordMessageBuilder();
    builder.AddEmbed(eb.Build());


    // Spam protection
    // Tags
    actions = new List<DiscordButtonComponent>();
    Config sc = Configs.GetConfig(ctx.Guild.Id, Config.ParamType.SpamProtection);
    actions.Add(new DiscordButtonComponent((sc == null || sc.IdVal == 0) ? DSharpPlus.ButtonStyle.Secondary : DSharpPlus.ButtonStyle.Primary, "idfeatrespamprotect", "Spam Protection", false, er));
    builder.AddComponents(actions);

    // Ranking/Scores
    // Emogi for roles
    actions = new List<DiscordButtonComponent>();
    Config.ConfVal cv = Configs.GetConfigValue(ctx.Guild.Id, Config.ParamType.Scores);
    actions.Add(new DiscordButtonComponent(GetStyle(cv), "idfeatscores", "Scores", false, er));
    cv = Configs.GetConfigValue(ctx.Guild.Id, Config.ParamType.Emoji4Role);
    actions.Add(new DiscordButtonComponent(GetStyle(cv), "idfeatem4r", "Emoji for Role", false, er));
    builder.AddComponents(actions);


    // - Exit
    // - Back
    actions = new List<DiscordButtonComponent> {
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Danger, "idexitconfig", "Exit", false, ec),
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idback", "Back", false, el)
    };
    builder.AddComponents(actions);

    return builder.SendAsync(ctx.Channel).Result;
  }

  private DiscordMessage CreateSpamProtectInteraction(CommandContext ctx, DiscordMessage prevMsg) {
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
    // - Back to features
    actions = new List<DiscordButtonComponent> {
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Danger, "idexitconfig", "Exit", false, ec),
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idback", "Back to Main", false, el),
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idconfigfeats", "Features", false, el)
    };
    builder.AddComponents(actions);

    return builder.SendAsync(ctx.Channel).Result;
  }


  private DiscordMessage CreateScoresInteraction(CommandContext ctx, DiscordMessage prevMsg) {
    if (prevMsg != null) ctx.Channel.DeleteMessageAsync(prevMsg).Wait();

    DiscordEmbedBuilder eb = new DiscordEmbedBuilder {
      Title = "UPBot Configuration - Scores"
    };
    eb.WithThumbnail(ctx.Guild.IconUrl);
    WhatToTrack wtt = Configs.WhatToTracks[ctx.Guild.Id];
    bool vala = wtt.HasFlag(WhatToTrack.Reputation);
    bool valf = wtt.HasFlag(WhatToTrack.Fun);
    bool valt = wtt.HasFlag(WhatToTrack.Thanks);
    bool valr = wtt.HasFlag(WhatToTrack.Rank);
    bool valm = wtt.HasFlag(WhatToTrack.Mention);
    eb.Description = "Configuration of the UP Bot for the Discord Server **" + ctx.Guild.Name + "**\n\n" +
      "The **Scores** feature provides some tracking of the users in 4 possible areas:\n" +
      "-> _Reputation_ increases by receiving particular emojis (they can be defined.)\n" +
      "-> _Fun_ increases by receiving particular emojis (they can be defined.)\n" +
      "-> _Thanks_ increases by receiving a **thank you** message.\n" +
      "-> _Rank_ increases by posting (1 minutes delay between posts to avoid spam).\n\n" +
      "-> _Mentions_ increases by being mentioned.\n\n" +
      "The users can call the command to see the status of the whole server or just to get the own ranking.\n" +
      "Each scoring can be enabled and disabled individually.\n\n";
    if (wtt == WhatToTrack.None) eb.Description += "**Scores** is _Disabled_"; else eb.Description += "_Enabled_ **Scores** are:\n";
    if (vala) {
      eb.Description += "- **Reputation** ";
      string emjs = "";
      bool missing = true;
      foreach (var e in Configs.RepEmojis[ctx.Guild.Id].Values)
        if (e.HasFlag(WhatToTrack.Reputation)) {
          emjs += e.GetEmoji(ctx.Guild) + " ";
          missing = false;
        }
      if (missing) eb.Description += "(_No emojis defined!_)\n";
      else eb.Description += emjs + "\n";
    }
    if (valf) {
      eb.Description += "- **Fun** ";
      string emjs = "";
      bool missing = true;
      foreach (var e in Configs.RepEmojis[ctx.Guild.Id].Values)
        if (e.HasFlag(WhatToTrack.Fun)) {
          emjs += e.GetEmoji(ctx.Guild) + " ";
          missing = false;
        }
      if (missing) eb.Description += "(_No emojis defined!_)\n";
      else eb.Description += emjs + "\n";
    }
    if (valt) eb.Description += "- **Thanks**\n";
    if (valr) eb.Description += "- **Rank**\n";
    if (valm) eb.Description += "- **Mentions**\n";
    eb.WithImageUrl(ctx.Guild.BannerUrl);
    eb.WithFooter("Member that started the configuration is: " + ctx.Member.DisplayName, ctx.Member.AvatarUrl);

    List<DiscordButtonComponent> actions;
    var builder = new DiscordMessageBuilder();
    builder.AddEmbed(eb.Build());


    actions = new List<DiscordButtonComponent> {
      new DiscordButtonComponent(vala ? DSharpPlus.ButtonStyle.Success : DSharpPlus.ButtonStyle.Danger, "idfeatscorese0", "Reputation", false, vala ? ey : en),
      new DiscordButtonComponent(valf ? DSharpPlus.ButtonStyle.Success : DSharpPlus.ButtonStyle.Danger, "idfeatscorese1", "Fun", false, valf ? ey : en),
      new DiscordButtonComponent(valt ? DSharpPlus.ButtonStyle.Success : DSharpPlus.ButtonStyle.Danger, "idfeatscorese2", "Thanks", false, valt ? ey : en),
      new DiscordButtonComponent(valr ? DSharpPlus.ButtonStyle.Success : DSharpPlus.ButtonStyle.Danger, "idfeatscorese3", "Ranking", false, valr ? ey : en),
      new DiscordButtonComponent(valm ? DSharpPlus.ButtonStyle.Success : DSharpPlus.ButtonStyle.Danger, "idfeatscorese4", "Mentions", false, valm ? ey : en)
    };
    builder.AddComponents(actions);

    actions = new List<DiscordButtonComponent> {
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "idfeatscoresere", "Define Reputation emojis", false, er),
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "idfeatscoresefe", "Define Fun emojis", false, er)
    };
    builder.AddComponents(actions);

    // - Exit
    // - Back
    // - Back to features
    actions = new List<DiscordButtonComponent> {
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Danger, "idexitconfig", "Exit", false, ec),
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idback", "Back to Main", false, el),
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idconfigfeats", "Features", false, el)
    };
    builder.AddComponents(actions);

    return builder.SendAsync(ctx.Channel).Result;
  }


  private DiscordMessage CreateEmoji4RoleInteraction(CommandContext ctx, DiscordMessage prevMsg) {
    if (prevMsg != null) ctx.Channel.DeleteMessageAsync(prevMsg).Wait();

    DiscordEmbedBuilder eb = new DiscordEmbedBuilder {
      Title = "UPBot Configuration - Emoji for Role"
    };
    eb.WithThumbnail(ctx.Guild.IconUrl);
    Config.ConfVal cv = Configs.GetConfigValue(ctx.Guild.Id, Config.ParamType.Emoji4Role);
    Config.ConfVal cv2 = Configs.GetConfigValue(ctx.Guild.Id, Config.ParamType.Emoji4RoleList);
    eb.Description = "Configuration of the UP Bot for the Discord Server **" + ctx.Guild.Name + "**\n\n" +
      "The bot allows to track emojis on specific messages to grant and remove roles.\n\n";
    if (cv == Config.ConfVal.NotAllowed) eb.Description += "**Emoji for roles** are _Disabled_";
    else eb.Description += "**Emoji for roles** are _Enabled_";
    eb.WithImageUrl(ctx.Guild.BannerUrl);
    eb.WithFooter("Member that started the configuration is: " + ctx.Member.DisplayName, ctx.Member.AvatarUrl);

    List<DiscordButtonComponent> actions;
    var builder = new DiscordMessageBuilder();
    builder.AddEmbed(eb.Build());

    // List existing (role name, emoji, for channel (part of name))
    // Add one (add emoji to a channel to pick the channel and then type the role)
    actions = new List<DiscordButtonComponent> {
      cv == Config.ConfVal.NotAllowed ?
        new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idfeatem4rendis", "Enable", false, ey) :
        new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "idfeatem4rendis", "Disable", false, en),
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "idfeatem4radd", "Add new", false, ey),

      new DiscordButtonComponent(GetIsStyle(cv2, Config.ConfVal.NotAllowed), "idfeatem4rshow0", "Nobody can list", false, GetYN(cv2, Config.ConfVal.NotAllowed)),
      new DiscordButtonComponent(GetIsStyle(cv2, Config.ConfVal.OnlyAdmins), "idfeatem4rshow1", "Only Admins can list", false, GetYN(cv2, Config.ConfVal.OnlyAdmins)),
      new DiscordButtonComponent(GetIsStyle(cv2, Config.ConfVal.Everybody), "idfeatem4rshow2", "Everybody can list", false, GetYN(cv2, Config.ConfVal.Everybody))
    };
    builder.AddComponents(actions);


    int num = 0;
    int pos = 0;
    actions = new List<DiscordButtonComponent>();
    foreach (var em4r in Configs.Em4Roles[ctx.Guild.Id]) {
      DiscordRole r = ctx.Guild.GetRole(em4r.Role);
      DiscordChannel c = ctx.Guild.GetChannel(em4r.Channel);
      DiscordMessage m = null;
      try {
        m = c?.GetMessageAsync(em4r.Message).Result; // This may fail
      } catch (Exception) {}
      string name;
      if (r == null || c == null || m == null) name = "..._invalid_...";
      else name = m.Content[0..12] + " (" + c.Name + ") -> " + r.Name;
      DiscordComponentEmoji em;
      if (em4r.EmojiId == 0) em = new DiscordComponentEmoji(em4r.EmojiName);
      else em = new DiscordComponentEmoji(em4r.EmojiId);
      actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Success, "idfeatem4rlist"+num, name, false, em));
      pos++;
      num++;
      if (pos == 5) {
        builder.AddComponents(actions);
        actions = new List<DiscordButtonComponent>();
        pos = 0;
      }
    }
    if (pos!=0) builder.AddComponents(actions);

    // - Exit
    // - Back
    // - Back to features
    actions = new List<DiscordButtonComponent> {
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Danger, "idexitconfig", "Exit", false, ec),
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idback", "Back to Main", false, el),
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idconfigfeats", "Features", false, el)
    };
    builder.AddComponents(actions);

    return builder.SendAsync(ctx.Channel).Result;
  }

  private DiscordMessage CreateEmoji4RoleInteractionRoleSelect(CommandContext ctx, DiscordMessage prevMsg) {
    if (prevMsg != null) ctx.Channel.DeleteMessageAsync(prevMsg).Wait();

    DiscordEmbedBuilder eb = new DiscordEmbedBuilder {
      Title = "UPBot Configuration - Emoji for Role"
    };
    eb.WithThumbnail(ctx.Guild.IconUrl);
    eb.Description = "Configuration of the UP Bot for the Discord Server **" + ctx.Guild.Name + "**\n\n" +
      "Select the role you want to add by adding an emoji to a message.\n\n";
    eb.WithImageUrl(ctx.Guild.BannerUrl);
    eb.WithFooter("Member that started the configuration is: " + ctx.Member.DisplayName, ctx.Member.AvatarUrl);

    List<DiscordButtonComponent> actions;
    var builder = new DiscordMessageBuilder();
    builder.AddEmbed(eb.Build());

    var roles = ctx.Guild.Roles.Values;
    actions = new List<DiscordButtonComponent>();
    int pos = 0;
    int num = 0;
    foreach (var r in roles) {
      if (r.IsManaged || r.Permissions.HasFlag(DSharpPlus.Permissions.Administrator) || r.Position == 0) continue;
      actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "idfeatem4raddr" + num, r.Name, false));
      pos++;
      num++;
      if (pos == 5) {
        pos = 0;
        builder.AddComponents(actions);
        actions = new List<DiscordButtonComponent>();
      }
    }
    if (pos != 0) builder.AddComponents(actions);

    // - Exit
    // - Back
    // - Back to features
    actions = new List<DiscordButtonComponent> {
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Danger, "idexitconfig", "Exit", false, ec),
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idback", "Back to Main", false, el),
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idconfigfeats", "Features", false, el)
    };
    builder.AddComponents(actions);

    return builder.SendAsync(ctx.Channel).Result;
  }

  private DiscordMessage CreateEmoji4RoleInteractionEmojiSelect(CommandContext ctx, DiscordMessage prevMsg) {
    if (prevMsg != null) ctx.Channel.DeleteMessageAsync(prevMsg).Wait();

    DiscordEmbedBuilder eb = new DiscordEmbedBuilder {
      Title = "UPBot Configuration - Emoji for Role"
    };
    eb.WithThumbnail(ctx.Guild.IconUrl);
    eb.Description = "Role is selected to **" + Configs.TempRoleSelected[ctx.Guild.Id].role.Name + "**\n\nAdd the emoji you want for this role to the message you want to monitor.\n\n";

    List<DiscordButtonComponent> actions;
    var builder = new DiscordMessageBuilder();
    builder.AddEmbed(eb.Build());

    // - Cancel
    actions = new List<DiscordButtonComponent> {
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idfeatem4rback", "Cancel", false, el)
    };
    builder.AddComponents(actions);

    return builder.SendAsync(ctx.Channel).Result;
  }


  private DiscordMessage CreateEmoji4RoleRemoveInteraction(CommandContext ctx, DiscordMessage prevMsg, EmojiForRoleValue em) {
    if (prevMsg != null) ctx.Channel.DeleteMessageAsync(prevMsg).Wait();

    DiscordChannel c = null;
    DiscordMessage m = null;
    DiscordRole r = null;
    try {
      c = ctx.Guild.GetChannel(em.Channel);
      m = c.GetMessageAsync(em.Message).Result;
      r = ctx.Guild.GetRole(em.Role);
    } catch (Exception) { }
    var emj = em.EmojiName;
    if (em.EmojiId != 0) emj = Utils.GetEmojiSnowflakeID(ctx.Guild.GetEmojiAsync(em.EmojiId).Result);

    string msgcontent = m == null ? "_Invalid_" : m.Content;
    if (msgcontent.Length > 200) msgcontent = msgcontent[0..200] + "...";

    DiscordEmbedBuilder eb = new DiscordEmbedBuilder {
      Title = "UPBot Configuration - Emoji for Role"
    };
    eb.WithThumbnail(ctx.Guild.IconUrl);
    eb.Description = "\n\n**For the message**:\n" + msgcontent + "\n" +
      "**in the channel**:\n" + c?.Name + "\n" +
      "**The emoji**: " + emj + "\n**Will grant the role**: " + r?.Name + "\n\n\n";
    eb.WithImageUrl(ctx.Guild.BannerUrl);
    eb.WithFooter("Member that started the configuration is: " + ctx.Member.DisplayName, ctx.Member.AvatarUrl);

    var builder = new DiscordMessageBuilder();
    builder.AddEmbed(eb.Build());
    List<DiscordButtonComponent> actions = new List<DiscordButtonComponent> {
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Danger, "idfeatem4rdel", "Delete", false, en),
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idfeatem4rback", "Cancel", false, el)
    };
    builder.AddComponents(actions);

    return builder.SendAsync(ctx.Channel).Result;
  }






  private DiscordComponentEmoji GetYN(Config.ConfVal cv, Config.ConfVal what) {
    if (cv == what) return ey;
    return en;
  }

  private DSharpPlus.ButtonStyle GetStyle(Config.ConfVal cv) {
    switch (cv) {
      case Config.ConfVal.NotAllowed: return DSharpPlus.ButtonStyle.Secondary;
      case Config.ConfVal.OnlyAdmins: return DSharpPlus.ButtonStyle.Danger;
      default: return DSharpPlus.ButtonStyle.Primary;
    }
  }

  private DSharpPlus.ButtonStyle GetIsStyle(Config.ConfVal cv, Config.ConfVal what) {
    if (cv == what) return DSharpPlus.ButtonStyle.Secondary;
    return DSharpPlus.ButtonStyle.Primary;
  }



}
