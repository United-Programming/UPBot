using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;

/// <summary>
/// This command implements simple games like:
/// Rock-Paper-Scissors
/// author: SlicEnDicE
/// </summary>
public class GameModule : BaseCommandModule {
  [Command("game")]
  public async Task GameCommand(CommandContext ctx) {
    if (!Setup.Permitted(ctx.Guild.Id, Config.ParamType.Games, ctx.Member.Roles)) return;
    Utils.LogUserCommand(ctx);
    StringBuilder sb = new StringBuilder("Available game commmands\n");
    sb.AppendLine("========================");
    sb.AppendLine(String.Format("{0, -10}: {1}", "bool", "True or False"));
    sb.AppendLine(String.Format("{0, -10}: {1}", "rps", "Rock, Paper, Scissors"));

    await ctx.RespondAsync(String.Format("```{0}```", sb.ToString()));
  }

  [Command("bool")]
  [Description("Returns True or False")]
  public async Task BoolCommand(CommandContext ctx) {
    if (!Setup.Permitted(ctx.Guild.Id, Config.ParamType.Games, ctx.Member.Roles)) return;
    Utils.LogUserCommand(ctx);
    await PlayBool(ctx);
  }

  [Command("rps")]
  [Description("Play Rock, Paper, Scissors")]
  public async Task RPSCommand(CommandContext ctx, [Description("rock | paper | scissors")] string kind) {
    if (!Setup.Permitted(ctx.Guild.Id, Config.ParamType.Games, ctx.Member.Roles)) return;
    Utils.LogUserCommand(ctx);
    await PlayRockPaperScissors(ctx, kind);
  }

  [Command("rps")]
  public async Task RPSCommand(CommandContext ctx) {
    if (!Setup.Permitted(ctx.Guild.Id, Config.ParamType.Games, ctx.Member.Roles)) return;
    Utils.LogUserCommand(ctx);

    ctx.Channel.DeleteMessageAsync(ctx.Message).Wait();
    var interact = ctx.Client.GetInteractivity();

    // Basic intro message
    var msg = CreateRPS(ctx);
    var result = await interact.WaitForButtonAsync(msg, TimeSpan.FromMinutes(1));
    var ir = result.Result;
    int val = random.Next(0, 3);

    while (ir != null) {
      ir.Handled = true;
      await ctx.Channel.DeleteMessageAsync(msg);
      if (ir.Id == "idrock") {
        if(val == 0) {
          await Utils.DeleteDelayed(15, await ctx.Channel.SendMessageAsync("You said 🪨 Rock " + ctx.Member.Mention + ", I played 🪨 Rock! **DRAW!**"));
        }
        else if(val == 1) {
          await Utils.DeleteDelayed(15, await ctx.Channel.SendMessageAsync("You said 🪨 Rock " + ctx.Member.Mention + ", I played 📄 Paper! **I win!**"));
        }
        else if(val == 2) {
          await Utils.DeleteDelayed(15, await ctx.Channel.SendMessageAsync("You said 🪨 Rock " + ctx.Member.Mention + ", I played ✂️ Scissor! **You win!**"));
        }
      }
      else if (ir.Id == "idpaper") {
        if(val == 0) {
          await Utils.DeleteDelayed(15, await ctx.Channel.SendMessageAsync("You said 📄 Paper " + ctx.Member.Mention + ", I played 🪨 Rock! **You win!**"));
        }
        else if(val == 1) {
          await Utils.DeleteDelayed(15, await ctx.Channel.SendMessageAsync("You said 📄 Paper " + ctx.Member.Mention + ", I played 📄 Paper! **DRAW!**"));
        }
        else if(val == 2) {
          await Utils.DeleteDelayed(15, await ctx.Channel.SendMessageAsync("You said 📄 Paper " + ctx.Member.Mention + ", I played ✂️ Scissor! **I win!**"));
        }
      }
      else if (ir.Id == "idscissors") {
        if(val == 0) {
          await Utils.DeleteDelayed(15, await ctx.Channel.SendMessageAsync("You said ✂️ Scissor " + ctx.Member.Mention + ", I played 🪨 Rock! **I win!**"));
        }
        else if(val == 1) {
          await Utils.DeleteDelayed(15, await ctx.Channel.SendMessageAsync("You said ✂️ Scissor " + ctx.Member.Mention + ", I played 📄 Paper! **You win!**"));
        }
        else if(val == 2) {
          await Utils.DeleteDelayed(15, await ctx.Channel.SendMessageAsync("You said ✂️ Scissor " + ctx.Member.Mention + ", I played ✂️ Scissor! **DRAW!**"));
        }
      }
    }
    if (ir == null) await ctx.Channel.DeleteMessageAsync(msg); // Expired
  }


