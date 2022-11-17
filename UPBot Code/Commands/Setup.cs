using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;

namespace UPBot {
  /// <summary>
  /// This command is used to configure the bot, so roles and messages can be set for other servers.
  /// author: CPU
  /// </summary>
  public class SlashSetup : ApplicationCommandModule {

    readonly DiscordComponentEmoji ey = new(DiscordEmoji.FromUnicode("✅"));
    readonly DiscordComponentEmoji en = new(DiscordEmoji.FromUnicode("❎"));
    readonly DiscordComponentEmoji el = new(DiscordEmoji.FromUnicode("↖️"));
    readonly DiscordComponentEmoji er = new(DiscordEmoji.FromUnicode("↘️"));
    readonly DiscordComponentEmoji ec = new(DiscordEmoji.FromUnicode("❌"));
    static DiscordComponentEmoji ok = null;
    static DiscordComponentEmoji ko = null;


    [SlashCommand("setup", "Configuration of the features")]
    public async Task SetupCommand(InteractionContext ctx, [Option("Command", "Show, List, Admins, or Dump")] SetupCommandItem? command = null) {
      if (ctx.Guild == null) {
        await ctx.CreateResponseAsync("I cannot be used in Direct Messages.", true);
        return;
      }
      Utils.LogUserCommand(ctx);
      DiscordGuild g = ctx.Guild;
      ulong gid = g.Id;

      if (!Configs.HasAdminRole(gid, ctx.Member.Roles, false)) {
        await ctx.CreateResponseAsync("Only admins can setup the bot.", true);
        return;
      }

      if (command == null || command == SetupCommandItem.Show) await HandleSetupInteraction(ctx, gid);
      else if (command == SetupCommandItem.List) await ctx.CreateResponseAsync(GenerateSetupList(g, gid));
      else if (command == SetupCommandItem.Save) {
        string theList = GenerateSetupList(g, gid);
        string rndName = "SetupList" + DateTime.Now.Second + "Tmp" + DateTime.Now.Millisecond + ".txt";
        File.WriteAllText(rndName, theList);
        using var fs = new FileStream(rndName, FileMode.Open, FileAccess.Read);
        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("Setup List in attachment").AddFile(fs));
        await Utils.DeleteFileDelayed(30, rndName);
      }
      else await ctx.CreateResponseAsync("Wrong choice", true);
    }

