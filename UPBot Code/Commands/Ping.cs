using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

/// <summary>
/// This command implements a basic ping command.
/// It is mostly for debug reasons.
/// author: CPU
/// </summary>
public class PingModule : BaseCommandModule {
  [Command("ping")]
  public async Task GreetCommand(CommandContext ctx) {
    await GeneratePong(ctx);
  }
  [Command("upbot")]
  public async Task GreetCommand2(CommandContext ctx) {
    await GeneratePong(ctx);
  }

  Task GeneratePong(CommandContext ctx) {
    return ctx.RespondAsync("I am alive!");
  }
}
