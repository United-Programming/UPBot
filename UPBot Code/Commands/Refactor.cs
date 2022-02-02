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

  enum Action {
    Analyze,
    Replace,
    Keep
  }

  enum Langs {
    NONE, cs, js, cpp, java, python
  }

  /*
  <reply> /refactor 
  <reply> /refactor lng
  <reply> /refactor lng replace
  <reply> /refactor keep lng
  <reply> /refactor best
  <reply> /refactor keep
   
  /refactor <usr> 
  /refactor <usr> lng
  /refactor <usr> lng replace
  /refactor <usr> keep lng
  /refactor <usr> best
  /refactor <usr> keep
   
   normal users can reformat their own posts
  admins can reformat messages from other users
   
   */


  [Command("checklanguage")]
  [Description("Checks what language is in the post you replied to, or the last post of a specified user or just the last post.")]
  public async Task CheckLanguage(CommandContext ctx) { // Refactors the previous post, if it is code
    if (ctx.Guild == null) return;
    if (!Setup.Permitted(ctx.Guild.Id, Config.ParamType.Refactor, ctx)) return;
    await RefactorCode(ctx, null, Action.Analyze, Langs.NONE);
  }

  [Command("checklanguage")]
  [Description("Checks what language is in the post you replied to, or the last post of a specified user or just the last post.")]
  public async Task CheckLanguage(CommandContext ctx, [Description("The user that posted the message to check")] DiscordMember member) { // Refactors the previous post, if it is code
    if (!Setup.Permitted(ctx.Guild.Id, Config.ParamType.Refactor, ctx)) return;
    await RefactorCode(ctx, member, Action.Analyze, Langs.NONE);
  }

  [Command("reformat")]
  [Description("Replace a specified post with a reformatted code block using the specified language or the best language")]
  public async Task RefactorCommand(CommandContext ctx) { // Refactors the previous post, if it is code
    if (ctx.Guild == null) return;
    if (!Setup.Permitted(ctx.Guild.Id, Config.ParamType.Refactor, ctx)) return;
    await RefactorCode(ctx, null, Action.Keep, Langs.NONE);
  }

  [Command("reformat")]
  [Description("Replace the last post of the specified user or the post you replied to with a formatted code block")]
  public async Task ReformatCommand(CommandContext ctx, [Description("Analyze the language with **Best** or **Analyze**, use **Replace** to replace the refactored post (only your own posts or if you are an admin), specify a **language** if you want to force one.")] string what) { // Refactors the previous post, if it is code
    if (!Setup.Permitted(ctx.Guild.Id, Config.ParamType.Refactor, ctx)) return;
    if (IsBest(what)) await RefactorCode(ctx, null, Action.Analyze, Langs.NONE);
    else if (IsReplace(what)) await RefactorCode(ctx, null, Action.Replace, Langs.NONE);
    else await RefactorCode(ctx, null, Action.Keep, NormalizeLanguage(what));
  }

  [Command("reformat")]
  [Description("Replace the last post of the specified user or the post you replied to with a formatted code block")]
  public async Task ReformatCommand(CommandContext ctx, [Description("Use **Replace** to replace the refactored post (only your own posts or if you are an admin), or specify a **language** if you want to force one.")] string cmd1, [Description("Use **Replace** to replace the refactored post (only your own posts or if you are an admin), or sa **language** if you want to force one.")] string cmd2) { // Refactors the previous post, if it is code
    if (!Setup.Permitted(ctx.Guild.Id, Config.ParamType.Refactor, ctx)) return;
    if (IsBest(cmd1) || IsBest(cmd2)) await RefactorCode(ctx, null, Action.Analyze, Langs.NONE);
    else if (IsReplace(cmd1)) await RefactorCode(ctx, null, Action.Replace, NormalizeLanguage(cmd2));
    else if (IsReplace(cmd2)) await RefactorCode(ctx, null, Action.Replace, NormalizeLanguage(cmd1));
    else {
      Langs l = NormalizeLanguage(cmd1);
      if (l == Langs.NONE) l = NormalizeLanguage(cmd2);
      await RefactorCode(ctx, null, Action.Keep, l);
    }
  }

  [Command("reformat")]
  public async Task ReformatCommand(CommandContext ctx, [Description("The user that posted the message to refactor")] DiscordMember member) { // Refactor the last post of the specified user in the channel
    if (!Setup.Permitted(ctx.Guild.Id, Config.ParamType.Refactor, ctx)) return;
    await RefactorCode(ctx, member, Action.Keep, Langs.NONE);
  }

  [Command("reformat")]
  public async Task RefacReformatCommandtorCommand(CommandContext ctx, [Description("The user that posted the message to refactor")] DiscordMember member, [Description("Analyze the language with **Best** or **Analyze**, use **Replace** to replace the refactored post, specify a **language** if you want to force one.")] string what) { // Refactor the last post of the specified user in the channel
    if (!Setup.Permitted(ctx.Guild.Id, Config.ParamType.Refactor, ctx)) return;
    if (IsBest(what)) await RefactorCode(ctx, member, Action.Analyze, Langs.NONE);
    else if (IsReplace(what)) await RefactorCode(ctx, member, Action.Replace, Langs.NONE);
    else await RefactorCode(ctx, member, Action.Keep, NormalizeLanguage(what));
  }

  [Command("reformat")]
  public async Task RefacReformatCommandtorCommand(CommandContext ctx, [Description("The user that posted the message to refactor")] DiscordMember member, [Description("Use **Replace** to replace the refactored post (only your own posts or if you are an admin), or specify a **language** if you want to force one.")] string cmd1, [Description("Use **Replace** to replace the refactored post (only your own posts or if you are an admin), or sa **language** if you want to force one.")] string cmd2) { // Refactors the previous post, if it is code
    if (!Setup.Permitted(ctx.Guild.Id, Config.ParamType.Refactor, ctx)) return;
    if (IsBest(cmd1) || IsBest(cmd2)) await RefactorCode(ctx, member, Action.Analyze, Langs.NONE);
    else if (IsReplace(cmd1)) await RefactorCode(ctx, member, Action.Replace, NormalizeLanguage(cmd2));
    else if (IsReplace(cmd2)) await RefactorCode(ctx, member, Action.Replace, NormalizeLanguage(cmd1));
    else {
      Langs l = NormalizeLanguage(cmd1);
      if (l == Langs.NONE) l = NormalizeLanguage(cmd2);
      await RefactorCode(ctx, member, Action.Keep, l);
    }
  }

  private async Task<Task<DiscordMessage>> RefactorCode(CommandContext ctx, DiscordMember m, Action action, Langs lang) {
    Utils.LogUserCommand(ctx);

    try {
      // Find the message
      DiscordMessage toRefactor = null;
      Langs msgLang = Langs.NONE;
      if (ctx.Message.Reference != null) toRefactor = ctx.Message.Reference.Message;
      else if (m != null) { // Get last post of the specified member
        IReadOnlyList<DiscordMessage> msgs = await ctx.Channel.GetMessagesAsync(50);
        for (int i = 1; i < msgs.Count; i++) {
          if (msgs[i].Author.Id.Equals(m.Id)) {
            toRefactor = msgs[i];
            break;
          }
        }
      } else { // Get last post that looks like code
        IReadOnlyList<DiscordMessage> msgs = await ctx.Channel.GetMessagesAsync(50);
        for (int i = 1; i < msgs.Count; i++) {
          string content = msgs[i].Content;
          msgLang = GetBestMatch(content);
          if (msgLang != Langs.NONE) {
            toRefactor = msgs[i];
            break;
          }
        }
      }

      if (toRefactor == null) return ctx.RespondAsync("Nothing to refactor found");
      // FIXME If we are not an admin, and the message is not from ourselves, do not accept the replace option.
      if (action == Action.Replace && !Utils.IsAdmin(ctx.Member) && toRefactor.Author.Id != ctx.Member.Id) action = Action.Keep;

      // Is the message some code, or at least somethign we recognize?
      string code = toRefactor.Content;
      if (msgLang == Langs.NONE)
        msgLang = GetBestMatch(code);

      if (action == Action.Analyze) {
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
        switch (msgLang) {
          case Langs.cs: guessed = "<:csharp:831465428214743060> C#"; break;
          case Langs.js: guessed = "<:Javascript:876103767068647435> Javascript"; break;
          case Langs.cpp: guessed = "<:cpp:831465408874676273> C++"; break;
          case Langs.java: guessed = "<:java:875852276017815634> Java"; break;
          case Langs.python: guessed = "<:python:831465381016895500> Python"; break;
        }
        return ctx.RespondAsync("Best guess for the language is: " + guessed + "\nC# = " + weightCs + " C++ = " + weightCp + " Java = " + weightJv + " Javascript = " + weightJs + " Python = " + weightPy);
      }


      // Remove the ``` at begin and end, if any. And the code name after initial ```
      bool deleteOrig = action == Action.Replace;
      Match codeMatch = codeBlock.Match(code);
      if (codeMatch.Success) {
        if (codeMatch.Groups[3].Value.IndexOf(';') != -1)
          code = codeMatch.Groups[3].Value.Substring(3) + codeMatch.Groups[6].Value;
        else
          code = codeMatch.Groups[6].Value;
      }
      code = code.Trim(' ', '\t', '\r', '\n');
      code = emptyLines.Replace(code, "\n");

      if (lang != Langs.NONE) msgLang = lang;
      EmojiEnum langEmoji = EmojiEnum.None;
      string lmd = "";
      switch (msgLang) {
        case Langs.cs: langEmoji = EmojiEnum.CSharp; lmd = "cs"; break;
        case Langs.js: langEmoji = EmojiEnum.Javascript; lmd = "js"; break;
        case Langs.cpp: langEmoji = EmojiEnum.Cpp; lmd = "cpp"; break;
        case Langs.java: langEmoji = EmojiEnum.Java; lmd = "java"; break;
        case Langs.python: langEmoji = EmojiEnum.Python; lmd = "python"; break;
      }

      if (langEmoji != EmojiEnum.None && langEmoji != EmojiEnum.Python) code = FixIndentation(code);

      code = "Reformatted " + toRefactor.Author.Mention + " code\n" + "```" + lmd + "\n" + code + "\n```";

      // We may need to do some multiple posts in case we have more than 2000 characters.
      DiscordMessage replacement;
      while (code.Length > 1995) { // Split in multiple messages
        int newlinePos = code.LastIndexOf('\n', 1995);
        string code1 = code.Substring(0, newlinePos).Trim(' ', '\t', '\r', '\n') + "\n```";
        await ctx.Channel.SendMessageAsync(code1);
        code = "```" + lmd + "\n" + code.Substring(newlinePos + 1).Trim('\r', '\n');
      }
      // Post the last part as is
      replacement = await ctx.Channel.SendMessageAsync(code);


      DiscordEmoji autoRefactored = Utils.GetEmoji(EmojiEnum.AutoRefactored);
      DiscordEmoji emoji = Utils.GetEmoji(langEmoji);
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
        } else {
          await Task.Delay(120);
          await ctx.Message.DeleteAsync();
        }
      } catch (Exception e) {
        return ctx.RespondAsync("Exception: " + e.Message);
      }
      await Task.Delay(150);
      return ctx.RespondAsync("");
    } catch (Exception ex) {
      return ctx.RespondAsync(Utils.GenerateErrorAnswer(ctx.Guild.Name, "Refactor", ex));
    }
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
      } else {
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

  private Langs GetBestMatch(string code) {
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
    int w = 0;
    Langs res = Langs.NONE;
    if (weightCs > w) { w = weightCs; res = Langs.cs; }
    if (weightCp > w) { w = weightCp; res = Langs.cpp; }
    if (weightJs > w) { w = weightJs; res = Langs.js; }
    if (weightJv > w) { w = weightJv; res = Langs.java; }
    if (weightPy > w) { w = weightPy; res = Langs.python; }
    return res;
  }

  private bool IsBest(string what) {
    if (what == null) return false;
    what = what.ToLowerInvariant();
    if (what == "best") return true;
    if (what == "what") return true;
    if (what == "whatis") return true;
    if (what == "analyze") return true;
    if (what == "analysis") return true;
    return false;
  }

  private bool IsReplace(string what) {
    if (what == null) return false;
    what = what.ToLowerInvariant();
    if (what == "rep") return true;
    if (what == "repl") return true;
    if (what == "replace") return true;
    if (what == "remove") return true;
    if (what == "change") return true;
    if (what == "substitute") return true;
    if (what == "destroy") return true;
    if (what == "delete") return true;
    return false;
  }

  private Langs NormalizeLanguage(string language) {
    if (language == null) return Langs.NONE;
    language = language.ToLowerInvariant();
    if (language == "c#") return Langs.cs;
    if (language == "cs") return Langs.cs;
    if (language == "csharp") return Langs.cs;
    if (language == "cpp") return Langs.cpp;
    if (language == "c++") return Langs.cpp;
    if (language == "c") return Langs.cpp;
    if (language == "java") return Langs.java;
    if (language == "javascript") return Langs.js;
    if (language == "jscript") return Langs.js;
    if (language == "js") return Langs.js;
    if (language == "json") return Langs.js;
    if (language == "typescript") return Langs.js;
    if (language == "phyton") return Langs.python;
    if (language == "python") return Langs.python;
    if (language == "py") return Langs.python;
    return Langs.NONE;
  }

  readonly Regex codeBlock = new Regex("(.*)(\\n|\\r|\\r\\n)?(```[a-z]*(\\n|\\r|\\r\\n)|```[^;]*;(\\n|\\r|\\r\\n))(.*)(```[a-z]*(\\n|\\r|\\r\\n)?)", RegexOptions.Singleline, TimeSpan.FromSeconds(1));
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