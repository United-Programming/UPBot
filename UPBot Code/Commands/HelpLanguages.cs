using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

/// <summary>
/// This command shows for the new users useful links to how to code on specific language.
/// Command author: J0nathan550 Source Code: CPU
/// </summary>
public class HelpLanguagesModel : BaseCommandModule {
  private List<LastRequestByMember> lastRequests = null;

  [Command("helplanguages")]
  public async Task HelpCommand(CommandContext ctx) {
    await GenerateHelpfulAnswer(ctx);
  }

  string[] helpfulAnswers =
  {
        "Hello! @@@, here some good tutorials on languages that you want to learn. \n Links: \n C#:https://youtu.be/GhQdlIFylQ8 \n C++: https://youtu.be/vLnPwxZdW4Y \n Phyton: https://youtu.be/rfscVS0vtbw \n Java: https://youtu.be/grEKMHGYyns",
        "Hey! hey! @@@, here some good tutorials on languages that you want to learn. \n Links: C#:https://youtu.be/GhQdlIFylQ8 \n C++: https://youtu.be/vLnPwxZdW4Y \n Phyton: https://youtu.be/rfscVS0vtbw \n Java: https://youtu.be/grEKMHGYyns"
    };

  Task GenerateHelpfulAnswer(CommandContext ctx) {

    // Check if we have to initiialize our history of pings
    if (lastRequests == null) lastRequests = new List<LastRequestByMember>();

    // Grab the current member id
    DiscordMember member = ctx.Member;
    ulong memberId = member.Id;

    // Find the last request
    LastRequestByMember lastRequest = null;
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
      lastRequest.dateTime = DateTime.Now;
      // Increase the number
      lastRequest.num++;
      if (lastRequest.num >= helpfulAnswers.Length) lastRequest.num = 0;
    }

    string msg = helpfulAnswers[lastRequest.num];
    msg = msg.Replace("$$$", member.DisplayName).Replace("@@@", member.Mention);
    return ctx.RespondAsync(msg);
  }

  public class LastRequestByMember {
    public ulong memberId;
    public DateTime dateTime;
    public int num;
  }
}
