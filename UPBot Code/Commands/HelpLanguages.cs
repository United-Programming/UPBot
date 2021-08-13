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

public class HelpLanguagesModel : BaseCommandModule
{
    private List<LastRequestByMember> lastRequests = null;

    [Command("helplanguage")]
    public async Task ErrorMessage(CommandContext ctx) {
        DiscordMember member = ctx.Member;
        await ctx.RespondAsync(member.Mention + "Available commands: c#, c++, phyton, java. \n Write command like this: `/helplanguage c#`");
    }
    [Command("helplanguage c#")]
    public async Task HelpCommand(CommandContext ctx) // c#
    {
        await GenerateHelpfulAnswer(ctx);
    }
    [Command("helplanguage c++")]
    public async Task HelpCommand1(CommandContext ctx) //c++
    {
        await GenerateHelpfulAnswer1(ctx);
    }
    [Command("helplanguage phyton")]
    public async Task HelpCommand2(CommandContext ctx) // phyton
    {
        await GenerateHelpfulAnswer2(ctx);
    }
    [Command("helplanguage java")]
    public async Task HelpCommand3(CommandContext ctx) // java
    {
        await GenerateHelpfulAnswer3(ctx);
    }

    string[] helpfulAnswersCsharp =
    {
        "Hello! @@@, here some good tutorial about :CSharp:! \n Link: \n C# :CSharp: :https://youtu.be/GhQdlIFylQ8, 
        "Hey! hey! @@@, here some sick tutorial about :CSharp:! \n Link: C# :CSharp: :https://youtu.be/GhQdlIFylQ8
    };
    string[] helpfulAnswersCplusplus =
{
        "Hello! @@@, here some good tutorial about :CPP:! \n Link: \n C++ :CPP: :https://youtu.be/vLnPwxZdW4Y, 
        "Hey! hey! @@@, here some basic tutorial about :CPP:! \n Link: C++ :CPP: :https://youtu.be/vLnPwxZdW4Y
    };
    string[] helpfulAnswersPhyton =
{
        "Hello! @@@, have a good one tutorial about how to code on :Phyton:! \n Link:\n Phyton :Phyton: :https://youtu.be/rfscVS0vtbw, 
        "Hey! hey! @@@, here some good simple course about :Phyton: language! \n Link: Phyton :Phyton: :https://youtu.be/rfscVS0vtbw
    };
    string[] helpfulAnswersJava =
{
        "Hello! @@@, here some good tutorial about how to code on :Java:! \n Link: \n Java :Java: :https://youtu.be/grEKMHGYyns, 
        "Hey! hey! @@@, here some sick tutorial about how to code on :Java:! \n Link: Java :Java: :https://youtu.be/grEKMHGYyns
    };

    Task GenerateHelpfulAnswer(CommandContext ctx)
    {

        // Check if we have to initiialize our history of pings
        if (lastRequests == null) lastRequests = new List<LastRequestByMember>();

        // Grab the current member id
        DiscordMember member = ctx.Member;
        ulong memberId = member.Id;

        // Find the last request
        LastRequestByMember lastRequest = null;
        foreach (LastRequestByMember lr in lastRequests)
            if (lr.memberId == memberId)
            {
                lastRequest = lr;
                break;
            }
        if (lastRequest == null)
        { // No last request, create one
            lastRequest = new LastRequestByMember { memberId = memberId, dateTime = DateTime.Now, num = 0 };
            lastRequests.Add(lastRequest);
        }
        else
        {
            lastRequest.dateTime = DateTime.Now;
            // Increase the number
            lastRequest.num++;
            if (lastRequest.num >= helpfulAnswersCsharp.Length) lastRequest.num = 0;
        }

        string msg = helpfulAnswersCsharp[lastRequest.num];
        msg = msg.Replace("$$$", member.DisplayName).Replace("@@@", member.Mention);
        return ctx.RespondAsync(msg);
    }
    Task GenerateHelpfulAnswer1(CommandContext ctx)
    {

        // Check if we have to initiialize our history of pings
        if (lastRequests == null) lastRequests = new List<LastRequestByMember>();

        // Grab the current member id
        DiscordMember member = ctx.Member;
        ulong memberId = member.Id;

        // Find the last request
        LastRequestByMember lastRequest = null;
        foreach (LastRequestByMember lr in lastRequests)
            if (lr.memberId == memberId)
            {
                lastRequest = lr;
                break;
            }
        if (lastRequest == null)
        { // No last request, create one
            lastRequest = new LastRequestByMember { memberId = memberId, dateTime = DateTime.Now, num = 0 };
            lastRequests.Add(lastRequest);
        }
        else
        {
            lastRequest.dateTime = DateTime.Now;
            // Increase the number
            lastRequest.num++;
            if (lastRequest.num >= helpfulAnswersCplusplus.Length) lastRequest.num = 0;
        }

        string msg = helpfulAnswersCplusplus[lastRequest.num];
        msg = msg.Replace("$$$", member.DisplayName).Replace("@@@", member.Mention);
        return ctx.RespondAsync(msg);
    }
    Task GenerateHelpfulAnswer2(CommandContext ctx)
    {

        // Check if we have to initiialize our history of pings
        if (lastRequests == null) lastRequests = new List<LastRequestByMember>();

        // Grab the current member id
        DiscordMember member = ctx.Member;
        ulong memberId = member.Id;

        // Find the last request
        LastRequestByMember lastRequest = null;
        foreach (LastRequestByMember lr in lastRequests)
            if (lr.memberId == memberId)
            {
                lastRequest = lr;
                break;
            }
        if (lastRequest == null)
        { // No last request, create one
            lastRequest = new LastRequestByMember { memberId = memberId, dateTime = DateTime.Now, num = 0 };
            lastRequests.Add(lastRequest);
        }
        else
        {
            lastRequest.dateTime = DateTime.Now;
            // Increase the number
            lastRequest.num++;
            if (lastRequest.num >= helpfulAnswersPhyton.Length) lastRequest.num = 0;
        }

        string msg = helpfulAnswersPhyton[lastRequest.num];
        msg = msg.Replace("$$$", member.DisplayName).Replace("@@@", member.Mention);
        return ctx.RespondAsync(msg);
    }
    Task GenerateHelpfulAnswer3(CommandContext ctx)
    {

        // Check if we have to initiialize our history of pings
        if (lastRequests == null) lastRequests = new List<LastRequestByMember>();

        // Grab the current member id
        DiscordMember member = ctx.Member;
        ulong memberId = member.Id;

        // Find the last request
        LastRequestByMember lastRequest = null;
        foreach (LastRequestByMember lr in lastRequests)
            if (lr.memberId == memberId)
            {
                lastRequest = lr;
                break;
            }
        if (lastRequest == null)
        { // No last request, create one
            lastRequest = new LastRequestByMember { memberId = memberId, dateTime = DateTime.Now, num = 0 };
            lastRequests.Add(lastRequest);
        }
        else
        {
            lastRequest.dateTime = DateTime.Now;
            // Increase the number
            lastRequest.num++;
            if (lastRequest.num >= helpfulAnswersJava.Length) lastRequest.num = 0;
        }

        string msg = helpfulAnswersJava[lastRequest.num];
        msg = msg.Replace("$$$", member.DisplayName).Replace("@@@", member.Mention);
        return ctx.RespondAsync(msg);
    }


    public class LastRequestByMember
    {
        public ulong memberId;
        public DateTime dateTime;
        public int num;
    }
}
