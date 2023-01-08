using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

/// <summary>
/// Utility functions that don't belong to a specific class or a specific command
/// "General-purpose" function, which can be needed anywhere.
/// </summary>
public static class Utils {
  public const int vmajor = 0, vminor = 3, vbuild = 4;
  public const char vrev = 'c';
  public static string LogsFolder = "./";
  public readonly static System.Diagnostics.StackTrace sttr = new();

  /// <summary>
  /// Common colors
  /// </summary>
  public static readonly DiscordColor Red = new("#f50f48");
  public static readonly DiscordColor Green = new("#32a852");

  public static readonly DiscordColor LightBlue = new("#34cceb");
  public static readonly DiscordColor Yellow = new("#f5bc42");

  // Fields relevant for InitClient()
  private static DiscordClient client;

  private class LogInfo {
    public StreamWriter sw;
    public string path;
  }

  readonly private static Dictionary<string, LogInfo> logs = new();

  public static string GetVersion() {
    return vmajor + "." + vminor + "." + vbuild + vrev + " - 2023/01/08";
  }

  public static DiscordClient GetClient() {
    return client;
  }

  public static void InitClient(DiscordClient c) {
    client = c;
    if (!DiscordEmoji.TryFromName(client, ":thinking:", out thinkingAsError)) {
      thinkingAsError = DiscordEmoji.FromUnicode("🤔");
    }
    emojiNames = new[] {
      ":thinking:", // Thinking = 0,
      ":OK:", // OK = 1,
      ":KO:", // KO = 2,
      ":whatthisguysaid:", // whatthisguysaid = 3,
      ":StrongSmile:", // StrongSmile = 4,
      ":CPP:", // Cpp = 5,
      ":CSharp:", // CSharp = 6,
      ":Java:", // Java = 7,
      ":Javascript:", // Javascript = 8,
      ":Python:", // Python = 9,
      ":UnitedProgramming:", // UnitedProgramming = 10,
      ":Unity:", // Unity = 11,
      ":Godot:", // Godot = 12,
      ":AutoRefactored:",  // AutoRefactored = 13,
      ":CodeMonkey:", // CodeMonkey = 14,
      ":TaroDev:", // TaroDev = 15,
    };
    emojiUrls = new string[emojiNames.Length];
    emojiSnowflakes = new string[emojiNames.Length];
  }

  public static void InitLogs(string guild) {
    string logPath = Path.Combine(LogsFolder, "BotLogs " + guild + " " + DateTime.Now.ToString("yyyyMMdd") + ".logs");
    LogInfo l;
    if (logs.ContainsKey(guild)) l = logs[guild];
    else {
      l = new LogInfo();
      logs[guild] = l;
    }
    l.path = logPath;
    if (File.Exists(logPath)) logs[guild].sw = new StreamWriter(logPath, append: true);
    else logs[guild].sw = File.CreateText(logPath);
  }

  public static string GetLogsPath(string guild) {
    if (!logs.ContainsKey(guild)) return null;
    return logs[guild].path;
  }

  public static string GetLastLogsFolder(string guild, string logPath) {
    string zipFolder = Path.Combine(LogsFolder, guild + " ZippedLog/");
    if (!Directory.Exists(zipFolder)) Directory.CreateDirectory(zipFolder);
    FileInfo fi = new(logPath);
    File.Copy(fi.FullName, Path.Combine(zipFolder, fi.Name), true);
    return zipFolder;
  }

  public static string GetAllLogsFolder(string guild) {
    Regex logsRE = new(@"BotLogs\s" + guild + @"\s[0-9]{8}\.logs", RegexOptions.IgnoreCase);
    string zipFolder = Path.Combine(LogsFolder, guild + " ZippedLogs/");
    if (!Directory.Exists(zipFolder)) Directory.CreateDirectory(zipFolder);
    foreach (var file in Directory.GetFiles(LogsFolder, "*.logs")) {
      if (logsRE.IsMatch(file)) {
        FileInfo fi = new(file);
        File.Copy(fi.FullName, Path.Combine(zipFolder, fi.Name), true);
      }
    }
    return zipFolder;
  }

  public static int DeleteAllLogs(string guild) {
    Regex logsRE = new(@"BotLogs\s" + guild + @"\s[0-9]{8}\.logs", RegexOptions.IgnoreCase);
    List<string> toDelete = new();
    foreach (var file in Directory.GetFiles(LogsFolder, "*.logs")) {
      if (logsRE.IsMatch(file)) {
        FileInfo fi = new(file);
        toDelete.Add(fi.FullName);
      }
    }
    LogInfo li = null;
    if (logs.ContainsKey(guild)) {
      li = logs[guild];
      li.sw.Close();
      li.sw = null;
    }

    int num = 0;
    foreach (var file in toDelete) {
      try {
        File.Delete(file);
        num++;
      } catch { }
    }
    if (li != null && li.sw == null) {
      InitLogs(guild);
    }
    return num;
  }

