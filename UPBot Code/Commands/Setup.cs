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

public class TempSetRole {
  public DiscordRole role;
  public CancellationTokenSource cancel;
  public ulong user;
  internal ulong channel;
  internal ulong message;
  internal ulong emojiid;
  internal string emojiname;

  public TempSetRole(ulong usr, DiscordRole r) {
    user = usr;
    role = r;
    cancel = new CancellationTokenSource();
    channel = 0;
    message = 0;
    emojiid = 0;
    emojiname = null;
  }
}

/// <summary>
/// This command is used to configure the bot, so roles and messages can be set for other servers.
/// author: CPU
/// </summary>
public class Setup : BaseCommandModule {
  readonly private static Dictionary<ulong, DiscordGuild> Guilds = new Dictionary<ulong, DiscordGuild>();
  readonly private static Dictionary<ulong, List<Config>> Configs = new Dictionary<ulong, List<Config>>();
  readonly public static Dictionary<ulong, TrackChannel> TrackChannels = new Dictionary<ulong, TrackChannel>();
  readonly public static Dictionary<ulong, List<ulong>> AdminRoles = new Dictionary<ulong, List<ulong>>();
  readonly public static Dictionary<ulong, ulong> SpamProtection = new Dictionary<ulong, ulong>();
  readonly public static Dictionary<ulong, List<string>> BannedWords = new Dictionary<ulong, List<string>>();
  readonly public static Dictionary<ulong, List<TagBase>> Tags = new Dictionary<ulong, List<TagBase>>();

  readonly public static Dictionary<ulong, WhatToTrack> WhatToTracks = new Dictionary<ulong, WhatToTrack>();
  readonly public static Dictionary<ulong, Dictionary<ulong, ReputationEmoji>> RepEmojis = new Dictionary<ulong, Dictionary<ulong, ReputationEmoji>>();
  readonly public static Dictionary<ulong, Dictionary<ulong, Reputation>> Reputations = new Dictionary<ulong, Dictionary<ulong, Reputation>>();
  readonly public static Dictionary<ulong, List<EmojiForRoleValue>> Em4Roles = new Dictionary<ulong, List<EmojiForRoleValue>>();

  readonly public static Dictionary<ulong, TempSetRole> TempRoleSelected = new Dictionary<ulong, TempSetRole>();