  readonly Random random = new Random();

  Task PlayBool(CommandContext ctx) {
    int value = random.Next(0, 2);

    switch (value) {
      case 1:
        return ctx.RespondAsync("true");
      default:
        return ctx.RespondAsync("false");
    }
  }

  enum RPSTypes : ushort {
    Rock = 0,
    Paper = 1,
    Scissors = 2
  }

  Task PlayRockPaperScissors(CommandContext ctx, string kind) {
    RPSTypes playerChoice;

    _ = Utils.DeleteDelayed(15, ctx.Message);
    switch (kind) {
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
        return ctx.RespondAsync($"I don't know what {kind} is, please try again...");
    }

    RPSTypes botChoice = (RPSTypes)random.Next(0, 3);

    if (playerChoice == RPSTypes.Rock) {
      if (botChoice == RPSTypes.Rock) {
        return Utils.DeleteDelayed(15, ctx.Channel.SendMessageAsync("You said 🪨 Rock " + ctx.Member.Mention + ", I played 🪨 Rock! **DRAW!**"));
      } else if (botChoice == RPSTypes.Paper) {
        return Utils.DeleteDelayed(15, ctx.Channel.SendMessageAsync("You said 🪨 Rock " + ctx.Member.Mention + ", I played 📄 Paper! **I win!**"));
      } else {
        return Utils.DeleteDelayed(15, ctx.Channel.SendMessageAsync("You said 🪨 Rock " + ctx.Member.Mention + ", I played ✂️ Scissor! **You win!**"));
      }
    } else if (playerChoice == RPSTypes.Paper) {
      if (botChoice == RPSTypes.Rock) {
        return Utils.DeleteDelayed(15, ctx.Channel.SendMessageAsync("You said 📄 Paper " + ctx.Member.Mention + ", I played 🪨 Rock! **You win!**"));
      } else if (botChoice == RPSTypes.Paper) {
        return Utils.DeleteDelayed(15, ctx.Channel.SendMessageAsync("You said 📄 Paper " + ctx.Member.Mention + ", I played 📄 Paper! **DRAW!**"));
      } else {
        return Utils.DeleteDelayed(15, ctx.Channel.SendMessageAsync("You said 📄 Paper " + ctx.Member.Mention + ", I played ✂️ Scissor! **I win!**"));
      }
    } else {
      if (botChoice == RPSTypes.Rock) {
        return Utils.DeleteDelayed(15, ctx.Channel.SendMessageAsync("You said ✂️ Scissor " + ctx.Member.Mention + ", I played 🪨 Rock! **I win!**"));
      } else if (botChoice == RPSTypes.Paper) {
        return Utils.DeleteDelayed(15, ctx.Channel.SendMessageAsync("You said ✂️ Scissor " + ctx.Member.Mention + ", I played 📄 Paper! **You win!**"));
      } else {
        return Utils.DeleteDelayed(15, ctx.Channel.SendMessageAsync("You said ✂️ Scissor " + ctx.Member.Mention + ", I played ✂️ Scissor! **DRAW!**"));
      }
    }
  }

  readonly DiscordComponentEmoji er = new DiscordComponentEmoji(DiscordEmoji.FromUnicode("🪨"));
  readonly DiscordComponentEmoji ep = new DiscordComponentEmoji(DiscordEmoji.FromUnicode("📄"));
  readonly DiscordComponentEmoji es = new DiscordComponentEmoji(DiscordEmoji.FromUnicode("✂️"));

  private DiscordMessage CreateRPS(CommandContext ctx) {
    DiscordEmbedBuilder eb = new DiscordEmbedBuilder {
      Title = "Rock Paper Scissor"
    };
    eb.Description = "Pick your move, " + ctx.Member.Mention + "!";
    eb.WithThumbnail(ctx.Member.AvatarUrl);

    var builder = new DiscordMessageBuilder();
    builder.AddEmbed(eb.Build());
    List<DiscordButtonComponent> actions = new List<DiscordButtonComponent>();
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "idrock", "Rock!", false, er));
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "idpaper", "Paper!", false, ep));
    actions.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "idscissors", "Scissors!", false, es));
    builder.AddComponents(actions);

    return builder.SendAsync(ctx.Channel).Result;
  }


  // 🪨📄✂️
}

