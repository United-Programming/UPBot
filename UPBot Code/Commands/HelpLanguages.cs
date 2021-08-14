using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

/// <summary>
/// This command shows for the new users useful links to how to code on specific language.
/// Command author: J0nathan550, Source Code: CPU
/// </summary>
public class HelpLanguagesModel : BaseCommandModule
{
    private List<LastRequestByMember> lastRequests = new List<LastRequestByMember>();
    private Dictionary<string, string> languageLinks = new Dictionary<string, string>()
    {
        { "c#", "<:csharp:831465428214743060>!\nLink: https://youtu.be/GhQdlIFylQ8" }, 
        { "c++", "<:cpp:831465408874676273>!\nLink: https://youtu.be/vLnPwxZdW4Y" },
        { "python", "<:python:831465381016895500>!\nLink: https://youtu.be/rfscVS0vtbw" },
        { "java", "<:java:875852276017815634>!\nLink: https://youtu.be/grEKMHGYyns" }
    };

    private Dictionary<string, string> colors = new Dictionary<string, string>()
    {
        { "c#", "812f84" },
        { "c++", "3f72db" },
        { "python", "d1e13b" },
        { "java", "e92c2c" }
    };
    
    private string[] helpfulAnswers =
    {
        "Hello! @@@, here is a good tutorial about ///",
        "Hey! hey! @@@, here is a sick tutorial about ///"
    };

    [Command("helplanguage")]
    public async Task ErrorMessage(CommandContext ctx)
    {
        DiscordEmbedBuilder deb = new DiscordEmbedBuilder();
        deb.Title = "Help Language - How To Use";
        deb.WithColor(new DiscordColor("ff0025"));
        
        DiscordMember member = ctx.Member;
        deb.Description = member.Mention + " , Available commands: c#, c++, python, java. \n Write command like this: `/helplanguage c#`";
        await ctx.RespondAsync(deb.Build());
    }
    
    [Command("helplanguage")]
    public async Task HelpCommand(CommandContext ctx, string lang) // C#
    {
        if (lang == null)
            await ErrorMessage(ctx);

        lang = lang.Trim().ToLowerInvariant();

        if (!languageLinks.ContainsKey(lang))
        {
            await ErrorMessage(ctx);
        }
        
        await GenerateHelpfulAnswer(ctx, lang);
    }

    private Task GenerateHelpfulAnswer(CommandContext ctx, string rawLang)
    {
        DiscordMember member = ctx.Member;
        ulong memberId = member.Id;
        DiscordEmbedBuilder deb = new DiscordEmbedBuilder();

        string language = "";
        if (rawLang == "cpp")
        {
            language = rawLang.ToUpperInvariant();
            rawLang = "c++";
        }
        else
            language = char.ToUpperInvariant(rawLang[0]) + rawLang.Substring(1);
        
        deb.Title = $"Help Language - {language}";
        deb.WithColor(new DiscordColor(colors[rawLang]));

        // Find the last request
        LastRequestByMember lastRequest = null;
        foreach (LastRequestByMember lr in lastRequests)
        {
            if (lr.MemberId == memberId)
            {
                lastRequest = lr;
                break;
            }
        }
        
        if (lastRequest == null) // No last request, create one
        {
            lastRequest = new LastRequestByMember { MemberId = memberId, DateTime = DateTime.Now, Num = 0 };
            lastRequests.Add(lastRequest);
        }
        else
        {
            lastRequest.DateTime = DateTime.Now;
            lastRequest.Num++;
            if (lastRequest.Num >= helpfulAnswers.Length) 
                lastRequest.Num = 0;
        }
        
        string msg = helpfulAnswers[lastRequest.Num];
        msg = msg.Replace("$$$", member.DisplayName).Replace("@@@", member.Mention)
              .Replace("///", languageLinks[rawLang]);
        deb.Description = msg;
        return ctx.RespondAsync(deb.Build());
    }

    public class LastRequestByMember
    {
        public ulong MemberId { get; set; }
        public DateTime DateTime { get; set; }
        public int Num { get; set; }
    }
}
