using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
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
      using (Bitmap b = new Bitmap(400, 150)) {
        using (Graphics g = Graphics.FromImage(b)) {
          Font f = new Font("Times New Roman", 20, FontStyle.Bold, GraphicsUnit.Pixel);
          Font f2 = new Font("Times New Roman", 16, FontStyle.Bold, GraphicsUnit.Pixel);

          g.Clear(Color.Transparent);
          Pen back = new Pen(Color.FromArgb(30, 30, 28));
          Pen lines = new Pen(Color.FromArgb(70, 70, 72));
          Brush fill = new SolidBrush(back.Color);
          Brush txtl = new SolidBrush(Color.FromArgb(150, 150, 102));
          Brush txte = new SolidBrush(Color.FromArgb(170, 182, 150));
          Brush txty = new SolidBrush(Color.FromArgb(204, 212, 170));
          Brush txtb = new SolidBrush(Color.FromArgb(50, 50, 107));

          Brush expfillg = new SolidBrush(Color.FromArgb(106, 188, 96));
          Pen explineg = new Pen(Color.FromArgb(106, 248, 169));
          Brush expfillb = new SolidBrush(Color.FromArgb(100, 96, 180));
          Pen explineb = new Pen(Color.FromArgb(106, 169, 248));

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



          exp = ran * 1.25 + rep * 2.5 + fun * 2.5 + tnk * 3.5

          2^4 = 16
          3^4 = 81
          4^4


          lev = (exp-9)^(1/4)
          exp = lev^4+9


      double lev = Math.Floor(
                      1.25 * Math.Pow(r.Ran, 0.25) +
                      2.5 * Math.Pow(r.Rep, 0.27) +
                      2.5 * Math.Pow(r.Fun, 0.27) +
                      3.5 * Math.Pow(r.Tnk, 0.27)) - 20;








          */


          DrawBox(g, fill, lines, 0, 0, 400, 150);

          g.DrawString("Rank for", f, txtl, 16, 16);
          g.DrawString(ctx.User.Username, f, txty, 105, 16);

          g.DrawString("Level:", f, txtl, 32, 48);
          g.DrawString("1", f, txte, 160, 48);

          g.FillRectangle(expfillg, new Rectangle(160, 80, 400 - 16 - 160, 24));
          g.DrawRectangle(explineg, new Rectangle(160, 80, 400 - 16 - 160, 24));
          g.FillRectangle(expfillb, new Rectangle(161, 81, 400 - 16 - 160 - 90, 22));
          g.DrawRectangle(explineb, new Rectangle(161, 81, 400 - 16 - 160 - 90, 22));

          g.DrawString("Experience:", f, txtl, 32, 80);
          g.DrawString("12345 / 56789", f2, txtb, 192, 84);

          g.DrawString("Position:", f, txtl, 32, 112);
          g.DrawString("#1", f, txte, 160, 112);
        }
        b.Save(@"green.png", ImageFormat.Png);

        using (var fs = new FileStream("green.png", FileMode.Open, FileAccess.Read)) {
          var msg = new DiscordMessageBuilder().WithFiles(new Dictionary<string, Stream>() { { "green.png", fs } });
          await ctx.RespondAsync(msg);
        }

        return;
      }
    } catch (Exception ex) {
      Console.WriteLine(ex.Message);
    }

    await ctx.Message.RespondAsync("Something wrong");
  }

  void DrawBox(Graphics g, Brush fill, Pen border, int t, int l, int r, int b) {
    int w = r - l;
    int h = b - t;
    int sw = w / 12;
    int sh = h / 10;
    Rectangle box = new Rectangle(t, l, t + sw, l + sh);
    GraphicsPath path = new GraphicsPath();
    box.X = l; box.Y = t;
    path.AddArc(box, 180, 90); // top left arc  

    box.X = w - sw - 1; box.Y = t;
    path.AddArc(box, 270, 90); // top right arc  

    box.X = w - sw - 1; box.Y = h - sh - 1;
    path.AddArc(box, 0, 90); // bottom right arc

    box.X = t; box.Y = h - sh - 1;
    path.AddArc(box, 90, 90); // bottom left arc 

    path.CloseFigure();
    g.FillPath(fill, path);
    g.DrawPath(border, path);
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