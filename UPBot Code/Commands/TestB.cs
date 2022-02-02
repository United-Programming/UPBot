using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
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
    try {
      using (Bitmap b = new Bitmap(320, 180)) {
        using (Graphics g = Graphics.FromImage(b)) {
          using (Font f = new Font("Times New Roman", 20, FontStyle.Bold, GraphicsUnit.Pixel)) {
            g.Clear(Color.Transparent);
            Pen back = new Pen(Color.FromArgb(30, 30, 28));
            Pen lines = new Pen(Color.FromArgb(70, 70, 72));
            Brush fill = new SolidBrush(back.Color);
            Brush txtl = new SolidBrush(Color.FromArgb(150, 150, 102));
            Brush txte = new SolidBrush(Color.FromArgb(170, 182, 150));

            /*
                 ----------------------------------------
                | Rank for <NAME>                        |
                |   Level: <rank>                        |
                |   Experience <abcd>/<defg>             |
                |   #<position>/<total counted>          |
                 ----------------------------------------

            <rank> is the calculated level
            <abcd> is the number we calculate
            <defg> is the number for the next level

            */


            Rectangle box = new Rectangle(0, 0, 32, 16);
            GraphicsPath path = new GraphicsPath();
            box.X = 0; box.Y = 0;
            path.AddArc(box, 180, 90); // top left arc  

            box.X = 320 - 33; box.Y = 0;
            path.AddArc(box, 270, 90); // top right arc  

            box.X = 320 - 33; box.Y = 180 - 17;
            path.AddArc(box, 0, 90); // bottom right arc

            box.X = 0; box.Y = 180 - 17;
            path.AddArc(box, 90, 90); // bottom left arc 

            path.CloseFigure();
            g.FillPath(fill, path);
            g.DrawPath(lines, path);

            g.DrawString("Rank for ", f, txtl, 16, 16);
            g.DrawString(ctx.User.Username, f, txte, 105, 16);
          }
        }
        b.Save(@"green.png", ImageFormat.Png);
      }
    } catch (Exception ex) {
      Console.WriteLine(ex.Message);
    }

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