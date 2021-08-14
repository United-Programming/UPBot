using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

/// This command shows for the new users useful links to how to code on specific language.
/// Command author: J0nathan550 Source Code: CPU
/// </summary>
public class HelpLanguagesModel : BaseCommandModule {
  private List<LastRequestByMember> lastRequests = null;

  [Command("helplanguage")]
  public async Task ErrorMessage(CommandContext ctx) {
    DiscordMember member = ctx.Member;
    await ctx.RespondAsync(member.Mention + " , Available commands: c#, c++, phyton, java. \n Write command like this: `/helplanguage c#`");
  }
  [Command("helplanguage")]
  public async Task HelpCommand(CommandContext ctx, string lang) // c#
  {
    if (lang == null) {
      await ctx.RespondAsync(ctx.Member.Mention + " , Available commands: c#, c++, phyton, java. \n Write command like this: `/helplanguage c#`");
      return;
    }
    lang = lang.Trim().ToLowerInvariant();
    if (lang=="c#") await GenerateHelpfulAnswer(ctx);
    else if (lang == "c++") await GenerateHelpfulAnswer1(ctx);
    else if (lang == "cpp") await GenerateHelpfulAnswer1(ctx);
    else if (lang == "phyton") await GenerateHelpfulAnswer2(ctx);
    else if (lang == "java") await GenerateHelpfulAnswer3(ctx);
    else await ctx.RespondAsync(ctx.Member.Mention + "Available commands: c#, c++, phyton, java. \n Write command like this: `/helplanguage c#`");
  }

  string[] helpfulAnswersCsharp =
  {
        "Hello! @@@, here some good tutorial about <:csharp:831465428214743060>! \n Link: \n<:csharp:831465428214743060>:https://youtu.be/GhQdlIFylQ8",
        "Hey! hey! @@@, here some sick tutorial about <:csharp:831465428214743060>! \n Link:<:csharp:831465428214743060>:https://youtu.be/GhQdlIFylQ8"
    };
  string[] helpfulAnswersCplusplus =
{
        "Hello! @@@, here some good tutorial about <:cpp:831465408874676273>! \n Link: \n<:cpp:831465408874676273>:https://youtu.be/vLnPwxZdW4Y",
        "Hey! hey! @@@, here some basic tutorial about <:cpp:831465408874676273>! \n Link:<:cpp:831465408874676273>:https://youtu.be/vLnPwxZdW4Y"
    };
  string[] helpfulAnswersPhyton =
{
        "Hello! @@@, have a good one tutorial about how to code on <:python:831465381016895500>! \n <:python:831465381016895500>:https://youtu.be/rfscVS0vtbw",
        "Hey! hey! @@@, here some good simple course about <:python:831465381016895500> language! \n<:python:831465381016895500>:https://youtu.be/rfscVS0vtbw"
    };
  string[] helpfulAnswersJava =
{
        "Hello! @@@, here some good tutorial about how to code on <:java:875852276017815634>!\n Link: \n<:java:875852276017815634>:https://youtu.be/grEKMHGYyns",
        "Hey! hey! @@@, here some sick tutorial about how to code on <:java:875852276017815634>! \n Link:<:java:875852276017815634>:https://youtu.be/grEKMHGYyns"
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
      if (lastRequest.num >= helpfulAnswersCsharp.Length) lastRequest.num = 0;
    }

    string msg = helpfulAnswersCsharp[lastRequest.num];
    msg = msg.Replace("$$$", member.DisplayName).Replace("@@@", member.Mention);
    return ctx.RespondAsync(msg);
  }
  Task GenerateHelpfulAnswer1(CommandContext ctx) {
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
      if (lastRequest.num >= helpfulAnswersCplusplus.Length) lastRequest.num = 0;
    }

    string msg = helpfulAnswersCplusplus[lastRequest.num];
    msg = msg.Replace("$$$", member.DisplayName).Replace("@@@", member.Mention);
    return ctx.RespondAsync(msg);
  }
  Task GenerateHelpfulAnswer2(CommandContext ctx) {

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
      if (lastRequest.num >= helpfulAnswersPhyton.Length) lastRequest.num = 0;
    }

    string msg = helpfulAnswersPhyton[lastRequest.num];
    msg = msg.Replace("$$$", member.DisplayName).Replace("@@@", member.Mention);
    return ctx.RespondAsync(msg);
  }
  Task GenerateHelpfulAnswer3(CommandContext ctx) {

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
      if (lastRequest.num >= helpfulAnswersJava.Length) lastRequest.num = 0;
    }

    string msg = helpfulAnswersJava[lastRequest.num];
    msg = msg.Replace("$$$", member.DisplayName).Replace("@@@", member.Mention);
    return ctx.RespondAsync(msg);
  }


  public class LastRequestByMember {
    public ulong memberId;
    public DateTime dateTime;
    public int num;
  }
    //public class EmojiDiscord {
    //    public string Csharp = "<:csharp:831465428214743060>";
    //    public string CPP = "<:cpp:831465408874676273>";
    //    public string Java = "<:java:875852276017815634>";
    //    public string Phyton = "<:python:831465381016895500>";
    //}
}
