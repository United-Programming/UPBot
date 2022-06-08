using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System.Text.RegularExpressions;
using DSharpPlus;

/// <summary>
/// Command used to refactor as codeblock some code pasted by a user
/// author: CPU
/// </summary>
/// 

public class SlashRefactor : ApplicationCommandModule {
  enum Action {
    Analyze,
    Replace,
    Keep
  }

  enum Langs {
    NONE, cs, js, cpp, java, python, Unity
  }


  [SlashCommand("whatlanguage", "Checks the programming language of a post")]
  public async Task CheckLanguage(InteractionContext ctx, [Option("Member", "The user that posted the message to check")] DiscordUser user = null) {
    // Checks the language of some code posted
    Utils.LogUserCommand(ctx);

    try {
      // Get last post that looks like code
      ulong usrId = user == null ? 0 : user.Id;
      IReadOnlyList<DiscordMessage> msgs = await ctx.Channel.GetMessagesAsync(50);
      for (int i = 0; i < msgs.Count; i++) {
        DiscordMessage m = msgs[i];
        if (usrId != 0 && m.Author.Id != usrId) continue;
        Langs lang = GetBestMatch(m.Content, out int weightCs, out int weightCp, out int weightJv, out int weightJs, out int weightPy, out int weightUn);
        string guessed = "no one";
        switch (lang) {
          case Langs.cs: guessed = "<:csharp:831465428214743060> C#"; break;
          case Langs.js: guessed = "<:Javascript:876103767068647435> Javascript"; break;
          case Langs.cpp: guessed = "<:cpp:831465408874676273> C++"; break;
          case Langs.java: guessed = "<:java:875852276017815634> Java"; break;
          case Langs.python: guessed = "<:python:831465381016895500> Python"; break;
          case Langs.Unity: guessed = "<:Unity:968043486379143168> Unity C#"; break;
        }
        string usrname = user == null ? "last code" : user.Username + "'s code";
        await ctx.CreateResponseAsync($"Best guess for the language in {usrname} is: {guessed}\nC# = {weightCs}  C++ = {weightCp}  Java = {weightJv}  Javascript = {weightJs}  Python = {weightPy}  Unity C# = {weightUn}");
      }

      await ctx.CreateResponseAsync("Cannot find something that looks like code.");
    } catch (Exception ex) {
      await ctx.CreateResponseAsync(Utils.GenerateErrorAnswer(ctx.Guild.Name, "WhatLanguage", ex));
    }
  }

  // Refactors the previous post, if it is code, without removing it
  [SlashCommand("format", "Format a specified post (from a user, if specified) as code block")]
  public async Task FactorCommand(InteractionContext ctx, [Option("Member", "The user that posted the message to format")] DiscordUser user = null) {
    Utils.LogUserCommand(ctx);

    try {
      // Get last post that looks like code
      DiscordMessage msg = null;
      Langs lang = Langs.NONE;
      ulong usrId = user == null ? 0 : user.Id;
      IReadOnlyList<DiscordMessage> msgs = await ctx.Channel.GetMessagesAsync(50);
      for (int i = 0; i < msgs.Count; i++) {
        DiscordMessage m = msgs[i];
        if (usrId != 0 && m.Author.Id != usrId) continue;
        lang = GetBestMatch(m.Content, out _, out _, out _, out _, out _, out _);
        if (lang != Langs.NONE) {
          msg = m;
          break;
        }
      }
      if (msg == null) {
        await ctx.CreateResponseAsync("Cannot find something that looks like code.");
        return;
      }

      lang = GetCodeBlock(msg.Content, lang, out string code);

      EmojiEnum langEmoji = EmojiEnum.None;
      string lmd = "";
      switch (lang) {
        case Langs.cs: langEmoji = EmojiEnum.CSharp; lmd = "cs"; break;
        case Langs.Unity: langEmoji = EmojiEnum.Unity; lmd = "cs"; break;
        case Langs.js: langEmoji = EmojiEnum.Javascript; lmd = "js"; break;
        case Langs.cpp: langEmoji = EmojiEnum.Cpp; lmd = "cpp"; break;
        case Langs.java: langEmoji = EmojiEnum.Java; lmd = "java"; break;
        case Langs.python: langEmoji = EmojiEnum.Python; lmd = "python"; break;
      }

      if (langEmoji != EmojiEnum.None && langEmoji != EmojiEnum.Python) code = FixIndentation(code);

      code = "Reformatted " + msg.Author.Mention + " code\n" + "```" + lmd + "\n" + code + "\n```";

      if (code.Length < 1990) { // Single message
        await ctx.CreateResponseAsync(code);
        DiscordMessage replacement = await ctx.GetOriginalResponseAsync();
        try {
          await replacement.CreateReactionAsync(Utils.GetEmoji(EmojiEnum.AutoRefactored));
          await replacement.CreateReactionAsync(Utils.GetEmoji(langEmoji));
        } catch (Exception e) {
          Utils.Log("Cannot add an emoji: " + e.Message, ctx.Guild.Name);
        }
      }
      else { // Split in multiple messages
        bool first = true;
        while (code.Length > 1995) { 
          int newlinePos = code.LastIndexOf('\n', 1995);
          string codepart = code[..newlinePos].Trim(' ', '\t', '\r', '\n') + "\n```";
          code = "```" + lmd + "\n" + code[(newlinePos + 1)..].Trim('\r', '\n');
          if (first) {
            first = false;
            await ctx.CreateResponseAsync(codepart);
          }
          else {
            await ctx.Channel.SendMessageAsync(codepart);
          }
        }
        // Post the last part as is
        DiscordMessage replacement = await ctx.Channel.SendMessageAsync(code);
        try {
          await replacement.CreateReactionAsync(Utils.GetEmoji(EmojiEnum.AutoRefactored));
          await replacement.CreateReactionAsync(Utils.GetEmoji(langEmoji));
        } catch (Exception e) {
          Utils.Log("Cannot add an emoji: " + e.Message, ctx.Guild.Name);
        }
      }

    } catch (Exception ex) {
      await ctx.CreateResponseAsync(Utils.GenerateErrorAnswer(ctx.Guild.Name, "Refactor", ex));
    }
  }

