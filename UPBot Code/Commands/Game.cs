using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;


/// <summary>
/// This command implements simple games like:
/// Rock-Paper-Scissors
/// author: SlicEnDicE
/// </summary>

[SlashCommandGroup("game", "Commands to play games with the bot")]
public class SlashGame : ApplicationCommandModule {
  readonly Random random = new Random();

  [SlashCommand("rockpaperscissors", "Play Rock, Paper, Scissors")]
  public async Task RPSCommand(InteractionContext ctx, [Option("yourmove", "Rock, Paper, or Scissors")] RPSTypes? yourmove = null) {
    if (!Setup.Permitted(ctx.Guild, Config.ParamType.Games, ctx)) { Utils.DefaultNotAllowed(ctx); return; }
    Utils.LogUserCommand(ctx);


    RPSTypes botChoice = (RPSTypes)random.Next(0, 3);
    if (yourmove != null) {
      if (yourmove == RPSTypes.Rock) {
        if (botChoice == RPSTypes.Rock) {
          await ctx.CreateResponseAsync($"You said 🪨 Rock {ctx.Member.Mention}, I played 🪨 Rock! **DRAW!**");
        }
        else if (botChoice == RPSTypes.Paper) {
          await ctx.CreateResponseAsync($"You said 🪨 Rock {ctx.Member.Mention}, I played 📄 Paper! **I win!**");
        }
        else {
          await ctx.CreateResponseAsync($"You said 🪨 Rock {ctx.Member.Mention}, I played ✂️ Scissor! **You win!**");
        }
      }
      else if (yourmove == RPSTypes.Paper) {
        if (botChoice == RPSTypes.Rock) {
          await ctx.CreateResponseAsync($"You said 📄 Paper {ctx.Member.Mention}, I played 🪨 Rock! **You win!**");
        }
        else if (botChoice == RPSTypes.Paper) {
          await ctx.CreateResponseAsync($"You said 📄 Paper {ctx.Member.Mention}, I played 📄 Paper! **DRAW!**");
        }
        else {
          await ctx.CreateResponseAsync($"You said 📄 Paper {ctx.Member.Mention}, I played ✂️ Scissor! **I win!**");
        }
      }
      else {
        if (botChoice == RPSTypes.Rock) {
          await ctx.CreateResponseAsync($"You said ✂️ Scissor {ctx.Member.Mention}, I played 🪨 Rock! **I win!**");
        }
        else if (botChoice == RPSTypes.Paper) {
          await ctx.CreateResponseAsync($"You said ✂️ Scissor {ctx.Member.Mention}, I played 📄 Paper! **You win!**");
        }
        else {
          await ctx.CreateResponseAsync($"You said ✂️ Scissor {ctx.Member.Mention}, I played ✂️ Scissor! **DRAW!**");
        }
      }
      return;
    }

    await ctx.CreateResponseAsync("Pick your move");

    var builder = new DiscordMessageBuilder().WithContent("Select 🪨, 📄, or ✂️");
    List<DiscordButtonComponent> actions = new List<DiscordButtonComponent> {
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "bRock", "🪨 Rock"),
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "bPaper", "📄 Paper"),
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "bScissors", "✂️ Scissors")
    };
    builder.AddComponents(actions);

    DiscordMessage msg = builder.SendAsync(ctx.Channel).Result;
    var interact = ctx.Client.GetInteractivity();
    var result = await interact.WaitForButtonAsync(msg, TimeSpan.FromMinutes(2));
    var interRes = result.Result;
    if (interRes != null) {
      if (result.Result.Id == "bRock") {
        if (botChoice == RPSTypes.Rock) {
          await ctx.Channel.SendMessageAsync($"You said 🪨 Rock {ctx.Member.Mention}, I played 🪨 Rock! **DRAW!**");
        }
        else if (botChoice == RPSTypes.Paper) {
          await ctx.Channel.SendMessageAsync($"You said 🪨 Rock {ctx.Member.Mention}, I played 📄 Paper! **I win!**");
        }
        else {
          await ctx.Channel.SendMessageAsync($"You said 🪨 Rock {ctx.Member.Mention}, I played ✂️ Scissor! **You win!**");
        }
      }
      else if (result.Result.Id == "bPaper") {
        if (botChoice == RPSTypes.Rock) {
          await ctx.Channel.SendMessageAsync($"You said 📄 Paper {ctx.Member.Mention}, I played 🪨 Rock! **You win!**");
        }
        else if (botChoice == RPSTypes.Paper) {
          await ctx.Channel.SendMessageAsync($"You said 📄 Paper {ctx.Member.Mention}, I played 📄 Paper! **DRAW!**");
        }
        else {
          await ctx.Channel.SendMessageAsync($"You said 📄 Paper {ctx.Member.Mention}, I played ✂️ Scissor! **I win!**");
        }
      }
      else if (result.Result.Id == "bScissors") {
        await ctx.Channel.SendMessageAsync($"You said ✂️ Scissor {ctx.Member.Mention}, I played 🪨 Rock! **I win!**");
      }
      else if (botChoice == RPSTypes.Paper) {
        await ctx.Channel.SendMessageAsync($"You said ✂️ Scissor {ctx.Member.Mention}, I played 📄 Paper! **You win!**");
      }
      else {
        await ctx.Channel.SendMessageAsync($"You said ✂️ Scissor {ctx.Member.Mention}, I played ✂️ Scissor! **DRAW!**");
      }
    }
    await ctx.Channel.DeleteMessageAsync(msg);
  }

  public enum RPSTypes { // 🪨📄
    [ChoiceName("Rock")] Rock = 0,
    [ChoiceName("Paper")] Paper = 1,
    [ChoiceName("Scissors")] Scissors = 2
  }
  public enum RPSLSTypes { // 🪨📄✂️🦎🖖
    [ChoiceName("🪨 Rock")] Rock = 0,
    [ChoiceName("📄 Paper")] Paper = 1,
    [ChoiceName("✂️ Scissors")] Scissors = 2,
    [ChoiceName("🦎 Lizard")] Lizard = 3,
    [ChoiceName("🖖 Spock")] Spock = 4
  }
  enum RPSRes { First, Second, Draw }
  readonly RPSRes[][] rpslsRes = {
    //                                  Rock          Paper         Scissors         Lizard         Spock 
    /* Rock     */ new RPSRes[] {RPSRes.Draw,   RPSRes.Second,  RPSRes.First,  RPSRes.First,  RPSRes.Second },
    /* Paper    */ new RPSRes[] {RPSRes.First,  RPSRes.Draw,    RPSRes.Second, RPSRes.Second, RPSRes.First  },
    /* Scissors */ new RPSRes[] {RPSRes.Second, RPSRes.First,   RPSRes.Draw,   RPSRes.First,  RPSRes.Second },
    /* Lizard   */ new RPSRes[] {RPSRes.Second, RPSRes.First,   RPSRes.Second, RPSRes.Draw,   RPSRes.First  },
    /* Spock    */ new RPSRes[] {RPSRes.First,  RPSRes.Second,  RPSRes.First,  RPSRes.Second, RPSRes.Draw   }
  };
  readonly string[][] rpslsMsgs = {
    //                            Rock                    Paper                     Scissors                        Lizard                          Spock 
    /* Rock     */ new string[] {"Draw",                  "Paper covers Rock",      "rock crushes scissors",        "Rock crushes Lizard",          "Spock vaporizes Rock"},
    /* Paper    */ new string[] {"Paper covers Rock",     "Draw",                   "Scissors cuts Paper",          "Lizard eats Paper",            "Paper disproves Spock" },
    /* Scissors */ new string[] {"Rock crushes scissors", "Scissors cuts Paper",    "Draw",                         "Scissors decapitates Lizard",  "Spock smashes Scissors" },
    /* Lizard   */ new string[] {"Rock crushes Lizard",   "Lizard eats Paper",      "Scissors decapitates Lizard",  "Draw",                         "Lizard poisons Spock"  },
    /* Spock    */ new string[] {"Spock vaporizes Rock",  "Paper disproves Spock",  "Spock smashes Scissors",       "Lizard poisons Spock",         "Draw" }
  };

  private string GetChoice(RPSLSTypes? move) {
    switch (move) {
      case RPSLSTypes.Rock: return "🪨 Rock";
      case RPSLSTypes.Paper: return "📄 Paper";
      case RPSLSTypes.Scissors: return "✂️ Scissors";
      case RPSLSTypes.Lizard: return "🦎 Lizard";
      case RPSLSTypes.Spock: return "🖖 Spock";
    }
    return "?";
  }


  [SlashCommand("rockpaperscissorslizardspock", "Play Rock, Paper, Scissors, Lizard, Spock")]
  public async Task RPSLKCommand(InteractionContext ctx, [Option("yourmove", "Rock, Paper, or Scissors")] RPSLSTypes? yourmove = null) {
    //[Option("yourmove", "Rock, Paper, or Scissors")] string yourmove = null) {
    if (!Setup.Permitted(ctx.Guild, Config.ParamType.Games, ctx)) { Utils.DefaultNotAllowed(ctx); return; }
    Utils.LogUserCommand(ctx);

    RPSLSTypes botChoice = (RPSLSTypes)random.Next(0, 5);
    if (yourmove != null) {
      string resmsg = rpslsMsgs[(int)yourmove][(int)botChoice];
      switch (rpslsRes[(int)yourmove][(int)botChoice]) {
        case RPSRes.First:
          await ctx.CreateResponseAsync($"You said {GetChoice(yourmove)} {ctx.Member.Mention}, I played {GetChoice(botChoice)}! {resmsg} **You win!**");
          break;
        case RPSRes.Second:
          await ctx.CreateResponseAsync($"You said {GetChoice(yourmove)} {ctx.Member.Mention}, I played {GetChoice(botChoice)}! {resmsg} **I win!**");
          break;
        case RPSRes.Draw:
          await ctx.CreateResponseAsync($"You said {GetChoice(yourmove)} {ctx.Member.Mention}, I played {GetChoice(botChoice)}! **DRAW!**");
          break;
      }
      return;
    }

    await ctx.CreateResponseAsync("Pick your move");


    var builder = new DiscordMessageBuilder().WithContent("Select 🪨, 📄, ✂️, 🦎, or 🖖");
    List<DiscordButtonComponent> actions = new List<DiscordButtonComponent> {
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "bRock", "🪨 Rock"),
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "bPaper", "📄 Paper"),
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "bScissors", "✂️ Scissors"),
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "bLizard", "🦎 Lizard"),
      new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "bSpock", "🖖 Spock")
    };
    builder.AddComponents(actions);

    DiscordMessage msg = builder.SendAsync(ctx.Channel).Result;
    var interact = ctx.Client.GetInteractivity();
    var result = await interact.WaitForButtonAsync(msg, TimeSpan.FromMinutes(2));
    var interRes = result.Result;
    if (interRes != null) {
      if (result.Result.Id == "bRock") yourmove = RPSLSTypes.Rock;
      else if (result.Result.Id == "bPaper") yourmove = RPSLSTypes.Paper;
      else if (result.Result.Id == "bScissors") yourmove = RPSLSTypes.Scissors;
      else if (result.Result.Id == "bLizard") yourmove = RPSLSTypes.Lizard;
      else if (result.Result.Id == "bSpock") yourmove = RPSLSTypes.Spock;
      string resmsg = rpslsMsgs[(int)yourmove][(int)botChoice];
      switch (rpslsRes[(int)yourmove][(int)botChoice]) {
        case RPSRes.First:
          await ctx.Channel.SendMessageAsync($"You said {GetChoice(yourmove)} {ctx.Member.Mention}, I played {GetChoice(botChoice)}! {resmsg}: **You win!**");
          break;
        case RPSRes.Second:
          await ctx.Channel.SendMessageAsync($"You said {GetChoice(yourmove)} {ctx.Member.Mention}, I played {GetChoice(botChoice)}! {resmsg}: **I win!**");
          break;
        case RPSRes.Draw:
          await ctx.Channel.SendMessageAsync($"You said {GetChoice(yourmove)} {ctx.Member.Mention}, I played {GetChoice(botChoice)}! **DRAW!**");
          break;
      }
    }
    await ctx.Channel.DeleteMessageAsync(msg); // Expired
  }

}

