﻿using DSharpPlus.SlashCommands;
using System.Threading.Tasks;
using UPBot.UPBot_Code;

/// <summary>
/// This command implements a Version command.
/// Just to check the version of the bot
/// author: CPU
/// </summary>
/// 

public class SlashVersion : ApplicationCommandModule
{

    [SlashCommand("version", "Get my version information")]
    public static async Task VInfoCommand(InteractionContext ctx)
    {
        string authors = "**CPU**, **J0nathan**, **Eremiell**, **Duck**, **SlicEnDicE**, **Apoorv**, **Revolution**";

        await ctx.CreateResponseAsync(Utils.BuildEmbed("United Programming Bot",
          $"**Version**: {Utils.GetVersion()}\n\nContributors: {authors}\n\nCode available on https://github.com/United-Programming/UPBot/\n\nJoin United Programming discord: https://discord.gg/unitedprogramming",
          Utils.Yellow).Build());
    }

}