  /// <summary>
  /// Builds a Discord embed with a given TITLE, DESCRIPTION and COLOR
  /// </summary>
  /// <param name="title">Embed title</param>
  /// <param name="description">Embed description</param>
  /// <param name="color">Embed color</param>
  public static DiscordEmbedBuilder BuildEmbed(string title, string description, DiscordColor color) {
    return new DiscordEmbedBuilder {
      Title = title,
      Color = color,
      Description = description
    };
  }

  /// <summary>
  /// Quick shortcut to generate an error message
  /// </summary>
  /// <param name="error">The error to display</param>
  /// <returns></returns>
  internal static DiscordEmbed GenerateErrorAnswer(string guild, string cmd, Exception exception) {
    string stack = exception.StackTrace;
    // Find all `.cs:` strings
    int pos = stack.IndexOf(".cs:");
    while (pos != -1) {
      // Find the " in "
      int inPos = stack.LastIndexOf(" in ", pos);
      if (inPos == -1) break;
      int bsPos = stack.LastIndexOf("\\", pos);
      int slPos = stack.LastIndexOf("/", pos);
      if (bsPos == -1 && slPos == -1) break;
      int sepPos = bsPos < slPos ? slPos : bsPos;
      stack = stack[..(inPos + 4)] + stack[(sepPos + 1)..];

      pos = stack.IndexOf(".cs:"); // This will be the previous cs, find the next if any
      pos = stack.IndexOf(".cs:", pos + 1);
    }
    DiscordEmbedBuilder e = new() {
      Color = Red,
      Title = "Error in " + cmd,
      Description = exception.Message + "\n" + stack
    };
    Console.ForegroundColor = ConsoleColor.Red;
    Log($"Error in " + cmd + ": " + exception.Message, guild);
    Console.ForegroundColor = ConsoleColor.White;
    return e.Build();
  }

  /// <summary>
  /// Quick shortcut to generate an error message
  /// </summary>
  /// <param name="error">The error to display</param>
  /// <returns></returns>
  internal static DiscordEmbed GenerateErrorAnswer(string guild, string cmd, string message) {
    DiscordEmbedBuilder e = new() {
      Color = Red,
      Title = "Error in " + cmd,
      Description = message
    };
    Console.ForegroundColor = ConsoleColor.Red;
    Log($"Error in " + cmd + ": " + message, guild);
    Console.ForegroundColor = ConsoleColor.White;
    return e.Build();
  }

  private static string[] emojiNames;
  private static string[] emojiUrls;
  private static string[] emojiSnowflakes;
  private static DiscordEmoji thinkingAsError;

  /// <summary>
  /// This function gets the Emoji object corresponding to the emojis of the server.
  /// They are cached to improve performance (this command will not work on other servers.)
  /// </summary>
  /// <param name="emoji">The emoji to get, specified from the enum</param>
  /// <returns>The requested emoji or the Thinking emoji in case something went wrong</returns>
  public static DiscordEmoji GetEmoji(EmojiEnum emoji) {
    int index = (int)emoji;
    if (index < 0 || index >= emojiNames.Length) {
      Console.ForegroundColor = ConsoleColor.Yellow;
      Console.WriteLine("WARNING: Requested wrong emoji");
      Console.ForegroundColor = ConsoleColor.White;
      return thinkingAsError;
    }
    if (!DiscordEmoji.TryFromName(client, emojiNames[index], out DiscordEmoji res)) {
      Console.ForegroundColor = ConsoleColor.Yellow;
      Console.WriteLine($"WARNING: Cannot get requested emoji: {emoji.ToString()}");
      Console.ForegroundColor = ConsoleColor.White;
      return thinkingAsError;
    }
    return res;
  }


  /// <summary>
  /// This function gets the url of the Emoji based on its name.
  /// No access to discord (so if the URL is no more valid it will fail (invalid image))
  /// </summary>
  /// <param name="emoji">The emoji to get, specified from the enum</param>
  /// <returns>The requested url for the emoji</returns>
  public static string GetEmojiURL(EmojiEnum emoji) {
    int index = (int)emoji;
    if (index < 0 || index >= emojiNames.Length) {
      Console.ForegroundColor = ConsoleColor.Yellow;
      Console.WriteLine($"WARNING: Requested wrong emoji");
      Console.ForegroundColor = ConsoleColor.White;
      return thinkingAsError.Url;
    }

    if (!string.IsNullOrEmpty(emojiUrls[index])) return emojiUrls[index];
    if (!DiscordEmoji.TryFromName(client, emojiNames[index], out DiscordEmoji res)) {
      Console.ForegroundColor = ConsoleColor.Yellow;
      Console.WriteLine($"WARNING: Cannot get requested emoji: {emoji.ToString()}");
      Console.ForegroundColor = ConsoleColor.White;
      return thinkingAsError;
    }
    emojiUrls[index] = res.Url;
    return res.Url;
  }

