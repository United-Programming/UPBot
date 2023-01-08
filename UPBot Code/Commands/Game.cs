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


  private string PrintBoard(int[] grid) {
    string board = "";
    for (int y = 0; y < 3; y++) {
      for (int x = 0; x < 3; x++) {
        int pos = x + 3 * y;
        if (grid[pos] == 1) board += ":o:";
        else if (grid[pos] == 2) board += ":x:";
        else board += ":black_large_square:";
        board += "¹²³⁴⁵⁶⁷⁸⁹"[pos];
      }
      board += "\n";
    }
    return board;
  }


  [SlashCommand("tictactoe", "Play Tic-Tac-Toe game with someone or aganinst the bot.")]
  public async Task TicTacToeGame(InteractionContext ctx, [Option("opponent", "Select a Discord user to play with (keep empty to play with the bot)")] DiscordUser opponent = null) {
    Utils.LogUserCommand(ctx);
    int[] grid = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
    DiscordMember player = ctx.Member;
    bool oMoves = true;

    var interact = ctx.Client.GetInteractivity();

    // Game loop
    try {
      bool firstMessage = true;
      while (true) {
        // Print the board

        DiscordEmbedBuilder message = new();
        if (opponent == null) {
          message.Title = "Tic-Tac-Toe Game";
          if (oMoves) message.Description = $"**Playing with the bot!**\n{player.DisplayName}: Type a number between 1 and 9 to make a move.\n\n{PrintBoard(grid)}";
          // no need to print the board for the bot
          message.Timestamp = DateTime.Now;
          message.Color = DiscordColor.Red;
        }
        else {
          if (oMoves) message.Description = $"**Playing with {opponent.Mention}**\n{opponent.Username}: Type a number between 1 and 9 to make a move.\n\n" + PrintBoard(grid);
          else message.Description = $"**Playing with {opponent.Mention}**\n{player.DisplayName}: Type a number between 1 and 9 to make a move.\n\n" + PrintBoard(grid);
          message.Title = "Tic-Tac-Toe Game";
          message.Timestamp = DateTime.Now;
          message.Color = DiscordColor.Red;
        }
        if (firstMessage) {
          await ctx.CreateResponseAsync(message.Build());
          firstMessage = false;
        }
        else {
          await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(message));
        }

        if (oMoves || opponent != null) { // Get the answer from the current user
          var answer = await interact.WaitForMessageAsync((dm) => {
            return (opponent == null || !oMoves ?
              dm.Channel == ctx.Channel && dm.Author.Id == ctx.Member.Id : dm.Channel == ctx.Channel && dm.Author.Id == opponent.Id
            );
          }, TimeSpan.FromMinutes(5));

          if (answer.Result == null) {
            message.Title = "Time expired!";
            message.Color = DiscordColor.Red;
            message.Description = $"You took too much time to type your move. Game is ended!";
            message.Timestamp = DateTime.Now;
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(message));
            return;
          }

          if (int.TryParse(answer.Result.Content, out var cell)) {
            if (cell < 1 || cell > 9) continue;
            cell--;
            if (grid[cell] != 0) continue;

            grid[cell] = oMoves ? 1 : 2;
          }
          else continue;

        }
        else { // Bot move
          BotPick(grid);
        }

        // Check victory
        bool oWins = false;
        bool xWins = false;
        for (int i = 0; i < 3 && !oWins && !xWins; i++) {
          if (grid[i * 3 + 0] == 1 && grid[i * 3 + 1] == 1 && grid[i * 3 + 2] == 1) { oWins = true; break; }
          if (grid[i * 3 + 0] == 2 && grid[i * 3 + 1] == 2 && grid[i * 3 + 2] == 2) { oWins = true; break; }
        }
        for (int i = 0; i < 3 && !oWins && !xWins; i++) {
          if (grid[0 * 3 + i] == 1 && grid[1 * 3 + i] == 1 && grid[2 * 3 + i] == 1) { oWins = true; break; }
          if (grid[0 * 3 + i] == 2 && grid[1 * 3 + i] == 2 && grid[2 * 3 + i] == 2) { oWins = true; break; }
        }
        if (grid[0] == 1 && grid[4] == 1 && grid[8] == 1) { oWins = true; break; }
        if (grid[2] == 1 && grid[4] == 1 && grid[6] == 1) { oWins = true; break; }
        if (grid[0] == 2 && grid[4] == 2 && grid[8] == 2) { oWins = true; break; }
        if (grid[2] == 2 && grid[4] == 2 && grid[6] == 2) { oWins = true; break; }


        if (oWins) {
          message.Title = $"Tic-Tac-Toe Game: :o: ({(opponent == null ? player.Username : opponent.Username)}) Wins!";
          message.Description = "**Game is ended!**";
          message.Color = DiscordColor.Red;
          message.Timestamp = DateTime.Now;
          await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(message));
          return;
        }
        if (xWins) {
          if (opponent == null) message.Title = $"Tic-Tac-Toe Game: :x: (Bot) Wins!";
          else message.Title = $"Tic-Tac-Toe Game: :x: ({player.Username}) Wins!";
          message.Description = "**Game is ended!**";
          message.Color = DiscordColor.Red;
          message.Timestamp = DateTime.Now;
          await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(message));
          return;
        }

        // Draw?
        bool draw = true;
        for (int i = 0; i < 9; i++) {
          if (grid[i] == 0) {
            draw = false;
            break;
          }
        }
        if (draw) {
          message.Title = "Tic-Tac-Toe Game: Draw!";
          message.Description = "**Game is ended!**";
          message.Color = DiscordColor.Red;
          message.Timestamp = DateTime.Now;
          await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(message));
          return;
        }


        // Make the other one move
        oMoves = !oMoves;
      }
    } catch (Exception) {
    }
  }

  private void BotPick(int[] grid) {
    int pos = -1;

    // Check if the center is used, if not pick it.
    if (grid[4] == 0) {
      grid[4] = 2;
      return;
    }




    // Check if there are at least 2 positions in sequence, in case block it or win it
    for (int c = 0; c < 3 && pos == -1; c++) {
      int r = 3 * c;
      if (grid[0 + r] == 0 && grid[1 + r] == 2 && grid[2 + r] == 2) pos = r;
      if (grid[0 + r] == 2 && grid[1 + r] == 0 && grid[2 + r] == 2) pos = r + 1;
      if (grid[0 + r] == 2 && grid[1 + r] == 2 && grid[2 + r] == 0) pos = r + 2;

      if (grid[0 + r] == 0 && grid[1 + r] == 1 && grid[2 + r] == 1) pos = r;
      if (grid[0 + r] == 1 && grid[1 + r] == 0 && grid[2 + r] == 1) pos = r + 1;
      if (grid[0 + r] == 1 && grid[1 + r] == 1 && grid[2 + r] == 0) pos = r + 2;

      if (grid[c] == 0 && grid[c + 3] == 2 && grid[c + 6] == 2) pos = c;
      if (grid[c] == 2 && grid[c + 3] == 0 && grid[c + 6] == 2) pos = c + 3;
      if (grid[c] == 2 && grid[c + 3] == 2 && grid[c + 6] == 0) pos = c + 6;

      if (grid[c] == 0 && grid[c + 3] == 1 && grid[c + 6] == 1) pos = c;
      if (grid[c] == 1 && grid[c + 3] == 0 && grid[c + 6] == 1) pos = c + 3;
      if (grid[c] == 1 && grid[c + 3] == 1 && grid[c + 6] == 0) pos = c + 6;
    }
    if (pos == -1 && grid[0] == 2 && grid[4] == 2 && grid[8] == 0) pos = 8;
    if (pos == -1 && grid[0] == 2 && grid[4] == 0 && grid[8] == 2) pos = 4;
    if (pos == -1 && grid[0] == 0 && grid[4] == 2 && grid[8] == 2) pos = 0;
    if (pos == -1 && grid[2] == 2 && grid[4] == 2 && grid[6] == 0) pos = 6;
    if (pos == -1 && grid[2] == 2 && grid[4] == 0 && grid[6] == 2) pos = 4;
    if (pos == -1 && grid[2] == 0 && grid[4] == 2 && grid[6] == 2) pos = 2;

    if (pos == -1 && grid[0] == 1 && grid[4] == 1 && grid[8] == 0) pos = 8;
    if (pos == -1 && grid[0] == 1 && grid[4] == 0 && grid[8] == 1) pos = 4;
    if (pos == -1 && grid[0] == 0 && grid[4] == 1 && grid[8] == 1) pos = 0;
    if (pos == -1 && grid[2] == 1 && grid[4] == 1 && grid[6] == 0) pos = 6;
    if (pos == -1 && grid[2] == 1 && grid[4] == 0 && grid[6] == 1) pos = 4;
    if (pos == -1 && grid[2] == 0 && grid[4] == 1 && grid[6] == 1) pos = 2;

    if (pos == -1) { // Pick a random position
      int times = 0;
      Random rand = new();
      while (times < 1000) { // Just to avoid problems
        times++;
        pos = rand.Next(0, 9);
        if (grid[pos] == 0) break;
      }
    }
    grid[pos] = 2;
  }
}
