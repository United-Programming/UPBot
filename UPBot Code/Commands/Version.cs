using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Threading.Tasks;
/// <summary>
/// This command implements a Version command.
/// Just to check the version of the bot
/// author: CPU
/// </summary>
public class Version : BaseCommandModule {

  [Command("Version")]
  [Description("Get version information about the bot.")]
  public async Task VersionCommand(CommandContext ctx) {
    string authors = "**CPU**, **Duck**, **Eremiell**, **SlicEnDicE**, **J0nathan**";

    await ctx.Message.RespondAsync(Utils.BuildEmbed("United Programming Bot",
      "**Version**: " + Utils.GetVersion() + "\n\nContributors: " +
      authors +
      "\n\nCode available on https://github.com/United-Programming/UPBot/", Utils.Yellow).Build());
  }
}