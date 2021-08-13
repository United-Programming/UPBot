using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

/// <summary>
/// This command implements a basic ping command.
/// It is mostly for debug reasons.
/// author: CPU
/// </summary>
public class PingModule : BaseCommandModule {
  private List<LastRequestByMember> lastRequests = null;

  [Command("ping")]
  public async Task GreetCommand(CommandContext ctx) {
    await GeneratePong(ctx);
  }
  [Command("upbot")]
  public async Task GreetCommand2(CommandContext ctx) {
    await GeneratePong(ctx);
  }

  string[] basicAnswers = {
    "I am alive!", "Pong", "Ack", "I am here", "I am here $$$", "I am here @@@", "Pong, $$$"
  };
  string[] areYouTesting = {
    "Are you testing something?", "Are you doing some debug?", "Are you testing something, $$$?", "Are you doing some debug, @@@?", "Yeah, I am here"
  };
  string[] stillDoingTests = {
    "Still testing?", "Are you still debugging?", "Still testing, $$$?", "Yeah, I am still here"
  };
  Random random = new Random();
  TimeSpan tenMins = TimeSpan.FromSeconds(600);
  TimeSpan oneMin = TimeSpan.FromSeconds(60);
  TimeSpan thirtySecs = TimeSpan.FromSeconds(30);

  Task GeneratePong(CommandContext ctx) {

    // Check if we have to initiialize our history of pings
    if (lastRequests == null) lastRequests = new List<LastRequestByMember>();

    // Grab the current member id
    DiscordMember member = ctx.Member;
    ulong memberId = member.Id;

    // Find the last request
    LastRequestByMember lastRequest = null;
    TimeSpan timeSpan = TimeSpan.Zero;
    foreach (LastRequestByMember lr in lastRequests)
      if (lr.memberId == memberId) {
        lastRequest = lr;
        break;
      }
    if (lastRequest == null) { // No last request, create one
      lastRequest = new LastRequestByMember { memberId = memberId, dateTime = DateTime.Now, num = 0 };
      lastRequests.Add(lastRequest);
    }
    else {
      timeSpan = DateTime.Now - lastRequest.dateTime;
      lastRequest.dateTime = DateTime.Now;
    }

    // Was the request already done recently?
    string msg;
    if (timeSpan == TimeSpan.Zero || timeSpan > tenMins) { // No request or more than 10 minutes ago
      msg = basicAnswers[random.Next(0, basicAnswers.Length)];
      msg = msg.Replace("$$$", member.DisplayName).Replace("@@@", member.Mention);
      lastRequest.num = 0;
      return ctx.RespondAsync(msg);
    }

    // Increase the number
    lastRequest.num++;
    if (lastRequest.num < 2) {
      msg = basicAnswers[random.Next(0, basicAnswers.Length)];
      msg = msg.Replace("$$$", member.DisplayName).Replace("@@@", member.Mention);
      msg += " Again.";
      return ctx.RespondAsync(msg);
    }
    if (lastRequest.num < 4) {
      if (timeSpan < thirtySecs) {
        msg = areYouTesting[random.Next(0, basicAnswers.Length)].Replace("$$$", member.DisplayName).Replace("@@@", member.Mention);
        return ctx.RespondAsync(msg);
      }
      if (timeSpan < oneMin) {
        msg = stillDoingTests[random.Next(0, basicAnswers.Length)];
        msg = msg.Replace("$$$", member.DisplayName).Replace("@@@", member.Mention);
        msg += " Again.";
        return ctx.RespondAsync(msg);
      }
      msg = basicAnswers[random.Next(0, basicAnswers.Length)];
      msg = msg.Replace("$$$", member.DisplayName).Replace("@@@", member.Mention);
      msg += " Again.";
      return ctx.RespondAsync(msg);
    }
    if (lastRequest.num < 6) {
      return ctx.RespondAsync("You are becoming annoying.");
    }
    if (lastRequest.num == 6) {
      if (timeSpan < oneMin)
        return ctx.RespondAsync("I am not going to aswer you for at least a minute.");
      else
        return ctx.RespondAsync("");
    }
    if (lastRequest.num == 7) {
      return ctx.RespondAsync("I am not going to aswer you for 10 minutes.");
    }
    return ctx.RespondAsync("");
  }

  public class LastRequestByMember {
    public ulong memberId;
    public DateTime dateTime;
    public int num;
  }
}


