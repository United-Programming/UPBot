using System;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus;
/// <summary>
/// Command used to refactor as codeblock some code pasted by a user
/// author: CPU
/// </summary>
public class Refactor : BaseCommandModule {

  [Command("checklanguage")]
  [Description("Check what language is in the last post or in the post you replied to")]
  public async Task CheckLanguage(CommandContext ctx) { // Refactors the previous post, if it is code
    await RefactorCode(ctx, null, "best");
  }

  [Command("checklanguage")]
  [Description("Check what language is in the last post of the user")]
  public async Task CheckLanguage(CommandContext ctx, [Description("The user the posted the message to check")] DiscordMember member) { // Refactors the previous post, if it is code
    await RefactorCode(ctx, member, "best");
  }

  [Command("reformat")]
  [Description("Replace the last post of the specified user or the post you replied to with a formatted code block using the specified language")]
  [RequirePermissions(Permissions.ManageMessages)] // Restrict this command to users/roles who have the "Manage Messages" permission
  [RequireRoles(RoleCheckMode.Any, "Helper", "Mod", "Owner")] // Restrict this command to "Helper", "Mod" and "Owner" roles only
  public async Task RefactorCommand(CommandContext ctx) { // Refactors the previous post, if it is code
    await RefactorCode(ctx, null, null);
  }

  [Command("reformat")]
  [Description("Replace the last post of the specified user or the post you replied to with a formatted code block")]
  [RequirePermissions(Permissions.ManageMessages)] // Restrict this command to users/roles who have the "Manage Messages" permission
  [RequireRoles(RoleCheckMode.Any, "Helper", "Mod", "Owner")] // Restrict this command to "Helper", "Mod" and "Owner" roles only
  public async Task ReformatCommand(CommandContext ctx, [Description("Force the Language to use. Use 'best' or 'Analyze' to find the best language.")] string language) { // Refactors the previous post, if it is code
    await RefactorCode(ctx, null, language);
  }

  [Command("reformat")]
  [RequirePermissions(Permissions.ManageMessages)] // Restrict this command to users/roles who have the "Manage Messages" permission
  [RequireRoles(RoleCheckMode.Any, "Helper", "Mod", "Owner")] // Restrict this command to "Helper", "Mod" and "Owner" roles only
  public async Task ReformatCommand(CommandContext ctx, [Description("The user the posted the message to refactor")] DiscordMember member) { // Refactor the last post of the specified user in the channel
    await RefactorCode(ctx, member, null);
  }

  [Command("reformat")]
  [RequirePermissions(Permissions.ManageMessages)] // Restrict this command to users/roles who have the "Manage Messages" permission
  [RequireRoles(RoleCheckMode.Any, "Helper", "Mod", "Owner")] // Restrict this command to "Helper", "Mod" and "Owner" roles only
  public async Task RefacReformatCommandtorCommand(CommandContext ctx, [Description("The user the posted the message to refactor")] DiscordMember member, [Description("Force the Language to use. Use 'best' or 'Analyze' to find the best language.")]  string language) { // Refactor the last post of the specified user in the channel
    await RefactorCode(ctx, member, language);
  }

