using System.Threading.Tasks;
using DSharpPlus.SlashCommands;

/// <summary>
/// This command implements a Version command.
/// Just to check the version of the bot
/// author: CPU
/// </summary>
public class SlashVersion : ApplicationCommandModule {


  [SlashCommand("version", "Get version information about the bot")]
  public async Task VersionCommand(InteractionContext ctx, [Option("what", "What type of stats to show")] StatsTypes? what) {
    string authors = "**CPU**, **Duck**, **Eremiell**, **SlicEnDicE**, **J0nathan**, **Revolution**";

    await ctx.CreateResponseAsync(Utils.BuildEmbed("United Programming Bot", "**Version**: " + Utils.GetVersion() + "\n\nContributors: " +
      authors + "\n\nCode available on https://github.com/United-Programming/UPBot/", Utils.Yellow).Build());
  }
}