  /// <summary>
  /// Used to get the <:UnitedProgramming:831407996453126236> format of an emoji object
  /// </summary>
  /// <param name="emoji">The emoji to convert</param>
  /// <returns>A string representation of the emoji that can be used in a message</returns>
  public static string GetEmojiSnowflakeID(EmojiEnum emoji) {
    int index = (int)emoji;
    if (index < 0 || index >= emojiNames.Length) {
      return "<" + thinkingAsError.GetDiscordName() + thinkingAsError.Id.ToString() + ">";
    }

    if (!string.IsNullOrEmpty(emojiSnowflakes[index])) return emojiSnowflakes[index];
    if (!DiscordEmoji.TryFromName(client, emojiNames[index], out DiscordEmoji res)) {
      Console.ForegroundColor = ConsoleColor.Yellow;
      Console.WriteLine($"WARNING: Cannot get requested emoji: {emoji.ToString()}");
      Console.ForegroundColor = ConsoleColor.White;
      return thinkingAsError;
    }
    emojiSnowflakes[index] = "<" + res.GetDiscordName() + res.Id.ToString() + ">";
    return emojiSnowflakes[index];
  }

  /// <summary>
  /// Used to get the <:UnitedProgramming:831407996453126236> format of an emoji object
  /// </summary>
  /// <param name="emoji">The emoji to convert</param>
  /// <returns>A string representation of the emoji that can be used in a message</returns>
  public static string GetEmojiSnowflakeID(DiscordEmoji emoji) {
    if (emoji == null) return "";
    return "<" + emoji.GetDiscordName() + emoji.Id.ToString() + ">";
  }

  internal static void LogUserCommand(InteractionContext ctx) {
    Console.ForegroundColor = ConsoleColor.Blue;
    string log = $"{DateTime.Now:yyyy/MM/dd hh:mm:ss} => {ctx.CommandName} FROM {ctx.Member.DisplayName}";
    if (ctx.Interaction.Data.Options != null)
      foreach (var p in ctx.Interaction.Data.Options) log += $" [{p.Name}]{p.Value}";
    Log(log, ctx.Guild.Name);
    Console.ForegroundColor = ConsoleColor.White;
  }

  /// <summary>
  /// Logs a text in the console
  /// </summary>
  /// <param name="msg"></param>
  /// <returns></returns>
  internal static void Log(string msg, string guild) {
    if (guild == null) guild = "GLOBAL";
    Console.WriteLine($"{guild}: {msg}");
    try {
      if (!logs.ContainsKey(guild)) InitLogs(guild);
      logs[guild].sw.WriteLine(msg.Replace("```", ""));
      logs[guild].sw.Flush();
    } catch (Exception e) {
      Console.ForegroundColor = ConsoleColor.Red;
      Console.WriteLine("Log error with stack trace following");
      Console.WriteLine("Log error: " + e.Message);
      Console.WriteLine(sttr.ToString());
      Console.WriteLine("Log error completed.");
      Console.ForegroundColor = ConsoleColor.White;
    }
  }

  /// <summary>
  /// Used to delete a folder after a while
  /// </summary>
  /// <param name="msg1"></param>
  public static Task DeleteFolderDelayed(int seconds, string path) {
    Task.Run(() => {
      try {
        Task.Delay(seconds * 1000).Wait();
        Directory.Delete(path, true);
      } catch (Exception ex) {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Cannot delete file: " + path + ": " + ex.Message);
        Console.ForegroundColor = ConsoleColor.White;
      }
    });
    return Task.FromResult(0);
  }

  /// <summary>
  /// Used to delete a file after a while
  /// </summary>
  /// <param name="msg1"></param>
  public static Task DeleteFileDelayed(int seconds, string path) {
    Task.Run(() => {
      try {
        Task.Delay(seconds * 1000).Wait();
        File.Delete(path);
      } catch (Exception ex) {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Cannot delete file: " + path + ": " + ex.Message);
        Console.ForegroundColor = ConsoleColor.White;
      }
    });
    return Task.FromResult(0);
  }

  /// <summary>
  /// Used to delete some messages after a while
  /// </summary>
  /// <param name="msg1"></param>
  public static Task DeleteDelayed(int seconds, DiscordMessage msg1) {
    Task.Run(() => DelayAfterAWhile(msg1, seconds * 1000));
    return Task.FromResult(0);
  }

  static void DelayAfterAWhile(DiscordMessage msg, int delay) {
    try {
      Task.Delay(delay).Wait();
      msg.DeleteAsync().Wait();
    } catch (Exception) { }
  }

  internal async static void DefaultNotAllowed(InteractionContext ctx) {
    await ctx.CreateResponseAsync($"The command {ctx.CommandName} is not allowed.");
    await DeleteDelayed(15, ctx.GetOriginalResponseAsync().Result);
  }


}

public enum EmojiEnum {
  None = -1,
  Thinking = 0,
  OK = 1,
  KO = 2,
  WhatThisGuySaid = 3,
  StrongSmile = 4,
  Cpp = 5,
  CSharp = 6,
  Java = 7,
  Javascript = 8,
  Python = 9,
  UnitedProgramming = 10,
  Unity = 11,
  Godot = 12,
  AutoRefactored = 13,
  CodeMonkey = 14,
  TaroDev = 15,
}
