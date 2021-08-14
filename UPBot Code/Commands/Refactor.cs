using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System.Threading.Tasks;
/// <summary>
/// Command used to refactor as codeblock some code pasted by a user
/// author: CPU
/// </summary>
public class Refactor : BaseCommandModule {

  [Command("refactor")]
  public async Task WhoIsCommand(CommandContext ctx) { // Refactors the previous post, if it is code
    await RefactorCode(ctx, null);
  }

  [Command("refactor")]
  public async Task WhoIsCommand(CommandContext ctx, DiscordMember member) { // Refactor the last post of the specified user in the channel
    await RefactorCode(ctx, member);
  }

  private Task RefactorCode(CommandContext ctx, DiscordMember m) {
    return ctx.RespondAsync("work in progress...");
  }
}