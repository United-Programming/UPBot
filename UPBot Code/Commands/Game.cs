using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;


/// <summary>
/// This command implements simple games like:
/// Rock-Paper-Scissors, Coin
/// author: SlicEnDicE, J0nathan550
/// </summary>

[SlashCommandGroup("game", "Commands to play games with the bot")]
<<<<<<< Updated upstream
public class SlashGame : ApplicationCommandModule {
  readonly Random random = new Random();

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
=======
public class SlashGame : ApplicationCommandModule
{
    readonly Random random = new();

    [SlashCommand("rockpaperscissors", "Play Rock, Paper, Scissors")]
    public async Task RPSCommand(InteractionContext ctx, [Option("yourmove", "Rock, Paper, or Scissors")] RPSTypes? yourmove = null)
    {
        Utils.LogUserCommand(ctx);

        RPSTypes botChoice = (RPSTypes)random.Next(0, 3);
        if (yourmove != null)
        {
            if (yourmove == RPSTypes.Rock)
            {
                if (botChoice == RPSTypes.Rock)
                {
                    await ctx.CreateResponseAsync($"You said 🪨 Rock {ctx.Member.Mention}, I played 🪨 Rock! **DRAW!**");
                }
                else if (botChoice == RPSTypes.Paper)
                {
                    await ctx.CreateResponseAsync($"You said 🪨 Rock {ctx.Member.Mention}, I played 📄 Paper! **I win!**");
                }
                else
                {
                    await ctx.CreateResponseAsync($"You said 🪨 Rock {ctx.Member.Mention}, I played ✂️ Scissor! **You win!**");
                }
            }
            else if (yourmove == RPSTypes.Paper)
            {
                if (botChoice == RPSTypes.Rock)
                {
                    await ctx.CreateResponseAsync($"You said 📄 Paper {ctx.Member.Mention}, I played 🪨 Rock! **You win!**");
                }
                else if (botChoice == RPSTypes.Paper)
                {
                    await ctx.CreateResponseAsync($"You said 📄 Paper {ctx.Member.Mention}, I played 📄 Paper! **DRAW!**");
                }
                else
                {
                    await ctx.CreateResponseAsync($"You said 📄 Paper {ctx.Member.Mention}, I played ✂️ Scissor! **I win!**");
                }
            }
            else
            {
                if (botChoice == RPSTypes.Rock)
                {
                    await ctx.CreateResponseAsync($"You said ✂️ Scissor {ctx.Member.Mention}, I played 🪨 Rock! **I win!**");
                }
                else if (botChoice == RPSTypes.Paper)
                {
                    await ctx.CreateResponseAsync($"You said ✂️ Scissor {ctx.Member.Mention}, I played 📄 Paper! **You win!**");
                }
                else
                {
                    await ctx.CreateResponseAsync($"You said ✂️ Scissor {ctx.Member.Mention}, I played ✂️ Scissor! **DRAW!**");
                }
            }
            return;
>>>>>>> Stashed changes
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
        if (interRes != null)
        {
            if (result.Result.Id == "bRock")
            {
                if (botChoice == RPSTypes.Rock)
                {
                    await ctx.Channel.SendMessageAsync($"You said 🪨 Rock {ctx.Member.Mention}, I played 🪨 Rock! **DRAW!**");
                }
                else if (botChoice == RPSTypes.Paper)
                {
                    await ctx.Channel.SendMessageAsync($"You said 🪨 Rock {ctx.Member.Mention}, I played 📄 Paper! **I win!**");
                }
                else
                {
                    await ctx.Channel.SendMessageAsync($"You said 🪨 Rock {ctx.Member.Mention}, I played ✂️ Scissor! **You win!**");
                }
            }
            else if (result.Result.Id == "bPaper")
            {
                if (botChoice == RPSTypes.Rock)
                {
                    await ctx.Channel.SendMessageAsync($"You said 📄 Paper {ctx.Member.Mention}, I played 🪨 Rock! **You win!**");
                }
                else if (botChoice == RPSTypes.Paper)
                {
                    await ctx.Channel.SendMessageAsync($"You said 📄 Paper {ctx.Member.Mention}, I played 📄 Paper! **DRAW!**");
                }
                else
                {
                    await ctx.Channel.SendMessageAsync($"You said 📄 Paper {ctx.Member.Mention}, I played ✂️ Scissor! **I win!**");
                }
            }
            else if (result.Result.Id == "bScissors")
            {
                await ctx.Channel.SendMessageAsync($"You said ✂️ Scissor {ctx.Member.Mention}, I played 🪨 Rock! **I win!**");
            }
            else if (botChoice == RPSTypes.Paper)
            {
                await ctx.Channel.SendMessageAsync($"You said ✂️ Scissor {ctx.Member.Mention}, I played 📄 Paper! **You win!**");
            }
            else
            {
                await ctx.Channel.SendMessageAsync($"You said ✂️ Scissor {ctx.Member.Mention}, I played ✂️ Scissor! **DRAW!**");
            }
        }
        await ctx.Channel.DeleteMessageAsync(msg);
    }

    public enum RPSTypes
    { // 🪨📄
        [ChoiceName("Rock")] Rock = 0,
        [ChoiceName("Paper")] Paper = 1,
        [ChoiceName("Scissors")] Scissors = 2
    }
    public enum RPSLSTypes
    { // 🪨📄✂️🦎🖖
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

<<<<<<< Updated upstream
  private string GetChoice(RPSLSTypes? move) {
    return move switch
    {
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
=======
    static private string GetChoice(RPSLSTypes? move)
    {
        return move switch
        {
            RPSLSTypes.Rock => "🪨 Rock",
            RPSLSTypes.Paper => "📄 Paper",
            RPSLSTypes.Scissors => "✂️ Scissors",
            RPSLSTypes.Lizard => "🦎 Lizard",
            RPSLSTypes.Spock => "🖖 Spock",
            _ => "?",
        };
>>>>>>> Stashed changes
    }

    [SlashCommand("rockpaperscissorslizardspock", "Play Rock, Paper, Scissors, Lizard, Spock")]
    public async Task RPSLKCommand(InteractionContext ctx, [Option("yourmove", "Rock, Paper, or Scissors")] RPSLSTypes? yourmove = null)
    {
        Utils.LogUserCommand(ctx);

        RPSLSTypes botChoice = (RPSLSTypes)random.Next(0, 5);
        if (yourmove != null)
        {
            string resmsg = rpslsMsgs[(int)yourmove][(int)botChoice];
            switch (rpslsRes[(int)yourmove][(int)botChoice])
            {
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
        if (interRes != null)
        {
            if (result.Result.Id == "bRock") yourmove = RPSLSTypes.Rock;
            else if (result.Result.Id == "bPaper") yourmove = RPSLSTypes.Paper;
            else if (result.Result.Id == "bScissors") yourmove = RPSLSTypes.Scissors;
            else if (result.Result.Id == "bLizard") yourmove = RPSLSTypes.Lizard;
            else if (result.Result.Id == "bSpock") yourmove = RPSLSTypes.Spock;
            string resmsg = rpslsMsgs[(int)yourmove][(int)botChoice];
            switch (rpslsRes[(int)yourmove][(int)botChoice])
            {
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

    public async Task CoinFlipCommand(InteractionContext ctx, [Option("firstoption", "Optional: Type your first option")] string firstOption = null, [Option("secondoption", "Optional: Type your second option")] string secondOption = null)
    {
        Utils.LogUserCommand(ctx);
        int randomNumber;
        DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
        if (firstOption == null || secondOption == null)
        {
            randomNumber = random.Next(0, 2);
            switch (randomNumber)
            {
                case 0:
                    var builder = new DiscordEmbedBuilder
                    {
                        Title = "Coin Flip!",
                        Color = DiscordColor.Yellow,
                        Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail
                        {
                            Url = "https://emojipedia-us.s3.dualstack.us-west-1.amazonaws.com/thumbs/120/apple/325/coin_1fa99.png"
                        },
                        Description = "Heads on the coin!",
                        Timestamp = DateTime.Now
                    };
                    await ctx.CreateResponseAsync(builder);
                    break;
                case 1:
                    var builder1 = new DiscordEmbedBuilder
                    {
                        Title = "Coin Flip!",
                        Color = DiscordColor.Yellow,
                        Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail
                        {
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
        switch (randomNumber)
        {
            case 0:
                var builder = new DiscordEmbedBuilder
                {
                    Title = "Coin Flip!",
                    Color = DiscordColor.Yellow,
                    Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail
                    {
                        Url = "https://emojipedia-us.s3.dualstack.us-west-1.amazonaws.com/thumbs/120/apple/325/coin_1fa99.png"
                    },
                    Description = "Heads on the coin!\n" +
                    $"You have to: **{firstOption}**",
                    Timestamp = DateTime.Now
                };
                await ctx.CreateResponseAsync(builder);
                break;
            case 1:
                var builder1 = new DiscordEmbedBuilder
                {
                    Title = "Coin Flip!",
                    Color = DiscordColor.Yellow,
                    Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail
                    {
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
}
