using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

/// <summary>
/// This command implements simple games like:
/// Rock-Paper-Scissors
/// author: SlicEnDicE
/// </summary>
public class GameModule : BaseCommandModule
{
    [Command("game")]
    public async Task GameCommand(CommandContext ctx)
    {
        await ctx.RespondAsync("Available commands: bool");
    }

    [Command("bool")]
    public async Task BoolCommand(CommandContext ctx)
    {
        await PlayBool(ctx);
    }

    [Command("rps")]
    public async Task RPSCommand(CommandContext ctx, string kind)
    {
        await PlayRockPaperScissors(ctx, kind);
    }


    readonly Random random = new Random();

    Task PlayBool(CommandContext ctx)
    {
        int value = random.Next(0, 2);

        switch(value)
        {
            case 1:
                return ctx.RespondAsync("true");
            default:
                return ctx.RespondAsync("false");
        }
        
    }

    enum RPSTypes : ushort
    {
        Rock = 0,
        Paper = 1,
        Scissors = 2
    }

    Task PlayRockPaperScissors(CommandContext ctx, string kind)
    {
        RPSTypes playerChoice;

        switch(kind)
        {
            case "rock":
                playerChoice = RPSTypes.Rock;
                break;
            case "paper":
                playerChoice = RPSTypes.Paper;
                break;
            case "scissors":
                playerChoice = RPSTypes.Scissors;
                break;
            default:
                return ctx.RespondAsync("I don't know what @@@ is, please try again...".Replace("@@@", kind));
        }

        RPSTypes botChoice = (RPSTypes)random.Next(0, 3);

        if(playerChoice == botChoice)
        {
            return ctx.RespondAsync("@@@\nDRAW!".Replace("@@@", botChoice.ToString()));
        }
        if(playerChoice == RPSTypes.Rock && botChoice == RPSTypes.Scissors)
        {
            return ctx.RespondAsync("@@@\nYou win!".Replace("@@@", botChoice.ToString()));
        }
        if(playerChoice == RPSTypes.Paper && botChoice == RPSTypes.Rock)
        {
            return ctx.RespondAsync("@@@\nYou win!".Replace("@@@", botChoice.ToString()));
        }
        if (playerChoice == RPSTypes.Scissors && botChoice == RPSTypes.Paper)
        {
            return ctx.RespondAsync("@@@\nYou win!".Replace("@@@", botChoice.ToString()));
        }

        return ctx.RespondAsync("@@@\nYou lose!".Replace("@@@", botChoice.ToString()));
    }


}