    async Task HandleSetupInteraction(InteractionContext ctx, ulong gid) {
      var interact = ctx.Client.GetInteractivity();
      if (ok == null) {
        ok = new DiscordComponentEmoji(Utils.GetEmoji(EmojiEnum.OK));
        ko = new DiscordComponentEmoji(Utils.GetEmoji(EmojiEnum.KO));
      }

      // Basic intro message
      CreateMainConfigPage(ctx, null);

      DiscordMessage msg = await ctx.GetOriginalResponseAsync();
      var result = await interact.WaitForButtonAsync(msg, TimeSpan.FromMinutes(2));
      var interRes = result.Result;
      await msg.DeleteAsync();
      msg = null;

      while (interRes != null && interRes.Id != "idexitconfig") {
        interRes.Handled = true;
        string cmdId = interRes.Id;

        // ******************************************************************** Back *************************************************************************
        if (cmdId == "idback") {
          msg = FollowMainConfigPage(ctx, msg);
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
          if (answer.Result != null) {
            if (answer.Result.MentionedRoles.Count > 0) {
              foreach (var dr in answer.Result.MentionedRoles) {
                if (!Configs.AdminRoles[gid].Contains(dr.Id)) {
                  Configs.AdminRoles[gid].Add(dr.Id);
                  Database.Add(new AdminRole(gid, dr.Id));
                }
              }
            }
            else { // Try to find if we have a role with the typed name
              string rname = answer.Result.Content.Trim();
              foreach (var role in ctx.Guild.Roles.Values) {
                if (role.Name.Equals(rname, StringComparison.InvariantCultureIgnoreCase)) {
                  if (!Configs.AdminRoles[gid].Contains(role.Id)) {
                    Configs.AdminRoles[gid].Add(role.Id);
                    Database.Add(new AdminRole(gid, role.Id));
                  }
                }
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
              TrackChannel tc = new();
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
          SpamProtection sp = Configs.SpamProtections[gid];
          if (sp == null) {
            sp = new SpamProtection(gid);
            Configs.SpamProtections[gid] = sp;
          }
          if (cmdId == "idfeatrespamprotect0") sp.protectDiscord = !sp.protectDiscord;
          if (cmdId == "idfeatrespamprotect1") sp.protectSteam = !sp.protectSteam;
          if (cmdId == "idfeatrespamprotect2") sp.protectEpic = !sp.protectEpic;
          Database.Add(sp);
          msg = CreateSpamProtectInteraction(ctx, msg);
        }
        else if (cmdId == "idfeatrespamprotectbl") {
          msg = CreateSpamBlackListInteraction(ctx, msg);
        }
        else if (cmdId == "idfeatrespamprotectwl") {
          msg = CreateSpamWhiteListInteraction(ctx, msg);
        }
        else if (cmdId.Length > 21 && cmdId[0..22] == "idfeatrespamprotectadd") { // Ask for the link, clean it up, and add it
          await ctx.Channel.DeleteMessageAsync(msg);
          bool whitelist = (cmdId == "idfeatrespamprotectaddwl");

          DiscordMessage prompt = await ctx.Channel.SendMessageAsync($"{ctx.Member.Mention}, type the url that should be {(whitelist ? "white listed" : "considered spam")}");
          var answer = await interact.WaitForMessageAsync((dm) => {
            return (dm.Channel == ctx.Channel && dm.Author.Id == ctx.Member.Id);
          }, TimeSpan.FromMinutes(2));
          if (string.IsNullOrWhiteSpace(answer.Result.Content) || !answer.Result.Content.Contains('.')) {
            await interRes.Interaction.CreateResponseAsync(DSharpPlus.InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent("Config timed out"));
            return;
          }

          string link = answer.Result.Content.Trim();
          Regex urlparts = new("[0-9a-z\\.\\-_~]+");
          foreach (Match m in urlparts.Matches(link)) {
            string url = m.Value.ToLowerInvariant();
            if (!url.Contains('.')) continue;

            int leftmostdot = url.LastIndexOf('.');
            int seconddot = url.LastIndexOf('.', leftmostdot - 1);
            if (seconddot != -1) url = url[(seconddot + 1)..].Trim();

            Database.Add(new SpamLink(gid, url, whitelist));
            bool found = false;
            var list = whitelist ? Configs.WhiteListLinks : Configs.SpamLinks;
            foreach (var s in list) {
              if (s.Equals(url)) {
                found = true;
                break;
              }
            }
            if (!found) {
              CheckSpam.SpamCheckTimeout = ctx.Member;
              if (whitelist) {
                Configs.WhiteListLinks[gid].Add(url);
                await ctx.Channel.SendMessageAsync("New white list URL added.");
                msg = null;
              }
              else {
                Configs.SpamLinks[gid].Add(url);
                await ctx.Channel.SendMessageAsync("New spam URL added.");
                msg = null;
              }
            }
          }
          msg = CreateSpamProtectInteraction(ctx, msg);
        }
        else if (cmdId.Length > 27 && cmdId[0..27] == "idfeatrespamprotectremovebl") {
          if (int.TryParse(cmdId[27..], out int num)) {
            string link = Configs.SpamLinks[gid][num];
            Configs.SpamLinks[gid].RemoveAt(num);
            Database.DeleteByKeys<SpamLink>(gid, link);
          }
          msg = CreateSpamProtectInteraction(ctx, msg);
        }
        else if (cmdId.Length > 27 && cmdId[0..27] == "idfeatrespamprotectremovewl") {
          if (int.TryParse(cmdId[27..], out int num)) {
            string link = Configs.WhiteListLinks[gid][num];
            Configs.WhiteListLinks[gid].RemoveAt(num);
            Database.DeleteByKeys<SpamLink>(gid, link);
          }
          msg = CreateSpamProtectInteraction(ctx, msg);
        }
        else if (cmdId == "idbackspam") {
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

    static string GenerateSetupList(DiscordGuild g, ulong gid) { // list

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
      SpamProtection sp = Configs.SpamProtections[gid];
      if (sp == null) msg += "**Spam Protection**: _not defined (disabled by default)_\n";
      else if (sp.protectDiscord) {
        if (sp.protectSteam) {
          if (sp.protectEpic) {
            msg += "**Spam Protection**: enabled for _Discord_, _Steam_, and _Epic_\n";
          }
          else {
            msg += "**Spam Protection**: enabled for _Discord_ and _Steam_\n";
          }
        }
        else {
          if (sp.protectEpic) {
            msg += "**Spam Protection**: enabled for _Discord_ and _Epic_\n";
          }
          else {
            msg += "**Spam Protection**: enabled for _Discord_ only\n";
          }
        }
      }
      else {
        if (sp.protectSteam) {
          if (sp.protectEpic) {
            msg += "**Spam Protection**: enabled for _Steam_ and _Epic_\n";
          }
          else {
            msg += "**Spam Protection**: enabled for _Steam_ only\n";
          }
        }
        else {
          if (sp.protectEpic) {
            msg += "**Spam Protection**: enabled for _Epic_ only\n";
          }
          else {
            msg += "**Spam Protection**: _disabled_\n";
          }
        }
      }
      if (Configs.SpamLinks.ContainsKey(gid) && Configs.SpamLinks[gid].Count > 0) {
        msg += "**Specific spam links**: ";
        bool first = true;
        foreach (string sl in Configs.SpamLinks[gid]) {
          if (!first) {
            msg += ", ";
            first = false;
          }
          msg += sl;
        }
      }

      return msg;
    }

    public enum SetupCommandItem {
      [ChoiceName("Show")] Show = 0,
      [ChoiceName("List")] List = 1,
      [ChoiceName("Save")] Save = 2,
      [ChoiceName("Admins")] Admins = 3
    }

    private static void AlterTracking(ulong gid, bool j, bool l, bool r) {
      TrackChannel tc = Configs.TrackChannels[gid];
      if (j) tc.trackJoin = !tc.trackJoin;
      if (l) tc.trackLeave = !tc.trackLeave;
      if (r) tc.trackRoles = !tc.trackRoles;
      Database.Update(tc);
    }


    private void CreateMainConfigPage(InteractionContext ctx, DiscordMessage prevMsg) {
      if (prevMsg != null) ctx.Channel.DeleteMessageAsync(prevMsg).Wait();

      DiscordEmbedBuilder eb = new() {
        Title = "UPBot Configuration"
      };
      eb.WithThumbnail(ctx.Guild.IconUrl);
      eb.Description = "Configuration of the UP Bot for the Discord Server **" + ctx.Guild.Name + "**";
      eb.WithImageUrl(ctx.Guild.BannerUrl);
      eb.WithFooter("Member that started the configuration is: " + ctx.Member.DisplayName, ctx.Member.AvatarUrl);

      var builder = new DiscordInteractionResponseBuilder();
      builder.AddEmbed(eb.Build());

      //- Set tracking
      //- Set Admins
      //- Spam Protection
      SpamProtection sp = Configs.SpamProtections[ctx.Guild.Id];
      bool spdisabled = sp == null || (!sp.protectDiscord && !sp.protectSteam && !sp.protectEpic);
      List<DiscordButtonComponent> actions = new() {
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "iddefineadmins", "Define Admins", false, er),
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "iddefinetracking", "Define Tracking channel", false, er),
      new DiscordButtonComponent( spdisabled ? DSharpPlus.ButtonStyle.Secondary : DSharpPlus.ButtonStyle.Primary, "idfeatrespamprotect", "Spam Protection", false, er)
    };
      builder.AddComponents(actions);

      //-Exit
      builder.AddComponents(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Danger, "idexitconfig", "Exit", false, ec));

      ctx.CreateResponseAsync(builder);
    }

    private DiscordMessage FollowMainConfigPage(InteractionContext ctx, DiscordMessage prevMsg) {
      if (prevMsg != null) ctx.Channel.DeleteMessageAsync(prevMsg).Wait();

      DiscordEmbedBuilder eb = new() {
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
      //- Spam Protection
      SpamProtection sp = Configs.SpamProtections[ctx.Guild.Id];
      bool spdisabled = sp == null || (!sp.protectDiscord && !sp.protectSteam && !sp.protectEpic);
      List<DiscordButtonComponent> actions = new() {
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "iddefineadmins", "Define Admins", false, er),
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "iddefinetracking", "Define Tracking channel", false, er),
      new DiscordButtonComponent(spdisabled ? DSharpPlus.ButtonStyle.Secondary : DSharpPlus.ButtonStyle.Primary, "idfeatrespamprotect", "Spam Protection", false, er)
    };
      builder.AddComponents(actions);

      //-Exit
      builder.AddComponents(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Danger, "idexitconfig", "Exit", false, ec));

      return ctx.Channel.SendMessageAsync(builder).Result;
    }

    private DiscordMessage CreateAdminsInteraction(InteractionContext ctx, DiscordMessage prevMsg) {
      if (prevMsg != null) ctx.Channel.DeleteMessageAsync(prevMsg).Wait();

      DiscordEmbedBuilder eb = new() {
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
      List<DiscordButtonComponent> actions = new();
      builder.AddComponents(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "idroleadd", "Add roles", false, ok));
      // - Remove roles
      int num = 0;
      int cols = 0;
      foreach (ulong rid in Configs.AdminRoles[ctx.Guild.Id]) {
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

      return ctx.Channel.SendMessageAsync(builder).Result;
    }

    private DiscordMessage CreateTrackingInteraction(InteractionContext ctx, DiscordMessage prevMsg) {
      if (prevMsg != null) ctx.Channel.DeleteMessageAsync(prevMsg).Wait();

      TrackChannel tc = Configs.TrackChannels[ctx.Guild.Id];

      DiscordEmbedBuilder eb = new() {
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

      return ctx.Channel.SendMessageAsync(builder).Result;
    }

    private DiscordMessage CreateSpamProtectInteraction(InteractionContext ctx, DiscordMessage prevMsg) {
      if (prevMsg != null) ctx.Channel.DeleteMessageAsync(prevMsg).Wait();

      DiscordEmbedBuilder eb = new() {
        Title = "UPBot Configuration - Spam Protection"
      };
      eb.WithThumbnail(ctx.Guild.IconUrl);
      SpamProtection sp = Configs.SpamProtections[ctx.Guild.Id];
      bool edisc = sp != null && sp.protectDiscord;
      bool esteam = sp != null && sp.protectSteam;
      bool eepic = sp != null && sp.protectEpic;
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

      actions = new List<DiscordButtonComponent> {
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Success, "idfeatrespamprotectbl", "Manage Black List", false, er),
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Success, "idfeatrespamprotectwl", "Manage White List", false, er)
    };
      builder.AddComponents(actions);

      // - Exit
      // - Back
      actions = new List<DiscordButtonComponent> {
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Danger, "idexitconfig", "Exit", false, ec),
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idback", "Back to Main", false, el)
    };
      builder.AddComponents(actions);

      return ctx.Channel.SendMessageAsync(builder).Result;
    }

