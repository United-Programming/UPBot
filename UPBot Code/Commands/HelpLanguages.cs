using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

/// This command shows for the new users useful links to how to code on specific language.
/// Command author: J0nathan550 Source Code: CPU
/// </summary>
public class HelpLanguagesModel : BaseCommandModule
{
    private List<LastRequestByMember> lastRequests = null;

    [Command("helplanguage")]
    public async Task ErrorMessage(CommandContext ctx)
    {
        DiscordMember member = ctx.Member;
        DiscordEmbedBuilder deb = new DiscordEmbedBuilder();
        deb.Title = "Help Language - How To Use";
        deb.WithColor(new DiscordColor("ff0025"));
        await ctx.RespondAsync(member.Mention + " , Available commands: c#, c++, python, java. \n Write command like this: `/helplanguage c#`");
        return ctx.RespondAsync(deb.Build());
    }
    [Command("helplanguage")]
    public async Task HelpCommand(CommandContext ctx, string lang) // c#
    {
        if (lang == null)
        {
            ErrorMessage();
            return;
        }
        lang = lang.Trim().ToLowerInvariant();
        if (lang == "c#")await GenerateHelpfulAnswerCsharp(ctx);
        else if (lang == "c++")await GenerateHelpfulAnswerCpp(ctx);
        else if (lang == "cpp")await GenerateHelpfulAnswerCpp(ctx);
        else if (lang == "python")await GenerateHelpfulAnswerPython(ctx);
        else if (lang == "java") await GenerateHelpfulAnswerJava(ctx);
        else await ctx.RespondAsync(ctx.Member.Mention + "Available commands: c#, c++, python, java. \n Write command like this: `/helplanguage c#`");
    }

    string[] helpfulAnswersCsharp =
    {
        "Hello! @@@, here some good tutorial about <:csharp:831465428214743060>!\nLink:https://youtu.be/GhQdlIFylQ8",
        "Hey! hey! @@@, here some sick tutorial about <:csharp:831465428214743060>!\nLink:https://youtu.be/GhQdlIFylQ8"
    };
    string[] helpfulAnswersCplusplus =
    {
        "Hello! @@@, here some good tutorial about <:cpp:831465408874676273>!\nLink:https://youtu.be/vLnPwxZdW4Y",
        "Hey! hey! @@@, here some basic tutorial about <:cpp:831465408874676273>!\nLink:https://youtu.be/vLnPwxZdW4Y"
    };
    string[] helpfulAnswersPython =
    {
        "Hello! @@@, have a good one tutorial about how to code on <:python:831465381016895500>!\nLink:https://youtu.be/rfscVS0vtbw",
        "Hey! hey! @@@, here some good simple course about <:python:831465381016895500>!\nLink:https://youtu.be/rfscVS0vtbw"
    };
    string[] helpfulAnswersJava =
    {
        "Hello! @@@, here some good tutorial about how to code on <:java:875852276017815634>!\nLink:https://youtu.be/grEKMHGYyns",
        "Hey! hey! @@@, here some sick tutorial about how to code on <:java:875852276017815634>!\nLink:https://youtu.be/grEKMHGYyns"
    };

    Task GenerateHelpfulAnswerCsharp(CommandContext ctx)
    {

        // Check if we have to initiialize our history of pings
        if (lastRequests == null) lastRequests = new List<LastRequestByMember>();

        // Grab the current member id
        DiscordMember member = ctx.Member;
        DiscordEmbedBuilder deb = new DiscordEmbedBuilder();
        ulong memberId = member.Id;

        deb.Title = "Help Language - C#";
        deb.WithColor(new DiscordColor("812f84"));

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
        deb.Description = msg;
        return ctx.RespondAsync(deb.Build());
    }
    Task GenerateHelpfulAnswerCpp(CommandContext ctx)
    {
        // Check if we have to initiialize our history of pings
        if (lastRequests == null) lastRequests = new List<LastRequestByMember>();

        // Grab the current member id
        DiscordMember member = ctx.Member;
        DiscordEmbedBuilder deb = new DiscordEmbedBuilder();
        ulong memberId = member.Id;

        deb.Title = "Help Language - C++";
        deb.WithColor(new DiscordColor("3f72db"));

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
        deb.Description = msg;
        return ctx.RespondAsync(deb.Build());
    }
    Task GenerateHelpfulAnswerPython(CommandContext ctx)
    {

        // Check if we have to initiialize our history of pings
        if (lastRequests == null) lastRequests = new List<LastRequestByMember>();

        // Grab the current member id
        DiscordMember member = ctx.Member;
        DiscordEmbedBuilder deb = new DiscordEmbedBuilder();
        ulong memberId = member.Id;

        deb.Title = "Help Language - Python";
        deb.WithColor(new DiscordColor("d1e13b"));

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
            if (lastRequest.num >= helpfulAnswersPython.Length) lastRequest.num = 0;
        }
        string msg = helpfulAnswersPython[lastRequest.num];
        msg = msg.Replace("$$$", member.DisplayName).Replace("@@@", member.Mention);
        deb.Description = msg;
        return ctx.RespondAsync(deb.Build());
    }
    Task GenerateHelpfulAnswerJava(CommandContext ctx)
    {

        // Check if we have to initiialize our history of pings
        if (lastRequests == null) lastRequests = new List<LastRequestByMember>();

        // Grab the current member id
        DiscordMember member = ctx.Member;
        DiscordEmbedBuilder deb = new DiscordEmbedBuilder();
        ulong memberId = member.Id;

        deb.Title = "Help Language - Java";
        deb.WithColor(new DiscordColor("e92c2c"));

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
        deb.Description = msg;
        return ctx.RespondAsync(deb.Build());
    }

    public class LastRequestByMember
    {
        public ulong memberId;
        public DateTime dateTime;
        public int num;
    }
}
