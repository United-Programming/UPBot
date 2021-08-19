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

  private Dictionary<string, LanguageInfo> languages = new Dictionary<string, LanguageInfo>()
  {
    { "C#", new LanguageInfo("<:csharp:831465428214743060>!\nLink: https://youtu.be/GhQdlIFylQ8",
        "<:csharp:831465428214743060>!\nLink: https://www.w3schools.com/cs/", "#812f84")
    },

    { "C++", new LanguageInfo("<:cpp:831465408874676273>!\nLink: https://youtu.be/vLnPwxZdW4Y",
        "<:cpp:831465408874676273>!\nLink: https://www.w3schools.com/cpp/", "#3f72db")
    },

    { "Python", new LanguageInfo("<:python:831465381016895500>!\nLink: https://youtu.be/rfscVS0vtbw",
      "<:python:831465381016895500>!\nLink: https://www.w3schools.com/python/", "#d1e13b")
    },

    { "JavaScript", new LanguageInfo("<:Javascript:876103767068647435>!\nLink: https://youtu.be/PkZNo7MFNFg",
      "<:Javascript:876103767068647435>!\nLink: https://www.w3schools.com/js/", "#f8ff00")
    },

    { "Java", new LanguageInfo("<:java:875852276017815634>!\nLink: https://youtu.be/grEKMHGYyns",
      "<:java:875852276017815634>!\nLink: https://www.w3schools.com/java/", "#e92c2c")
    }
  };

  private string[] helpfulAnswers = // words for video course
  {
        "Hello! @@@, here is a good video tutorial about ///",
        "Hey! hey! @@@, here is a sick video tutorial about ///",
        "Hello @@@, here is your video tutorial ///"
  };

  [Command("helplanguage")]
  public async Task ErrorMessage(CommandContext ctx) {
    string title = "Help Language - How To Use";

    DiscordMember member = ctx.Member;
    string description = member.Mention + " , if you want to get video course about specific language type: `helplanguage video C#`" +
        " \nIf you want to get full online course about specific language type: \n`helplanguage course C#`" +
        " \nAvailable languages: `ะก#, C++, Python, JavaScript, Java`";
    await Utils.BuildEmbedAndExecute(title, description, Utils.Red, ctx, true);
  }

  [Command("helplanguage")]
  [Description("Gives good tutorials on specific language.\n**Usage**: `helplanguage language`")]
  public async Task HelpCommand(CommandContext ctx, [Description("Choose what you want video or course on specific language.")] string typeOfHelp, [Description("As string `<lang>` put the name of language that you want to learn")] string lang) // C#
  {
    Utils.LogUserCommand(ctx);
    try {
    lang = NormalizeLanguage(lang);

    if (lang == null)
    {
      await ErrorMessage(ctx);
      return;
    }
    
    if (!languages.ContainsKey(lang)) {
      await ErrorMessage(ctx);
      return;
    }

    bool course = typeOfHelp.ToLowerInvariant() != "video";
    await GenerateHelpfulAnswer(ctx, lang, course);
    } catch (Exception ex) {
      await ctx.RespondAsync(Utils.GenerateErrorAnswer("WhoIs", ex));
    }
  }

  private string NormalizeLanguage(string language) {
    if (language == null) return null;
    language = language.ToLowerInvariant();
    switch (language) {
      case "c++":
      case "cpp":
      case "cp": 
        return "C++";
      case "cs":
      case "csharp":
      case "c#": 
        return "C#";
      case "java": 
        return "Java";
      case "javascript":
      case "jscript":
      case "js": 
        return "JavaScript";
      case "python":
      case "phyton":
      case "py": 
        return "Python";
      default:
        return char.ToUpperInvariant(language[0]) + language.Substring(1);
    }
  }
  
  private async Task GenerateHelpfulAnswer(CommandContext ctx, string language, bool isCourse) {
    try{
    DiscordMember member = ctx.Member;
    ulong memberId = member.Id;

    string title = $"Help Language - {language}";

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

    string link = isCourse ? languages[language].CourseLink :languages[language].VideoLink;
    string msg = helpfulAnswers[lastRequest.Num];
    msg = msg.Replace("$$$", member.DisplayName).Replace("@@@", member.Mention).Replace("///", link);
    if (isCourse)
      msg = msg.Replace("video", "course");

    await Utils.BuildEmbedAndExecute(title, msg, languages[language].Color, ctx, true);
    } catch (Exception ex) {
      await ctx.RespondAsync(Utils.GenerateErrorAnswer("HelpLanguage", ex));
    }
  }

}
