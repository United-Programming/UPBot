using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class MembersTracking {
  static Dictionary<ulong, DateTime> tracking = null;

  public static async Task DiscordMemberRemoved(DiscordClient client, DSharpPlus.EventArgs.GuildMemberRemoveEventArgs args) {
    try {
      if (tracking == null) tracking = new Dictionary<ulong, DateTime>();
      TrackChannel trackChannel = SetupModule.TrackChannels[args.Guild.Id];
      if (trackChannel == null || trackChannel.channel == null || !trackChannel.trackLeave) return;

      int daysJ = (int)(DateTime.Now - args.Member.JoinedAt.DateTime).TotalDays;

      if (tracking.ContainsKey(args.Member.Id) || daysJ < 2) {
        tracking.Remove(args.Member.Id);
        string msg = "User " + args.Member.DisplayName + " did a kiss and go. (" + args.Guild.MemberCount + " members total)";
        await trackChannel.channel.SendMessageAsync(msg);
        Utils.Log(msg);
      }
      else {
        string msgC = Utils.GetEmojiSnowflakeID(EmojiEnum.KO) + " User " + args.Member.Mention + " (" + args.Member.DisplayName + ") left on " + DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss") + " after " + daysJ + " days (" + args.Guild.MemberCount + " members total)";
        string msgL = "- User " + args.Member.DisplayName + " left on " + DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss") + " (" + args.Guild.MemberCount + " members total)";
        await trackChannel.channel.SendMessageAsync(msgC);
        Utils.Log(msgL);
      }
    } catch (Exception ex) {
      Utils.Log("Error in DiscordMemberRemoved: " + ex.Message);
    }

    await Task.Delay(50);
  }

  public static async Task DiscordMemberAdded(DiscordClient client, DSharpPlus.EventArgs.GuildMemberAddEventArgs args) {
    try{
      if (tracking == null) tracking = new Dictionary<ulong, DateTime>();
      TrackChannel trackChannel = SetupModule.TrackChannels[args.Guild.Id];
      if (trackChannel == null || trackChannel.channel == null || !trackChannel.trackJoin) return;

      tracking[args.Member.Id] = DateTime.Now;
    _ = SomethingAsync(trackChannel.channel, args.Member.Id, args.Member.DisplayName, args.Member.Mention, args.Guild.MemberCount);
    } catch (Exception ex) {
      Utils.Log("Error in DiscordMemberAdded: " + ex.Message);
    }
    await Task.Delay(10);
  }

  public static async Task DiscordMemberUpdated(DiscordClient client, DSharpPlus.EventArgs.GuildMemberUpdateEventArgs args) {
    try {
      if (tracking == null) tracking = new Dictionary<ulong, DateTime>();
      TrackChannel trackChannel = SetupModule.TrackChannels[args.Guild.Id];
      if (trackChannel == null || trackChannel.channel == null || !trackChannel.trackRoles) return;

      IReadOnlyList<DiscordRole> rolesBefore = args.RolesBefore;
      IReadOnlyList<DiscordRole> rolesAfter = args.RolesAfter;
      List<DiscordRole> rolesAdded = new List<DiscordRole>();
      // Changed role? We can track only additions. Removals are not really sent

      foreach (DiscordRole r1 in rolesAfter) {
        bool addedRole = true;
        foreach (DiscordRole r2 in rolesBefore) {
          if (r1.Equals(r2)) {
            addedRole = false;
            break;
          }
        }
        if (addedRole) rolesAdded.Add(r1);
      }
      string msgC;
      string msgL;
      if (rolesBefore.Count > 0 && rolesAdded.Count > 0) {
        msgC = "User " + args.Member.Mention + " has the new role" + (rolesAdded.Count > 1 ? "s:" : ":");
        msgL = "User \"" + args.Member.DisplayName + "\" has the new role" + (rolesAdded.Count > 1 ? "s:" : ":");
        foreach (DiscordRole r in rolesAdded) {
          msgC += r.Mention;
          msgL += r.Name;
        }
        await trackChannel.channel.SendMessageAsync(msgC);
        Utils.Log(msgL);
      }
    } catch (Exception ex) {
      Utils.Log("Error in DiscordMemberUpdated: " + ex.Message);
    }

    await Task.Delay(10);
  }


  static async Task SomethingAsync(DiscordChannel trackChannel, ulong id, string name, string mention, int numMembers) {
    await Task.Delay(25000);
    if (tracking.ContainsKey(id)) {
      string msgC = Utils.GetEmojiSnowflakeID(EmojiEnum.OK) + " User " + mention + " joined on " + DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss") + " (" + numMembers + " members total)";
      string msgL = "+ User " + name + " joined on " + DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss") + " (" + numMembers + " members total)";
      await trackChannel.SendMessageAsync(msgC);
      Utils.Log(msgL);
      tracking.Remove(id);
    }
  }
}