  private async Task<Task<DiscordMessage>> RefactorCode(CommandContext ctx, DiscordMember m, string language) {
    UtilityFunctions.LogUserCommand(ctx);
    DiscordChannel c = ctx.Channel;
    DiscordMessage toRefactor = null;
    if (ctx.Message.Reference != null) toRefactor = ctx.Message.Reference.Message;
    else {
      IReadOnlyList<DiscordMessage> msgs = await c.GetMessagesAsync(50);
      if (m == null) toRefactor = msgs[1];
      else {
        for (int i = 1; i < msgs.Count; i++) {
          if (msgs[i].Author.Id.Equals(m.Id)) {
            toRefactor = msgs[i];
            break;
          }
        }
      }
      if (toRefactor == null) return ctx.RespondAsync("Nothing to refactor found");
    }

    // Is the message some code?
    string code = toRefactor.Content;
    int weightCs = 0, weightCp = 0, weightJv = 0, weightJs = 0, weightPy = 0;
    foreach (LangKWord k in keywords) {
      if (k.regexp.IsMatch(code)) {
        weightCs += k.wCs;
        weightCp += k.wCp;
        weightJv += k.wJv;
        weightJs += k.wJs;
        weightPy += k.wPy;
      }
    }

    string guessed = "no one";
    string best = "";
    EmojiEnum langEmoji = EmojiEnum.None;
    int w = 0;
    if (weightCs > w) { guessed = "<:csharp:831465428214743060> C#"; w = weightCs; best = "cs"; langEmoji = EmojiEnum.CSharp; }
    if (weightCp > w) { guessed = "<:cpp:831465408874676273> C++"; w = weightCp; best = "cpp"; langEmoji = EmojiEnum.Cpp; }
    if (weightJs > w) { guessed = "<:Javascript:876103767068647435> Javascript"; w = weightJs; best = "js"; langEmoji = EmojiEnum.Javascript; }
    if (weightJv > w) { guessed = "<:java:875852276017815634> Java"; w = weightJv; best = "java"; langEmoji = EmojiEnum.Java; }
    if (weightPy > w) { guessed = "<:python:831465381016895500> Python"; w = weightPy; best = "python"; langEmoji = EmojiEnum.Python; }
    if (w == 0 && language == null) return ctx.RespondAsync("Nothing to reformat");

    language = NormalizeLanguage(language, best);
    if (language == null)
      return ctx.RespondAsync("Best guess for the language is: " + guessed + "\nC# = " + weightCs + " C++ = " + weightCp + " Java = " + weightJv + " Javascript = " + weightJs + " Python = " + weightPy);

    // Remove the ``` at begin and end, if any. And the code name after initial ```
    bool deleteOrig = true;
    Match codeMatch = codeBlock.Match(code);
    if (codeMatch.Success) {
      code = codeMatch.Groups[5].Value;
      deleteOrig = string.IsNullOrWhiteSpace(codeMatch.Groups[1].Value);
    }
    code = code.Trim(' ', '\t', '\r', '\n');
    code = emptyLines.Replace(code, "\n");

    if (langEmoji == EmojiEnum.CSharp || langEmoji == EmojiEnum.Cpp || langEmoji == EmojiEnum.Java || langEmoji == EmojiEnum.Javascript) code = FixIndentation(code);

    code = "Reformatted " + toRefactor.Author.Mention + " code\n" + "```" + language + "\n" + code + "\n```";

    if (guessed == "no one" && language != null) {
      langEmoji = GetLanguageEmoji(language);
    }

    DiscordMessage replacement = await ctx.Channel.SendMessageAsync(code);
    DiscordEmoji autoRefactored = UtilityFunctions.GetEmoji(EmojiEnum.AutoRefactored);
    DiscordEmoji emoji = UtilityFunctions.GetEmoji(langEmoji);
    try {
      if (autoRefactored != null) {
        await Task.Delay(120);
        await replacement.CreateReactionAsync(autoRefactored);
      }
      if (emoji != null) {
        await Task.Delay(120);
        await replacement.CreateReactionAsync(emoji);
      }
      if (deleteOrig) {
        await Task.Delay(120);
        List<DiscordMessage> toDelete = new List<DiscordMessage> { toRefactor, ctx.Message };
        await ctx.Channel.DeleteMessagesAsync(toDelete);
      }
      else {
        await Task.Delay(120);
        await ctx.Message.DeleteAsync();
      }
    } catch (Exception e) {
      return ctx.RespondAsync("Exception: " + e.Message);
    }
    await Task.Delay(150);
    return ctx.RespondAsync("");
  }

