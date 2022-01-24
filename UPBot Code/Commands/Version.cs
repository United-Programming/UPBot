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
public class Version : BaseCommandModule {

  [Command("Version")]
  [Description("Get version information about the bot.")]
  public async Task VersionCommand(CommandContext ctx) {
    string authors = "<@!231753250687287296>, <@!411526180873961494>, <@!340661564556312591>, <@!486133356858310656>, <@!608994148313333763>";
    if (ctx.Guild != null && ctx.Guild.Id == 830900174553481236ul) {
      DiscordMember cpu = ctx.Guild.GetMemberAsync(231753250687287296ul).Result;
      DiscordMember duck = ctx.Guild.GetMemberAsync(411526180873961494ul).Result;
      DiscordMember eremiell = ctx.Guild.GetMemberAsync(340661564556312591ul).Result;
      DiscordMember slice = ctx.Guild.GetMemberAsync(486133356858310656ul).Result;
      DiscordMember jonathan = ctx.Guild.GetMemberAsync(608994148313333763ul).Result;
      authors = cpu.Mention + ", " + duck.Mention + ", " + eremiell.Mention + ", " + slice.Mention + ", " + jonathan.Mention;
    }

    await ctx.Message.RespondAsync(Utils.BuildEmbed("United Programming Bot",
      "**Version**: " + Utils.GetVersion() + "\n\nContributors: " +
      authors +
      "\n\nCode available on https://github.com/United-Programming/UPBot/", Utils.Yellow).Build());
  }
}