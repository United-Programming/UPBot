using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
/// <summary>
/// This command implements a WhoIs command.
/// It gives info about a Discord User or yourself
/// author: CPU
/// </summary>
public class TestB : BaseCommandModule {

  [Command("TestB")]
  [Description("Test command to produce buttons")]
  public async Task TestBCommand(CommandContext ctx) {
    var interact = ctx.Client.GetInteractivity();
    await ctx.Message.RespondAsync("Say hello, please");
    await interact.WaitForMessageAsync((dm) => {
      return (dm.Channel == ctx.Channel && dm.Content.ToLowerInvariant() == "hello");
    });

    await ctx.Message.RespondAsync("Hello to you");
  }

  [Command("TestC")]
  [Description("Test command to produce buttons")]
  public async Task TestCCommand(CommandContext ctx) {
    var interact = ctx.Client.GetInteractivity();
    await ctx.Message.RespondAsync("Poll...");
    DiscordMessage msg = await ctx.Channel.SendMessageAsync("Do you think you are right?");
    await interact.DoPollAsync(msg, new List<DiscordEmoji> { Utils.GetEmoji(EmojiEnum.OK), Utils.GetEmoji(EmojiEnum.KO) });
  }

  [Command("TestD")]
  [Description("Test command to produce buttons")]
  public async Task TestDCommand(CommandContext ctx) {
    var interact = ctx.Client.GetInteractivity();
    await ctx.Message.RespondAsync("Poll...");
    DiscordComponentEmoji ok = new DiscordComponentEmoji(Utils.GetEmoji(EmojiEnum.OK));
    DiscordComponentEmoji ko = new DiscordComponentEmoji(Utils.GetEmoji(EmojiEnum.KO));
    List<DiscordButtonComponent> btns = new List<DiscordButtonComponent>();


    var builder = new DiscordMessageBuilder();
    builder.WithContent("Click a button...");

    var b = new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "btn1", "Option 1", false, ok);
    btns.Add(b);
    b = new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, "btn2", "Option 2", false, ok);
    btns.Add(b);
    b = new DiscordButtonComponent(DSharpPlus.ButtonStyle.Danger, "btn3", "Option 3", false, ko);
    btns.Add(b);
    b = new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, "btnd", "Option x", true, ok);
    btns.Add(b);
    builder.AddComponents(btns);

    var msg = builder.SendAsync(ctx.Channel);

    var result = await interact.WaitForButtonAsync(msg.Result, TimeSpan.FromSeconds(30));
    var ir = result.Result;
    ir.Handled = true;
    await ir.Interaction.CreateResponseAsync(DSharpPlus.InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent("Completed?"));

//    await ir.Message.RespondAsync($"{ir.User.Username} pressed a button with id {ir.Id}");
  }
}