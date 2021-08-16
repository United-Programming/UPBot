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
public class HelpLanguagesModel : BaseCommandModule {
  private List<LastRequestByMember> lastRequests = new List<LastRequestByMember>();

  private Dictionary<string, string> languageLinks = new Dictionary<string, string>() // video course
  {
        { "C#", "<:csharp:831465428214743060>!\nLink: https://youtu.be/GhQdlIFylQ8" },
        { "C++", "<:cpp:831465408874676273>!\nLink: https://youtu.be/vLnPwxZdW4Y" },
        { "Python", "<:python:831465381016895500>!\nLink: https://youtu.be/rfscVS0vtbw" },
        { "JavaScript", "<:Javascript:876103767068647435>!\nLink: https://youtu.be/PkZNo7MFNFg" },
        { "Java", "<:java:875852276017815634>!\nLink: https://youtu.be/grEKMHGYyns" }
    };
  private Dictionary<string, string> languageCourseLink = new Dictionary<string, string>() // site course 
  {
            { "C#", "<:csharp:831465428214743060>!\nLink: https://www.w3schools.com/cs/"},
            { "C++", "<:cpp:831465408874676273>!\nLink: https://www.w3schools.com/cpp/"},
            { "Python", "<:python:831465381016895500>!\nLink: https://www.w3schools.com/python/"},
            { "JavaScript", "<:Javascript:876103767068647435>!\nLink: https://www.w3schools.com/js/" },
            { "Java", "<:java:875852276017815634>!\nLink: https://www.w3schools.com/java/" }
    };


  private Dictionary<string, string> colors = new Dictionary<string, string>() // colors for embed
  {
        { "C#", "812f84" },
        { "C++", "3f72db" },
        { "Python", "d1e13b" },
        { "JavaScript", "f8ff00"},
        { "Java", "e92c2c" }
    };

  private string[] helpfulAnswers = // words for video course
  {
        "Hello! @@@, here is a good video tutorial about ///",
        "Hey! hey! @@@, here is a sick video tutorial about ///",
        "Hello @@@, here is your video tutorial ///"
    };
  private string[] helpFulCourseAnswers = { // words for site course
        "Hello! @@@, here is a good course about ///",
        "Hey! hey! @@@, here is a sick course about ///",
        "Hello @@@, here is your course ///"
    };

  [Description("Gives good tutorials on specific language.")] // for \help helplanguage info
  [Command("helplanguage")]
  public async Task ErrorMessage(CommandContext ctx) {
    DiscordEmbedBuilder deb = new DiscordEmbedBuilder();
    deb.Title = "Help Language - How To Use";
    deb.WithColor(new DiscordColor("ff0025"));

    DiscordMember member = ctx.Member;
    deb.Description = member.Mention + " , if you want to get video course about specific language type: `\\helplanguage video C#`" +
        " \nIf you want to get full online course about specific language type: \n`\\helplanguage course C#`" +
        " \nAvailable languages: `ะก#, C++, Python, JavaScript, Java`";
    await ctx.RespondAsync(deb.Build()); // \helplanguage 
  }

  [Description("For use this command you should type `\\helplanguage C#`")]
  [Command("helplanguage")]
  public async Task HelpCommand(CommandContext ctx, [Description("Choose what you want video or course on specific language.")] string TypeOfHelp, [Description("As string `<lang>` put the name of language that you want to learn")] string lang) // C#
  {
    UtilityFunctions.LogUserCommand(ctx);
    lang = NormalizeLanguage(lang);

    if (lang == null)
      await ErrorMessage(ctx);
    else if (TypeOfHelp.ToLowerInvariant() == "video") {
      if (!languageLinks.ContainsKey(lang)) {
        await ErrorMessage(ctx);
      }
      else
        await GenerateHelpfulAnswer(ctx, lang);
    }
    else if (TypeOfHelp.ToLowerInvariant() == "course") {
      if (!languageCourseLink.ContainsKey(lang)) {
        await ErrorMessage(ctx);
      }
      else
        await GenerateHelpfulAnswerCourse(ctx, lang);
    }
  }

