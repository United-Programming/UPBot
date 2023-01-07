using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;


/// <summary>
/// This command implements simple games like:
/// Rock-Paper-Scissors, Coin Flip, Tic-Tac-Toe
/// author: SlicEnDicE, J0nathan550
/// </summary>

[SlashCommandGroup("game", "Commands to play games with the bot")]
public class SlashGame : ApplicationCommandModule {
  readonly Random random = new();

  [SlashCommand("rockpaperscissors", "Play Rock, Paper, Scissors")]
  public async Task RPSCommand(InteractionContext ctx, [Option("yourmove", "Rock, Paper, or Scissors")] RPSTypes? yourmove = null) {
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
    List<DiscordButtonComponent> actions = new() {
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

  static private string GetChoice(RPSLSTypes? move) {
    return move switch {
      RPSLSTypes.Rock => "🪨 Rock",
      RPSLSTypes.Paper => "📄 Paper",
      RPSLSTypes.Scissors => "✂️ Scissors",
      RPSLSTypes.Lizard => "🦎 Lizard",
      RPSLSTypes.Spock => "🖖 Spock",
      _ => "?",
    };
  }


  [SlashCommand("rockpaperscissorslizardspock", "Play Rock, Paper, Scissors, Lizard, Spock")]
  public async Task RPSLKCommand(InteractionContext ctx, [Option("yourmove", "Rock, Paper, or Scissors")] RPSLSTypes? yourmove = null) {
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
    List<DiscordButtonComponent> actions = new() {
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
  [SlashCommand("coin", "Flip a coin, to deside your choice!")]

  public async Task CoinFlipCommand(InteractionContext ctx, [Option("firstoption", "Optional: You have to do this is the coin is Head")] string firstOption = null, [Option("secondoption", "Optional: You have to do this is the coin is Tails")] string secondOption = null) {
    Utils.LogUserCommand(ctx);
    int randomNumber;
    if (firstOption == null || secondOption == null) {
      randomNumber = random.Next(0, 2);
      switch (randomNumber) {
        case 0:
          var builder = new DiscordEmbedBuilder {
            Title = "Coin Flip!",
            Color = DiscordColor.Yellow,
            Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail {
              Url = "https://emojipedia-us.s3.dualstack.us-west-1.amazonaws.com/thumbs/120/apple/325/coin_1fa99.png"
            },
            Description = "Heads on the coin!",
            Timestamp = DateTime.Now
          };
          await ctx.CreateResponseAsync(builder);
          break;
        case 1:
          var builder1 = new DiscordEmbedBuilder {
            Title = "Coin Flip!",
            Color = DiscordColor.Yellow,
            Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail {
              Url = "https://emojipedia-us.s3.dualstack.us-west-1.amazonaws.com/thumbs/160/samsung/265/coin_1fa99.png"
            },
            Description = "Tails on the coin!",
            Timestamp = DateTime.Now
          };
          await ctx.CreateResponseAsync(builder1);
          break;
      }
      return;
    }
    randomNumber = random.Next(0, 2);
    switch (randomNumber) {
      case 0:
        var builder = new DiscordEmbedBuilder {
          Title = "Coin Flip!",
          Color = DiscordColor.Yellow,
          Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail {
            Url = "https://emojipedia-us.s3.dualstack.us-west-1.amazonaws.com/thumbs/120/apple/325/coin_1fa99.png"
          },
          Description = "Heads on the coin!\n" +
            $"You have to: **{firstOption}**",
          Timestamp = DateTime.Now
        };
        await ctx.CreateResponseAsync(builder);
        break;
      case 1:
        var builder1 = new DiscordEmbedBuilder {
          Title = "Coin Flip!",
          Color = DiscordColor.Yellow,
          Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail {
            Url = "https://emojipedia-us.s3.dualstack.us-west-1.amazonaws.com/thumbs/160/samsung/265/coin_1fa99.png"
          },
          Description = "Tails on the coin!\n" +
            $"You have to: **{secondOption}**",
          Timestamp = DateTime.Now
        };
        await ctx.CreateResponseAsync(builder1);
        break;
    }
  }

  private enum TicTacToe_GameStates {
    Idle,
    Playing
  }
  private TicTacToe_GameStates gameStates = TicTacToe_GameStates.Idle;
  private bool xSide = true;
  private int[] ticTacToeGrid = { 0, 0, 0, 0, 0, 0, 0, 0, 0 }; // x  x  x
                                                               // x  x  x
                                                               // x  x  x 
  [SlashCommand("tictactoe", "Play Tic-Tac-Toe game with someone, or yourself.")]
  public async Task TicTacToeGame(InteractionContext ctx) {
    // fix two issues with options where you can't interact with them.
    DiscordEmbedBuilder message = new DiscordEmbedBuilder();
    if (gameStates != TicTacToe_GameStates.Playing) {
      gameStates = TicTacToe_GameStates.Playing;
      message.Title = "Tic-Tac-Toe Game";
      message.Description = "Write number between 1-9 to make a move.\n" +
          "\n" +
          ":black_large_square::black_large_square::black_large_square:\r\n:black_large_square::black_large_square::black_large_square:\r\n:black_large_square::black_large_square::black_large_square:";
      message.Timestamp = DateTime.Now;
      message.Color = DiscordColor.Red;
      await ctx.CreateResponseAsync(message.Build());
    }

    var interact = ctx.Client.GetInteractivity();
    var answer = await interact.WaitForMessageAsync((dm) => {
      return (dm.Channel == ctx.Channel && dm.Author.Id == ctx.Member.Id);
    }, TimeSpan.FromMinutes(5));

    if (answer.Result == null) {
      message.Title = "Time expired!";
      message.Color = DiscordColor.Red;
      message.Description = $"You took too much time to type your move. Game is ended!";
      message.Timestamp = DateTime.Now;
      gameStates = TicTacToe_GameStates.Idle;
      await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(message)); // // ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed));
      return;
    }

    if (answer.Result.Content.ToLower() == "1" && xSide && ticTacToeGrid[0] == 0) {
      ticTacToeGrid[0] = 1;
      xSide = false;
    }
    else if (answer.Result.Content.ToLower() == "2" && xSide && ticTacToeGrid[1] == 0) {
      ticTacToeGrid[1] = 1;
      xSide = false;
    }
    else if (answer.Result.Content.ToLower() == "3" && xSide && ticTacToeGrid[2] == 0) {
      ticTacToeGrid[2] = 1;
      xSide = false;
    }
    else if (answer.Result.Content.ToLower() == "4" && xSide && ticTacToeGrid[3] == 0) {
      ticTacToeGrid[3] = 1;
      xSide = false;
    }
    else if (answer.Result.Content.ToLower() == "5" && xSide && ticTacToeGrid[4] == 0) {
      ticTacToeGrid[4] = 1;
      xSide = false;
    }
    else if (answer.Result.Content.ToLower() == "6" && xSide && ticTacToeGrid[5] == 0) {
      ticTacToeGrid[5] = 1;
      xSide = false;
    }
    else if (answer.Result.Content.ToLower() == "7" && xSide && ticTacToeGrid[6] == 0) {
      ticTacToeGrid[6] = 1;
      xSide = false;
    }
    else if (answer.Result.Content.ToLower() == "8" && xSide && ticTacToeGrid[7] == 0) {
      ticTacToeGrid[7] = 1;
      xSide = false;
    }
    else if (answer.Result.Content.ToLower() == "9" && xSide && ticTacToeGrid[8] == 0) {
      ticTacToeGrid[8] = 1;
      xSide = false;
    }
    else if (answer.Result.Content.ToLower() == "1" && !xSide && ticTacToeGrid[0] == 0) {
      ticTacToeGrid[0] = 2;
      xSide = true;
    }
    else if (answer.Result.Content.ToLower() == "2" && !xSide && ticTacToeGrid[1] == 0) {
      ticTacToeGrid[1] = 2;
      xSide = true;
    }
    else if (answer.Result.Content.ToLower() == "3" && !xSide && ticTacToeGrid[2] == 0) {
      ticTacToeGrid[2] = 2;
      xSide = true;
    }
    else if (answer.Result.Content.ToLower() == "4" && !xSide && ticTacToeGrid[3] == 0) {
      ticTacToeGrid[3] = 2;
      xSide = true;
    }
    else if (answer.Result.Content.ToLower() == "5" && !xSide && ticTacToeGrid[4] == 0) {
      ticTacToeGrid[4] = 2;
      xSide = true;
    }
    else if (answer.Result.Content.ToLower() == "6" && !xSide && ticTacToeGrid[5] == 0) {
      ticTacToeGrid[5] = 2;
      xSide = true;
    }
    else if (answer.Result.Content.ToLower() == "7" && !xSide && ticTacToeGrid[6] == 0) {
      ticTacToeGrid[6] = 2;
      xSide = true;
    }
    else if (answer.Result.Content.ToLower() == "8" && !xSide && ticTacToeGrid[7] == 0) {
      ticTacToeGrid[7] = 2;
      xSide = true;
    }
    else if (answer.Result.Content.ToLower() == "9" && !xSide && ticTacToeGrid[8] == 0) {
      ticTacToeGrid[8] = 2;
      xSide = true;
    }

    message.Title = "Tic-Tac-Toe Game";
    string ticTacToeMessage = "";
    for (int i = 0; i < ticTacToeGrid.Length; i++) {
      if (ticTacToeGrid[i] == 0) {
        if (i == 2 || i == 5 || i == 8) {
          ticTacToeMessage += ":black_large_square:\r\n";
          continue;
        }
        ticTacToeMessage += ":black_large_square:";
      }
      else if (ticTacToeGrid[i] == 1) {
        if (i == 2 || i == 5 || i == 8) {
          ticTacToeMessage += ":x:\r\n";
          continue;
        }
        ticTacToeMessage += ":x:";
      }
      else if (ticTacToeGrid[i] == 2) {
        if (i == 2 || i == 5 || i == 8) {
          ticTacToeMessage += ":o:\r\n";
          continue;
        }
        ticTacToeMessage += ":o:";
      }
    }
    message.Description = message.Title = "Tic-Tac-Toe Game";
    message.Description = "Write number between 1-9 to make a move.\n" +
        "\n" +
        $"{ticTacToeMessage}"; ;
    message.Timestamp = DateTime.Now;
    message.Color = DiscordColor.Red;
    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(message));
    await checkWin(ticTacToeGrid, ctx);
  }

  private void BotPick() {
    while (!xSide) {
      Random rand = new Random();
      int bot_pick = rand.Next(0, 9);
      for (int i = 0; i < ticTacToeGrid.Length; i++) {
        if (ticTacToeGrid[bot_pick] != 1 && ticTacToeGrid[bot_pick] == 0 && ticTacToeGrid[bot_pick] != 2) {
          ticTacToeGrid[bot_pick] = 2;
          xSide = true;
        }
      }
    }
  }

  private async Task checkWin(int[] grid, InteractionContext ctx) {
    DiscordEmbedBuilder message = new DiscordEmbedBuilder();
    for (int i = 0; i < grid.Length; i++) {
      if (gameStates == TicTacToe_GameStates.Idle) {
        return;
      }
      if (ticTacToeGrid[0] != 0 && ticTacToeGrid[1] != 0 && ticTacToeGrid[2] != 0 && ticTacToeGrid[3] != 0 && ticTacToeGrid[4] != 0 && ticTacToeGrid[5] != 0 && ticTacToeGrid[6] != 0 && ticTacToeGrid[7] != 0 && ticTacToeGrid[8] != 0) {
        message.Title = "Tic-Tac-Toe Game: Draw!";
        message.Description = "**Game is ended!**";
        message.Color = DiscordColor.Red;
        message.Timestamp = DateTime.Now;
        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(message));
        CleanStats();
        return;
      }
      else if (grid[0] == 1 && grid[1] == 1 && grid[2] == 1) {
        message.Title = "Tic-Tac-Toe Game: :x: Wins!";
        message.Description = "**Game is ended!**";
        message.Color = DiscordColor.Red;
        message.Timestamp = DateTime.Now;
        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(message));
        CleanStats();
        return;
      }
      else if (grid[3] == 1 && grid[4] == 1 && grid[5] == 1) {
        message.Title = "Tic-Tac-Toe Game: :x: Wins!";
        message.Description = "**Game is ended!**";
        message.Color = DiscordColor.Red;
        message.Timestamp = DateTime.Now;
        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(message));
        CleanStats();
        return;
      }
      else if (grid[6] == 1 && grid[7] == 1 && grid[8] == 1) {
        message.Title = "Tic-Tac-Toe Game: :x: Wins!";
        message.Description = "**Game is ended!**";
        message.Color = DiscordColor.Red;
        message.Timestamp = DateTime.Now;
        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(message));
        CleanStats();
        return;
      }
      else if (grid[0] == 1 && grid[3] == 1 && grid[6] == 1) {
        message.Title = "Tic-Tac-Toe Game: :x: Wins!";
        message.Description = "**Game is ended!**";
        message.Color = DiscordColor.Red;
        message.Timestamp = DateTime.Now;
        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(message));
        CleanStats();
        return;
      }
      else if (grid[1] == 1 && grid[4] == 1 && grid[7] == 1) {
        message.Title = "Tic-Tac-Toe Game: :x: Wins!";
        message.Description = "**Game is ended!**";
        message.Color = DiscordColor.Red;
        message.Timestamp = DateTime.Now;
        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(message));
        CleanStats();
        return;
      }
      else if (grid[2] == 1 && grid[5] == 1 && grid[8] == 1) {
        message.Title = "Tic-Tac-Toe Game: :x: Wins!";
        message.Description = "**Game is ended!**";
        message.Color = DiscordColor.Red;
        message.Timestamp = DateTime.Now;
        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(message));
        CleanStats();
        return;
      }
      else if (grid[0] == 1 && grid[4] == 1 && grid[8] == 1) {
        message.Title = "Tic-Tac-Toe Game: :x: Wins!";
        message.Description = "**Game is ended!**";
        message.Color = DiscordColor.Red;
        message.Timestamp = DateTime.Now;
        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(message));
        CleanStats();
        return;
      }
      else if (grid[2] == 1 && grid[4] == 1 && grid[6] == 1) {
        message.Title = "Tic-Tac-Toe Game: :x: Wins!";
        message.Description = "**Game is ended!**";
        message.Color = DiscordColor.Red;
        message.Timestamp = DateTime.Now;
        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(message));
        CleanStats();
        return;
      }
      else if (grid[0] == 2 && grid[1] == 2 && grid[2] == 2) {
        message.Title = "Tic-Tac-Toe Game: :o: Wins!";
        message.Description = "**Game is ended!**";
        message.Color = DiscordColor.Red;
        message.Timestamp = DateTime.Now;
        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(message));
        CleanStats();
        return;
      }
      else if (grid[3] == 2 && grid[4] == 2 && grid[5] == 2) {
        message.Title = "Tic-Tac-Toe Game: :o: Wins!";
        message.Description = "**Game is ended!**";
        message.Color = DiscordColor.Red;
        message.Timestamp = DateTime.Now;
        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(message));
        CleanStats();
        return;
      }
      else if (grid[6] == 2 && grid[7] == 2 && grid[8] == 2) {
        message.Title = "Tic-Tac-Toe Game: :o: Wins!";
        message.Description = "**Game is ended!**";
        message.Color = DiscordColor.Red;
        message.Timestamp = DateTime.Now;
        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(message));
        CleanStats();
        return;
      }
      else if (grid[0] == 2 && grid[3] == 2 && grid[6] == 2) {
        message.Title = "Tic-Tac-Toe Game: :o: Wins!";
        message.Description = "**Game is ended!**";
        message.Color = DiscordColor.Red;
        message.Timestamp = DateTime.Now;
        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(message));
        CleanStats();
        return;
      }
      else if (grid[1] == 2 && grid[4] == 2 && grid[7] == 2) {
        message.Title = "Tic-Tac-Toe Game: :o: Wins!";
        message.Description = "**Game is ended!**";
        message.Color = DiscordColor.Red;
        message.Timestamp = DateTime.Now;
        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(message));
        CleanStats();
        return;
      }
      else if (grid[2] == 2 && grid[5] == 2 && grid[8] == 2) {
        message.Title = "Tic-Tac-Toe Game: :o: Wins!";
        message.Description = "**Game is ended!**";
        message.Color = DiscordColor.Red;
        message.Timestamp = DateTime.Now;
        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(message));
        CleanStats();
        return;
      }
      else if (grid[0] == 2 && grid[4] == 2 && grid[8] == 2) {
        message.Title = "Tic-Tac-Toe Game: :o: Wins!";
        message.Description = "**Game is ended!**";
        message.Color = DiscordColor.Red;
        message.Timestamp = DateTime.Now;
        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(message));
        CleanStats();
        return;
      }
      else if (grid[3] == 2 && grid[4] == 2 && grid[6] == 2) {
        message.Title = "Tic-Tac-Toe Game: :o: Wins!";
        message.Description = "**Game is ended!**";
        message.Color = DiscordColor.Red;
        message.Timestamp = DateTime.Now;
        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(message));
        CleanStats();
        return;
      }
      await TicTacToeGame(ctx);
    }
  }
  private void CleanStats() {
    xSide = true;
    for (int i = 0; i < ticTacToeGrid.Length; i++) {
      ticTacToeGrid[i] = 0;
    }
    gameStates = TicTacToe_GameStates.Idle;
  }
}
