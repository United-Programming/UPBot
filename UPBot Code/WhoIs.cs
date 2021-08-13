using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Threading.Tasks;
/// <summary>
/// This command implements a basic ping command.
/// It is mostly for debug reasons.
/// author: CPU
/// </summary>
public class WhoIs : BaseCommandModule {

  [Command("wz")]
  public async Task WhoIsCommand(CommandContext ctx) {
    await GenerateWhoIs(ctx, null);
  }

  [Command("wz")]
  public async Task WhoIsCommand(CommandContext ctx, DiscordMember member) {
    await GenerateWhoIs(ctx, member);
  }

  private Task GenerateWhoIs(CommandContext ctx, DiscordMember m) {
    if (m == null) {
      m = ctx.Member;
    }
    bool you = m == ctx.Member;

    DiscordEmbedBuilder b = new DiscordEmbedBuilder();
    b.Title = "Who Is user " + m.DisplayName + "#" + m.Discriminator;
    b.WithThumbnail(m.AvatarUrl, 64, 64);

    b.WithColor(m.Color);
    DateTimeOffset jdate = m.JoinedAt.UtcDateTime;
    string joined = jdate.Year + "/" + jdate.Month + "/" + jdate.Day;
    DateTimeOffset cdate = m.CreationTimestamp.UtcDateTime;
    string creation = cdate.Year + "/" + cdate.Month + "/" + cdate.Day;

    b.WithDescription(m.Username + " joined on " + joined + " (has an account from " + creation + ")");

    b.AddField("Is you",      you        ? "✓" : "❌", true);
    b.AddField("Is a bot",    m.IsBot    ? "🤖" : "❌", true);
    b.AddField("Is the boss", m.IsOwner  ? "👑" : "❌", true);
    b.AddField("Is Muted", m.IsMuted            ? "✓" : "❌", true);
    b.AddField("Is Deafened", m.IsDeafened      ? "✓" : "❌", true);

    if (m.Locale != null) b.AddField("Speaks", m.Locale, true);
    if (m.Nickname != null) b.AddField("Is called", m.Nickname, true);
    b.AddField("Avatar Hex Color", m.Color.ToString(), true);

    if (m.PremiumSince != null) {
      DateTimeOffset bdate = ((DateTimeOffset)m.PremiumSince).UtcDateTime;
      string booster = bdate.Year + "/" + bdate.Month + "/" + bdate.Day;
      b.AddField("Booster", "Form " + booster, true);
    }
    if (m.Flags != null) b.AddField("Flags", m.Flags.ToString(), true);

    string roles = "";
    int num = 0;
    foreach (DiscordRole role in m.Roles) {
      roles += role.Mention + " ";
      num++;
    }
    if (num == 1)
      b.AddField("Role", roles, false);
    else
      b.AddField(num + " Roles", roles, false);


    //    m.Permissions;
    //    m.Presence;
    //    m.VoiceState;
    

    return ctx.RespondAsync(b.Build());
  }
}