    private DiscordMessage CreateSpamWhiteListInteraction(InteractionContext ctx, DiscordMessage prevMsg) {
      if (prevMsg != null) ctx.Channel.DeleteMessageAsync(prevMsg).Wait();

      DiscordEmbedBuilder eb = new() {
        Title = "UPBot Configuration - Spam Protection"
      };
      eb.WithThumbnail(ctx.Guild.IconUrl);
      eb.Description = "Configuration of the UP Bot for the Discord Server **" + ctx.Guild.Name + "**\n\n" +
        "White List of links for the **Spam Protection**, these links will always be allowed.\n" +
        "Add with the button a link that will always be accepted in all posted messages.\n" +
        "Click on an existing link button to remove it from the white list";
      eb.WithImageUrl(ctx.Guild.BannerUrl);
      eb.WithFooter("Member that started the configuration is: " + ctx.Member.DisplayName, ctx.Member.AvatarUrl);

      List<DiscordButtonComponent> actions;
      var builder = new DiscordMessageBuilder();
      builder.AddEmbed(eb.Build());

      actions = new List<DiscordButtonComponent> {
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Success, "idfeatrespamprotectaddwl", "Add custom non spam url", false, ok)
    };
      builder.AddComponents(actions);

      // List all custom spam links
      int counter = 0;
      actions = new List<DiscordButtonComponent>();
      foreach (string sl in Configs.WhiteListLinks[ctx.Guild.Id]) {
        actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Success, $"idfeatrespamprotectremovewl{counter}", sl, false, ko));
        counter++;
        if (counter == 4) {
          counter = 0;
          builder.AddComponents(actions);
          actions = new List<DiscordButtonComponent>();
        }
      }
      if (actions.Count > 0) builder.AddComponents(actions);


      // - Exit
      // - Back
      actions = new List<DiscordButtonComponent> {
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Danger, "idexitconfig", "Exit", false, ec),
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idback", "Back to Main", false, el),
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idbackspam", "Back to Spam Protection", false, el)
    };
      builder.AddComponents(actions);

      return ctx.Channel.SendMessageAsync(builder).Result;
    }

    private DiscordMessage CreateSpamBlackListInteraction(InteractionContext ctx, DiscordMessage prevMsg) {
      if (prevMsg != null) ctx.Channel.DeleteMessageAsync(prevMsg).Wait();

      DiscordEmbedBuilder eb = new() {
        Title = "UPBot Configuration - Spam Protection"
      };
      eb.WithThumbnail(ctx.Guild.IconUrl);
      eb.Description = "Configuration of the UP Bot for the Discord Server **" + ctx.Guild.Name + "**\n\n" +
        "Black List of links for the **Spam Protection**\n" +
        "Add with the button a link that will be banned from all messages posted.\n" +
        "Click on an existing link button to remove it from the black list";
      eb.WithImageUrl(ctx.Guild.BannerUrl);
      eb.WithFooter("Member that started the configuration is: " + ctx.Member.DisplayName, ctx.Member.AvatarUrl);

      List<DiscordButtonComponent> actions;
      var builder = new DiscordMessageBuilder();
      builder.AddEmbed(eb.Build());

      actions = new List<DiscordButtonComponent> {
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Success, "idfeatrespamprotectaddbl", "Add custom spam url", false, ok)
    };
      builder.AddComponents(actions);

      // List all custom spam links
      int counter = 0;
      actions = new List<DiscordButtonComponent>();
      foreach (string sl in Configs.SpamLinks[ctx.Guild.Id]) {
        actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Success, $"idfeatrespamprotectremovebl{counter}", sl, false, ko));
        counter++;
        if (counter == 4) {
          counter = 0;
          builder.AddComponents(actions);
          actions = new List<DiscordButtonComponent>();
        }
      }
      if (actions.Count > 0) builder.AddComponents(actions);


      // - Exit
      // - Back
      actions = new List<DiscordButtonComponent> {
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Danger, "idexitconfig", "Exit", false, ec),
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idback", "Back to Main", false, el),
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idbackspam", "Back to Spam Protection", false, el)
    };
      builder.AddComponents(actions);

      return ctx.Channel.SendMessageAsync(builder).Result;
    }
  }
}
