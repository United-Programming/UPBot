using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class MembersTracking {
  static Dictionary<ulong, DateTime> tracking = null;
  static DiscordChannel trackChannel = null;

  public static async Task DiscordMemberRemoved(DiscordClient client, DSharpPlus.EventArgs.GuildMemberRemoveEventArgs args) {
    if (tracking == null) tracking = new Dictionary<ulong, DateTime>();
    if (trackChannel == null) trackChannel = args.Guild.GetChannel(831186370445443104ul);

    if (tracking.ContainsKey(args.Member.Id)) {
      tracking.Remove(args.Member.Id);
      string msg = "User " + args.Member.DisplayName + " did a kiss and go.";
      await trackChannel.SendMessageAsync(msg);
      UtilityFunctions.Log(msg);
    }
    else {
      string msgC = UtilityFunctions.GetEmojiSnowflakeID(EmojiEnum.KO) + " User " + args.Member.Mention + " left on " + DateTime.Now.ToString("yyyyMMdd HH:mm:ss") + " (" + args.Guild.MemberCount + " memebrs total)";
      string msgL = "- User " + args.Member.DisplayName + " left on " + DateTime.Now.ToString("yyyyMMdd HH:mm:ss") + " (" + args.Guild.MemberCount + " memebrs total)";
      await trackChannel.SendMessageAsync(msgC);
      UtilityFunctions.Log(msgL);
    }

    await Task.Delay(10);
  }

  public static async Task DiscordMemberAdded(DiscordClient client, DSharpPlus.EventArgs.GuildMemberAddEventArgs args) {
    if (tracking == null) tracking = new Dictionary<ulong, DateTime>();
    if (trackChannel == null) trackChannel = args.Guild.GetChannel(831186370445443104ul);

    tracking[args.Member.Id] = DateTime.Now;
    await Task.Delay(15000);
    if (tracking.ContainsKey(args.Member.Id)) {
      string msgC = UtilityFunctions.GetEmojiSnowflakeID(EmojiEnum.OK) + " User " + args.Member.Mention + " joined on " + DateTime.Now.ToString("yyyyMMdd HH:mm:ss") + " (" + args.Guild.MemberCount + " memebrs total)";
      string msgL = "+ User " + args.Member.DisplayName + " joined on " + DateTime.Now.ToString("yyyyMMdd HH:mm:ss") + " (" + args.Guild.MemberCount + " memebrs total)";
      await trackChannel.SendMessageAsync(msgC);
      UtilityFunctions.Log(msgL);
      tracking.Remove(args.Member.Id);
    }
    await Task.Delay(10);
  }

}