  private Langs GetCodeBlock(string content, Langs lang, out string code) {
    // Find if we have a code block, and (in case) also a closing codeblock
    string writtenLang = null;
    code = content;
    int cbPos = code.IndexOf("```");
    if (cbPos != -1) {
      code = code[(cbPos + 3)..];
      char nl = code[0];
      if (nl != '\r' && nl != '\r') { // We have a possible language
        int nlPos1 = code.IndexOf('\n');
        int nlPos2 = code.IndexOf('\r');
        int pos = (nlPos1 != -1) ? nlPos1 : -1;
        if (nlPos2 != -1 && (nlPos2 < pos || pos == -1)) pos = nlPos2;
        if (pos != -1) {
          writtenLang = code[..pos].Trim(' ', '\t', '\r', '\n');
          code = code[pos..].Trim(' ', '\t', '\r', '\n');
        }
      }
      cbPos = code.IndexOf("```");
      if (cbPos != -1) code = code[..(cbPos - 1)].Trim(' ', '\t', '\r', '\n');
    }
    code = emptyLines.Replace(code, "\n");
    if (writtenLang != null) { // Do another best match with the given language
      Langs bl = Langs.NONE;
      switch (writtenLang.ToLowerInvariant()) {
        case "ph": bl = Langs.python; break;
        case "phy": bl = Langs.python; break;
        case "phyton": bl = Langs.python; break;
        case "pt": bl = Langs.python; break;
        case "c": bl = Langs.cpp; break;
        case "c++": bl = Langs.cpp; break;
        case "cp": bl = Langs.cpp; break;
        case "cpp": bl = Langs.cpp; break;
        case "cs": bl = Langs.cs; break;
        case "csharp": bl = Langs.cs; break;
        case "c#": bl = Langs.cs; break;
        case "jv": bl = Langs.java; break;
        case "java": bl = Langs.java; break;
        case "js": bl = Langs.js; break;
        case "json": bl = Langs.js; break;
        case "jscript": bl = Langs.js; break;
        case "javascript": bl = Langs.js; break;
      }
      return GetBestMatchWithHint(code, bl);
    }
    return lang;
  }

