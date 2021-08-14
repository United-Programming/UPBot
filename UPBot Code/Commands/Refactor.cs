using System;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
/// <summary>
/// Command used to refactor as codeblock some code pasted by a user
/// author: CPU
/// </summary>
public class Refactor : BaseCommandModule {

  [Command("refactor")]
  public async Task WhoIsCommand(CommandContext ctx) { // Refactors the previous post, if it is code
    await RefactorCode(ctx, null);
  }

  [Command("refactor")]
  public async Task WhoIsCommand(CommandContext ctx, DiscordMember member) { // Refactor the last post of the specified user in the channel
    await RefactorCode(ctx, member);
  }

  private async Task<Task<DiscordMessage>> RefactorCode(CommandContext ctx, DiscordMember m) {
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

    // FIXME remove the ``` at begin and end, if any. And the code name after initial ```

    string guessed = "C# <:csharp:831465428214743060>";
    int w = weightCs;
    if (weightCp > w) { guessed = "C++ <:cpp:831465408874676273>"; w = weightCp; }
    if (weightJs > w) { guessed = "Javascript <:Javascript:876103767068647435>"; w = weightJs; }
    if (weightJv > w) { guessed = "Java <:java:875852276017815634>"; w = weightJv; }
    if (weightPy > w) { guessed = "Python <:python:831465381016895500>"; w = weightPy; }

    if (w == 0) return ctx.RespondAsync("Nothing to refactor");

    return ctx.RespondAsync("Refactoring: " + toRefactor.Content.Substring(0, 20) + "...\nC# = " + weightCs + " C++ = " + weightCp + " Java = " + weightJv + " Javascript = " + weightJs + " Python = " + weightPy + " guessed language → " + guessed);
  }

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
    new LangKWord{regexp = new Regex("boolean\\s+[a-z]", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),          wCs = 0, wCp = 1, wJv = 2, wJs = 0, wPy = 0 },
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
    new LangKWord{regexp = new Regex("===", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),                       wCs = 0, wCp = 0, wJv = 0, wJs = 9, wPy = 0 },
    new LangKWord{regexp = new Regex("auto", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),                      wCs = 0, wCp = 5, wJv = 0, wJs = 0, wPy = 0 },
    new LangKWord{regexp = new Regex("public\\s+[a-z0-9<>]+\\s[a-z0-9]+\\s*;", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),  wCs = 9, wCp = 0, wJv = 0, wJs = 0, wPy = 0 },
    new LangKWord{regexp = new Regex("private\\s+[a-z0-9<>]+\\s[a-z0-9]+\\s*;", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),  wCs = 9, wCp = 0, wJv = 0, wJs = 0, wPy = 0 },
    new LangKWord{regexp = new Regex("public\\s", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),                 wCs = 1, wCp = 1, wJv = 1, wJs = 0, wPy = 0 },
    new LangKWord{regexp = new Regex("public:", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),                   wCs = 0, wCp = 5, wJv = 0, wJs = 0, wPy = 0 },
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
    new LangKWord{regexp = new Regex("while[\\sa-z0-9\\(\\)]+:\\n", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),   wCs = 0, wCp = 0, wJv = 0, wJs = 0, wPy = 5 },
    new LangKWord{regexp = new Regex("\n\\s*#\\s*[a-z0-9]", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)),   wCs = 0, wCp = 0, wJv = 0, wJs = 0, wPy = 6 },
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