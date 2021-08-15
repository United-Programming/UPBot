using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Threading.Tasks;
/// <summary>
/// This command implements a WhoIs command.
/// It gives info about a Discord User or yourself
/// author: CPU
/// </summary>
public class WhoIs : BaseCommandModule {

  [Command("whois")]
  public async Task WhoIsCommand(CommandContext ctx) { // Basic version without parameters
    await GenerateWhoIs(ctx, null);
  }

  [Command("whoami")]
  public async Task WhoAmICommand(CommandContext ctx) { // Alternate version without parameters
    await GenerateWhoIs(ctx, null);
  }

  [Command("whois")]
  public async Task WhoIsCommand(CommandContext ctx, DiscordMember member) { // Standard version with a user
    await GenerateWhoIs(ctx, member);
  }

  private Task GenerateWhoIs(CommandContext ctx, DiscordMember m) {
    UtilityFunctions.LogUserCommand(ctx);
    if (m == null) { // If we do not have a user we use the member that invoked the command
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

    int daysJ = (int)(DateTime.Now - m.JoinedAt.DateTime).TotalDays;
    int daysA = (int)(DateTime.Now - m.CreationTimestamp.DateTime).TotalDays;
    double years = daysA / 365.25;
    b.WithDescription(m.Username + " joined on " + joined + " (" + daysJ + " days)\n Account created on " + creation + " (" + daysA + " days, " + years.ToString("N1") + " years)");

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
    if (m.Flags != null) b.AddField("Flags", m.Flags.ToString(), true); // Only the default flags will be shown. This bot will not be very diffused so probably we do not need specific checks for flags

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

    string perms = ""; // Not all permissions are shown
    if (m.Permissions.HasFlag(DSharpPlus.Permissions.CreateInstantInvite)) perms += ", Invite";
    if (m.Permissions.HasFlag(DSharpPlus.Permissions.KickMembers)) perms += ", Kick";
    if (m.Permissions.HasFlag(DSharpPlus.Permissions.BanMembers)) perms += ", Ban";
    if (m.Permissions.HasFlag(DSharpPlus.Permissions.Administrator)) perms += ", Admin";
    if (m.Permissions.HasFlag(DSharpPlus.Permissions.ManageChannels)) perms += ", Manage Channels";
    if (m.Permissions.HasFlag(DSharpPlus.Permissions.ManageGuild)) perms += ", Manage Server";
    if (m.Permissions.HasFlag(DSharpPlus.Permissions.AddReactions)) perms += ", Reactions";
    if (m.Permissions.HasFlag(DSharpPlus.Permissions.ViewAuditLog)) perms += ", Audit";
    if (m.Permissions.HasFlag(DSharpPlus.Permissions.ManageMessages)) perms += ", Manage Messages";
    if (m.Permissions.HasFlag(DSharpPlus.Permissions.EmbedLinks)) perms += ", Links";
    if (m.Permissions.HasFlag(DSharpPlus.Permissions.AttachFiles)) perms += ", Files";
    if (m.Permissions.HasFlag(DSharpPlus.Permissions.UseExternalEmojis)) perms += ", Ext Emojis";
    if (m.Permissions.HasFlag(DSharpPlus.Permissions.Speak)) perms += ", Speak";
    if (m.Permissions.HasFlag(DSharpPlus.Permissions.ManageRoles)) perms += ", Manage Roles";
    if (m.Permissions.HasFlag(DSharpPlus.Permissions.ManageEmojis)) perms += ", Manage Emojis";
    if (m.Permissions.HasFlag(DSharpPlus.Permissions.UseSlashCommands)) perms += ", Use Bot";
    if (m.Permissions.HasFlag(DSharpPlus.Permissions.UsePublicThreads)) perms += ", Use Threads";
    if (perms.Length > 0) b.AddField("Permissions", perms.Substring(2), false);

    return ctx.RespondAsync(b.Build());
  }
}