using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

/// <summary>
/// This command implements a basic ping command.
/// It is mostly for debug reasons.
/// author: CPU
/// </summary>
public class GameModule : BaseCommandModule
{
    [Command("game")]
    public async Task GameCommand(CommandContext ctx)
    {
        await ctx.RespondAsync("Available commands: bool");
    }


}


