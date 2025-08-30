using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System;
using System.Threading.Tasks;
using UPBot.UPBot_Code;

/// <summary>
/// This command implements a WhoIs command.
/// It gives info about a Discord User or yourself
/// author: CPU
/// </summary>
/// 

public class SlashWhoIs : ApplicationCommandModule
{

    [SlashCommand("whois", "Get information about a specific user (or yourself)")]
    public static async Task WhoIsCommand(InteractionContext ctx, [Option("user", "The user to get info from")] DiscordUser user = null)
    {
        Utils.LogUserCommand(ctx);

        try
        {
            DiscordMember m;

            m = user == null ? ctx.Member : ctx.Guild.GetMemberAsync(user.Id).Result; // If we do not have a user we use the member that invoked the command
            bool you = m == ctx.Member;

            DateTimeOffset jdate = m.JoinedAt.UtcDateTime;
            string joined = jdate.Year + "/" + jdate.Month + "/" + jdate.Day;
            DateTimeOffset cdate = m.CreationTimestamp.UtcDateTime;
            string creation = cdate.Year + "/" + cdate.Month + "/" + cdate.Day;

            int daysJ = (int)(DateTime.Now - m.JoinedAt.DateTime).TotalDays;
            int daysA = (int)(DateTime.Now - m.CreationTimestamp.DateTime).TotalDays;
            double years = daysA / 365.25;

            string title = "Who is the user " + m.DisplayName + "#" + m.Discriminator;
            string description = m.Username + " joined on " + joined + " (" + daysJ + " days)\n Account created on " +
                                 creation + " (" + daysA + " days, " + years.ToString("N1") + " years)";
            var embed = Utils.BuildEmbed(title, description, m.Color);
            embed.WithThumbnail(m.AvatarUrl, 64, 64);

            embed.AddField("Is you", you ? "✓" : "❌", true);
            embed.AddField("Is a bot", m.IsBot ? "🤖" : "❌", true);
            embed.AddField("Is the boss", m.IsOwner ? "👑" : "❌", true);
            embed.AddField("Is Muted", m.IsMuted ? "✓" : "❌", true);
            embed.AddField("Is Deafened", m.IsDeafened ? "✓" : "❌", true);

            if (m.Locale != null) embed.AddField("Speaks", m.Locale, true);
            if (m.Nickname != null) embed.AddField("Is called", m.Nickname, true);
            embed.AddField("Avatar Hex Color", m.Color.ToString(), true);

            if (m.PremiumSince != null)
            {
                DateTimeOffset bdate = ((DateTimeOffset)m.PremiumSince).UtcDateTime;
                string booster = bdate.Year + "/" + bdate.Month + "/" + bdate.Day;
                embed.AddField("Booster", "From " + booster, true);
            }
            if (m.Flags != null) embed.AddField("Flags", m.Flags.ToString(), true); // Only the default flags will be shown. This bot will not be very diffused so probably we do not need specific checks for flags

            string roles = "";
            int num = 0;
            foreach (DiscordRole role in m.Roles)
            {
                roles += role.Mention + " ";
                num++;
            }
            if (num == 1)
                embed.AddField("Role", roles);
            else if (num != 0)
                embed.AddField(num + " Roles", roles);

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
            if (m.Permissions.HasFlag(DSharpPlus.Permissions.UseApplicationCommands)) perms += ", Use Bot";
            if (m.Permissions.HasFlag(DSharpPlus.Permissions.CreatePublicThreads)) perms += ", Use Threads";
            if (perms.Length > 0) embed.AddField("Permissions", perms[2..]);

            await ctx.CreateResponseAsync(embed.Build());
        }
        catch (Exception ex)
        {
            await ctx.CreateResponseAsync(Utils.GenerateErrorAnswer(ctx.Guild.Name, "WhoIs", ex));
        }
    }
}