  readonly Regex lineOpenBlock = new Regex("^{(\\s*//.*|\\s*/\\*/.*)?$", RegexOptions.Multiline, TimeSpan.FromSeconds(1));
  readonly Regex afterOpenBlock = new Regex("^{(.+)?$", RegexOptions.Multiline, TimeSpan.FromSeconds(1));
  readonly Regex cppModifiers = new Regex("^\\s*(private|public|protected):\\s*$", RegexOptions.Multiline, TimeSpan.FromSeconds(1));
  readonly Regex switchModifiers = new Regex("^(case\\s+[^:]+|default):", RegexOptions.Multiline, TimeSpan.FromSeconds(1));
  readonly Regex singleLineBlocksIF = new Regex("^(else\\s+if|if|else)[^;{\\n]*$", RegexOptions.Multiline, TimeSpan.FromSeconds(1));
  readonly Regex singleLineBlocksFOR = new Regex("^for\\s*\\([^\\)]+\\)[^;{\\n]*$", RegexOptions.Multiline, TimeSpan.FromSeconds(1));
  readonly Regex singleLineBlocksFOREACH = new Regex("^foreach\\s*\\([^\\)]+\\)[^;{\\n]*$", RegexOptions.Multiline, TimeSpan.FromSeconds(1));
  readonly Regex singleLineBlocksWHILE = new Regex("^while\\s*\\([^\\)]+\\)[^;{\\n]*$", RegexOptions.Multiline, TimeSpan.FromSeconds(1));
  readonly Regex operatorsEnd = new Regex("(\\+|\\-|\\||\\&|\\^|(\\|\\|)|\\&\\&|\\>\\>|\\<\\<)\\s*$", RegexOptions.Multiline, TimeSpan.FromSeconds(1));
  readonly Regex operatorsStart = new Regex("^(\\+|\\-|\\||\\&|\\^|(\\|\\|)|\\&\\&|\\>\\>|\\<\\<)", RegexOptions.Multiline, TimeSpan.FromSeconds(1));

  private string FixIndentation(string code) {
    string[] lines = code.Split('\n');
    for (int i = 0; i < lines.Length; i++)
      lines[i] = lines[i].Trim(' ', '\t', '\r', '\n');
    for (int i = 1; i < lines.Length; i++) {
      if (lineOpenBlock.IsMatch(lines[i])) {
        lines[i - 1] += " " + lines[i];
        lines[i] = null;
      }
      else {
        Match afterOpen = afterOpenBlock.Match(lines[i]);
        if (afterOpen.Success) {
          lines[i - 1] += " { ";
          lines[i] = afterOpen.Groups[1].Value.Trim(' ', '\t', '\r', '\n');
        }
      }
    }
    int indent = 0;
    string res = "";
    bool nextLineIndent = false;
    for (int i = 0; i < lines.Length; i++) {
      bool tempRemoveIndent = false;
      string line = lines[i];
      if (line == null) continue;
      if (line.IndexOf('}') != -1 && line.IndexOf('{') == -1) indent--;
      if (cppModifiers.IsMatch(line) || switchModifiers.IsMatch(line)) tempRemoveIndent = true;
      
      string tabs = "";
      for (int j = tempRemoveIndent ? 1 : 0; j < (nextLineIndent ? indent + 1 : indent); j++) tabs += "  ";
      if (operatorsStart.IsMatch(line)) tabs += "  ";
      if (singleLineBlocksIF.IsMatch(line) || singleLineBlocksFOR.IsMatch(line) || singleLineBlocksFOREACH.IsMatch(line) || singleLineBlocksWHILE.IsMatch(line) || operatorsEnd.IsMatch(line))
        nextLineIndent = true;
      else nextLineIndent = false;
      res += tabs + line + "\n";
      if (line.IndexOf('{') != -1 && line.IndexOf('}') == -1) indent++;
    }
    return res;
  }