  private string NormalizeLanguage(string language) {
    if (language == null) return null;
    language = language.ToLowerInvariant();
    switch (language) {
      case "c++": return "C++";
      case "cpp": return "C++";
      case "cp": return "C++";
      case "cs": return "C#";
      case "csharp": return "C#";
      case "c#": return "C#";
      case "java": return "Java";
      case "javascript": return "JavaScript";
      case "jscript": return "JavaScript";
      case "js": return "JavaScript";
      case "python": return "Python";
      case "phyton": return "Python";
      case "py": return "Python";
    }
    return language;
  }


  private Task GenerateHelpfulAnswer(CommandContext ctx, string language) {
    DiscordMember member = ctx.Member;
    ulong memberId = member.Id;
    DiscordEmbedBuilder deb = new DiscordEmbedBuilder();

    deb.Title = $"Help Language - {language}";
    deb.WithColor(new DiscordColor(colors[language]));

    // Find the last request
    LastRequestByMember lastRequest = null;
    foreach (LastRequestByMember lr in lastRequests) {
      if (lr.MemberId == memberId) {
        lastRequest = lr;
        break;
      }
    }

    if (lastRequest == null) // No last request, create one
    {
      lastRequest = new LastRequestByMember { MemberId = memberId, DateTime = DateTime.Now, Num = 0 };
      lastRequests.Add(lastRequest);
    }
    else {
      lastRequest.DateTime = DateTime.Now;
      lastRequest.Num++;
      if (lastRequest.Num >= helpfulAnswers.Length)
        lastRequest.Num = 0;
    }
    string msg = helpfulAnswers[lastRequest.Num];
    msg = msg.Replace("$$$", member.DisplayName).Replace("@@@", member.Mention)
          .Replace("///", languageLinks[language]);
    deb.Description = msg;
    return ctx.RespondAsync(deb.Build());
  }
  private Task GenerateHelpfulAnswerCourse(CommandContext ctx, string rawLang) {

    DiscordMember member = ctx.Member;
    ulong memberId = member.Id;
    DiscordEmbedBuilder deb = new DiscordEmbedBuilder();

    string language = "";
    if (rawLang == "cpp" || rawLang == "CPP" || rawLang == "c++" || rawLang == "C++") // all possible words to type (including the same words as command one)
    {
      language = rawLang.ToUpperInvariant();
      language = rawLang = "C++";
    }
    else if (rawLang == "c#" || rawLang == "C#" || rawLang == "csharp" || rawLang == "CSharp") {
      language = rawLang.ToUpperInvariant();
      language = rawLang = "C#";
    }
    else if (rawLang == "python" || rawLang == "Python" || rawLang == "py" || rawLang == "PY") {
      language = rawLang = "Python";
    }
    else if (rawLang == "js" || rawLang == "JS" || rawLang == "javascript" || rawLang == "JavaScript") {
      language = rawLang = "JavaScript";
    }
    else if (rawLang == "java") {
      language = rawLang = "Java";
    }
    else
      language = char.ToUpperInvariant(rawLang[0]) + rawLang.Substring(1);

    deb.Title = $"Help Language - {language}";
    deb.WithColor(new DiscordColor(colors[rawLang]));

    // Find the last request
    LastRequestByMember lastRequest = null;
    foreach (LastRequestByMember lr in lastRequests) {
      if (lr.MemberId == memberId) {
        lastRequest = lr;
        break;
      }
    }

    if (lastRequest == null) // No last request, create one
    {
      lastRequest = new LastRequestByMember { MemberId = memberId, DateTime = DateTime.Now, Num = 0 };
      lastRequests.Add(lastRequest);
    }
    else {
      lastRequest.DateTime = DateTime.Now;
      lastRequest.Num++;
      if (lastRequest.Num >= helpFulCourseAnswers.Length)
        lastRequest.Num = 0;
    }
    string msg = helpFulCourseAnswers[lastRequest.Num];
    msg = msg.Replace("$$$", member.DisplayName).Replace("@@@", member.Mention)
          .Replace("///", languageCourseLink[rawLang]);
    deb.Description = msg;
    return ctx.RespondAsync(deb.Build());
  }

  public class LastRequestByMember {
    public ulong MemberId { get; set; }
    public DateTime DateTime { get; set; }
    public int Num { get; set; }
  }
}
