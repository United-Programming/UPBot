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

    [Command("helplanguages")]
    public async Task HelpCommand(CommandContext ctx)
    {
        await GeneratePong(ctx);
    }

    string[] helpfulAnswers
    {
        "Hello! @@@, here some good tutorials on languages that you want to learn. \n Links: \n C#:https://youtu.be/GhQdlIFylQ8 \n C++: https://youtu.be/vLnPwxZdW4Y \n Phyton: https://youtu.be/rfscVS0vtbw \n Java: https://youtu.be/grEKMHGYyns",
        "Hey! hey! @@@, here some good tutorials on languages that you want to learn. \n Links: C#:https://youtu.be/GhQdlIFylQ8 \n C++: https://youtu.be/vLnPwxZdW4Y \n Phyton: https://youtu.be/rfscVS0vtbw \n Java: https://youtu.be/grEKMHGYyns"
    };

    Random random = new Random();
    TimeSpan tenMins = TimeSpan.FromSeconds(600);
    TimeSpan oneMin = TimeSpan.FromSeconds(60);
    TimeSpan thirtySecs = TimeSpan.FromSeconds(30);

    Task GeneratePong(CommandContext ctx)
    {

        // Check if we have to initiialize our history of pings
        if (lastRequests == null) lastRequests = new List<LastRequestByMember>();

        // Grab the current member id
        DiscordMember member = ctx.Member;
        ulong memberId = member.Id;

        // Find the last request
        LastRequestByMember lastRequest = null;
        TimeSpan timeSpan = TimeSpan.Zero;
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
            timeSpan = DateTime.Now - lastRequest.dateTime;
            lastRequest.dateTime = DateTime.Now;
        }

        // Was the request already done recently?
        string msg;
        if (timeSpan == TimeSpan.Zero || timeSpan > tenMins)
        { // No request or more than 10 minutes ago
            msg = helpfulAnswers[random.Next(0, helpfulAnswers.Length)];
            msg = msg.Replace("$$$", member.DisplayName).Replace("@@@", member.Mention);
            lastRequest.num = 0;
            return ctx.RespondAsync(msg);
        }

        // Increase the number
        lastRequest.num++;
        if (lastRequest.num < 2)
        {
            msg = helpfulAnswers[random.Next(0, helpfulAnswers.Length)];
            msg = msg.Replace("$$$", member.DisplayName).Replace("@@@", member.Mention);
            msg += " Eh? You want link again? here sure.";
            return ctx.RespondAsync(msg);
        }
    }

    public class LastRequestByMember
    {
        public ulong memberId;
        public DateTime dateTime;
        public int num;
    }
}
