using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System.Threading.Tasks;

public class SlashPing : ApplicationCommandModule {
  [SlashCommand("testping", "A slash command test for new bot version.")]
  public async Task PingCommand(InteractionContext ctx) {
    DiscordInteractionResponseBuilder b = new DiscordInteractionResponseBuilder();
    b.WithContent("Pong test");
    await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, b);
  }
}