  // Refactors the previous post, if it is code, replacing it
  [SlashCommand("reformat", "Reformat a specified post as code block, the original message will be deleted")]
  public async Task RefactorCommand(InteractionContext ctx, [Option("Member", "The user that posted the message to format")] DiscordUser user = null) {
    Utils.LogUserCommand(ctx);

    try {
      // Get last post that looks like code
      DiscordMessage msg = null;
      Langs lang = Langs.NONE;
      ulong usrId = user == null ? 0 : user.Id;
      IReadOnlyList<DiscordMessage> msgs = await ctx.Channel.GetMessagesAsync(50);
      for (int i = 0; i < msgs.Count; i++) {
        DiscordMessage m = msgs[i];
        if (usrId != 0 && m.Author.Id != usrId) continue;
        lang = GetBestMatch(m.Content, out _, out _, out _, out _, out _, out _);
        if (lang != Langs.NONE) {
          msg = m;
          break;
        }
      }
      if (msg == null) {
        await ctx.CreateResponseAsync("Cannot find something that looks like code.");
        return;
      }


      lang = GetCodeBlock(msg.Content, lang, out string code);

      EmojiEnum langEmoji = EmojiEnum.None;
      string lmd = "";
      switch (lang) {
        case Langs.cs: langEmoji = EmojiEnum.CSharp; lmd = "cs"; break;
        case Langs.Unity: langEmoji = EmojiEnum.Unity; lmd = "cs"; break;
        case Langs.js: langEmoji = EmojiEnum.Javascript; lmd = "js"; break;
        case Langs.cpp: langEmoji = EmojiEnum.Cpp; lmd = "cpp"; break;
        case Langs.java: langEmoji = EmojiEnum.Java; lmd = "java"; break;
        case Langs.python: langEmoji = EmojiEnum.Python; lmd = "python"; break;
      }

      if (langEmoji != EmojiEnum.None && langEmoji != EmojiEnum.Python) code = FixIndentation(code);

      code = "Replaced " + msg.Author.Mention + " code (original code has been deleted)\n" + "```" + lmd + "\n" + code + "\n```";

      if (code.Length < 1990) { // Single message
        await ctx.CreateResponseAsync(code);
        DiscordMessage replacement = await ctx.GetOriginalResponseAsync();
        try {
          await replacement.CreateReactionAsync(Utils.GetEmoji(EmojiEnum.AutoRefactored));
          await replacement.CreateReactionAsync(Utils.GetEmoji(langEmoji));
        } catch (Exception e) {
          Utils.Log("Cannot add an emoji: " + e.Message, ctx.Guild.Name);
        }
      }
      else { // Split in multiple messages
        await ctx.DeferAsync();
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
        while (code.Length > 1995) { 
          int newlinePos = code.LastIndexOf('\n', 1995);
          string codepart = code[..newlinePos].Trim(' ', '\t', '\r', '\n') + "\n```";
          code = "```" + lmd + "\n" + code[(newlinePos + 1)..].Trim('\r', '\n');
          await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(codepart));
        }
        // Post the last part as is
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(code));
        DiscordMessage replacement = await ctx.GetOriginalResponseAsync();
        try {
          await replacement.CreateReactionAsync(Utils.GetEmoji(EmojiEnum.AutoRefactored));
          await replacement.CreateReactionAsync(Utils.GetEmoji(langEmoji));
        } catch (Exception e) {
          Utils.Log("Cannot add an emoji: " + e.Message, ctx.Guild.Name);
        }
      }