  private string NormalizeLanguage(string language, string best) {
    if (language == null) return best;
    language = language.ToLowerInvariant();
    if (language == "best") return null;
    if (language == "what") return null;
    if (language == "whatis") return null;
    if (language == "analyze") return null;
    if (language == "analysis") return null;
    if (language == "c#") return "cs";
    if (language == "cs") return "cs";
    if (language == "csharp") return "cs";
    if (language == "cpp") return "cpp";
    if (language == "c++") return "cpp";
    if (language == "java") return "java";
    if (language == "javascript") return "js";
    if (language == "jscript") return "js";
    if (language == "js") return "js";
    if (language == "json") return "js";
    if (language == "typescript") return "js";
    if (language == "phyton") return "python";
    if (language == "python") return "python";
    if (language == "py") return "python";
    return "";
  }
  private EmojiEnum GetLanguageEmoji(string language) {
    language = language.ToLowerInvariant();
    if (language == "c#") return EmojiEnum.CSharp;
    if (language == "cs") return EmojiEnum.CSharp;
    if (language == "csharp") return EmojiEnum.CSharp;
    if (language == "cpp") return EmojiEnum.Cpp;
    if (language == "c++") return EmojiEnum.Cpp;
    if (language == "java") return EmojiEnum.Java;
    if (language == "javascript") return EmojiEnum.Javascript;
    if (language == "jscript") return EmojiEnum.Javascript;
    if (language == "js") return EmojiEnum.Javascript;
    if (language == "typescript") return EmojiEnum.Javascript;
    if (language == "json") return EmojiEnum.Javascript;
    if (language == "phyton") return EmojiEnum.Python;
    if (language == "python") return EmojiEnum.Python;
    if (language == "py") return EmojiEnum.Python;
    return EmojiEnum.None;
  }

  readonly Regex codeBlock = new Regex("(.*)(\\n|\\r|\\r\\n)?(```[a-z]*(\\n|\\r|\\r\\n))(.*)(```[a-z]*(\\n|\\r|\\r\\n)?)", RegexOptions.Singleline, TimeSpan.FromSeconds(1));
  readonly Regex emptyLines = new Regex("(\\r?\\n\\s*){1,}(\\r?\\n)", RegexOptions.Singleline, TimeSpan.FromSeconds(1));