  private readonly static Regex emjSnowflakeRE = new Regex(@"<:[a-z0-9_]+:([0-9]+)>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
  private readonly Regex roleSnowflakeER = new Regex("<@[^0-9]+([0-9]*)>", RegexOptions.Compiled);
  private readonly static Regex emjUnicodeER = new Regex(@"[#*0-9]\uFE0F?\u20E3|©\uFE0F?|[®\u203C\u2049\u2122\u2139\u2194-\u2199\u21A9\u21AA]\uFE0F?|[\u231A\u231B]|[\u2328\u23CF]\uFE0F?|[\u23E9-\u23EC]|[\u23ED-\u23EF]\uFE0F?|\u23F0|[\u23F1\u23F2]\uFE0F?|\u23F3|[\u23F8-\u23FA\u24C2\u25AA\u25AB\u25B6\u25C0\u25FB\u25FC]\uFE0F?|[\u25FD\u25FE]|[\u2600-\u2604\u260E\u2611]\uFE0F?|[\u2614\u2615]|\u2618\uFE0F?|\u261D(?:\uD83C[\uDFFB-\uDFFF]|\uFE0F)?|[\u2620\u2622\u2623\u2626\u262A\u262E\u262F\u2638-\u263A\u2640\u2642]\uFE0F?|[\u2648-\u2653]|[\u265F\u2660\u2663\u2665\u2666\u2668\u267B\u267E]\uFE0F?|\u267F|\u2692\uFE0F?|\u2693|[\u2694-\u2697\u2699\u269B\u269C\u26A0]\uFE0F?|\u26A1|\u26A7\uFE0F?|[\u26AA\u26AB]|[\u26B0\u26B1]\uFE0F?|[\u26BD\u26BE\u26C4\u26C5]|\u26C8\uFE0F?|\u26CE|[\u26CF\u26D1\u26D3]\uFE0F?|\u26D4|\u26E9\uFE0F?|\u26EA|[\u26F0\u26F1]\uFE0F?|[\u26F2\u26F3]|\u26F4\uFE0F?|\u26F5|[\u26F7\u26F8]\uFE0F?|\u26F9(?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?|\uFE0F(?:\u200D[\u2640\u2642]\uFE0F?)?)?|[\u26FA\u26FD]|\u2702\uFE0F?|\u2705|[\u2708\u2709]\uFE0F?|[\u270A\u270B](?:\uD83C[\uDFFB-\uDFFF])?|[\u270C\u270D](?:\uD83C[\uDFFB-\uDFFF]|\uFE0F)?|\u270F\uFE0F?|[\u2712\u2714\u2716\u271D\u2721]\uFE0F?|\u2728|[\u2733\u2734\u2744\u2747]\uFE0F?|[\u274C\u274E\u2753-\u2755\u2757]|\u2763\uFE0F?|\u2764(?:\u200D(?:\uD83D\uDD25|\uD83E\uDE79)|\uFE0F(?:\u200D(?:\uD83D\uDD25|\uD83E\uDE79))?)?|[\u2795-\u2797]|\u27A1\uFE0F?|[\u27B0\u27BF]|[\u2934\u2935\u2B05-\u2B07]\uFE0F?|[\u2B1B\u2B1C\u2B50\u2B55]|[\u3030\u303D\u3297\u3299]\uFE0F?|\uD83C(?:[\uDC04\uDCCF]|[\uDD70\uDD71\uDD7E\uDD7F]\uFE0F?|[\uDD8E\uDD91-\uDD9A]|\uDDE6\uD83C[\uDDE8-\uDDEC\uDDEE\uDDF1\uDDF2\uDDF4\uDDF6-\uDDFA\uDDFC\uDDFD\uDDFF]|\uDDE7\uD83C[\uDDE6\uDDE7\uDDE9-\uDDEF\uDDF1-\uDDF4\uDDF6-\uDDF9\uDDFB\uDDFC\uDDFE\uDDFF]|\uDDE8\uD83C[\uDDE6\uDDE8\uDDE9\uDDEB-\uDDEE\uDDF0-\uDDF5\uDDF7\uDDFA-\uDDFF]|\uDDE9\uD83C[\uDDEA\uDDEC\uDDEF\uDDF0\uDDF2\uDDF4\uDDFF]|\uDDEA\uD83C[\uDDE6\uDDE8\uDDEA\uDDEC\uDDED\uDDF7-\uDDFA]|\uDDEB\uD83C[\uDDEE-\uDDF0\uDDF2\uDDF4\uDDF7]|\uDDEC\uD83C[\uDDE6\uDDE7\uDDE9-\uDDEE\uDDF1-\uDDF3\uDDF5-\uDDFA\uDDFC\uDDFE]|\uDDED\uD83C[\uDDF0\uDDF2\uDDF3\uDDF7\uDDF9\uDDFA]|\uDDEE\uD83C[\uDDE8-\uDDEA\uDDF1-\uDDF4\uDDF6-\uDDF9]|\uDDEF\uD83C[\uDDEA\uDDF2\uDDF4\uDDF5]|\uDDF0\uD83C[\uDDEA\uDDEC-\uDDEE\uDDF2\uDDF3\uDDF5\uDDF7\uDDFC\uDDFE\uDDFF]|\uDDF1\uD83C[\uDDE6-\uDDE8\uDDEE\uDDF0\uDDF7-\uDDFB\uDDFE]|\uDDF2\uD83C[\uDDE6\uDDE8-\uDDED\uDDF0-\uDDFF]|\uDDF3\uD83C[\uDDE6\uDDE8\uDDEA-\uDDEC\uDDEE\uDDF1\uDDF4\uDDF5\uDDF7\uDDFA\uDDFF]|\uDDF4\uD83C\uDDF2|\uDDF5\uD83C[\uDDE6\uDDEA-\uDDED\uDDF0-\uDDF3\uDDF7-\uDDF9\uDDFC\uDDFE]|\uDDF6\uD83C\uDDE6|\uDDF7\uD83C[\uDDEA\uDDF4\uDDF8\uDDFA\uDDFC]|\uDDF8\uD83C[\uDDE6-\uDDEA\uDDEC-\uDDF4\uDDF7-\uDDF9\uDDFB\uDDFD-\uDDFF]|\uDDF9\uD83C[\uDDE6\uDDE8\uDDE9\uDDEB-\uDDED\uDDEF-\uDDF4\uDDF7\uDDF9\uDDFB\uDDFC\uDDFF]|\uDDFA\uD83C[\uDDE6\uDDEC\uDDF2\uDDF3\uDDF8\uDDFE\uDDFF]|\uDDFB\uD83C[\uDDE6\uDDE8\uDDEA\uDDEC\uDDEE\uDDF3\uDDFA]|\uDDFC\uD83C[\uDDEB\uDDF8]|\uDDFD\uD83C\uDDF0|\uDDFE\uD83C[\uDDEA\uDDF9]|\uDDFF\uD83C[\uDDE6\uDDF2\uDDFC]|\uDE01|\uDE02\uFE0F?|[\uDE1A\uDE2F\uDE32-\uDE36]|\uDE37\uFE0F?|[\uDE38-\uDE3A\uDE50\uDE51\uDF00-\uDF20]|[\uDF21\uDF24-\uDF2C]\uFE0F?|[\uDF2D-\uDF35]|\uDF36\uFE0F?|[\uDF37-\uDF7C]|\uDF7D\uFE0F?|[\uDF7E-\uDF84]|\uDF85(?:\uD83C[\uDFFB-\uDFFF])?|[\uDF86-\uDF93]|[\uDF96\uDF97\uDF99-\uDF9B\uDF9E\uDF9F]\uFE0F?|[\uDFA0-\uDFC1]|\uDFC2(?:\uD83C[\uDFFB-\uDFFF])?|[\uDFC3\uDFC4](?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|[\uDFC5\uDFC6]|\uDFC7(?:\uD83C[\uDFFB-\uDFFF])?|[\uDFC8\uDFC9]|\uDFCA(?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|[\uDFCB\uDFCC](?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?|\uFE0F(?:\u200D[\u2640\u2642]\uFE0F?)?)?|[\uDFCD\uDFCE]\uFE0F?|[\uDFCF-\uDFD3]|[\uDFD4-\uDFDF]\uFE0F?|[\uDFE0-\uDFF0]|\uDFF3(?:\u200D(?:\u26A7\uFE0F?|\uD83C\uDF08)|\uFE0F(?:\u200D(?:\u26A7\uFE0F?|\uD83C\uDF08))?)?|\uDFF4(?:\u200D\u2620\uFE0F?|\uDB40\uDC67\uDB40\uDC62\uDB40(?:\uDC65\uDB40\uDC6E\uDB40\uDC67|\uDC73\uDB40\uDC63\uDB40\uDC74|\uDC77\uDB40\uDC6C\uDB40\uDC73)\uDB40\uDC7F)?|[\uDFF5\uDFF7]\uFE0F?|[\uDFF8-\uDFFF])|\uD83D(?:[\uDC00-\uDC07]|\uDC08(?:\u200D\u2B1B)?|[\uDC09-\uDC14]|\uDC15(?:\u200D\uD83E\uDDBA)?|[\uDC16-\uDC3A]|\uDC3B(?:\u200D\u2744\uFE0F?)?|[\uDC3C-\uDC3E]|\uDC3F\uFE0F?|\uDC40|\uDC41(?:\u200D\uD83D\uDDE8\uFE0F?|\uFE0F(?:\u200D\uD83D\uDDE8\uFE0F?)?)?|[\uDC42\uDC43](?:\uD83C[\uDFFB-\uDFFF])?|[\uDC44\uDC45]|[\uDC46-\uDC50](?:\uD83C[\uDFFB-\uDFFF])?|[\uDC51-\uDC65]|[\uDC66\uDC67](?:\uD83C[\uDFFB-\uDFFF])?|\uDC68(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D\uD83D(?:\uDC8B\u200D\uD83D)?\uDC68|\uD83C[\uDF3E\uDF73\uDF7C\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D(?:\uDC66(?:\u200D\uD83D\uDC66)?|\uDC67(?:\u200D\uD83D[\uDC66\uDC67])?|[\uDC68\uDC69]\u200D\uD83D(?:\uDC66(?:\u200D\uD83D\uDC66)?|\uDC67(?:\u200D\uD83D[\uDC66\uDC67])?)|[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92])|\uD83E[\uDDAF-\uDDB3\uDDBC\uDDBD])|\uD83C(?:\uDFFB(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D\uD83D(?:\uDC8B\u200D\uD83D)?\uDC68\uD83C[\uDFFB-\uDFFF]|\uD83C[\uDF3E\uDF73\uDF7C\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83D\uDC68\uD83C[\uDFFC-\uDFFF]|[\uDDAF-\uDDB3\uDDBC\uDDBD])))?|\uDFFC(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D\uD83D(?:\uDC8B\u200D\uD83D)?\uDC68\uD83C[\uDFFB-\uDFFF]|\uD83C[\uDF3E\uDF73\uDF7C\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83D\uDC68\uD83C[\uDFFB\uDFFD-\uDFFF]|[\uDDAF-\uDDB3\uDDBC\uDDBD])))?|\uDFFD(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D\uD83D(?:\uDC8B\u200D\uD83D)?\uDC68\uD83C[\uDFFB-\uDFFF]|\uD83C[\uDF3E\uDF73\uDF7C\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83D\uDC68\uD83C[\uDFFB\uDFFC\uDFFE\uDFFF]|[\uDDAF-\uDDB3\uDDBC\uDDBD])))?|\uDFFE(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D\uD83D(?:\uDC8B\u200D\uD83D)?\uDC68\uD83C[\uDFFB-\uDFFF]|\uD83C[\uDF3E\uDF73\uDF7C\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83D\uDC68\uD83C[\uDFFB-\uDFFD\uDFFF]|[\uDDAF-\uDDB3\uDDBC\uDDBD])))?|\uDFFF(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D\uD83D(?:\uDC8B\u200D\uD83D)?\uDC68\uD83C[\uDFFB-\uDFFF]|\uD83C[\uDF3E\uDF73\uDF7C\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83D\uDC68\uD83C[\uDFFB-\uDFFE]|[\uDDAF-\uDDB3\uDDBC\uDDBD])))?))?|\uDC69(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D\uD83D(?:\uDC8B\u200D\uD83D)?[\uDC68\uDC69]|\uD83C[\uDF3E\uDF73\uDF7C\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D(?:\uDC66(?:\u200D\uD83D\uDC66)?|\uDC67(?:\u200D\uD83D[\uDC66\uDC67])?|\uDC69\u200D\uD83D(?:\uDC66(?:\u200D\uD83D\uDC66)?|\uDC67(?:\u200D\uD83D[\uDC66\uDC67])?)|[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92])|\uD83E[\uDDAF-\uDDB3\uDDBC\uDDBD])|\uD83C(?:\uDFFB(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D\uD83D(?:[\uDC68\uDC69]\uD83C[\uDFFB-\uDFFF]|\uDC8B\u200D\uD83D[\uDC68\uDC69]\uD83C[\uDFFB-\uDFFF])|\uD83C[\uDF3E\uDF73\uDF7C\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83D[\uDC68\uDC69]\uD83C[\uDFFC-\uDFFF]|[\uDDAF-\uDDB3\uDDBC\uDDBD])))?|\uDFFC(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D\uD83D(?:[\uDC68\uDC69]\uD83C[\uDFFB-\uDFFF]|\uDC8B\u200D\uD83D[\uDC68\uDC69]\uD83C[\uDFFB-\uDFFF])|\uD83C[\uDF3E\uDF73\uDF7C\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83D[\uDC68\uDC69]\uD83C[\uDFFB\uDFFD-\uDFFF]|[\uDDAF-\uDDB3\uDDBC\uDDBD])))?|\uDFFD(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D\uD83D(?:[\uDC68\uDC69]\uD83C[\uDFFB-\uDFFF]|\uDC8B\u200D\uD83D[\uDC68\uDC69]\uD83C[\uDFFB-\uDFFF])|\uD83C[\uDF3E\uDF73\uDF7C\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83D[\uDC68\uDC69]\uD83C[\uDFFB\uDFFC\uDFFE\uDFFF]|[\uDDAF-\uDDB3\uDDBC\uDDBD])))?|\uDFFE(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D\uD83D(?:[\uDC68\uDC69]\uD83C[\uDFFB-\uDFFF]|\uDC8B\u200D\uD83D[\uDC68\uDC69]\uD83C[\uDFFB-\uDFFF])|\uD83C[\uDF3E\uDF73\uDF7C\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83D[\uDC68\uDC69]\uD83C[\uDFFB-\uDFFD\uDFFF]|[\uDDAF-\uDDB3\uDDBC\uDDBD])))?|\uDFFF(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D\uD83D(?:[\uDC68\uDC69]\uD83C[\uDFFB-\uDFFF]|\uDC8B\u200D\uD83D[\uDC68\uDC69]\uD83C[\uDFFB-\uDFFF])|\uD83C[\uDF3E\uDF73\uDF7C\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83D[\uDC68\uDC69]\uD83C[\uDFFB-\uDFFE]|[\uDDAF-\uDDB3\uDDBC\uDDBD])))?))?|\uDC6A|[\uDC6B-\uDC6D](?:\uD83C[\uDFFB-\uDFFF])?|\uDC6E(?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|\uDC6F(?:\u200D[\u2640\u2642]\uFE0F?)?|[\uDC70\uDC71](?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|\uDC72(?:\uD83C[\uDFFB-\uDFFF])?|\uDC73(?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|[\uDC74-\uDC76](?:\uD83C[\uDFFB-\uDFFF])?|\uDC77(?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|\uDC78(?:\uD83C[\uDFFB-\uDFFF])?|[\uDC79-\uDC7B]|\uDC7C(?:\uD83C[\uDFFB-\uDFFF])?|[\uDC7D-\uDC80]|[\uDC81\uDC82](?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|\uDC83(?:\uD83C[\uDFFB-\uDFFF])?|\uDC84|\uDC85(?:\uD83C[\uDFFB-\uDFFF])?|[\uDC86\uDC87](?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|[\uDC88-\uDC8E]|\uDC8F(?:\uD83C[\uDFFB-\uDFFF])?|\uDC90|\uDC91(?:\uD83C[\uDFFB-\uDFFF])?|[\uDC92-\uDCA9]|\uDCAA(?:\uD83C[\uDFFB-\uDFFF])?|[\uDCAB-\uDCFC]|\uDCFD\uFE0F?|[\uDCFF-\uDD3D]|[\uDD49\uDD4A]\uFE0F?|[\uDD4B-\uDD4E\uDD50-\uDD67]|[\uDD6F\uDD70\uDD73]\uFE0F?|\uDD74(?:\uD83C[\uDFFB-\uDFFF]|\uFE0F)?|\uDD75(?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?|\uFE0F(?:\u200D[\u2640\u2642]\uFE0F?)?)?|[\uDD76-\uDD79]\uFE0F?|\uDD7A(?:\uD83C[\uDFFB-\uDFFF])?|[\uDD87\uDD8A-\uDD8D]\uFE0F?|\uDD90(?:\uD83C[\uDFFB-\uDFFF]|\uFE0F)?|[\uDD95\uDD96](?:\uD83C[\uDFFB-\uDFFF])?|\uDDA4|[\uDDA5\uDDA8\uDDB1\uDDB2\uDDBC\uDDC2-\uDDC4\uDDD1-\uDDD3\uDDDC-\uDDDE\uDDE1\uDDE3\uDDE8\uDDEF\uDDF3\uDDFA]\uFE0F?|[\uDDFB-\uDE2D]|\uDE2E(?:\u200D\uD83D\uDCA8)?|[\uDE2F-\uDE34]|\uDE35(?:\u200D\uD83D\uDCAB)?|\uDE36(?:\u200D\uD83C\uDF2B\uFE0F?)?|[\uDE37-\uDE44]|[\uDE45-\uDE47](?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|[\uDE48-\uDE4A]|\uDE4B(?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|\uDE4C(?:\uD83C[\uDFFB-\uDFFF])?|[\uDE4D\uDE4E](?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|\uDE4F(?:\uD83C[\uDFFB-\uDFFF])?|[\uDE80-\uDEA2]|\uDEA3(?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|[\uDEA4-\uDEB3]|[\uDEB4-\uDEB6](?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|[\uDEB7-\uDEBF]|\uDEC0(?:\uD83C[\uDFFB-\uDFFF])?|[\uDEC1-\uDEC5]|\uDECB\uFE0F?|\uDECC(?:\uD83C[\uDFFB-\uDFFF])?|[\uDECD-\uDECF]\uFE0F?|[\uDED0-\uDED2\uDED5-\uDED7]|[\uDEE0-\uDEE5\uDEE9]\uFE0F?|[\uDEEB\uDEEC]|[\uDEF0\uDEF3]\uFE0F?|[\uDEF4-\uDEFC\uDFE0-\uDFEB])|\uD83E(?:\uDD0C(?:\uD83C[\uDFFB-\uDFFF])?|[\uDD0D\uDD0E]|\uDD0F(?:\uD83C[\uDFFB-\uDFFF])?|[\uDD10-\uDD17]|[\uDD18-\uDD1C](?:\uD83C[\uDFFB-\uDFFF])?|\uDD1D|[\uDD1E\uDD1F](?:\uD83C[\uDFFB-\uDFFF])?|[\uDD20-\uDD25]|\uDD26(?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|[\uDD27-\uDD2F]|[\uDD30-\uDD34](?:\uD83C[\uDFFB-\uDFFF])?|\uDD35(?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|\uDD36(?:\uD83C[\uDFFB-\uDFFF])?|[\uDD37-\uDD39](?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|\uDD3A|\uDD3C(?:\u200D[\u2640\u2642]\uFE0F?)?|[\uDD3D\uDD3E](?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|[\uDD3F-\uDD45\uDD47-\uDD76]|\uDD77(?:\uD83C[\uDFFB-\uDFFF])?|[\uDD78\uDD7A-\uDDB4]|[\uDDB5\uDDB6](?:\uD83C[\uDFFB-\uDFFF])?|\uDDB7|[\uDDB8\uDDB9](?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|\uDDBA|\uDDBB(?:\uD83C[\uDFFB-\uDFFF])?|[\uDDBC-\uDDCB]|[\uDDCD-\uDDCF](?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|\uDDD0|\uDDD1(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\uD83C[\uDF3E\uDF73\uDF7C\uDF84\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83E\uDDD1|[\uDDAF-\uDDB3\uDDBC\uDDBD]))|\uD83C(?:\uDFFB(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D(?:\uD83D\uDC8B\u200D)?\uD83E\uDDD1\uD83C[\uDFFC-\uDFFF]|\uD83C[\uDF3E\uDF73\uDF7C\uDF84\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83E\uDDD1\uD83C[\uDFFB-\uDFFF]|[\uDDAF-\uDDB3\uDDBC\uDDBD])))?|\uDFFC(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D(?:\uD83D\uDC8B\u200D)?\uD83E\uDDD1\uD83C[\uDFFB\uDFFD-\uDFFF]|\uD83C[\uDF3E\uDF73\uDF7C\uDF84\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83E\uDDD1\uD83C[\uDFFB-\uDFFF]|[\uDDAF-\uDDB3\uDDBC\uDDBD])))?|\uDFFD(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D(?:\uD83D\uDC8B\u200D)?\uD83E\uDDD1\uD83C[\uDFFB\uDFFC\uDFFE\uDFFF]|\uD83C[\uDF3E\uDF73\uDF7C\uDF84\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83E\uDDD1\uD83C[\uDFFB-\uDFFF]|[\uDDAF-\uDDB3\uDDBC\uDDBD])))?|\uDFFE(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D(?:\uD83D\uDC8B\u200D)?\uD83E\uDDD1\uD83C[\uDFFB-\uDFFD\uDFFF]|\uD83C[\uDF3E\uDF73\uDF7C\uDF84\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83E\uDDD1\uD83C[\uDFFB-\uDFFF]|[\uDDAF-\uDDB3\uDDBC\uDDBD])))?|\uDFFF(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D(?:\uD83D\uDC8B\u200D)?\uD83E\uDDD1\uD83C[\uDFFB-\uDFFE]|\uD83C[\uDF3E\uDF73\uDF7C\uDF84\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83E\uDDD1\uD83C[\uDFFB-\uDFFF]|[\uDDAF-\uDDB3\uDDBC\uDDBD])))?))?|[\uDDD2\uDDD3](?:\uD83C[\uDFFB-\uDFFF])?|\uDDD4(?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|\uDDD5(?:\uD83C[\uDFFB-\uDFFF])?|[\uDDD6-\uDDDD](?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|[\uDDDE\uDDDF](?:\u200D[\u2640\u2642]\uFE0F?)?|[\uDDE0-\uDDFF\uDE70-\uDE74\uDE78-\uDE7A\uDE80-\uDE86\uDE90-\uDEA8\uDEB0-\uDEB6\uDEC0-\uDEC2\uDED0-\uDED6])", RegexOptions.Compiled);
  private readonly static Regex chnnelRefRE = new Regex(@"<#([0-9]+)>", RegexOptions.Compiled);

  internal static void LoadParams() {
    try {
      foreach (var g in Utils.GetClient().Guilds.Values) {
        Guilds[g.Id] = g;
      }

      List<Config> dbconfig = Database.GetAll<Config>();
      foreach (var c in dbconfig) {
        ulong gid = c.Guild;

        if (!Configs.ContainsKey(gid)) Configs[gid] = new List<Config>();
        Configs[gid].Add(c);

        // Guilds
        if (!Guilds.ContainsKey(gid)) {
          if (TryGetGuild(gid) == null) continue; // Guild is missing
        }

        // Spam Protection
        if (c.IsParam(Config.ParamType.SpamProtection)) {
          SpamProtection[gid] = c.IdVal;
        }

        // Reputation Tracking
        if (c.IsParam(Config.ParamType.Scores)) {
          WhatToTracks[c.Guild] = (WhatToTrack)c.IdVal;
        }
      }

      // Banned Words
      List<BannedWord> words = Database.GetAll<BannedWord>();
      Utils.Log("Found " + words.Count + " banned words from all servers", null);
      foreach (BannedWord word in words) {
        ulong gid = word.Guild;
        if (!BannedWords.ContainsKey(gid)) BannedWords[gid] = new List<string>();
        BannedWords[gid].Add(word.Word);
      }
      foreach (var bwords in BannedWords.Values)
        bwords.Sort((a, b) => { return a.CompareTo(b); });

      // Reputation Tracking
      List<Reputation> allReps = Database.GetAll<Reputation>();
      foreach (var r in allReps) {
        if (!Reputations.ContainsKey(r.Guild)) Reputations[r.Guild] = new Dictionary<ulong, Reputation>();
        Reputations[r.Guild][r.User] = r;
      }

      // Reputation Emojis
      List<ReputationEmoji> allEmojis = Database.GetAll<ReputationEmoji>();
      if (allEmojis != null) {
        foreach (var r in allEmojis) {
          ulong gid = r.Guild;
          if (!RepEmojis.ContainsKey(gid)) RepEmojis[gid] = new Dictionary<ulong, ReputationEmoji>();
          if (r.For == 0) {
            Database.Delete(r);
            Utils.Log("Removed emoji with ID " + r.GetKeyValue() + " from Guild " + r.Guild + ": no valid use.", null);
            continue;
          }
          try {
            RepEmojis[gid].Add(r.GetKeyValue(), r);

          } catch (ArgumentException aex) {
            Database.Delete(r);
            Utils.Log("Removed emoji with ID " + r.GetKeyValue() + " from Guild " + r.Guild + ": " + aex.Message, null);
          }
        }
      }

      // Admin Roles
      List<AdminRole> allAdminRoles = Database.GetAll<AdminRole>();
      if (allAdminRoles != null) {
        foreach (var r in allAdminRoles) {
          ulong gid = r.Guild;
          if (!AdminRoles.ContainsKey(gid)) AdminRoles[gid] = new List<ulong>();
          AdminRoles[gid].Add(r.Role);
        }
      }

      // Tracking channels
      List<TrackChannel> allTrackChannels = Database.GetAll<TrackChannel>();
      if (allTrackChannels != null) {
        foreach (var r in allTrackChannels) {
          TrackChannels[r.Guild] = r;
          if (!Guilds.ContainsKey(r.Guild) && TryGetGuild(r.Guild) == null) continue; // Guild is missing
          DiscordChannel ch = Guilds[r.Guild].GetChannel(r.ChannelId);
          if (ch == null) {
            Utils.Log("Missing track channel id " + r.ChannelId + " from Guild " + Guilds[r.Guild].Name, Guilds[r.Guild].Name);
            TrackChannels[r.Guild] = null;
          }
          else {
            r.channel = ch;
          }
        }
      }

      // Tags
      List<TagBase> allTags = Database.GetAll<TagBase>();
      if (allTags != null) {
        foreach (var t in allTags) {
          ulong gid = t.Guild;
          if (!Tags.ContainsKey(gid)) Tags[gid] = new List<TagBase>();
          Tags[gid].Add(t);
        }
      }

      // Emoji for role
      List<EmojiForRoleValue> allEmj4rs = Database.GetAll<EmojiForRoleValue>();
      if (allEmj4rs != null) {
        foreach (var e in allEmj4rs) {
          ulong gid = e.Guild;
          if (!Em4Roles.ContainsKey(gid)) Em4Roles[gid] = new List<EmojiForRoleValue>();
          Em4Roles[gid].Add(e);
        }
      }


      // Fill all missing guilds
      foreach (var g in Guilds.Keys) {
        if (!Configs.ContainsKey(g)) Configs[g] = new List<Config>();
        if (!TrackChannels.ContainsKey(g)) TrackChannels[g] = null;
        if (!AdminRoles.ContainsKey(g)) AdminRoles[g] = new List<ulong>();
        if (!SpamProtection.ContainsKey(g)) SpamProtection[g] = 0;
        if (!BannedWords.ContainsKey(g)) BannedWords[g] = new List<string>();
        if (!WhatToTracks.ContainsKey(g)) WhatToTracks[g] = WhatToTrack.None;
        if (!RepEmojis.ContainsKey(g)) RepEmojis[g] = new Dictionary<ulong, ReputationEmoji>();
        if (!Tags.ContainsKey(g)) Tags[g] = new List<TagBase>();
        if (!Em4Roles.ContainsKey(g)) Em4Roles[g] = new List<EmojiForRoleValue>();
        if (!TempRoleSelected.ContainsKey(g)) TempRoleSelected[g] = null;
      }

      Utils.Log("Params fully loaded. " + Configs.Count + " Discord servers found", null);
    } catch (Exception ex) {
      Utils.Log("Error in SetupLoadParams:" + ex.Message, null);
    }
  }


  public static DiscordGuild TryGetGuild(ulong id) {
    if (Guilds.ContainsKey(id)) return Guilds[id];

    Task.Delay(1000);
    int t = 0;
    while (Utils.GetClient() == null) { t += 1000; Task.Delay(t); if (t > 30000) Utils.Log("We are not connecting! (no client)", null); }
    t = 0;
    while (Utils.GetClient().Guilds == null) { t += 1000; Task.Delay(t); if (t > 30000) Utils.Log("We are not connecting! (no guilds)", null); }

    while (Utils.GetClient().Guilds.Count == 0) { t += 1000; Task.Delay(t); if (t > 30000) Utils.Log("We are not connecting! (guilds count is zero", null); }

    IReadOnlyDictionary<ulong, DiscordGuild> cguilds = Utils.GetClient().Guilds;
    foreach (var guildId in cguilds.Keys) {
      if (!Guilds.ContainsKey(guildId)) Guilds[guildId] = cguilds[guildId];
    }
    if (Guilds.ContainsKey(id)) return Guilds[id];

    return null;
  }

  internal static bool Disabled(ulong guild, Config.ParamType t) {
    if (Configs[guild].Count == 0) return t != Config.ParamType.Ping; // Only ping is available by default
    return GetConfigValue(guild, t) == Config.ConfVal.NotAllowed;
  }

  internal static bool Permitted(DiscordGuild guild, Config.ParamType t, CommandContext ctx) {
    if (guild == null) return false;
    if (Configs[guild.Id].Count == 0) return t == Config.ParamType.Ping; // Only ping is available by default
    Config.ConfVal cv = GetConfigValue(guild.Id, t);
    switch (cv) {
      case Config.ConfVal.NotAllowed: return false;
      case Config.ConfVal.Everybody: return true;
      case Config.ConfVal.OnlyAdmins:
        if (ctx.Member.IsOwner) return true;
        IEnumerable<DiscordRole> roles = ctx.Member.Roles;
        foreach (var role in roles) {
          if (IsAdminRole(guild.Id, role)) return true;
        }
        break;
    }
    return t == Config.ParamType.Ping; // Only ping is available by default
  }


  internal static Reputation GetReputation(ulong gid, ulong uid) {
    if (!Reputations.ContainsKey(gid)) Reputations[gid] = new Dictionary<ulong, Reputation>();
    if (!Reputations[gid].ContainsKey(uid)) {
      Reputations[gid][uid] = new Reputation(gid, uid);
      Database.Add(Reputations[gid][uid]);
    }
    return Reputations[gid][uid];
  }

  internal static IReadOnlyCollection<Reputation> GetReputations(ulong gid) {
    if (!Reputations.ContainsKey(gid)) Reputations[gid] = new Dictionary<ulong, Reputation>();
    return Reputations[gid].Values;
  }

  internal static Task NewGuildAdded(DSharpPlus.DiscordClient _, GuildCreateEventArgs e) {
    // FIXME handle this to fill all values for a new guild added

    DiscordGuild g = e.Guild;
    ulong gid = g.Id;
    // Do we have the guild?
    if (TryGetGuild(gid) == null) { // No, that is a problem.
      Utils.Log("Impossible to connect to a new Guild: " + g.Name, null);
      return Task.FromResult(0);
    }
    // Fill all values
    Configs[gid] = new List<Config>();
    TrackChannels[gid] = null;
    AdminRoles[gid] = new List<ulong>();
    SpamProtection[gid] = 0;
    BannedWords[gid] = new List<string>();
    WhatToTracks[gid] = WhatToTrack.None;
    RepEmojis[gid] = new Dictionary<ulong, ReputationEmoji>();
    Tags[gid] = new List<TagBase>();
    Em4Roles[gid] = new List<EmojiForRoleValue>();
    TempRoleSelected[gid] = null;
    Utils.Log("Guild " + g.Name + " joined", g.Name);
    Utils.Log("Guild " + g.Name + " joined", null);

    return Task.FromResult(0);
  }

  internal static bool IsAdminRole(ulong guild, DiscordRole role) {
    if (AdminRoles[guild].Contains(role.Id)) return true;
    return (role.Permissions.HasFlag(DSharpPlus.Permissions.Administrator)); // Fall back
  }
  internal static bool HasAdminRole(ulong guild, IEnumerable<DiscordRole> roles, bool withManageMessages) {
    foreach(var r in roles)
      if (AdminRoles[guild].Contains(r.Id)) return true;
    foreach (var r in roles)
      if (r.Permissions.HasFlag(DSharpPlus.Permissions.Administrator) || (withManageMessages && r.Permissions.HasFlag(DSharpPlus.Permissions.ManageMessages))) return true;
    return false;
  }

  internal static string GetAdminsMentions(ulong gid) {
    if (!AdminRoles.ContainsKey(gid) || AdminRoles[gid].Count == 0) return "";
    string res = "";
    foreach (var rid in AdminRoles[gid]) {
      DiscordRole r = Guilds[gid].GetRole(rid);
      if (r != null) res += r.Mention + " ";
    }
    if (res.Length > 0) return res[0..^1];
    return "";
  }

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

    if (!HasAdminRole(gid, ctx.Member.Roles, false)) {
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
            if (!AdminRoles[gid].Contains(dr.Id)) {
              AdminRoles[gid].Add(dr.Id);
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
          ulong rid = AdminRoles[ctx.Guild.Id][rpos]; ;
          Database.DeleteByKeys<AdminRole>(gid, rid);
          AdminRoles[ctx.Guild.Id].RemoveAt(rpos);
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
          if (TrackChannels[gid] == null) {
            TrackChannel tc = new TrackChannel();
            TrackChannels[gid] = tc;
            tc.trackJoin = true;
            tc.trackLeave = true;
            tc.trackRoles = true;
            tc.channel = answer.Result.MentionedChannels[0];
            tc.Guild = gid;
            tc.ChannelId = tc.channel.Id;
          }
          else {
            Database.Delete(TrackChannels[gid]);
            TrackChannels[gid].channel = answer.Result.MentionedChannels[0];
            TrackChannels[gid].ChannelId = TrackChannels[gid].channel.Id;
          }
          Database.Add(TrackChannels[gid]);

        }
        else if (answer.Result.Content.Contains("remove", StringComparison.InvariantCultureIgnoreCase)) {
          if (TrackChannels[gid] != null) {
            Database.Delete(TrackChannels[gid]);
            TrackChannels[gid] = null;
          }
        }

        await ctx.Channel.DeleteMessageAsync(prompt);
        msg = CreateTrackingInteraction(ctx, null);
      }

      // ************************************************************ DefTracking.Remove Tracking ************************************************************************
      else if (cmdId == "idremtrackch") {
        if (TrackChannels[gid] != null) {
          Database.Delete(TrackChannels[gid]);
          TrackChannels[gid] = null;
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


      // *********** Config Ping ***********************************************************************
      else if (cmdId == "idfeatping" || cmdId == "idfeatping0" || cmdId == "idfeatping1" || cmdId == "idfeatping2") {
        if (cmdId == "idfeatping0") SetConfigValue(gid, Config.ParamType.Ping, Config.ConfVal.NotAllowed);
        if (cmdId == "idfeatping1") SetConfigValue(gid, Config.ParamType.Ping, Config.ConfVal.OnlyAdmins);
        if (cmdId == "idfeatping2") SetConfigValue(gid, Config.ParamType.Ping, Config.ConfVal.Everybody);
        msg = CreatePingInteraction(ctx, msg);
      }

      // ********* Config WhoIs ***********************************************************************
      else if (cmdId == "idfeatwhois" || cmdId == "idfeatwhois0" || cmdId == "idfeatwhois1" || cmdId == "idfeatwhois2") {
        if (cmdId == "idfeatwhois0") SetConfigValue(gid, Config.ParamType.WhoIs, Config.ConfVal.NotAllowed);
        if (cmdId == "idfeatwhois1") SetConfigValue(gid, Config.ParamType.WhoIs, Config.ConfVal.OnlyAdmins);
        if (cmdId == "idfeatwhois2") SetConfigValue(gid, Config.ParamType.WhoIs, Config.ConfVal.Everybody);
        msg = CreateWhoIsInteraction(ctx, msg);
      }

      // ********* Config MassDel ***********************************************************************
      else if (cmdId == "idfeatmassdel" || cmdId == "idfeatmassdel0" || cmdId == "idfeatmassdel1" || cmdId == "idfeatmassdel2") {
        if (cmdId == "idfeatmassdel0") SetConfigValue(gid, Config.ParamType.MassDel, Config.ConfVal.NotAllowed);
        if (cmdId == "idfeatmassdel1") SetConfigValue(gid, Config.ParamType.MassDel, Config.ConfVal.OnlyAdmins);
        if (cmdId == "idfeatmassdel2") SetConfigValue(gid, Config.ParamType.MassDel, Config.ConfVal.Everybody);
        msg = CreateMassDelInteraction(ctx, msg);
      }

      // ********* Config Games ***********************************************************************
      else if (cmdId == "idfeatgames" || cmdId == "idfeatgames0" || cmdId == "idfeatgames1" || cmdId == "idfeatgames2") {
        if (cmdId == "idfeatgames0") SetConfigValue(gid, Config.ParamType.Games, Config.ConfVal.NotAllowed);
        if (cmdId == "idfeatgames1") SetConfigValue(gid, Config.ParamType.Games, Config.ConfVal.OnlyAdmins);
        if (cmdId == "idfeatgames2") SetConfigValue(gid, Config.ParamType.Games, Config.ConfVal.Everybody);
        msg = CreateGamesInteraction(ctx, msg);
      }

      // ********* Config Refactor ***********************************************************************
      else if (cmdId == "idfeatrefactor" || cmdId == "idfeatrefactor0" || cmdId == "idfeatrefactor1" || cmdId == "idfeatrefactor2") {
        if (cmdId == "idfeatrefactor0") SetConfigValue(gid, Config.ParamType.Refactor, Config.ConfVal.NotAllowed);
        if (cmdId == "idfeatrefactor1") SetConfigValue(gid, Config.ParamType.Refactor, Config.ConfVal.OnlyAdmins);
        if (cmdId == "idfeatrefactor2") SetConfigValue(gid, Config.ParamType.Refactor, Config.ConfVal.Everybody);
        msg = CreateRefactorInteraction(ctx, msg);
      }


      // ********* Config Unity Docs ***********************************************************************
      else if (cmdId == "idfeatreunitydocs" || cmdId == "idfeatreunitydocs0" || cmdId == "idfeatreunitydocs1" || cmdId == "idfeatreunitydocs2") {
        if (cmdId == "idfeatreunitydocs0") SetConfigValue(gid, Config.ParamType.UnityDocs, Config.ConfVal.NotAllowed);
        if (cmdId == "idfeatreunitydocs1") SetConfigValue(gid, Config.ParamType.UnityDocs, Config.ConfVal.OnlyAdmins);
        if (cmdId == "idfeatreunitydocs2") SetConfigValue(gid, Config.ParamType.UnityDocs, Config.ConfVal.Everybody);
        msg = CreateUnityDocsInteraction(ctx, msg);
      }

      // ********* Config Spam Protection ***********************************************************************
      else if (cmdId == "idfeatrespamprotect" || cmdId == "idfeatrespamprotect0" || cmdId == "idfeatrespamprotect1" || cmdId == "idfeatrespamprotect2") {
        Config c = GetConfig(gid, Config.ParamType.SpamProtection);
        ulong val = 0;
        if (c != null) val = c.IdVal;
        ulong old = val;
        if (cmdId == "idfeatrespamprotect0") val ^= 1ul;
        if (cmdId == "idfeatrespamprotect1") val ^= 2ul;
        if (cmdId == "idfeatrespamprotect2") val ^= 4ul;
        if (val != old) {
          if (c == null) {
            c = new Config(gid, Config.ParamType.SpamProtection, val);
            Configs[gid].Add(c);
          }
          else c.IdVal = val;
          Database.Add(c);
          SpamProtection[gid] = val;
        }
        msg = CreateSpamProtectInteraction(ctx, msg);
      }

      // ********* Config Timezones ***********************************************************************
      else if (cmdId == "idfeattz" || cmdId == "idfeattzs0" || cmdId == "idfeattzs1" || cmdId == "idfeattzs2" || cmdId == "idfeattzg0" || cmdId == "idfeattzg1" || cmdId == "idfeattzg2") {
        if (cmdId == "idfeattzs0") SetConfigValue(gid, Config.ParamType.TimezoneS, Config.ConfVal.NotAllowed);
        if (cmdId == "idfeattzs1") SetConfigValue(gid, Config.ParamType.TimezoneS, Config.ConfVal.OnlyAdmins);
        if (cmdId == "idfeattzs2") SetConfigValue(gid, Config.ParamType.TimezoneS, Config.ConfVal.Everybody);
        if (cmdId == "idfeattzg0") { SetConfigValue(gid, Config.ParamType.TimezoneG, Config.ConfVal.NotAllowed); SetConfigValue(gid, Config.ParamType.TimezoneS, Config.ConfVal.NotAllowed); }
        if (cmdId == "idfeattzg1") SetConfigValue(gid, Config.ParamType.TimezoneG, Config.ConfVal.OnlyAdmins);
        if (cmdId == "idfeattzg2") SetConfigValue(gid, Config.ParamType.TimezoneG, Config.ConfVal.Everybody);
        msg = CreateTimezoneInteraction(ctx, msg);
      }

      // ********* Config Tags ***********************************************************************
      else if (cmdId == "idfeattags" || cmdId == "idfeattagss0" || cmdId == "idfeattagss1" || cmdId == "idfeattagss2" || cmdId == "idfeattagsg0" || cmdId == "idfeattagsg1" || cmdId == "idfeattagsg2") {
        if (cmdId == "idfeattagss0") SetConfigValue(gid, Config.ParamType.TagsDefine, Config.ConfVal.NotAllowed);
        if (cmdId == "idfeattagss1") SetConfigValue(gid, Config.ParamType.TagsDefine, Config.ConfVal.OnlyAdmins);
        if (cmdId == "idfeattagss2") SetConfigValue(gid, Config.ParamType.TagsDefine, Config.ConfVal.Everybody);
        if (cmdId == "idfeattagsg0") { SetConfigValue(gid, Config.ParamType.TagsUse, Config.ConfVal.NotAllowed); SetConfigValue(gid, Config.ParamType.TagsDefine, Config.ConfVal.NotAllowed); }
        if (cmdId == "idfeattagsg1") SetConfigValue(gid, Config.ParamType.TagsUse, Config.ConfVal.OnlyAdmins);
        if (cmdId == "idfeattagsg2") SetConfigValue(gid, Config.ParamType.TagsUse, Config.ConfVal.Everybody);
        msg = CreateTagsInteraction(ctx, msg);
      }

      // ********* Config Stats ***********************************************************************
      else if (cmdId == "idfeatstats" || cmdId == "idfeatstats0" || cmdId == "idfeatstats1" || cmdId == "idfeatstats2") {
        if (cmdId == "idfeatstats0") SetConfigValue(gid, Config.ParamType.Stats, Config.ConfVal.NotAllowed);
        if (cmdId == "idfeatstats1") SetConfigValue(gid, Config.ParamType.Stats, Config.ConfVal.OnlyAdmins);
        if (cmdId == "idfeatstats2") SetConfigValue(gid, Config.ParamType.Stats, Config.ConfVal.Everybody);
        msg = CreateStatsInteraction(ctx, msg);
      }

      // ********* Config Banned Words ***********************************************************************
      else if (cmdId.Length > 12 && cmdId[0..13] == "idfeatbannedw") {
        if (cmdId == "idfeatbannedwed") {
          if (GetConfigValue(gid, Config.ParamType.BannedWords) == Config.ConfVal.NotAllowed) SetConfigValue(gid, Config.ParamType.BannedWords, Config.ConfVal.Everybody);
          else SetConfigValue(gid, Config.ParamType.BannedWords, Config.ConfVal.NotAllowed);
        }
        else if (cmdId == "idfeatbannedwadd") {
          await ctx.Channel.DeleteMessageAsync(msg);
          msg = null;
          DiscordMessage prompt = await ctx.Channel.SendMessageAsync(ctx.Member.Mention + ", type the word to be banned (at least 4 letters), you have 1 minute before timeout");
          var answer = await interact.WaitForMessageAsync((dm) => {
            return (dm.Channel == ctx.Channel && dm.Author.Id == ctx.Member.Id);
          }, TimeSpan.FromMinutes(1));
          if (answer.Result == null || answer.Result.Content.Length < 4) {
            _ = Utils.DeleteDelayed(10, ctx.Channel.SendMessageAsync("Config timed out"));
          }
          else { // Is it already here? (avoid duplicates)
            string bw = answer.Result.Content.ToLowerInvariant().Trim();
            if (BannedWords[gid].Contains(bw)) {
              _ = Utils.DeleteDelayed(10, ctx.Channel.SendMessageAsync("The word is already there"));
            }
            else {
              BannedWords[gid].Add(bw);
              Database.Add(new BannedWord(gid, bw));
            }
          }
          await ctx.Channel.DeleteMessageAsync(prompt);

        }
        else if (cmdId.Length > 13 && cmdId[0..14] == "idfeatbannedwr") {
          // Get the word by number, remove it. In case there are no more disable the feature
          int.TryParse(cmdId[13..], out int num);
          Database.DeleteByKeys<BannedWord>(gid, BannedWords[gid][num]);
          BannedWords[gid].RemoveAt(num);
        }
        msg = CreateBannedWordsInteraction(ctx, msg);
      }

      // ********* Config Scoring emojis ***********************************************************************
      else if (cmdId == "idfeatscoresere" || cmdId == "idfeatscoresefe") {
        if (msg != null) await ctx.Channel.DeleteMessageAsync(msg);
        msg = null;
        string emjs = "";
        bool missing = true;
        WhatToTrack wtt = cmdId == "idfeatscoresere" ? WhatToTrack.Reputation : WhatToTrack.Fun;
        foreach (var e in RepEmojis[gid].Values)
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
          resp = emjSnowflakeRE.Replace(resp, (m) => {
            if (ulong.TryParse(m.Groups[1].Value, out ulong id)) {
              var rem = new ReputationEmoji(gid, id, null, wtt);
              eset.Add(rem.GetKeyValue(), rem);
            }

            return "";
          });
          // And then the values of the unicode emojis regex
          resp = emjUnicodeER.Replace(resp, (m) => {
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
          foreach (var ek in RepEmojis[gid].Keys) {
            if (RepEmojis[gid][ek].HasFlag(wtt) && !eset.ContainsKey(ek)) toRemove.Add(ek);
          }
          foreach (var e in toRemove) {
            ReputationEmoji re = RepEmojis[gid][e];
            RepEmojis[gid].Remove(e);
            Database.Delete(re);
          }
          // Add all missing entries
          foreach (var ek in eset.Keys) {
            RepEmojis[gid][ek] = eset[ek];
            Database.Add(eset[ek]);
          }

          await ctx.Channel.DeleteMessageAsync(prompt);
        }
        msg = CreateScoresInteraction(ctx, msg);
      }

      // ********* Config Scoring ***********************************************************************
      else if (cmdId.Length > 10 && cmdId[0..11] == "idfeatscore") {
        Config c = GetConfig(gid, Config.ParamType.Scores);
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
            Configs[gid].Add(c);
          }
          else c.IdVal = val;
          Database.Add(c);
          WhatToTracks[gid] = (WhatToTrack)val;
        }
        msg = CreateScoresInteraction(ctx, msg);
      }

      // ********* Emoji for roles ***********************************************************************
      else if (cmdId == "idfeatem4r") {
        msg = CreateEmoji4RoleInteraction(ctx, msg);
      }

      // ********* Emoji for roles.EanbleDisable ***********************************************************************
      else if (cmdId == "idfeatem4rendis") {
        if (GetConfigValue(gid, Config.ParamType.Emoji4Role) == Config.ConfVal.NotAllowed) SetConfigValue(gid, Config.ParamType.Emoji4Role, Config.ConfVal.Everybody);
        else SetConfigValue(gid, Config.ParamType.Emoji4Role, Config.ConfVal.NotAllowed);
        msg = CreateEmoji4RoleInteraction(ctx, msg);
      }

      // ********* Emoji for roles.Add (select role) ***********************************************************************
      else if (cmdId == "idfeatem4radd") { // Just show the interaction with the list of roles
        msg = CreateEmoji4RoleInteractionRoleSelect(ctx, msg);
      }

      // ********* Emoji for roles.Add (role selected) ***********************************************************************
      else if (cmdId.Length > 13 && cmdId[0..14] == "idfeatem4raddr") { // Role selected, do the message to add the emoji to the post
        TempRoleSelected[gid] = null;
        int.TryParse(cmdId[14..], out int rnum);
        var roles = ctx.Guild.Roles.Values;
        int num = 0;
        foreach (var r in roles) {
          if (r.IsManaged || r.Permissions.HasFlag(DSharpPlus.Permissions.Administrator) || r.Position == 0) continue;
          if (num == rnum) {
            TempRoleSelected[gid] = new TempSetRole(ctx.User.Id, r);
            break;
          }
          num++;
        }
        if (TempRoleSelected[gid] == null) { // Something wrong, show just the default interaction
          TempRoleSelected[gid] = null;
          msg = CreateEmoji4RoleInteraction(ctx, msg);
        }
        else {
          msg = CreateEmoji4RoleInteractionEmojiSelect(ctx, msg);
          var waitem = await interact.WaitForButtonAsync(msg, TempRoleSelected[gid].cancel.Token);

          if (TempRoleSelected[gid].cancel.IsCancellationRequested) { // We should have what we need here
            if (TempRoleSelected[gid].message != 0) { // We have a result
              EmojiForRoleValue em = new EmojiForRoleValue {
                Guild = gid,
                Role = TempRoleSelected[gid].role.Id,
                Channel = TempRoleSelected[gid].channel,
                Message = TempRoleSelected[gid].message,
                EmojiId = TempRoleSelected[gid].emojiid,
                EmojiName = TempRoleSelected[gid].emojiname
              };
              Em4Roles[gid].Add(em);
              Database.Add(em);
              TempRoleSelected[gid] = null;
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
          TempRoleSelected[gid] = null;
        }
      }

      // ********* Emoji for roles.remove ***********************************************************************
      else if (cmdId.Length > 13 && cmdId[0..14] == "idfeatem4rlist") {
        int.TryParse(cmdId[14..], out int num);
        EmojiForRoleValue em = Em4Roles[gid][num];
        // Details
        // Do you want to delete it? Yes/No

        msg = CreateEmoji4RoleRemoveInteraction(ctx, msg, em);
        result = await interact.WaitForButtonAsync(msg, TimeSpan.FromMinutes(2));

        if (result.Result != null && result.Result.Id == "idfeatem4rdel") {
          Em4Roles[gid].Remove(em);
          Database.Delete(em);
          msg = CreateEmoji4RoleInteraction(ctx, msg);
        }
        else {
          msg = CreateEmoji4RoleInteraction(ctx, msg);
        }
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
    if (AdminRoles[gid].Count == 0) msg += "**AdminRoles**: _no roles defined. Owner and roles with Admin flag will be considered bot Admins_\n";
    else {
      foreach (var rid in AdminRoles[gid]) {
        DiscordRole r = g.GetRole(rid);
        if (r != null) part += r.Name + ", ";
      }
      if (part.Length == 0) msg += "**AdminRoles**: _no roles defined. Owner and roles with Admin flag will be considered bot Admins_\n";
      else msg += "**AdminRoles**: " + part[0..^2] + "\n";
    }

    // TrackingChannel ******************************************************
    if (TrackChannels[gid] == null) msg += "**TrackingChannel**: _no tracking channel defined_\n";
    else {
      msg += "**TrackingChannel**: " + TrackChannels[gid].channel.Mention + " for ";
      if (TrackChannels[gid].trackJoin || TrackChannels[gid].trackLeave || TrackChannels[gid].trackRoles) {
        if (TrackChannels[gid].trackJoin) msg += "_Join_ ";
        if (TrackChannels[gid].trackLeave) msg += "_Leave_ ";
        if (TrackChannels[gid].trackRoles) msg += "_Roles_ ";
      }
      else msg += "nothing";
      msg += "\n";
    }

    // Ping ******************************************************
    cfg = GetConfig(gid, Config.ParamType.Ping);
    if (cfg == null) msg += "**Ping**: _not defined (allowed to all by default)_\n";
    else msg += "**Ping**: " + (Config.ConfVal)cfg.IdVal + "\n";

    // WhoIs ******************************************************
    cfg = GetConfig(gid, Config.ParamType.WhoIs);
    if (cfg == null) msg += "**WhoIs**: _not defined (disabled by default)_\n";
    else msg += "**WhoIs**: " + (Config.ConfVal)cfg.IdVal + "\n";

    // MassDel ******************************************************
    cfg = GetConfig(gid, Config.ParamType.MassDel);
    if (cfg == null) msg += "**Mass Delete**: _not defined (disabled by default)_\n";
    else msg += "**Mass Delete**: " + (Config.ConfVal)cfg.IdVal + "\n";

    // Games ******************************************************
    cfg = GetConfig(gid, Config.ParamType.Games);
    if (cfg == null) msg += "**Games**: _not defined (disabled by default)_\n";
    else msg += "**Games**: " + (Config.ConfVal)cfg.IdVal + "\n";

    // Refactor ******************************************************
    cfg = GetConfig(gid, Config.ParamType.Refactor);
    if (cfg == null) msg += "**Code Refactor**: _not defined (disabled by default)_\n";
    else msg += "**Code Refactor**: " + (Config.ConfVal)cfg.IdVal + "\n";

    // Timezones ******************************************************
    cfg = GetConfig(gid, Config.ParamType.TimezoneS);
    cfg2 = GetConfig(gid, Config.ParamType.TimezoneG);
    if (cfg == null || cfg2 == null) msg += "**Timezones**: _not defined (disabled by default)_\n";
    else {
      msg += "**Timezones**: Set = " + (Config.ConfVal)cfg.IdVal + "; Read = " + (Config.ConfVal)cfg2.IdVal;
      if ((Config.ConfVal)cfg.IdVal != Config.ConfVal.Everybody && (Config.ConfVal)cfg2.IdVal == Config.ConfVal.Everybody) msg += " (_everybody can set its own timezone_)";
      msg += "\n";
    }

    // UnityDocs ******************************************************
    cfg = GetConfig(gid, Config.ParamType.UnityDocs);
    if (cfg == null) msg += "**Unity Docs**: _not defined (disabled by default)_\n";
    else msg += "**Unity Docs**: " + (Config.ConfVal)cfg.IdVal + "\n";

    // SpamProtection ******************************************************
    cfg = GetConfig(gid, Config.ParamType.SpamProtection);
    if (cfg == null) msg += "**Spam Protection**: _not defined (disabled by default)_\n";
    else if (cfg.IdVal == 0) msg += "**Spam Protection**: _disabled_\n";
    else msg += "**Spam Protection**: enabled for" +
        ((cfg.IdVal & 1ul) == 1 ? " _Discord_" : "") +
        ((cfg.IdVal & 2ul) == 2 ? ((cfg.IdVal & 1ul) == 1 ? ", _Steam_" : " _Steam_") : "") +
        ((cfg.IdVal & 4ul) == 4 ? ((cfg.IdVal & 1ul) != 0 ? ",  _Epic Game Store_" : " _Epic Game Store_") : "") +
        "\n";

    // Stats ******************************************************
    cfg = GetConfig(gid, Config.ParamType.Stats);
    if (cfg == null) msg += "**Stats**: _not defined (disabled by default)_\n";
    else msg += "**Stats**: " + (Config.ConfVal)cfg.IdVal + "\n";

    // Banned Words ******************************************************
    cfg = GetConfig(gid, Config.ParamType.BannedWords);
    if (cfg == null) msg += "**BannedWords**: _not defined (disabled by default)_\n";
    else {
      if (BannedWords[gid].Count == 0) msg += "**BannedWords**: _disabled_ (no words defined)\n";
      else {
        string bws = "";
        foreach (var w in BannedWords[gid]) bws += w + ", ";
        msg += "**BannedWords**: " + bws[0..^2] + ".\n";
      }
    }

    // Scores ******************************************************
    cfg = GetConfig(gid, Config.ParamType.Scores);
    if (cfg == null) msg += "**Scores**: _not defined (disabled by default)_\n";
    else {
      WhatToTrack wtt = (WhatToTrack)cfg.IdVal;
      if (wtt == WhatToTrack.None) msg += "**Scores** is _Disabled_\n"; else msg += "**Scores** _Enabled_ are";
      if (wtt.HasFlag(WhatToTrack.Reputation)) {
        msg += " **Reputation**";
        bool missing = true;
        foreach (var e in RepEmojis[gid].Values)
          if (e.HasFlag(WhatToTrack.Reputation)) {
            missing = false;
            break;
          }
        if (missing) msg += " (_No emojis defined!_)  ";
      }
      if (wtt.HasFlag(WhatToTrack.Fun)) {
        msg += " **Fun**";
        bool missing = true;
        foreach (var e in RepEmojis[gid].Values)
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

    // Tags ******************************************************
    cfg = GetConfig(gid, Config.ParamType.TagsDefine);
    cfg2 = GetConfig(gid, Config.ParamType.TagsUse);
    if (cfg == null || cfg2 == null) msg += "**Tags**: _not defined (disabled by default)_\n";
    else {
      msg += "**Tags**: Set = " + (Config.ConfVal)cfg.IdVal + "; Use = " + (Config.ConfVal)cfg2.IdVal + "\n";
    }

    // Emoji for Role ******************************************************
    cfg = GetConfig(gid, Config.ParamType.Emoji4Role);
    if (cfg == null) msg += "**Emoji for role**: _not defined (disabled by default)_\n";
    else {
      if ((Config.ConfVal)cfg.IdVal == Config.ConfVal.NotAllowed) {
        msg += "**Emoji for role**: _disabled_ ";
      }
      else {
        msg += "**Emoji for role**: _enabled_ ";
      }
      var ems4role = Em4Roles[gid];
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
      if (!HasAdminRole(gid, ctx.Member.Roles, false)) {
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

      // ****************** PING *********************************************************************************************************************************************
      if (cmds[0].Equals("ping")) {
      if (cmds.Length > 1) {
        char mode = cmds[1][0];
        Config c = GetConfig(gid, Config.ParamType.Ping);
        if (c == null) {
          c = new Config(gid, Config.ParamType.Ping, 1);
          Configs[gid].Add(c);
        }
        if (mode == 'n' || mode == 'd') c.IdVal = (int)Config.ConfVal.NotAllowed;
        if (mode == 'a' || mode == 'r' || mode == 'o') c.IdVal = (int)Config.ConfVal.OnlyAdmins;
        if (mode == 'e' || mode == 'y') c.IdVal = (int)Config.ConfVal.Everybody;
        Database.Add(c);
        _ = Utils.DeleteDelayed(15, ctx.Message);
        await Utils.DeleteDelayed(15, ctx.RespondAsync("Ping command changed to " + (Config.ConfVal)c.IdVal));
        } else
          await Utils.DeleteDelayed(15, ctx.RespondAsync("Use: `setup ping` _mode_ (modes can be: _everybody_, _admins_, _disabled_)"));
        return;
      }

      // ****************** WHOIS *********************************************************************************************************************************************
      if (cmds[0].Equals("whois")) {
        if (cmds.Length > 1) {
          char mode = cmds[1][0];
          Config c = GetConfig(gid, Config.ParamType.WhoIs);
          if (c == null) {
            c = new Config(gid, Config.ParamType.WhoIs, 1);
            Configs[gid].Add(c);
          }
          if (mode == 'n' || mode == 'd') c.IdVal = (int)Config.ConfVal.NotAllowed;
          if (mode == 'a' || mode == 'r' || mode == 'o') c.IdVal = (int)Config.ConfVal.OnlyAdmins;
          if (mode == 'e' || mode == 'y') c.IdVal = (int)Config.ConfVal.Everybody;
          Database.Add(c);
          _ = Utils.DeleteDelayed(15, ctx.Message);
          await Utils.DeleteDelayed(15, ctx.RespondAsync("WhoIs command changed to " + (Config.ConfVal)c.IdVal));
        }
        else
          await Utils.DeleteDelayed(15, ctx.RespondAsync("Use: `setup whois` _mode_ (modes can be: _everybody_, _admins_, _disabled_)"));
        return;
      }

      // ****************** MASSDEL *********************************************************************************************************************************************
      if (cmds[0].Equals("massdel")) {
        if (cmds.Length > 1) {
          char mode = cmds[1][0];
          Config c = GetConfig(gid, Config.ParamType.MassDel);
          if (c == null) {
            c = new Config(gid, Config.ParamType.MassDel, 1);
            Configs[gid].Add(c);
          }
          if (mode == 'n' || mode == 'd') c.IdVal = (int)Config.ConfVal.NotAllowed;
          if (mode == 'a' || mode == 'r' || mode == 'o') c.IdVal = (int)Config.ConfVal.OnlyAdmins;
          if (mode == 'e' || mode == 'y') c.IdVal = (int)Config.ConfVal.Everybody;
          Database.Add(c);
          _ = Utils.DeleteDelayed(15, ctx.Message);
          await Utils.DeleteDelayed(15, ctx.RespondAsync("MassDel command changed to " + (Config.ConfVal)c.IdVal));
        } else
          await Utils.DeleteDelayed(15, ctx.RespondAsync("Use: `setup massdel` _mode_ (modes can be: _everybody_ (**NOT RECOMMENDED!**), _admins_, _disabled_)"));
        return;
      }

      // ****************** GAMES *********************************************************************************************************************************************
      if (cmds[0].Equals("games")) {
        if (cmds.Length > 1) {
          char mode = cmds[1][0];
          Config c = GetConfig(gid, Config.ParamType.Games);
          if (c == null) {
            c = new Config(gid, Config.ParamType.Games, 1);
            Configs[gid].Add(c);
          }
          if (mode == 'n' || mode == 'd') c.IdVal = (int)Config.ConfVal.NotAllowed;
          if (mode == 'a' || mode == 'r' || mode == 'o') c.IdVal = (int)Config.ConfVal.OnlyAdmins;
          if (mode == 'e' || mode == 'y') c.IdVal = (int)Config.ConfVal.Everybody;
          Database.Add(c);
          _ = Utils.DeleteDelayed(15, ctx.Message);
          await Utils.DeleteDelayed(15, ctx.RespondAsync("Games command changed to " + (Config.ConfVal)c.IdVal));
        } else
          await Utils.DeleteDelayed(15, ctx.RespondAsync("Use: `setup games` _mode_ (modes can be: _everybody_, _admins_, _disabled_)"));
        return;
      }

      // ****************** REFACTOR *********************************************************************************************************************************************
      if (cmds[0].Equals("refactor") || cmds[0].Equals("reformat")) {
        if (cmds.Length > 1) {
          char mode = cmds[1][0];
          Config c = GetConfig(gid, Config.ParamType.Refactor);
          if (c == null) {
            c = new Config(gid, Config.ParamType.Refactor, 1);
            Configs[gid].Add(c);
          }
          if (mode == 'n' || mode == 'd') c.IdVal = (int)Config.ConfVal.NotAllowed;
          if (mode == 'a' || mode == 'r' || mode == 'o') c.IdVal = (int)Config.ConfVal.OnlyAdmins;
          if (mode == 'e' || mode == 'y') c.IdVal = (int)Config.ConfVal.Everybody;
          Database.Add(c);
          _ = Utils.DeleteDelayed(15, ctx.Message);
          await Utils.DeleteDelayed(15, ctx.RespondAsync("Code Refactor command changed to " + (Config.ConfVal)c.IdVal));
        } else
          await Utils.DeleteDelayed(15, ctx.RespondAsync("Use: `setup refactor` _mode_ (modes can be: _everybody_, _admins_, _disabled_)"));
        return;
      }

      // ****************** UNITYDOCS *********************************************************************************************************************************************
      if (cmds[0].Equals("unitydocs")) {
        if (cmds.Length > 1) {
          char mode = cmds[1][0];
          Config c = GetConfig(gid, Config.ParamType.UnityDocs);
          if (c == null) {
            c = new Config(gid, Config.ParamType.UnityDocs, 1);
            Configs[gid].Add(c);
          }
          if (mode == 'n' || mode == 'd') c.IdVal = (int)Config.ConfVal.NotAllowed;
          if (mode == 'a' || mode == 'r' || mode == 'o') c.IdVal = (int)Config.ConfVal.OnlyAdmins;
          if (mode == 'e' || mode == 'y') c.IdVal = (int)Config.ConfVal.Everybody;
          Database.Add(c);
          _ = Utils.DeleteDelayed(15, ctx.Message);
          await Utils.DeleteDelayed(15, ctx.RespondAsync("UnityDocs command changed to " + (Config.ConfVal)c.IdVal));
        } else
          await Utils.DeleteDelayed(15, ctx.RespondAsync("Use: `setup unitydocs` _mode_ (modes can be: _everybody_, _admins_, _disabled_)"));
        return;
      }

      // ****************** TIMEZONES *********************************************************************************************************************************************
      if (cmds[0].Equals("timezones")) {
        if (cmds.Length > 2) {
          // get|set who
          // who who
          char who = cmds[2][0];
          Config cg = GetConfig(gid, Config.ParamType.TimezoneG);
          Config cs = GetConfig(gid, Config.ParamType.TimezoneS);
          if (cmds[1].Trim().Equals("get", StringComparison.InvariantCultureIgnoreCase)) {
            if (cg == null) {
              cg = new Config(gid, Config.ParamType.TimezoneG, 1);
              Configs[gid].Add(cg);
            }
            if (who == 'n' || who == 'd') cg.IdVal = (int)Config.ConfVal.NotAllowed;
            if (who == 'a' || who == 'r' || who == 'o') cg.IdVal = (int)Config.ConfVal.OnlyAdmins;
            if (who == 'e' || who == 'y') cg.IdVal = (int)Config.ConfVal.Everybody;

          } else if (cmds[1].Trim().Equals("set", StringComparison.InvariantCultureIgnoreCase)) {
            if (cs == null) {
              cs = new Config(gid, Config.ParamType.TimezoneS, 1);
              Configs[gid].Add(cs);
            }
            if (who == 'n' || who == 'd') cs.IdVal = (int)Config.ConfVal.NotAllowed;
            if (who == 'a' || who == 'r' || who == 'o') cs.IdVal = (int)Config.ConfVal.OnlyAdmins;
            if (who == 'e' || who == 'y') cs.IdVal = (int)Config.ConfVal.Everybody;

          } else {
            char whos = cmds[1][0];
            if (cg == null) {
              cg = new Config(gid, Config.ParamType.TimezoneG, 1);
              Configs[gid].Add(cg);
            }
            if (who == 'n' || who == 'd') cg.IdVal = (int)Config.ConfVal.NotAllowed;
            if (who == 'a' || who == 'r' || who == 'o') cg.IdVal = (int)Config.ConfVal.OnlyAdmins;
            if (who == 'e' || who == 'y') cg.IdVal = (int)Config.ConfVal.Everybody;

            if (cs == null) {
              cs = new Config(gid, Config.ParamType.TimezoneS, 1);
              Configs[gid].Add(cs);
            }
            if (whos == 'n' || whos == 'd') cs.IdVal = (int)Config.ConfVal.NotAllowed;
            if (whos == 'a' || whos == 'r' || whos == 'o') cs.IdVal = (int)Config.ConfVal.OnlyAdmins;
            if (whos == 'e' || whos == 'y') cs.IdVal = (int)Config.ConfVal.Everybody;
          }

          if (cg != null || cs != null) {
            if (cg == null) {
              cg = new Config(gid, Config.ParamType.TimezoneG, (int)Config.ConfVal.Everybody);
              Configs[gid].Add(cg);
            }
            if (cs == null) {
              cs = new Config(gid, Config.ParamType.TimezoneG, (int)Config.ConfVal.OnlyAdmins);
              Configs[gid].Add(cs);
            }
            Database.Add(cg);
            Database.Add(cs);
          }
          _ = Utils.DeleteDelayed(15, ctx.Message);
          await Utils.DeleteDelayed(15, ctx.RespondAsync("Timezones command changed to  SET = " + (Config.ConfVal)cs.IdVal + "  GET = " + (Config.ConfVal)cg.IdVal));
        } else
          await Utils.DeleteDelayed(15, ctx.RespondAsync("Use: `setup timezones` [set|get] _mode_ (modes can be: _everybody_, _admins_, _disabled_, you have to specify who can set (define) and who can read them)"));
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
              if (!TrackChannels.ContainsKey(gid) || TrackChannels[gid] == null) {
                await Utils.DeleteDelayed(15, ctx.RespondAsync("No tracking channel was defined for this server"));
              } else {
                Database.Delete(TrackChannels[gid]);
                TrackChannels[gid] = null;
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
              Match cm = chnnelRefRE.Match(cmd);
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
                if (!TrackChannels.ContainsKey(gid) || TrackChannels[gid] == null) {
                  tc = new TrackChannel(gid, ch.Id) {  // Create
                    trackJoin = true,
                    trackLeave = true,
                    trackRoles = true,
                    channel = ch
                  };
                  TrackChannels[gid] = tc;
                } else {
                  tc = TrackChannels[gid]; // Grab
                  tc.channel = ch;
                }
              }
            }
          }
          if (atLestOne && tc == null && TrackChannels[gid] != null)
            tc = TrackChannels[gid];
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
            foreach (var r in AdminRoles[gid]) {
              Database.DeleteByKeys<AdminRole>(gid, r);
            }
            AdminRoles[gid].Clear();

            _ = Utils.DeleteDelayed(15, ctx.Message);
            msg = "**AdminRoles** removed";
            await Utils.DeleteDelayed(15, ctx.RespondAsync(msg));
            return;

          }
          else if (cmds[1].Equals("list", StringComparison.InvariantCultureIgnoreCase)) {
            msg = "**AdminRoles** are: ";
            if (!AdminRoles.ContainsKey(gid) || AdminRoles[gid].Count == 0) {
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
              foreach (var rid in AdminRoles[gid]) {
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
              Match rm = roleSnowflakeER.Match(cmds[i]);
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
                if (!AdminRoles[gid].Contains(id)) {
                  AdminRoles[gid].Add(id);
                  Database.Add(new AdminRole(gid, id));
                }
              }
            }
          }
          // And show the result
          if (AdminRoles[gid].Count == 0) msg = "No valid **AdminRoles** defined";
          else {
            msg = "**AdminRoles** are: ";
            foreach (var rid in AdminRoles[gid]) {
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
          Config c = GetConfig(gid, Config.ParamType.SpamProtection);
          if (c == null) {
            c = new Config(gid, Config.ParamType.SpamProtection, val);
            Configs[gid].Add(c);
          }
          else c.IdVal = val;
          SpamProtection[gid] = val;

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

      // ****************** STATS *********************************************************************************************************************************************
      if (cmds[0].Equals("stats") && cmds.Length > 1) {
        char mode = cmds[1][0];
        Config c = GetConfig(gid, Config.ParamType.Stats);
        if (c == null) {
          c = new Config(gid, Config.ParamType.Stats, 1);
          Configs[gid].Add(c);
        }
        if (mode == 'n' || mode == 'd') c.IdVal = (int)Config.ConfVal.NotAllowed;
        if (mode == 'a' || mode == 'r' || mode == 'o') c.IdVal = (int)Config.ConfVal.OnlyAdmins;
        if (mode == 'e' || mode == 'y') c.IdVal = (int)Config.ConfVal.Everybody;
        Database.Add(c);
        _ = Utils.DeleteDelayed(15, ctx.Message);
        await Utils.DeleteDelayed(15, ctx.RespondAsync("Stats command changed to " + (Config.ConfVal)c.IdVal));
        return;
      }

      // ****************** BANNEDWORDS *********************************************************************************************************************************************
      if (cmds[0].Equals("bannedwords")) {
        if (cmds.Length == 1 || cmds[1].Equals("?", StringComparison.InvariantCultureIgnoreCase) || cmds[1].Equals("help", StringComparison.InvariantCultureIgnoreCase)) { // HELP *******************************************
          await Utils.DeleteDelayed(15, ctx.RespondAsync("Use: `enable` or `disable` to change the status\n`add` _word_ to add a new word to ban\n`remove` _word_ to remove a word from the list\n`clear` to remove all words\n`list` to show all banned words."));
        }
        if (cmds.Length > 1) {
          if (cmds[1].Equals("list", StringComparison.InvariantCultureIgnoreCase)) { // LIST ********************************************************************************************************************
            if (BannedWords[gid].Count == 0) await Utils.DeleteDelayed(15, ctx.RespondAsync("No banned words are defined"));
            string bws = "Banned Words: ";
            foreach (var w in BannedWords[gid]) bws += w + ", ";
            bws = bws[0..^2];
            if (GetConfigValue(gid, Config.ParamType.BannedWords) == Config.ConfVal.NotAllowed) bws += " (_disabled_)";
            else bws += " (_enabled_)";
            await Utils.DeleteDelayed(15, ctx.RespondAsync(bws));

          } else if (cmds[1].Equals("enable", StringComparison.InvariantCultureIgnoreCase)) { // ENABLE ******************************************************************************************
            SetConfigValue(ctx.Guild.Id, Config.ParamType.BannedWords, Config.ConfVal.Everybody);
            if (BannedWords[gid].Count == 0)
              await Utils.DeleteDelayed(15, ctx.RespondAsync("Banned words command changed to _enabled_ (but no banned words are deifned)"));
            else
              await Utils.DeleteDelayed(15, ctx.RespondAsync("Banned words command changed to _enabled_"));

          } else if (cmds[1].Equals("disable", StringComparison.InvariantCultureIgnoreCase)) { // DISABLE ******************************************************************************************
            SetConfigValue(ctx.Guild.Id, Config.ParamType.BannedWords, Config.ConfVal.NotAllowed);
            await Utils.DeleteDelayed(15, ctx.RespondAsync("Banned words command changed to _disabled_"));

          } else if (cmds[1].Equals("add", StringComparison.InvariantCultureIgnoreCase) && cmds.Length > 2) { // ADD ******************************************************************************************
            string bw = cmds[2].ToLowerInvariant().Trim();
            if (BannedWords[gid].Contains(bw)) {
              await Utils.DeleteDelayed(15, ctx.RespondAsync("The word is already there"));
            } else {
              BannedWords[gid].Add(bw);
              Database.Add(new BannedWord(gid, bw));
            }
            await Utils.DeleteDelayed(15, ctx.RespondAsync("The word is added the the list of banned words"));

          } else if (cmds[1].Equals("remove", StringComparison.InvariantCultureIgnoreCase) && cmds.Length > 2) { // REMOVE *************************************************************************************************
            string bw = cmds[2].ToLowerInvariant().Trim();
            if (BannedWords[gid].Contains(bw)) {
              Database.DeleteByKeys<BannedWord>(gid, bw);
              BannedWords[gid].Remove(bw);
              await Utils.DeleteDelayed(15, ctx.RespondAsync("The word is removed from the list of banned words"));
            } else {
              await Utils.DeleteDelayed(15, ctx.RespondAsync("The word is not in the list of banned words"));
            }

          } else if (cmds[1].Equals("clear", StringComparison.InvariantCultureIgnoreCase) || cmds[1].Equals("clean", StringComparison.InvariantCultureIgnoreCase)) { // CLEAR *********************************************
            foreach (var bw in BannedWords[gid]) {
              Database.DeleteByKeys<BannedWord>(gid, bw);
            }
            BannedWords[gid].Clear();
          }
        }
        return;
      }

      // ****************** SCORES *********************************************************************************************************************************************
      if (cmds[0].Equals("scores")) {
        if (cmds.Length > 1) {
          // [rep*|fun|thank*|rank*|mention*] (val) -> enable disable
          // [rep*|fun] set -> set emojis
          // [rep*|fun] list -> list emojis

          Config c = GetConfig(gid, Config.ParamType.Scores);
          string what = (cmds[1].Trim() + "  ")[0..3].ToLowerInvariant();
          if (what == "res") {

            foreach (var em in RepEmojis[gid].Values) {
              Database.Delete(em);
            }
            int num = RepEmojis[gid].Count;
            RepEmojis[gid].Clear();
            await Utils.DeleteDelayed(15, ctx.RespondAsync("All emojis for Scores have been removed (" + num + " items.)"));

          } else if (what == "rep" || what == "fun") {
            WhatToTrack wtt = (what == "rep") ? WhatToTrack.Reputation : WhatToTrack.Fun;
            string wtts = (what == "rep") ? "Reputation" : "Fun";

            if (c == null) { c = new Config(gid, Config.ParamType.Scores, 0); Configs[gid].Add(c); }
            if (cmds.Length == 2 || (cmds.Length > 2 && cmds[2].Trim().ToLowerInvariant() == "list")) {
              string emjs = "Emojis defined for the _" + wtts + "_ score: ";
              bool missing = true;
              foreach (var e in RepEmojis[ctx.Guild.Id].Values)
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
              foreach (var k in RepEmojis[gid].Keys) {
                var em = RepEmojis[gid][k];
                if (em.HasFlag(wtt)) {
                  Database.Delete(em);
                  toRemove.Add(k);
                }
              }
              foreach (var k in toRemove)
                RepEmojis[gid].Remove(k);
              await Utils.DeleteDelayed(15, ctx.RespondAsync("All emojis for " + wtts + " have been removed."));

            } else if (cmds.Length > 2 && cmds[2].Trim().ToLowerInvariant() == "set") {
              string emjs = "";
              bool missing = true;
              foreach (var e in RepEmojis[gid].Values)
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
                resp = emjSnowflakeRE.Replace(resp, (m) => {
                  if (ulong.TryParse(m.Groups[1].Value, out ulong id)) {
                    var rem = new ReputationEmoji(gid, id, null, wtt);
                    eset.Add(rem.GetKeyValue(), rem);
                  }
                  return "";
                });
                // And then the values of the unicode emojis regex
                resp = emjUnicodeER.Replace(resp, (m) => {
                  var rem = new ReputationEmoji(gid, 0, m.Value, wtt);
                  eset.Add(rem.GetKeyValue(), rem);
                  return "";
                });

                // Remove all entries that are no more in the list
                List<ulong> toRemove = new List<ulong>();
                foreach (var ek in RepEmojis[gid].Keys) {
                  if (RepEmojis[gid][ek].HasFlag(wtt) && !eset.ContainsKey(ek)) toRemove.Add(ek);
                }
                foreach (var e in toRemove) {
                  ReputationEmoji re = RepEmojis[gid][e];
                  RepEmojis[gid].Remove(e);
                  Database.Delete(re);
                }
                // Add all missing entries
                foreach (var ek in eset.Keys) {
                  if (!RepEmojis[gid].ContainsKey(ek)) {
                    RepEmojis[gid].Add(ek, eset[ek]);
                    Database.Add(eset[ek]);
                  } else {
                    Database.Update(eset[ek]);
                  }
                }

                // Show the result, just to check
                emjs = "Emojis defined for the _" + wtts + "_ score: ";
                missing = true;
                foreach (var e in RepEmojis[gid].Values)
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
              WhatToTracks[gid] = (WhatToTrack)c.IdVal;
              Database.Update(c);
              if (((WhatToTrack)c.IdVal).HasFlag(wtt))
                await Utils.DeleteDelayed(15, ctx.RespondAsync("Scores " + wtts + " changed to _enabled_"));
              else
                await Utils.DeleteDelayed(15, ctx.RespondAsync("Scores " + wtts + " changed to _disabled_"));
            }
          } else if ((what == "tha" || what == "ran" || what == "men") && cmds.Length > 2) {
            if (c == null) { c = new Config(gid, Config.ParamType.Scores, 0); Configs[gid].Add(c); }
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
            WhatToTracks[gid] = (WhatToTrack)c.IdVal;
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
            if (Em4Roles[gid].Count == 0) {
              await Utils.DeleteDelayed(15, ctx.RespondAsync("No emoji for roles are defined"));
              return;
            }
            string ems = "Emojis for role: ";
            if (GetConfigValue(gid, Config.ParamType.Emoji4Role) == Config.ConfVal.NotAllowed) ems += " (_disabled_)";
            else ems += " (_enabled_)";
            int pos = 1;
            foreach (var em4r in Em4Roles[gid]) {
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

          else if (cmds[1].Equals("enable", StringComparison.InvariantCultureIgnoreCase)) { // ENABLE ******************************************************************************************
            SetConfigValue(ctx.Guild.Id, Config.ParamType.Emoji4Role, Config.ConfVal.Everybody);
            if (BannedWords[gid].Count == 0)
              await Utils.DeleteDelayed(15, ctx.RespondAsync("Emoji for Role command changed to _enabled_ (but no roles are deifned)"));
            else
              await Utils.DeleteDelayed(15, ctx.RespondAsync("Emoji for Role command changed to _enabled_"));
          }

          else if (cmds[1].Equals("disable", StringComparison.InvariantCultureIgnoreCase)) { // DISABLE ******************************************************************************************
            SetConfigValue(ctx.Guild.Id, Config.ParamType.Emoji4Role, Config.ConfVal.NotAllowed);
            await Utils.DeleteDelayed(15, ctx.RespondAsync("Emoji for Role command changed to _disabled_"));
          }

          else if (cmds[1].Equals("add", StringComparison.InvariantCultureIgnoreCase) && cmds.Length > 2) { // ADD ******************************************************************************************
            string rolename = cmds[2].ToLowerInvariant().Trim();

            // Get the role from the guild, as id or name
            DiscordRole role = null;
            Match rm = roleSnowflakeER.Match(rolename);
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
              TempRoleSelected[gid] = new TempSetRole(ctx.User.Id, role);

              string msg = "Role is selected to **" + TempRoleSelected[gid].role.Name + "**\nAdd the emoji you want for this role to the message you want to monitor.";
              DiscordMessage toDelete = await ctx.RespondAsync(msg);
              _ = TempRoleSelected[gid].cancel.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(120));

              await toDelete.DeleteAsync();
              if (TempRoleSelected[gid].cancel.IsCancellationRequested) { // We should have what we need here
                if (TempRoleSelected[gid].message != 0) { // We have a result
                  EmojiForRoleValue em = new EmojiForRoleValue {
                    Guild = gid,
                    Role = TempRoleSelected[gid].role.Id,
                    Channel = TempRoleSelected[gid].channel,
                    Message = TempRoleSelected[gid].message,
                    EmojiId = TempRoleSelected[gid].emojiid,
                    EmojiName = TempRoleSelected[gid].emojiname
                  };
                  Em4Roles[gid].Add(em);
                  Database.Add(em);
                  TempRoleSelected[gid] = null;
                  await Utils.DeleteDelayed(15, ctx.RespondAsync("The emoji for role (" + Utils.GetEmojiSnowflakeID(em.EmojiId, em.EmojiName, g) + ") has been added"));
                }
              }
            }
          }

          else if (cmds[1].Equals("remove", StringComparison.InvariantCultureIgnoreCase) && cmds.Length > 2) { // REMOVE *************************************************************************************************
            // We can have either a number or an emoji
            string identifier = cmds[2].Trim();

            if (int.TryParse(identifier, out int id)) { // Is an index
              if (Em4Roles[gid].Count >= id && id > 0) {
                Em4Roles[gid].RemoveAt(id - 1);
                await Utils.DeleteDelayed(15, ctx.RespondAsync("Emoji for role has been removed"));
              }
              else {
                await Utils.DeleteDelayed(15, ctx.RespondAsync("Cannot find the Emoji for role to remove"));
              }
            }
            else { // Check by emoji
              Match m = emjSnowflakeRE.Match(identifier);
              if (m.Success && ulong.TryParse(m.Groups[1].Value, out ulong eid)) { // Get the id
                EmojiForRoleValue e = null;
                foreach (var em4r in Em4Roles[gid]) {
                  if (em4r.EmojiId == eid) {
                    e = em4r;
                    break;
                  }
                }
                if (e != null) {
                  Em4Roles[gid].Remove(e);
                  await Utils.DeleteDelayed(15, ctx.RespondAsync("Emoji for role has been removed"));
                }
                else {
                  await Utils.DeleteDelayed(15, ctx.RespondAsync("Cannot find the Emoji for role to remove"));
                }
              }
              else {
                EmojiForRoleValue e = null;
                foreach (var em4r in Em4Roles[gid]) {
                  if (em4r.EmojiName.Equals(identifier, StringComparison.InvariantCultureIgnoreCase)) {
                    e = em4r;
                    break;
                  }
                }
                if (e != null) {
                  Em4Roles[gid].Remove(e);
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



      await Utils.DeleteDelayed(15, ctx.RespondAsync("I do not understand the command: " + ctx.Message.Content));

    } catch (Exception ex) {
      Utils.Log("Error in Setup by command line: " + ex.Message, ctx.Guild.Name);
    }
  }

  private void AlterTracking(ulong gid, bool j, bool l, bool r) {
    TrackChannel tc = TrackChannels[gid];
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
    if (AdminRoles[ctx.Guild.Id].Count == 0) desc += "_**No admin roles defined.** Owner and server Admins will be used_";
    else {
      List<ulong> roles = AdminRoles[ctx.Guild.Id];
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
    foreach(ulong rid in AdminRoles[ctx.Guild.Id]) {
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

    TrackChannel tc = TrackChannels[ctx.Guild.Id];

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
    if (TrackChannels[ctx.Guild.Id] != null)
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


    // Ping
    // Whois
    // Mass delete
    // Games
    // Refactor code
    actions = new List<DiscordButtonComponent>();
    Config.ConfVal cv = GetConfigValue(ctx.Guild.Id, Config.ParamType.Ping);
    actions.Add(new DiscordButtonComponent(GetStyle(cv), "idfeatping", "Ping", false, er));

    cv = GetConfigValue(ctx.Guild.Id, Config.ParamType.WhoIs);
    actions.Add(new DiscordButtonComponent(GetStyle(cv), "idfeatwhois", "WhoIs", false, er));

    cv = GetConfigValue(ctx.Guild.Id, Config.ParamType.MassDel);
    actions.Add(new DiscordButtonComponent(GetStyle(cv), "idfeatmassdel", "Mass Delete", false, er));

    cv = GetConfigValue(ctx.Guild.Id, Config.ParamType.Games);
    actions.Add(new DiscordButtonComponent(GetStyle(cv), "idfeatgames", "Games", false, er));

    cv = GetConfigValue(ctx.Guild.Id, Config.ParamType.Refactor);
    actions.Add(new DiscordButtonComponent(GetStyle(cv), "idfeatrefactor", "Refactor Code", false, er));

    builder.AddComponents(actions);

    // Timezones
    // UnityDocs
    // Spam protection
    // Stats
    // Tags
    actions = new List<DiscordButtonComponent>();
    cv = GetConfigValue(ctx.Guild.Id, Config.ParamType.TimezoneG);
    actions.Add(new DiscordButtonComponent(GetStyle(cv), "idfeattz", "Timezone", false, er));
    cv = GetConfigValue(ctx.Guild.Id, Config.ParamType.UnityDocs);
    actions.Add(new DiscordButtonComponent(GetStyle(cv), "idfeatreunitydocs", "Unity Docs", false, er));
    Config sc = GetConfig(ctx.Guild.Id, Config.ParamType.SpamProtection);
    actions.Add(new DiscordButtonComponent((sc == null || sc.IdVal == 0) ? DSharpPlus.ButtonStyle.Secondary : DSharpPlus.ButtonStyle.Primary, "idfeatrespamprotect", "Spam Protection", false, er));
    cv = GetConfigValue(ctx.Guild.Id, Config.ParamType.Stats);
    actions.Add(new DiscordButtonComponent(GetStyle(cv), "idfeatstats0", "Stats", false, er));
    cv = GetConfigValue(ctx.Guild.Id, Config.ParamType.TagsUse);
    actions.Add(new DiscordButtonComponent(GetStyle(cv), "idfeattags", "Tags", false, er));
    builder.AddComponents(actions);

    // Ranking/Scores
    // Banned words
    // Emogi for roles
    actions = new List<DiscordButtonComponent>();
    cv = GetConfigValue(ctx.Guild.Id, Config.ParamType.Scores);
    actions.Add(new DiscordButtonComponent(GetStyle(cv), "idfeatscores", "Scores", false, er));
    cv = GetConfigValue(ctx.Guild.Id, Config.ParamType.BannedWords);
    actions.Add(new DiscordButtonComponent(GetStyle(cv), "idfeatbannedw", "Banned Words", false, er));
    cv = GetConfigValue(ctx.Guild.Id, Config.ParamType.Emoji4Role);
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


  private DiscordMessage CreatePingInteraction(CommandContext ctx, DiscordMessage prevMsg) {
    if (prevMsg != null) ctx.Channel.DeleteMessageAsync(prevMsg).Wait();

    DiscordEmbedBuilder eb = new DiscordEmbedBuilder {
      Title = "UPBot Configuration - Ping"
    };
    eb.WithThumbnail(ctx.Guild.IconUrl);
    Config.ConfVal cv = GetConfigValue(ctx.Guild.Id, Config.ParamType.Ping);
    eb.Description = "Configuration of the UP Bot for the Discord Server **" + ctx.Guild.Name + "**\n\n" +
      "The **ping** command will just make the bot to asnwer when it is alive.\n\n";
    if (cv == Config.ConfVal.NotAllowed) eb.Description += "**Ping** feature is _Disabled_";
    if (cv == Config.ConfVal.OnlyAdmins) eb.Description += "**Ping** feature is _Enabled_ for Admins";
    if (cv == Config.ConfVal.Everybody) eb.Description += "**Ping** feature is _Enabled_ for Everybody";
    eb.WithImageUrl(ctx.Guild.BannerUrl);
    eb.WithFooter("Member that started the configuration is: " + ctx.Member.DisplayName, ctx.Member.AvatarUrl);

    List<DiscordButtonComponent> actions;
    var builder = new DiscordMessageBuilder();
    builder.AddEmbed(eb.Build());

    actions = new List<DiscordButtonComponent> {
      new DiscordButtonComponent(GetIsStyle(cv, Config.ConfVal.NotAllowed), "idfeatping0", "Not allowed", false, GetYN(cv, Config.ConfVal.NotAllowed)),
      new DiscordButtonComponent(GetIsStyle(cv, Config.ConfVal.OnlyAdmins), "idfeatping1", "Only Admins", false, GetYN(cv, Config.ConfVal.OnlyAdmins)),
      new DiscordButtonComponent(GetIsStyle(cv, Config.ConfVal.Everybody), "idfeatping2", "Everybody", false, GetYN(cv, Config.ConfVal.Everybody))
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

  private DiscordMessage CreateWhoIsInteraction(CommandContext ctx, DiscordMessage prevMsg) {
    if (prevMsg != null) ctx.Channel.DeleteMessageAsync(prevMsg).Wait();

    DiscordEmbedBuilder eb = new DiscordEmbedBuilder {
      Title = "UPBot Configuration - WhoIs"
    };
    eb.WithThumbnail(ctx.Guild.IconUrl);
    Config.ConfVal cv = GetConfigValue(ctx.Guild.Id, Config.ParamType.WhoIs);
    eb.Description = "Configuration of the UP Bot for the Discord Server **" + ctx.Guild.Name + "**\n\n" +
      "The **whois** command allows to see who an user is with some statistics.\n\n";
    if (cv == Config.ConfVal.NotAllowed) eb.Description += "**WhoIs** feature is _Disabled_";
    if (cv == Config.ConfVal.OnlyAdmins) eb.Description += "**WhoIs** feature is _Enabled_ for Admins";
    if (cv == Config.ConfVal.Everybody) eb.Description += "**WhoIs** feature is _Enabled_ for Everybody";
    eb.WithImageUrl(ctx.Guild.BannerUrl);
    eb.WithFooter("Member that started the configuration is: " + ctx.Member.DisplayName, ctx.Member.AvatarUrl);

    List<DiscordButtonComponent> actions;
    var builder = new DiscordMessageBuilder();
    builder.AddEmbed(eb.Build());

    actions = new List<DiscordButtonComponent> {
      new DiscordButtonComponent(GetIsStyle(cv, Config.ConfVal.NotAllowed), "idfeatwhois0", "Not allowed", false, GetYN(cv, Config.ConfVal.NotAllowed)),
      new DiscordButtonComponent(GetIsStyle(cv, Config.ConfVal.OnlyAdmins), "idfeatwhois1", "Only Admins", false, GetYN(cv, Config.ConfVal.OnlyAdmins)),
      new DiscordButtonComponent(GetIsStyle(cv, Config.ConfVal.Everybody), "idfeatwhois2", "Everybody", false, GetYN(cv, Config.ConfVal.Everybody))
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

  private DiscordMessage CreateMassDelInteraction(CommandContext ctx, DiscordMessage prevMsg) {
    if (prevMsg != null) ctx.Channel.DeleteMessageAsync(prevMsg).Wait();

    DiscordEmbedBuilder eb = new DiscordEmbedBuilder {
      Title = "UPBot Configuration - Mass Delete"
    };
    eb.WithThumbnail(ctx.Guild.IconUrl);
    Config.ConfVal cv = GetConfigValue(ctx.Guild.Id, Config.ParamType.MassDel);
    eb.Description = "Configuration of the UP Bot for the Discord Server **" + ctx.Guild.Name + "**\n\n" +
      "The **delete** command can mass remove a set of messages from a channel. It is recommended to limit it to admins.\n\n";
    if (cv == Config.ConfVal.NotAllowed) eb.Description += "**Mass Delete** feature is _Disabled_";
    if (cv == Config.ConfVal.OnlyAdmins) eb.Description += "**Mass Delete** feature is _Enabled_ for Admins";
    if (cv == Config.ConfVal.Everybody) eb.Description += "**Mass Delete** feature is _Enabled_ for Everybody";
    eb.WithImageUrl(ctx.Guild.BannerUrl);
    eb.WithFooter("Member that started the configuration is: " + ctx.Member.DisplayName, ctx.Member.AvatarUrl);

    List<DiscordButtonComponent> actions;
    var builder = new DiscordMessageBuilder();
    builder.AddEmbed(eb.Build());

    actions = new List<DiscordButtonComponent> {
      new DiscordButtonComponent(GetIsStyle(cv, Config.ConfVal.NotAllowed), "idfeatmassdel0", "Not allowed", false, GetYN(cv, Config.ConfVal.NotAllowed)),
      new DiscordButtonComponent(GetIsStyle(cv, Config.ConfVal.OnlyAdmins), "idfeatmassdel1", "Only Admins", false, GetYN(cv, Config.ConfVal.OnlyAdmins)),
      new DiscordButtonComponent(GetIsStyle(cv, Config.ConfVal.Everybody), "idfeatmassdel2", "Everybody (not recommended)", false, GetYN(cv, Config.ConfVal.Everybody))
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

  private DiscordMessage CreateGamesInteraction(CommandContext ctx, DiscordMessage prevMsg) {
    if (prevMsg != null) ctx.Channel.DeleteMessageAsync(prevMsg).Wait();

    DiscordEmbedBuilder eb = new DiscordEmbedBuilder {
      Title = "UPBot Configuration - Games"
    };
    eb.WithThumbnail(ctx.Guild.IconUrl);
    Config.ConfVal cv = GetConfigValue(ctx.Guild.Id, Config.ParamType.Games);
    eb.Description = "Configuration of the UP Bot for the Discord Server **" + ctx.Guild.Name + "**\n\n" +
      "The bot has some simple games that can be played. Games are _RPS_ (Rock,Paper, Scissors) and _bool_ that will just return randomly true or false.\n\n";
    if (cv == Config.ConfVal.NotAllowed) eb.Description += "**Games** feature is _Disabled_";
    if (cv == Config.ConfVal.OnlyAdmins) eb.Description += "**Games** feature is _Enabled_ for Admins";
    if (cv == Config.ConfVal.Everybody) eb.Description += "**Games** feature is _Enabled_ for Everybody";
    eb.WithImageUrl(ctx.Guild.BannerUrl);
    eb.WithFooter("Member that started the configuration is: " + ctx.Member.DisplayName, ctx.Member.AvatarUrl);

    List<DiscordButtonComponent> actions;
    var builder = new DiscordMessageBuilder();
    builder.AddEmbed(eb.Build());

    actions = new List<DiscordButtonComponent> {
      new DiscordButtonComponent(GetIsStyle(cv, Config.ConfVal.NotAllowed), "idfeatgames0", "Not allowed", false, GetYN(cv, Config.ConfVal.NotAllowed)),
      new DiscordButtonComponent(GetIsStyle(cv, Config.ConfVal.OnlyAdmins), "idfeatgames1", "Only Admins", false, GetYN(cv, Config.ConfVal.OnlyAdmins)),
      new DiscordButtonComponent(GetIsStyle(cv, Config.ConfVal.Everybody), "idfeatgames2", "Everybody", false, GetYN(cv, Config.ConfVal.Everybody))
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

  private DiscordMessage CreateStatsInteraction(CommandContext ctx, DiscordMessage prevMsg) {
    if (prevMsg != null) ctx.Channel.DeleteMessageAsync(prevMsg).Wait();

    DiscordEmbedBuilder eb = new DiscordEmbedBuilder {
      Title = "UPBot Configuration - Stats"
    };
    eb.WithThumbnail(ctx.Guild.IconUrl);
    Config.ConfVal cv = GetConfigValue(ctx.Guild.Id, Config.ParamType.Stats);
    eb.Description = "Configuration of the UP Bot for the Discord Server **" + ctx.Guild.Name + "**\n\n" +
      "The bot can produce some stats for the server.\n Stats can be gloabl or even checkign specific things in a channel, like used emojis, mentioned people, mentioned roles.\n\n";
    if (cv == Config.ConfVal.NotAllowed) eb.Description += "**Stats** feature is _Disabled_";
    if (cv == Config.ConfVal.OnlyAdmins) eb.Description += "**Stats** feature is _Enabled_ for Admins";
    if (cv == Config.ConfVal.Everybody) eb.Description += "**Stats** feature is _Enabled_ for Everybody";
    eb.WithImageUrl(ctx.Guild.BannerUrl);
    eb.WithFooter("Member that started the configuration is: " + ctx.Member.DisplayName, ctx.Member.AvatarUrl);

    List<DiscordButtonComponent> actions;
    var builder = new DiscordMessageBuilder();
    builder.AddEmbed(eb.Build());

    actions = new List<DiscordButtonComponent> {
      new DiscordButtonComponent(GetIsStyle(cv, Config.ConfVal.NotAllowed), "idfeatstats0", "Not allowed", false, GetYN(cv, Config.ConfVal.NotAllowed)),
      new DiscordButtonComponent(GetIsStyle(cv, Config.ConfVal.OnlyAdmins), "idfeatstats1", "Only Admins", false, GetYN(cv, Config.ConfVal.OnlyAdmins)),
      new DiscordButtonComponent(GetIsStyle(cv, Config.ConfVal.Everybody), "idfeatstats2", "Everybody", false, GetYN(cv, Config.ConfVal.Everybody))
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

  private DiscordMessage CreateUnityDocsInteraction(CommandContext ctx, DiscordMessage prevMsg) {
    if (prevMsg != null) ctx.Channel.DeleteMessageAsync(prevMsg).Wait();

    DiscordEmbedBuilder eb = new DiscordEmbedBuilder {
      Title = "UPBot Configuration - UnityDocs"
    };
    eb.WithThumbnail(ctx.Guild.IconUrl);
    Config.ConfVal cv = GetConfigValue(ctx.Guild.Id, Config.ParamType.Refactor);
    eb.Description = "Configuration of the UP Bot for the Discord Server **" + ctx.Guild.Name + "**\n\n" +
      "The **unitydocs** command allows to find the **Unity** online documentation (last LTS version.)\n" +
      "The best 3 links are proposed.\n";
    if (cv == Config.ConfVal.NotAllowed) eb.Description += "**UnityDocs** feature is _Disabled_";
    if (cv == Config.ConfVal.OnlyAdmins) eb.Description += "**UnityDocs** feature is _Enabled_ for Admins";
    if (cv == Config.ConfVal.Everybody) eb.Description += "**UnityDocs** feature is _Enabled_ for Everybody";
    eb.WithImageUrl(ctx.Guild.BannerUrl);
    eb.WithFooter("Member that started the configuration is: " + ctx.Member.DisplayName, ctx.Member.AvatarUrl);

    List<DiscordButtonComponent> actions;
    var builder = new DiscordMessageBuilder();
    builder.AddEmbed(eb.Build());

    actions = new List<DiscordButtonComponent> {
      new DiscordButtonComponent(GetIsStyle(cv, Config.ConfVal.NotAllowed), "idfeatreunitydocs0", "Not allowed", false, GetYN(cv, Config.ConfVal.NotAllowed)),
      new DiscordButtonComponent(GetIsStyle(cv, Config.ConfVal.OnlyAdmins), "idfeatreunitydocs1", "Only Admins", false, GetYN(cv, Config.ConfVal.OnlyAdmins)),
      new DiscordButtonComponent(GetIsStyle(cv, Config.ConfVal.Everybody), "idfeatreunitydocs2", "Everybody", false, GetYN(cv, Config.ConfVal.Everybody))
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

  private DiscordMessage CreateRefactorInteraction(CommandContext ctx, DiscordMessage prevMsg) {
    if (prevMsg != null) ctx.Channel.DeleteMessageAsync(prevMsg).Wait();

    DiscordEmbedBuilder eb = new DiscordEmbedBuilder {
      Title = "UPBot Configuration - Refactor"
    };
    eb.WithThumbnail(ctx.Guild.IconUrl);
    Config.ConfVal cv = GetConfigValue(ctx.Guild.Id, Config.ParamType.Refactor);
    eb.Description = "Configuration of the UP Bot for the Discord Server **" + ctx.Guild.Name + "**\n\n" +
      "The **refactor** command allows to convert some random code posted in a compact code block.\nIt works with C++, C#, Java, Javascript, and Python\n" +
      "You can reformat last posted code or you can mention the post to reformat. You can also _analyze_ the code to find the probable language.\n" +
      "And you can delete the original code post, in case was done by you. (_Admins_ can force the delete of the reformatted code)\n\n";
    if (cv == Config.ConfVal.NotAllowed) eb.Description += "**Refactor** feature is _Disabled_";
    if (cv == Config.ConfVal.OnlyAdmins) eb.Description += "**Refactor** feature is _Enabled_ for Admins";
    if (cv == Config.ConfVal.Everybody) eb.Description += "**Refactor** feature is _Enabled_ for Everybody";
    eb.WithImageUrl(ctx.Guild.BannerUrl);
    eb.WithFooter("Member that started the configuration is: " + ctx.Member.DisplayName, ctx.Member.AvatarUrl);

    List<DiscordButtonComponent> actions;
    var builder = new DiscordMessageBuilder();
    builder.AddEmbed(eb.Build());

    actions = new List<DiscordButtonComponent> {
      new DiscordButtonComponent(GetIsStyle(cv, Config.ConfVal.NotAllowed), "idfeatrefactor0", "Not allowed", false, GetYN(cv, Config.ConfVal.NotAllowed)),
      new DiscordButtonComponent(GetIsStyle(cv, Config.ConfVal.OnlyAdmins), "idfeatrefactor1", "Only Admins", false, GetYN(cv, Config.ConfVal.OnlyAdmins)),
      new DiscordButtonComponent(GetIsStyle(cv, Config.ConfVal.Everybody), "idfeatrefactor2", "Everybody", false, GetYN(cv, Config.ConfVal.Everybody))
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

  private DiscordMessage CreateTimezoneInteraction(CommandContext ctx, DiscordMessage prevMsg) {
    if (prevMsg != null) ctx.Channel.DeleteMessageAsync(prevMsg).Wait();

    DiscordEmbedBuilder eb = new DiscordEmbedBuilder {
      Title = "UPBot Configuration - Timezone"
    };
    eb.WithThumbnail(ctx.Guild.IconUrl);
    Config.ConfVal cvs = GetConfigValue(ctx.Guild.Id, Config.ParamType.TimezoneS);
    Config.ConfVal cvg = GetConfigValue(ctx.Guild.Id, Config.ParamType.TimezoneG);
    eb.Description = "Configuration of the UP Bot for the Discord Server **" + ctx.Guild.Name + "**\n\n" +
      "The **timezone** command allows to specify timezones for the users and check the local time.\n" +
      "You can use `list` to have a list to all known timezones.\n" +
      "You can mention a user to see its time zone or mention a user with a timezone to define the timezone for the users (_recommended_ only for admins)\n" +
      "You can also just specify the timezone and it will be applied to yourself\n\n";
    if (cvs == Config.ConfVal.NotAllowed) eb.Description += "**Set Timezone** is _Disabled_";
    if (cvs == Config.ConfVal.OnlyAdmins) eb.Description += "**Set Timezone** is _Enabled_ for Admins";
    if (cvs == Config.ConfVal.Everybody) eb.Description += "**Set Timezone** is _Enabled_ for Everybody";
    if (cvg == Config.ConfVal.NotAllowed) eb.Description += "**Get Timezone** is _Disabled_";
    if (cvg == Config.ConfVal.OnlyAdmins) eb.Description += "**Get Timezone** is _Enabled_ for Admins";
    if (cvg == Config.ConfVal.Everybody) eb.Description += "**Get Timezone** is _Enabled_ for Everybody";
    eb.WithImageUrl(ctx.Guild.BannerUrl);
    eb.WithFooter("Member that started the configuration is: " + ctx.Member.DisplayName, ctx.Member.AvatarUrl);

    List<DiscordButtonComponent> actions;
    var builder = new DiscordMessageBuilder();
    builder.AddEmbed(eb.Build());

    actions = new List<DiscordButtonComponent> {
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Success, "idfeattzlabs", "Set values", true),
      new DiscordButtonComponent(GetIsStyle(cvs, Config.ConfVal.NotAllowed), "idfeattzs0", "Not allowed", false, GetYN(cvs, Config.ConfVal.NotAllowed)),
      new DiscordButtonComponent(GetIsStyle(cvs, Config.ConfVal.OnlyAdmins), "idfeattzs1", "Only Admins (recommended)", false, GetYN(cvs, Config.ConfVal.OnlyAdmins)),
      new DiscordButtonComponent(GetIsStyle(cvs, Config.ConfVal.Everybody), "idfeattzs2", "Everybody", false, GetYN(cvs, Config.ConfVal.Everybody))
    };
    builder.AddComponents(actions);

    actions = new List<DiscordButtonComponent> {
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Success, "idfeattzlabg", "Read values", true),
      new DiscordButtonComponent(GetIsStyle(cvg, Config.ConfVal.NotAllowed), "idfeattzg0", "Not allowed", false, GetYN(cvg, Config.ConfVal.NotAllowed)),
      new DiscordButtonComponent(GetIsStyle(cvg, Config.ConfVal.OnlyAdmins), "idfeattzg1", "Only Admins", false, GetYN(cvg, Config.ConfVal.OnlyAdmins)),
      new DiscordButtonComponent(GetIsStyle(cvg, Config.ConfVal.Everybody), "idfeattzg2", "Everybody", false, GetYN(cvg, Config.ConfVal.Everybody))
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

  private DiscordMessage CreateSpamProtectInteraction(CommandContext ctx, DiscordMessage prevMsg) {
    if (prevMsg != null) ctx.Channel.DeleteMessageAsync(prevMsg).Wait();

    DiscordEmbedBuilder eb = new DiscordEmbedBuilder {
      Title = "UPBot Configuration - Spam Protection"
    };
    eb.WithThumbnail(ctx.Guild.IconUrl);
    Config c = GetConfig(ctx.Guild.Id, Config.ParamType.SpamProtection);
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
    WhatToTrack wtt = WhatToTracks[ctx.Guild.Id];
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
      foreach (var e in RepEmojis[ctx.Guild.Id].Values)
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
      foreach (var e in RepEmojis[ctx.Guild.Id].Values)
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

  private DiscordMessage CreateBannedWordsInteraction(CommandContext ctx, DiscordMessage prevMsg) {
    if (prevMsg != null) ctx.Channel.DeleteMessageAsync(prevMsg).Wait();

    DiscordEmbedBuilder eb = new DiscordEmbedBuilder {
      Title = "UPBot Configuration - Banned Words"
    };
    eb.WithThumbnail(ctx.Guild.IconUrl);
    Config.ConfVal cv = GetConfigValue(ctx.Guild.Id, Config.ParamType.BannedWords);
    eb.Description = "Configuration of the UP Bot for the Discord Server **" + ctx.Guild.Name + "**\n\n" +
      "The bot can automatically remove messages containing banned words.\n\n";

    if (BannedWords[ctx.Guild.Id].Count == 0) {
      eb.Description += "No banned words defined.\n\n";
    } else {
      eb.Description += "Banned words: ";
      foreach (var w in BannedWords[ctx.Guild.Id])
        eb.Description += w + ", ";
      eb.Description = eb.Description[0..^2] + "\n\n";
    }
    if (cv == Config.ConfVal.NotAllowed) eb.Description += "**Banned Words** feature is _Disabled_";
    else eb.Description += "**Banned Words** feature is _Enabled_";
    eb.WithImageUrl(ctx.Guild.BannerUrl);
    eb.WithFooter("Member that started the configuration is: " + ctx.Member.DisplayName, ctx.Member.AvatarUrl);

    List<DiscordButtonComponent> actions;
    var builder = new DiscordMessageBuilder();
    builder.AddEmbed(eb.Build());

    actions = new List<DiscordButtonComponent> {
      cv == Config.ConfVal.NotAllowed ?
        new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idfeatbannedwed", "Enable", false, ey) :
        new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "idfeatbannedwed", "Disable", false, en),
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "idfeatbannedwadd", "Add", false, er)
    };

    // Goups of 5, but we start at 1 for the global enable/disable
    int num = 1;
    int count = 0;
    foreach (var w in BannedWords[ctx.Guild.Id]) {
      actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Danger, "idfeatbannedwr" + count, w, false, en));
      num++;
      count++;
      if (num == 5) {
        builder.AddComponents(actions);
        actions = new List<DiscordButtonComponent>();
      }
    }
    if (actions.Count > 0) builder.AddComponents(actions);

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

  private DiscordMessage CreateTagsInteraction(CommandContext ctx, DiscordMessage prevMsg) {
    if (prevMsg != null) ctx.Channel.DeleteMessageAsync(prevMsg).Wait();

    DiscordEmbedBuilder eb = new DiscordEmbedBuilder {
      Title = "UPBot Configuration - Tags"
    };
    eb.WithThumbnail(ctx.Guild.IconUrl);
    Config.ConfVal cvs = GetConfigValue(ctx.Guild.Id, Config.ParamType.TagsDefine);
    Config.ConfVal cvg = GetConfigValue(ctx.Guild.Id, Config.ParamType.TagsUse);
    eb.Description = "Configuration of the UP Bot for the Discord Server **" + ctx.Guild.Name + "**\n\n" +
      "The **tag** command allows to define some contant and post it quickly with a keyword.\n" +
      "You can use `list` to have a list to all known tags.\n" +
      "You can use `add`, `remove`, and `edit` to alter the list of tags.\n\n";
    if (cvs == Config.ConfVal.NotAllowed) eb.Description += "**Set Tags** is _Disabled_";
    if (cvs == Config.ConfVal.OnlyAdmins) eb.Description += "**Set Tags** is _Enabled_ for Admins (_recommended_)";
    if (cvs == Config.ConfVal.Everybody) eb.Description += "**Set Tags** is _Enabled_ for Everybody";
    if (cvg == Config.ConfVal.NotAllowed) eb.Description += "**Use Tags** is _Disabled_";
    if (cvg == Config.ConfVal.OnlyAdmins) eb.Description += "**Use Tags** is _Enabled_ for Admins";
    if (cvg == Config.ConfVal.Everybody) eb.Description += "**Use Tags** is _Enabled_ for Everybody";
    eb.WithImageUrl(ctx.Guild.BannerUrl);
    eb.WithFooter("Member that started the configuration is: " + ctx.Member.DisplayName, ctx.Member.AvatarUrl);

    List<DiscordButtonComponent> actions;
    var builder = new DiscordMessageBuilder();
    builder.AddEmbed(eb.Build());

    actions = new List<DiscordButtonComponent> {
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Success, "idfeattagslabs", "Set tags", true),
      new DiscordButtonComponent(GetIsStyle(cvs, Config.ConfVal.NotAllowed), "idfeattagss0", "Not allowed", false, GetYN(cvs, Config.ConfVal.NotAllowed)),
      new DiscordButtonComponent(GetIsStyle(cvs, Config.ConfVal.OnlyAdmins), "idfeattagss1", "Only Admins (recommended)", false, GetYN(cvs, Config.ConfVal.OnlyAdmins)),
      new DiscordButtonComponent(GetIsStyle(cvs, Config.ConfVal.Everybody), "idfeattagss2", "Everybody", false, GetYN(cvs, Config.ConfVal.Everybody))
    };
    builder.AddComponents(actions);

    actions = new List<DiscordButtonComponent> {
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Success, "idfeattagslabg", "Use tags", true),
      new DiscordButtonComponent(GetIsStyle(cvg, Config.ConfVal.NotAllowed), "idfeattagsg0", "Not allowed", false, GetYN(cvg, Config.ConfVal.NotAllowed)),
      new DiscordButtonComponent(GetIsStyle(cvg, Config.ConfVal.OnlyAdmins), "idfeattagsg1", "Only Admins", false, GetYN(cvg, Config.ConfVal.OnlyAdmins)),
      new DiscordButtonComponent(GetIsStyle(cvg, Config.ConfVal.Everybody), "idfeattagsg2", "Everybody", false, GetYN(cvg, Config.ConfVal.Everybody))
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
    Config.ConfVal cv = GetConfigValue(ctx.Guild.Id, Config.ParamType.Emoji4Role);
    eb.Description = "Configuration of the UP Bot for the Discord Server **" + ctx.Guild.Name + "**\n\n" +
      "The bot allows to track emojis on specific messages to grant and remove roles.\n\n";
    if (cv == Config.ConfVal.NotAllowed) eb.Description += "**Emoji for roles** are _Disabled_";
    else eb.Description += "**Emoji for roles** are _Enabled_";
    eb.WithImageUrl(ctx.Guild.BannerUrl);
    eb.WithFooter("Member that started the configuration is: " + ctx.Member.DisplayName, ctx.Member.AvatarUrl);

    List<DiscordButtonComponent> actions = new List<DiscordButtonComponent>();
    var builder = new DiscordMessageBuilder();
    builder.AddEmbed(eb.Build());

    // List existing (role name, emoji, for channel (part of name))
    // Add one (add emoji to a channel to pick the channel and then type the role)


    actions = new List<DiscordButtonComponent> {
      cv == Config.ConfVal.NotAllowed ?
        new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "idfeatem4rendis", "Enable", false, ey) :
        new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "idfeatem4rendis", "Disable", false, en),
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "idfeatem4radd", "Add new", false, ey)
    };
    builder.AddComponents(actions);


    int num = 0;
    int pos = 0;
    actions = new List<DiscordButtonComponent>();
    foreach (var em4r in Em4Roles[ctx.Guild.Id]) {
      DiscordRole r = ctx.Guild.GetRole(em4r.Role);
      DiscordChannel c = ctx.Guild.GetChannel(em4r.Channel);
      DiscordMessage m = null;
      try {
        m = c?.GetMessageAsync(em4r.Message).Result; // This may fail
      } catch (Exception) {}
      string name = "";
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
    eb.Description = "Role is selected to **" + TempRoleSelected[ctx.Guild.Id].role.Name + "**\n\nAdd the emoji you want for this role to the message you want to monitor.\n\n";

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
    Config.ConfVal cv = GetConfigValue(ctx.Guild.Id, Config.ParamType.Emoji4Role);
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






  private static Config GetConfig(ulong gid, Config.ParamType t) {
    List<Config> cs = Configs[gid];
    foreach (var c in cs) {
      if (c.IsParam(t)) return c;
    }
    return null;
  }
  private static Config.ConfVal GetConfigValue(ulong gid, Config.ParamType t) {
    List<Config> cs = Configs[gid];
    foreach (var c in cs) {
      if (c.IsParam(t)) return (Config.ConfVal)c.IdVal;
    }
    return Config.ConfVal.NotAllowed;
  }
  private void SetConfigValue(ulong gid, Config.ParamType t, Config.ConfVal v) {
    List<Config> cs = Configs[gid];
    Config tc = null;
    foreach (var c in cs) {
      if (c.IsParam(t)) {
        tc = c;
        break;
      }
    }
    if (tc == null) {
      tc = new Config(gid, t, (ulong)v);
      Configs[gid].Add(tc);
    } else {
      Database.Delete(tc);
      tc.SetVal(v); 
    }
    Database.Add(tc);
  }

  private DiscordComponentEmoji GetYN(Config.ConfVal cv) {
    if (cv == Config.ConfVal.NotAllowed) return en;
    return ey;
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