      // If we are not an admin, and the message is not from ourselves, do not accept the replace option.
      if (Configs.HasAdminRole(ctx.Guild.Id, ctx.Member.Roles, false) || msg.Author.Id != ctx.Member.Id) {
        await msg.DeleteAsync();
      }

    } catch (Exception ex) {
      await ctx.CreateResponseAsync(Utils.GenerateErrorAnswer(ctx.Guild.Name, "Refactor", ex));
    }
  }




  readonly Regex lineOpenBlock = new("^{(\\s*//.*|\\s*/\\*/.*)?$", RegexOptions.Multiline, TimeSpan.FromSeconds(10));
  readonly Regex afterOpenBlock = new("^{(.+)?$", RegexOptions.Multiline, TimeSpan.FromSeconds(10));
  readonly Regex cppModifiers = new("^\\s*(private|public|protected):\\s*$", RegexOptions.Multiline, TimeSpan.FromSeconds(10));
  readonly Regex switchModifiers = new("^(case\\s+[^:]+|default):", RegexOptions.Multiline, TimeSpan.FromSeconds(10));
  readonly Regex singleLineBlocksIF = new("^(else\\s+if|if|else)[^;{\\n]*$", RegexOptions.Multiline, TimeSpan.FromSeconds(10));
  readonly Regex singleLineBlocksFOR = new("^for\\s*\\([^\\)]+\\)[^;{\\n]*$", RegexOptions.Multiline, TimeSpan.FromSeconds(10));
  readonly Regex singleLineBlocksFOREACH = new("^foreach\\s*\\([^\\)]+\\)[^;{\\n]*$", RegexOptions.Multiline, TimeSpan.FromSeconds(10));
  readonly Regex singleLineBlocksWHILE = new("^while\\s*\\([^\\)]+\\)[^;{\\n]*$", RegexOptions.Multiline, TimeSpan.FromSeconds(10));
  readonly Regex operatorsEnd = new("(\\+|\\-|\\||\\&|\\^|(\\|\\|)|\\&\\&|\\>\\>|\\<\\<)\\s*$", RegexOptions.Multiline, TimeSpan.FromSeconds(10));
  readonly Regex operatorsStart = new("^(\\+|\\-|\\||\\&|\\^|(\\|\\|)|\\&\\&|\\>\\>|\\<\\<)", RegexOptions.Multiline, TimeSpan.FromSeconds(10));
  readonly Regex doubleBrackets = new("{[^\\n]+}", RegexOptions.Multiline, TimeSpan.FromSeconds(10));
  readonly Regex closeBrackets = new("[^\n{]+}", RegexOptions.Multiline, TimeSpan.FromSeconds(10));

  private string FixIndentation(string code) {
    string[] prelines = code.Split('\n');
    for (int i = 0; i < prelines.Length; i++)
      prelines[i] = prelines[i].Trim(' ', '\t', '\r', '\n');

    List<string> lines = new();
    foreach (var l in prelines) {
      string line = l;
      bool found = true;
      while (found) {
        if (doubleBrackets.IsMatch(line)) {
          // Check it is not inside a string
          bool instrings = false;
          bool instringd = false;
          int pos = 1;
          bool afterfirst = false;
          foreach (char c in line) {
            if (c == '"') instringd = !instringd;
            if (c == '\'') instrings = !instrings;
            if (c == '{' && pos != 1) {
              afterfirst = true;
              break;
            }
            pos++;
          }
          if (!instringd && !instrings && afterfirst) {
            lines.Add(line[..pos].Trim(' ', '\t', '\r', '\n'));
            line = line[pos..].Trim(' ', '\t', '\r', '\n');
          }
          else {
            lines.Add(line);
            found = false;
          }
        }
        else if (closeBrackets.IsMatch(line)) {
          // Check it is not inside a string
          bool instrings = false;
          bool instringd = false;
          int pos = 0;
          bool afterfirst = false;
          foreach (char c in line) {
            if (c == '"') instringd = !instringd;
            if (c == '\'') instrings = !instrings;
            if (c == '}' && pos != 0) {
              afterfirst = true;
              break;
            }
            pos++;
          }
          if (!instringd && !instrings && afterfirst) {
            lines.Add(line[..pos].Trim(' ', '\t', '\r', '\n'));
            line = line[pos..].Trim(' ', '\t', '\r', '\n');
          }
          else {
            lines.Add(line);
            found = false;
          }
        }
        else {
          lines.Add(line);
          found = false;
        }
      }
    }

    for (int i = 1; i < lines.Count; i++) {
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
    for (int i = 0; i < lines.Count; i++) {
      bool tempRemoveIndent = false;
      string line = lines[i];
      if (line == null) continue;
      if (line.IndexOf('}') != -1 && !line.Contains('{')) indent--;
      if (cppModifiers.IsMatch(line) || switchModifiers.IsMatch(line)) tempRemoveIndent = true;

      string tabs = "";
      for (int j = tempRemoveIndent ? 1 : 0; j < (nextLineIndent ? indent + 1 : indent); j++) tabs += "  ";
      if (operatorsStart.IsMatch(line)) tabs += "  ";
      if (singleLineBlocksIF.IsMatch(line) || singleLineBlocksFOR.IsMatch(line) || singleLineBlocksFOREACH.IsMatch(line) || singleLineBlocksWHILE.IsMatch(line) || operatorsEnd.IsMatch(line))
        nextLineIndent = true;
      else nextLineIndent = false;
      res += tabs + line + "\n";
      if (line.IndexOf('{') != -1 && !line.Contains('}')) indent++;
    }
    return res;
  }

  private Langs GetBestMatchWithHint(string code, Langs hint) {
    _ = GetBestMatch(code, out int weightCs, out int weightCp, out int weightJv, out int weightJs, out int weightPy, out int weightUn);
    switch (hint) {
      case Langs.cs: weightCs += 10; break;
      case Langs.js: weightJs += 10; break;
      case Langs.cpp: weightCp += 10; break;
      case Langs.java: weightJv += 10; break;
      case Langs.python: weightPy += 10; break;
      case Langs.Unity: weightUn += 10; break;
    }
    Langs res = Langs.NONE;
    int w = 0;
    if (weightCs > w) { w = weightCs; res = Langs.cs; }
    if (weightUn > w) { w = weightUn; res = Langs.Unity; }
    if (weightCp > w) { w = weightCp; res = Langs.cpp; }
    if (weightJs > w) { w = weightJs; res = Langs.js; }
    if (weightJv > w) { w = weightJv; res = Langs.java; }
    if (weightPy > w) { res = Langs.python; }
    return res;
  }


  private Langs GetBestMatch(string code, out int weightCs, out int weightCp, out int weightJv, out int weightJs, out int weightPy, out int weightUn) {
    if (code.Length > 4 && code[..3] == "```" && code.IndexOf('\n') != -1) {
      code = code[(code.IndexOf('\n') + 1)..];
    }
    if (code.Length > 4 && code[^3..] == "```") {
      code = code[..^3];
    }
    weightCs = 0; weightCp = 0; weightJv = 0; weightJs = 0; weightPy = 0; weightUn = 0;
    foreach (LangKWord k in keywords) {
      if (k.regexp.IsMatch(code)) {
        weightCs += k.wCs;
        weightCp += k.wCp;
        weightJv += k.wJv;
        weightJs += k.wJs;
        weightPy += k.wPy;
        weightUn += k.wUn;
      }
    }
    int w = 0;
    Langs res = Langs.NONE;
    if (weightCs > w) { w = weightCs; res = Langs.cs; }
    if (weightUn > w) { w = weightUn; res = Langs.Unity; }
    if (weightCp > w) { w = weightCp; res = Langs.cpp; }
    if (weightJs > w) { w = weightJs; res = Langs.js; }
    if (weightJv > w) { w = weightJv; res = Langs.java; }
    if (weightPy > w) { res = Langs.python; }
    return res;
  }

  readonly Regex emptyLines = new("(\\r?\\n\\s*){1,}(\\r?\\n)", RegexOptions.Singleline, TimeSpan.FromSeconds(10));

  readonly LangKWord[] keywords = {
    new LangKWord{regexp = new("getline", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                                    wCs = 0, wCp = 5, wJv = 0, wJs = 0, wPy = 0, wUn = 0 },
    new LangKWord{regexp = new("cin", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                                        wCs = 0, wCp = 5, wJv = 0, wJs = 0, wPy = 0, wUn = 0 },
    new LangKWord{regexp = new("cout", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                                       wCs = 0, wCp = 5, wJv = 0, wJs = 0, wPy = 0, wUn = 0 },
    new LangKWord{regexp = new("endl", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                                       wCs = 0, wCp = 5, wJv = 0, wJs = 0, wPy = 0, wUn = 0 },
    new LangKWord{regexp = new("size_t", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                                     wCs = 0, wCp = 5, wJv = 0, wJs = 0, wPy = 0, wUn = 0 },
    new LangKWord{regexp = new("if\\s*\\(", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                                  wCs = 2, wCp = 2, wJv = 2, wJs = 2, wPy = 0, wUn = 2 },
    new LangKWord{regexp = new("for\\s*\\([^;]+;", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                           wCs = 2, wCp = 2, wJv = 0, wJs = 0, wPy = 0, wUn = 2 },
    new LangKWord{regexp = new("for\\s*\\([^:;]+:", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                          wCs = 0, wCp = 0, wJv = 0, wJs = 4, wPy = 0, wUn = 0 },
    new LangKWord{regexp = new("foreach\\s*\\([^;]+in", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                      wCs = 3, wCp = 0, wJv = 0, wJs = 0, wPy = 0, wUn = 2 },
    new LangKWord{regexp = new("for_each\\s*\\(", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                            wCs = 0, wCp = 3, wJv = 0, wJs = 0, wPy = 0, wUn = 2 },
    new LangKWord{regexp = new("while\\s*\\(", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                               wCs = 2, wCp = 2, wJv = 2, wJs = 2, wPy = 0, wUn = 2 },
    new LangKWord{regexp = new("\\.Equals\\(", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                               wCs = 3, wCp = 1, wJv = 3, wJs = 0, wPy = 0, wUn = 2 },
    new LangKWord{regexp = new("switch\\s*\\([a-z\\s]", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                      wCs = 2, wCp = 2, wJv = 2, wJs = 2, wPy = 0, wUn = 2 },
    new LangKWord{regexp = new("break;", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                                     wCs = 2, wCp = 2, wJv = 0, wJs = 0, wPy = 0, wUn = 2 },
    new LangKWord{regexp = new("[a-z\\)];", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                                  wCs = 1, wCp = 1, wJv = 0, wJs = 0, wPy = 0, wUn = 0 },
    new LangKWord{regexp = new("string\\s+[a-z]", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                            wCs = 1, wCp = 1, wJv = 1, wJs = 0, wPy = 0, wUn = 1 },
    new LangKWord{regexp = new("int\\s+[a-z]", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                               wCs = 1, wCp = 1, wJv = 1, wJs = 0, wPy = 0, wUn = 1 },
    new LangKWord{regexp = new("long\\s+[a-z]", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                              wCs = 1, wCp = 1, wJv = 1, wJs = 0, wPy = 0, wUn = 1 },
    new LangKWord{regexp = new("float\\s+[a-z]", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                             wCs = 1, wCp = 1, wJv = 0, wJs = 0, wPy = 0, wUn = 1 },
    new LangKWord{regexp = new("bool\\s+[a-z]", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                              wCs = 3, wCp = 2, wJv = 0, wJs = 0, wPy = 0, wUn = 1 },
    new LangKWord{regexp = new("boolean\\s+[a-z]", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                           wCs = 0, wCp = 1, wJv = 9, wJs = 0, wPy = 0, wUn = 0 },
    new LangKWord{regexp = new("Vector2\\s+[a-z]", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                           wCs = 3, wCp = 3, wJv = 0, wJs = 0, wPy = 0, wUn = 10 },
    new LangKWord{regexp = new("Vector3\\s+[a-z]", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                           wCs = 3, wCp = 3, wJv = 0, wJs = 0, wPy = 0, wUn = 10 },
    new LangKWord{regexp = new("GameObject\\s+[a-z]", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                        wCs = 4, wCp = 3, wJv = 0, wJs = 0, wPy = 0, wUn = 10 },
    new LangKWord{regexp = new("MonoBehaviour", RegexOptions.None, TimeSpan.FromSeconds(10)),                                    wCs = 4, wCp = 3, wJv = 0, wJs = 0, wPy = 0, wUn = 10 },
    new LangKWord{regexp = new("ScriptableObject", RegexOptions.None, TimeSpan.FromSeconds(10)),                                 wCs = 4, wCp = 3, wJv = 0, wJs = 0, wPy = 0, wUn = 10 },
    new LangKWord{regexp = new("Transform\\s+[a-z]", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                         wCs = 4, wCp = 3, wJv = 0, wJs = 0, wPy = 0, wUn = 10 },
    new LangKWord{regexp = new("Rigidbody\\s+[a-z]", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                         wCs = 4, wCp = 3, wJv = 0, wJs = 0, wPy = 0, wUn = 10 },
    new LangKWord{regexp = new("Rigidbody2D\\s+[a-z]", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                       wCs = 4, wCp = 3, wJv = 0, wJs = 0, wPy = 0, wUn = 10 },
    new LangKWord{regexp = new("Quaternion\\.", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                              wCs = 4, wCp = 3, wJv = 0, wJs = 0, wPy = 0, wUn = 10 },
    new LangKWord{regexp = new("Start\\(", RegexOptions.None, TimeSpan.FromSeconds(10)),                                         wCs = 3, wCp = 3, wJv = 1, wJs = 1, wPy = 0, wUn = 10 },
    new LangKWord{regexp = new("Awake\\(", RegexOptions.None, TimeSpan.FromSeconds(10)),                                         wCs = 3, wCp = 3, wJv = 1, wJs = 1, wPy = 0, wUn = 10 },
    new LangKWord{regexp = new("Update\\(", RegexOptions.None, TimeSpan.FromSeconds(10)),                                        wCs = 3, wCp = 3, wJv = 1, wJs = 1, wPy = 0, wUn = 10 },
    new LangKWord{regexp = new("Debug\\.Log\\(", RegexOptions.None, TimeSpan.FromSeconds(10)),                                   wCs = 0, wCp = 0, wJv = 0, wJs = 0, wPy = 0, wUn = 10 },
    new LangKWord{regexp = new("OnTriggerEnter\\(", RegexOptions.None, TimeSpan.FromSeconds(10)),                                wCs = 4, wCp = 3, wJv = 0, wJs = 0, wPy = 0, wUn = 10 },
    new LangKWord{regexp = new("OnTriggerEnter2D\\(", RegexOptions.None, TimeSpan.FromSeconds(10)),                              wCs = 4, wCp = 3, wJv = 0, wJs = 0, wPy = 0, wUn = 10 },
    new LangKWord{regexp = new("OnCollisionEnter\\(", RegexOptions.None, TimeSpan.FromSeconds(10)),                              wCs = 4, wCp = 3, wJv = 0, wJs = 0, wPy = 0, wUn = 10 },
    new LangKWord{regexp = new("OnCollisionEnter2D\\(", RegexOptions.None, TimeSpan.FromSeconds(10)),                            wCs = 4, wCp = 3, wJv = 0, wJs = 0, wPy = 0, wUn = 10 },
    new LangKWord{regexp = new("\\.position", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                                wCs = 2, wCp = 1, wJv = 0, wJs = 0, wPy = 0, wUn = 2 },
    new LangKWord{regexp = new("\\.rotation", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                                wCs = 2, wCp = 2, wJv = 0, wJs = 0, wPy = 0, wUn = 2 },
    new LangKWord{regexp = new("\\.Count", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                                   wCs = 2, wCp = 2, wJv = 0, wJs = 0, wPy = 0, wUn = 2 },
    new LangKWord{regexp = new("\\.Length", RegexOptions.None, TimeSpan.FromSeconds(10)),                                        wCs = 2, wCp = 2, wJv = 0, wJs = 0, wPy = 0, wUn = 2 },
    new LangKWord{regexp = new("\\.length", RegexOptions.None, TimeSpan.FromSeconds(10)),                                        wCs = 0, wCp = 2, wJv = 0, wJs = 3, wPy = 0, wUn = 0 },
    new LangKWord{regexp = new("[a-z0-9]\\([^\n]*\\)", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                       wCs = 2, wCp = 2, wJv = 2, wJs = 2, wPy = 0, wUn = 0 },
    new LangKWord{regexp = new("\\{.*\\}", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                                   wCs = 2, wCp = 2, wJv = 2, wJs = 2, wPy = 0, wUn = 0 },
    new LangKWord{regexp = new("\\[.*\\]", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                                   wCs = 1, wCp = 1, wJv = 0, wJs = 0, wPy = 0, wUn = 0 },
    new LangKWord{regexp = new("#include\\s+[\"<]", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                          wCs = 0, wCp = 9, wJv = 0, wJs = 0, wPy = 0, wUn = 0 },
    new LangKWord{regexp = new("#define\\s+", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                                wCs = 0, wCp = 9, wJv = 0, wJs = 0, wPy = 0, wUn = 0 },
    new LangKWord{regexp = new("[^#]include\\s+[\"<]", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                       wCs = 0, wCp = 0, wJv = 9, wJs = 0, wPy = 0, wUn = 0 },
    new LangKWord{regexp = new("using((?!::).)*;", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                           wCs = 9, wCp = 0, wJv = 0, wJs = 0, wPy = 0, wUn = 0 },
    new LangKWord{regexp = new("using unityengine;", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                         wCs = 1, wCp = 0, wJv = 0, wJs = 0, wPy = 0, wUn = 20 },
    new LangKWord{regexp = new("using[^;]+::[^;]+;", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                         wCs = 0, wCp = 9, wJv = 0, wJs = 0, wPy = 0, wUn = 0 },
    new LangKWord{regexp = new("std::", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                                      wCs = 0, wCp = 9, wJv = 0, wJs = 0, wPy = 0, wUn = 0 },
    new LangKWord{regexp = new("[!=]==", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                                     wCs = 0, wCp = 0, wJv = 0, wJs = 9, wPy = 0, wUn = 0 },
    new LangKWord{regexp = new("auto", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                                       wCs = 0, wCp = 5, wJv = 0, wJs = 0, wPy = 0, wUn = 0 },
    new LangKWord{regexp = new("public\\s+[a-z0-9<>]+\\s[a-z0-9]+\\s*;", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),     wCs = 9, wCp = 0, wJv = 0, wJs = 0, wPy = 0, wUn = 2 },
    new LangKWord{regexp = new("private\\s+[a-z0-9<>]+\\s[a-z0-9]+\\s*;", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),    wCs = 9, wCp = 0, wJv = 0, wJs = 0, wPy = 0, wUn = 2 },
    new LangKWord{regexp = new("public\\s", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                                  wCs = 1, wCp = 1, wJv = 1, wJs = 0, wPy = 0, wUn = 0 },
    new LangKWord{regexp = new("public:", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                                    wCs = 0, wCp = 5, wJv = 0, wJs = 0, wPy = 0, wUn = 0 },
    new LangKWord{regexp = new("private:", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                                   wCs = 0, wCp = 5, wJv = 0, wJs = 0, wPy = 0, wUn = 0 },
    new LangKWord{regexp = new("private\\s", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                                 wCs = 1, wCp = 1, wJv = 1, wJs = 0, wPy = 0, wUn = 0 },
    new LangKWord{regexp = new("\\};", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                                       wCs = 0, wCp = 2, wJv = 2, wJs = 1, wPy = 0, wUn = 0 },
    new LangKWord{regexp = new("let\\s+[a-z0-9_]+\\s*=", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                     wCs = 0, wCp = 0, wJv = 0, wJs = 9, wPy = 0, wUn = 0 },
    new LangKWord{regexp = new("import\\s[a-z][a-z0-9]+", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                    wCs = 0, wCp = 0, wJv = 0, wJs = 0, wPy = 4, wUn = 0 },
    new LangKWord{regexp = new("'''[\\sa-z0-9]+", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                            wCs = 0, wCp = 0, wJv = 0, wJs = 0, wPy = 4, wUn = 0 },
    new LangKWord{regexp = new("for\\s[a-z][a-z0-9]*\\sin.+:", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),               wCs = 0, wCp = 0, wJv = 0, wJs = 0, wPy = 4, wUn = 0 },
    new LangKWord{regexp = new("print\\(\"", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                                 wCs = 2, wCp = 0, wJv = 0, wJs = 0, wPy = 4, wUn = 0 },
    new LangKWord{regexp = new("Console\\.Write", RegexOptions.None, TimeSpan.FromSeconds(10)),                                  wCs = 2, wCp = 0, wJv = 0, wJs = 0, wPy = 0, wUn = 0 },
    new LangKWord{regexp = new("console\\.log", RegexOptions.None, TimeSpan.FromSeconds(10)),                                    wCs = 0, wCp = 0, wJv = 0, wJs = 4, wPy = 0, wUn = 0 },
    new LangKWord{regexp = new("else:", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                                      wCs = 0, wCp = 0, wJv = 0, wJs = 0, wPy = 8, wUn = 0 },
    new LangKWord{regexp = new("\\[(\\s*[0-9]+\\s*\\,{0,1})+\\s*\\]", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),        wCs = 0, wCp = 0, wJv = 0, wJs = 4, wPy = 5, wUn = 0 },
    new LangKWord{regexp = new("\\[(\\s*\"[^\"]*\"\\s*\\,{0,1})+\\s*\\]", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),    wCs = 0, wCp = 0, wJv = 0, wJs = 4, wPy = 5, wUn = 0 },
    new LangKWord{regexp = new("while[\\sa-z0-9\\(\\)]+:\\n", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                wCs = 0, wCp = 0, wJv = 0, wJs = 0, wPy = 5, wUn = 0 },
    new LangKWord{regexp = new("\\s*#\\s*[a-z0-9]", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10)),                          wCs = 0, wCp = 0, wJv = 0, wJs = 0, wPy = 6, wUn = 0 },
    new LangKWord{regexp = new("\\{.+\"{0,1}[a-z0-9_]+\"{0,1}\\s*:\\s*((\".*\")|[0-9\\.]+)\\s*[,\\}]", RegexOptions.IgnoreCase | RegexOptions.Singleline, TimeSpan.FromSeconds(10)),   wCs = 0, wCp = 0, wJv = 0, wJs = 9, wPy = 0, wUn = 0 },
    new LangKWord{regexp = new("System\\.out\\.println", RegexOptions.None, TimeSpan.FromSeconds(10)),                           wCs = 0, wCp = 0, wJv = 9, wJs = 0, wPy = 0, wUn = 0 },
    new LangKWord{regexp = new("String\\[\\]", RegexOptions.None, TimeSpan.FromSeconds(10)),                                     wCs = 0, wCp = 0, wJv = 9, wJs = 0, wPy = 0, wUn = 0 },
    new LangKWord{regexp = new("\\?\\.", RegexOptions.None, TimeSpan.FromSeconds(10)),                                           wCs = 2, wCp = 0, wJv = 0, wJs = 0, wPy = 0, wUn = 0 },
    new LangKWord{regexp = new("\\-\\>", RegexOptions.None, TimeSpan.FromSeconds(10)),                                           wCs = 0, wCp = 9, wJv = 0, wJs = 0, wPy = 0, wUn = 0 },
  };

  public class LangKWord {
    public Regex regexp;
    public int wCs; // Weight for C#
    public int wCp; // Weight for C++
    public int wJv; // Weight for Java
    public int wJs; // Weight for Javascript
    public int wPy; // Weight for Python
    public int wUn; // Weight for Unity
  }
}