  readonly LangKWord[] keywords = {
    new LangKWord{regexp = new Regex("if\\s*\\(", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),                 wCs = 2, wCp = 2, wJv = 2, wJs = 2, wPy = 0 },
    new LangKWord{regexp = new Regex("for\\s*\\([^;]+;", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),          wCs = 2, wCp = 2, wJv = 0, wJs = 0, wPy = 0 },
    new LangKWord{regexp = new Regex("for\\s*\\([^:;]+:", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),         wCs = 0, wCp = 0, wJv = 0, wJs = 4, wPy = 0 },
    new LangKWord{regexp = new Regex("foreach\\s*\\([^;]+in", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),     wCs = 3, wCp = 0, wJv = 0, wJs = 0, wPy = 0 },
    new LangKWord{regexp = new Regex("for_each\\s*\\(", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),           wCs = 0, wCp = 3, wJv = 0, wJs = 0, wPy = 0 },
    new LangKWord{regexp = new Regex("while\\s*\\(", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),              wCs = 2, wCp = 2, wJv = 2, wJs = 2, wPy = 0 },
    new LangKWord{regexp = new Regex("\\.Equals\\(", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),              wCs = 3, wCp = 1, wJv = 3, wJs = 0, wPy = 0 },
    new LangKWord{regexp = new Regex("switch\\s*\\([a-z\\s]", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),     wCs = 2, wCp = 2, wJv = 2, wJs = 2, wPy = 0 },
    new LangKWord{regexp = new Regex("break;", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),                    wCs = 2, wCp = 2, wJv = 0, wJs = 0, wPy = 0 },
    new LangKWord{regexp = new Regex("[a-z\\)];", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),                 wCs = 1, wCp = 1, wJv = 0, wJs = 0, wPy = 0 },
    new LangKWord{regexp = new Regex("string\\s+[a-z]", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),           wCs = 1, wCp = 1, wJv = 1, wJs = 0, wPy = 0 },
    new LangKWord{regexp = new Regex("int\\s+[a-z]", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),              wCs = 1, wCp = 1, wJv = 1, wJs = 0, wPy = 0 },
    new LangKWord{regexp = new Regex("long\\s+[a-z]", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),             wCs = 1, wCp = 1, wJv = 1, wJs = 0, wPy = 0 },
    new LangKWord{regexp = new Regex("float\\s+[a-z]", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),            wCs = 1, wCp = 1, wJv = 0, wJs = 0, wPy = 0 },
    new LangKWord{regexp = new Regex("bool\\s+[a-z]", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),             wCs = 3, wCp = 2, wJv = 0, wJs = 0, wPy = 0 },
    new LangKWord{regexp = new Regex("boolean\\s+[a-z]", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),          wCs = 0, wCp = 1, wJv = 9, wJs = 0, wPy = 0 },
    new LangKWord{regexp = new Regex("GameObject\\s+[a-z]", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),       wCs = 5, wCp = 3, wJv = 0, wJs = 0, wPy = 0 },
    new LangKWord{regexp = new Regex("Transform\\s+[a-z]", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),        wCs = 5, wCp = 3, wJv = 0, wJs = 0, wPy = 0 },
    new LangKWord{regexp = new Regex("Rigidbody\\s+[a-z]", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),        wCs = 5, wCp = 3, wJv = 0, wJs = 0, wPy = 0 },
    new LangKWord{regexp = new Regex("Rigidbody2D\\s+[a-z]", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),      wCs = 5, wCp = 3, wJv = 0, wJs = 0, wPy = 0 },
    new LangKWord{regexp = new Regex("Quaternion\\.", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),             wCs = 4, wCp = 3, wJv = 0, wJs = 0, wPy = 0 },
    new LangKWord{regexp = new Regex("\\.position", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),               wCs = 2, wCp = 1, wJv = 0, wJs = 0, wPy = 0 },
    new LangKWord{regexp = new Regex("\\.rotation", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),               wCs = 2, wCp = 2, wJv = 0, wJs = 0, wPy = 0 },
    new LangKWord{regexp = new Regex("\\.Count", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),                  wCs = 2, wCp = 2, wJv = 0, wJs = 0, wPy = 0 },
    new LangKWord{regexp = new Regex("\\.Length", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),                 wCs = 2, wCp = 2, wJv = 0, wJs = 0, wPy = 0 },
    new LangKWord{regexp = new Regex("\\(.*\\)", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),                  wCs = 2, wCp = 2, wJv = 2, wJs = 2, wPy = 0 },
    new LangKWord{regexp = new Regex("\\{.*\\}", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),                  wCs = 2, wCp = 2, wJv = 2, wJs = 2, wPy = 0 },
    new LangKWord{regexp = new Regex("\\[.*\\]", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),                  wCs = 1, wCp = 1, wJv = 0, wJs = 0, wPy = 0 },
    new LangKWord{regexp = new Regex("#include\\s+[\"<]", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),         wCs = 0, wCp = 9, wJv = 0, wJs = 0, wPy = 0 },
    new LangKWord{regexp = new Regex("#define\\s+", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),               wCs = 0, wCp = 9, wJv = 0, wJs = 0, wPy = 0 },
    new LangKWord{regexp = new Regex("[^#]include\\s+[\"<]", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),      wCs = 0, wCp = 0, wJv = 9, wJs = 0, wPy = 0 },
    new LangKWord{regexp = new Regex("using((?!::).)*;", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),          wCs = 9, wCp = 0, wJv = 0, wJs = 0, wPy = 0 },
    new LangKWord{regexp = new Regex("using[^;]+::[^;]+;", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),        wCs = 0, wCp = 9, wJv = 0, wJs = 0, wPy = 0 },
    new LangKWord{regexp = new Regex("std::", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),                     wCs = 0, wCp = 9, wJv = 0, wJs = 0, wPy = 0 },
    new LangKWord{regexp = new Regex("[!=]==", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),                    wCs = 0, wCp = 0, wJv = 0, wJs = 9, wPy = 0 },
    new LangKWord{regexp = new Regex("auto", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),                      wCs = 0, wCp = 5, wJv = 0, wJs = 0, wPy = 0 },
    new LangKWord{regexp = new Regex("public\\s+[a-z0-9<>]+\\s[a-z0-9]+\\s*;", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),  wCs = 9, wCp = 0, wJv = 0, wJs = 0, wPy = 0 },
    new LangKWord{regexp = new Regex("private\\s+[a-z0-9<>]+\\s[a-z0-9]+\\s*;", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),  wCs = 9, wCp = 0, wJv = 0, wJs = 0, wPy = 0 },
    new LangKWord{regexp = new Regex("public\\s", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),                 wCs = 1, wCp = 1, wJv = 1, wJs = 0, wPy = 0 },
    new LangKWord{regexp = new Regex("public:", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),                   wCs = 0, wCp = 5, wJv = 0, wJs = 0, wPy = 0 },
    new LangKWord{regexp = new Regex("private:", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),                   wCs = 0, wCp = 5, wJv = 0, wJs = 0, wPy = 0 },
    new LangKWord{regexp = new Regex("private\\s", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),                wCs = 1, wCp = 1, wJv = 1, wJs = 0, wPy = 0 },
    new LangKWord{regexp = new Regex("\\};", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),                      wCs = 0, wCp = 2, wJv = 2, wJs = 1, wPy = 0 },
    new LangKWord{regexp = new Regex("let\\s+[a-z0-9_]+\\s*=", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),    wCs = 0, wCp = 0, wJv = 0, wJs = 9, wPy = 0 },
    new LangKWord{regexp = new Regex("import\\s[a-z][a-z0-9]+", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),   wCs = 0, wCp = 0, wJv = 0, wJs = 0, wPy = 4 },
    new LangKWord{regexp = new Regex("'''[\\sa-z0-9]+", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),   wCs = 0, wCp = 0, wJv = 0, wJs = 0, wPy = 4 },
    new LangKWord{regexp = new Regex("for\\s[a-z][a-z0-9]*\\sin.+:", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),   wCs = 0, wCp = 0, wJv = 0, wJs = 0, wPy = 4 },
    new LangKWord{regexp = new Regex("print\\(\"", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),   wCs = 2, wCp = 0, wJv = 0, wJs = 0, wPy = 4 },
    new LangKWord{regexp = new Regex("else:", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),   wCs = 0, wCp = 0, wJv = 0, wJs = 0, wPy = 8 },
    new LangKWord{regexp = new Regex("\\[(\\s*[0-9]+\\s*\\,{0,1})+\\s*\\]", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),   wCs = 0, wCp = 0, wJv = 0, wJs = 4, wPy = 5 },
    new LangKWord{regexp = new Regex("\\[(\\s*\"[^\"]*\"\\s*\\,{0,1})+\\s*\\]", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),   wCs = 0, wCp = 0, wJv = 0, wJs = 4, wPy = 5 },
    new LangKWord{regexp = new Regex("while[\\sa-z0-9\\(\\)]+:\\n", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),  wCs = 0, wCp = 0, wJv = 0, wJs = 0, wPy = 5 },
    new LangKWord{regexp = new Regex("\\s*#\\s*[a-z0-9]", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),            wCs = 0, wCp = 0, wJv = 0, wJs = 0, wPy = 6 },
    new LangKWord{regexp = new Regex("\\{.+\"{0,1}[a-z0-9_]+\"{0,1}\\s*:\\s*((\".*\")|[0-9\\.]+)\\s*[,\\}]", RegexOptions.IgnoreCase | RegexOptions.Singleline, TimeSpan.FromSeconds(1)),   wCs = 0, wCp = 0, wJv = 0, wJs = 9, wPy = 0 },
    new LangKWord{regexp = new Regex("System\\.out\\.println", RegexOptions.None, TimeSpan.FromSeconds(1)),             wCs = 0, wCp = 0, wJv = 9, wJs = 0, wPy = 0 },
    new LangKWord{regexp = new Regex("String\\[\\]", RegexOptions.None, TimeSpan.FromSeconds(1)),                       wCs = 0, wCp = 0, wJv = 9, wJs = 0, wPy = 0 },
    new LangKWord{regexp = new Regex("\\?\\.", RegexOptions.None, TimeSpan.FromSeconds(1)),                             wCs = 2, wCp = 0, wJv = 0, wJs = 0, wPy = 0 },
    new LangKWord{regexp = new Regex("\\-\\>", RegexOptions.None, TimeSpan.FromSeconds(1)),                             wCs = 0, wCp = 9, wJv = 0, wJs = 0, wPy = 0 },
  };

  public class LangKWord {
    public Regex regexp;
    public int wCs; // Weight for C#
    public int wCp; // Weight for C++
    public int wJv; // Weight for Java
    public int wJs; // Weight for Javascript
    public int wPy; // Weight for Python
  }
}