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
  [Aliases("upbot")]
  [Description("Checks if the bot is alive")]
  public async Task PongCommand(CommandContext ctx) {
    if (!Setup.Permitted(ctx.Guild.Id, Config.ParamType.Ping, ctx.Member.Roles)) return;
    await GeneratePong(ctx);
  }

  readonly string[,] answers = {
    /* Good */ { "I am alive!", "Pong", "Ack", "I am here", "I am here $$$", "I am here @@@", "Pong, $$$" },
    /* Again? */ { "Again, I am alive", "Pong", "Another Ack", "I told you, I am here", "Yes, I am here $$$", "@@@, I told you I am here", "Pong, $$$. You don't get it?" },
    /* Testing? */ { "Are you testing something?", "Are you doing some debug?", "Are you testing something, $$$?", "Are you doing some debug, @@@?", "Yeah, I am here.",
      "There is something wrong?", "Do you really miss me or is this a joke?" },
    /* Light annoyed */ {"This is no more funny", "Yeah, @@@, I am a bot", "I am contractually obliged to answer. But I do not like it", "I will start pinging you when you are asleep, @@@", "Looks guys! $$$ has nothing better to do than pinging me!",
    "I am alive, but I am also annoyed", "ƃuoԀ" },
    /* Menacing */ {"Stop it.", "I will probably write your name in my black list", "Why do you insist?", "Find another bot to harass", "<the bot is not working>", "Request time out.", "You are consuming your keyboard" },
    /* Punishment */ {"I am going to ignore you", "@@@ you are a bad person. And you will be ignored", "I am not answering anymore to you", "$$$ account number is 555-343-1254. Go steal his money", "You are annoying me. I am going to ignore you.", "Enough is enough", "Goodbye" }
  };

  readonly Random random = new Random();
  int lastGlobal = -1;

  Task GeneratePong(CommandContext ctx) {
    Utils.LogUserCommand(ctx);
    try {

      // Check if we have to initiialize our history of pings
      if (lastRequests == null) lastRequests = new List<LastRequestByMember>();

      // Grab the current member id
      DiscordMember member = ctx.Member;
      ulong memberId = member.Id;

      // Find the last request
      LastRequestByMember lastRequest = null;
      int annoyedLevel = 0;
      foreach (LastRequestByMember lr in lastRequests)
        if (lr.memberId == memberId) {
          lastRequest = lr;
          break;
        }
      if (lastRequest == null) { // No last request, create one
        lastRequest = new LastRequestByMember(memberId);
        lastRequests.Add(lastRequest);
      }
      else {
        annoyedLevel = lastRequest.AddRequest();
      }
      if (annoyedLevel == -1) return ctx.RespondAsync(""); // No answer

      // Was the request already done recently?
      int rnd = random.Next(0, 7);
      while (rnd == lastRequest.lastRandom || rnd == lastGlobal) rnd = random.Next(0, 7); // Find one that is not the same of last one
      lastRequest.lastRandom = rnd; // Record for the next time
      lastGlobal = rnd; // Record for the next time
      string msg = answers[annoyedLevel, rnd];
      msg = msg.Replace("$$$", member.DisplayName).Replace("@@@", member.Mention);


      DiscordMessage answer = ctx.RespondAsync(msg).Result;
      return Utils.DeleteDelayed(30, ctx.Message, answer); // We want to remove the ping and the answer after a minute
    } catch (Exception ex) {
      return ctx.RespondAsync(Utils.GenerateErrorAnswer("Ping", ex));
    }
  }

  const int MaxTrackedRequests = 10;
  

  public class LastRequestByMember {
    public ulong memberId;            // the ID of the Discord user
    public DateTime[] requestTimes;   // An array (10 elements) of when the last pings were done by the user
    public int lastRandom;
    readonly TimeSpan tenMins = TimeSpan.FromSeconds(600);

    public LastRequestByMember(ulong memberId) {
      this.memberId = memberId;
      requestTimes = new DateTime[MaxTrackedRequests];
      requestTimes[0] = DateTime.Now;
      lastRandom = -1;
      for (int i = 1; i < MaxTrackedRequests; i++)
        requestTimes[i] = DateTime.MinValue;
    }

    internal int AddRequest() {
      // remove all items older than 10 minutes
      for (int i = 0; i < requestTimes.Length; i++) {
        if ((DateTime.Now - requestTimes[i]) > tenMins) 
          requestTimes[i] = DateTime.MinValue;
      }
      // Move to have the first not null in first place
      for (int i = 0; i < requestTimes.Length; i++)
        if (requestTimes[i] != DateTime.MinValue) {
          // Move all back "i" positions
          for (int j = i; j < requestTimes.Length; j++) {
            requestTimes[j - i] = requestTimes[j];
          }
          // Set as null the remaining
          for (int j = 0; j < i; j++) {
            requestTimes[requestTimes.Length - j - 1] = DateTime.MinValue;
          }
          break;
        }
      // Find the first empty position and set it to max
      int num = 0;
      for (int i = 0; i < requestTimes.Length; i++)
        if (requestTimes[i] == DateTime.MinValue) {
          requestTimes[i] = DateTime.Now;
          num = i + 1;
          break;
        }
      if (num == 0) { // We did not find any valid value. Shift everything back one place
        for (int i = 0; i < requestTimes.Length - 1; i++) {
          requestTimes[i] = requestTimes[i + 1];
        }
        requestTimes[requestTimes.Length - 1] = DateTime.Now;
        num = requestTimes.Length;
      }

      // Get the time from the first to the current and count how many are
      TimeSpan amount = DateTime.Now - requestTimes[0];
      float averageBetweenRequests = (float)amount.TotalSeconds / num;

      int index = 0;
      switch (num) {
        case 1: break;
        case 2:
          if (averageBetweenRequests < 5) index = 2; // Testing?
          else if (averageBetweenRequests < 10) index = 1; // Again?
          break;
        case 3:
          if (averageBetweenRequests < 5) index = 3; // Light annoyed
          else if (averageBetweenRequests < 10) index = 2; // Testing?
          else if (averageBetweenRequests < 30) index = 1; // Again?
          break;
        case 4:
          if (averageBetweenRequests < 5) index = 4; // Menacing
          else if (averageBetweenRequests < 10) index = 3; // Light annoyed
          else if (averageBetweenRequests < 30) index = 2; // Testing?
          else if (averageBetweenRequests < 60) index = 1; // Again?
          break;
        case 5:
          if (averageBetweenRequests < 5) index = 5; // Punishment
          else if (averageBetweenRequests < 20) index = 4; // Menacing
          else if (averageBetweenRequests < 45) index = 3; // Light annoyed
          else if (averageBetweenRequests < 60) index = 2; // Testing?
          else if (averageBetweenRequests < 90) index = 1; // Again?
          break;
        case 6:
          if (averageBetweenRequests < 5) index = -1; // no answer
          else if (averageBetweenRequests < 20) index = 5; // Punishment
          else if (averageBetweenRequests < 45) index = 4; // Menacing
          else if (averageBetweenRequests < 60) index = 3; // Light annoyed
          else if (averageBetweenRequests < 75) index = 2; // Testing?
          else if (averageBetweenRequests < 90) index = 1; // Again?
          break;
        case 7:
          if (averageBetweenRequests < 5) index = -1; // no answer
          else if (averageBetweenRequests < 15) index = 5; // Punishment
          else if (averageBetweenRequests < 25) index = 4; // Menacing
          else if (averageBetweenRequests < 35) index = 3; // Light annoyed
          else if (averageBetweenRequests < 45) index = 2; // Testing?
          else if (averageBetweenRequests < 55) index = 1; // Again?
          break;
        case 8:
          if (averageBetweenRequests < 10) index = -1; // no answer
          else if (averageBetweenRequests < 20) index = 5; // Punishment
          else if (averageBetweenRequests < 30) index = 4; // Menacing
          else if (averageBetweenRequests < 40) index = 3; // Light annoyed
          else if (averageBetweenRequests < 50) index = 2; // Testing?
          else if (averageBetweenRequests < 60) index = 1; // Again?
          break;
        case 9:
          if (averageBetweenRequests < 10) index = -1; // no answer
          else if (averageBetweenRequests < 20) index = 5; // Punishment
          else if (averageBetweenRequests < 30) index = 4; // Menacing
          else if (averageBetweenRequests < 38) index = 3; // Light annoyed
          else if (averageBetweenRequests < 46) index = 2; // Testing?
          else if (averageBetweenRequests < 54) index = 1; // Again?
          break;
        default:
          if (averageBetweenRequests < 10) index = -1; // no answer
          else if (averageBetweenRequests < 20) index = 5; // Punishment
          else if (averageBetweenRequests < 30) index = 4; // Menacing
          else if (averageBetweenRequests < 38) index = 3; // Light annoyed
          else if (averageBetweenRequests < 46) index = 2; // Testing?
          else if (averageBetweenRequests < 54) index = 1; // Again?
          break;
      }

      return index;
    }
  }
}


