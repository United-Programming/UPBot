using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

/// <summary>
/// Utility functions that don't belong to a specific class or a specific command
/// "General-purpose" function, which can be needed anywhere.
/// </summary>
public static class Utils
{
  public const int vmajor = 0, vminor = 1, vbuild = 5;


  /// <summary>
  /// Common colors
  /// </summary>
  public static readonly DiscordColor Red = new DiscordColor("#f50f48");
  public static readonly DiscordColor Green = new DiscordColor("#32a852");
  public static readonly DiscordColor LightBlue = new DiscordColor("#34cceb");
  public static readonly DiscordColor Yellow = new DiscordColor("#f5bc42");
  
  // Fields relevant for InitClient()
  private static DiscordClient client;
  private static DateTimeFormatInfo sortableDateTimeFormat;
  private static StreamWriter logs;
  private static DiscordMember mySelf;
  private static DiscordGuild guild;

  public static string GetVersion() {
    return vmajor + "." + vminor + "." + vbuild + "b - 2022/01/24";
  }

  /// <summary>
  /// Returns the Bot user as Member of the United Programming Guild
  /// </summary>
  /// <returns></returns>
  public static DiscordMember GetMyself() {
    if (mySelf==null) mySelf = client.Guilds[830900174553481236ul].CurrentMember;
    return mySelf;
  }

  /// <summary>
  /// Gets the UnitedProgramming Guild
  /// </summary>
  /// <returns></returns>
  public static DiscordGuild GetGuild() {
    if (guild != null) return guild;
    while (client == null) Task.Delay(1000);
    while (client.Guilds == null) Task.Delay(1000);
    while (client.Guilds.Count == 0) Task.Delay(1000);

    guild = client.Guilds[830900174553481236ul]; // United programming GUID
    return guild;
  }



  public static void InitClient(DiscordClient c) {
    client = c;
    thinkingAsError = DiscordEmoji.FromUnicode("🤔");
    emojiIDs = new [] {
      830907665869570088ul, // OK = 0,
      830907684085039124ul, // KO = 1,
      840702597216337990ul, // whatthisguysaid = 2,
      830907626928996454ul, // StrongSmile = 3,
      831465408874676273ul, // Cpp = 4,
      831465428214743060ul, // CSharp = 5,
      875852276017815634ul, // Java = 6,
      876103767068647435ul, // Javascript = 7,
      831465381016895500ul, // Python = 8,
      831407996453126236ul, // UnitedProgramming = 9,
      830908553908060200ul, // Unity = 10,
      830908576951304212ul, // Godot = 11,
      876180793213464606ul  // AutoRefactored = 12,
    };
    sortableDateTimeFormat = CultureInfo.GetCultureInfo("en-US").DateTimeFormat;
    string logPath = ConstructPath("Logs", "BotLogs " + DateTime.Now.ToString("yyyyMMdd"), ".logs");
    if (File.Exists(logPath)) logs = new StreamWriter(logPath, append: true);
    else logs = File.CreateText(logPath);
  }

  internal static string GetSafeMemberName(CommandContext ctx, ulong userSnoflake) {
    try {
      return ctx.Guild.GetMemberAsync(userSnoflake).Result.DisplayName;
    } catch (Exception e) {
      Log("Invalid user snowflake: " + userSnoflake + " -> " + e.Message);
      return null;
    }
  }

  /// <summary>
  /// Change a string based on the count it's referring to (e.g. "one apple", "two apples")
  /// </summary>
  /// <param name="count">The count, the string is referring to</param>
  /// <param name="singular">The singular version (referring to only one)</param>
  /// <param name="plural">The singular version (referring to more than one)</param>
  public static string PluralFormatter(int count, string singular, string plural)
  {
    return count > 1 ? plural : singular;
  }

  /// <summary>
  /// This functions constructs a path in the base directory of the current executable
  /// with a given raw file name and the fileSuffix (file type)
  /// NOTE: The file suffix must contain a period (e.g. ".txt" or ".png")
  /// </summary>
  /// <param name="directoryName">The name of the final folder, in which the file will be saved</param>
  /// <param name="fileNameRaw">The name of the file (without file type)</param>
  /// <param name="fileSuffix">The file-suffix (file-type, e.g. ".txt" or ".png")</param>
  public static string ConstructPath(string directoryName, string fileNameRaw, string fileSuffix)
  {
    string directoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, directoryName);
    if (!Directory.Exists(Path.Combine(directoryPath)))
      Directory.CreateDirectory(directoryPath);
    
    return Path.Combine(directoryPath, fileNameRaw.Trim().ToLowerInvariant() + fileSuffix);
  }

  /// <summary>
  /// Builds a Discord embed with a given TITLE, DESCRIPTION and COLOR
  /// </summary>
  /// <param name="title">Embed title</param>
  /// <param name="description">Embed description</param>
  /// <param name="color">Embed color</param>
  public static DiscordEmbedBuilder BuildEmbed(string title, string description, DiscordColor color)
  {
    DiscordEmbedBuilder b = new DiscordEmbedBuilder();
    b.Title = title;
    b.Color = color;
    b.Description = description;
    
    return b;
  }

  /// <summary>
  /// Builds a Discord embed with a given TITLE, DESCRIPTION and COLOR
  /// and SENDS the embed as a message
  /// </summary>
  /// <param name="title">Embed title</param>
  /// <param name="description">Embed description</param>
  /// <param name="color">Embed color</param>
  /// <param name="ctx">CommandContext, required to send a message</param>
  /// <param name="respond">Respond to original message or send an independent message?</param>
  public static async Task<DiscordMessage> BuildEmbedAndExecute(string title, string description, DiscordColor color, 
    CommandContext ctx, bool respond)
  {
    var embedBuilder = BuildEmbed(title, description, color);
    return await LogEmbed(embedBuilder, ctx, respond);
  }

  /// <summary>
  /// Quick shortcut to generate an error message
  /// </summary>
  /// <param name="error">The error to display</param>
  /// <returns></returns>
  internal static DiscordEmbed GenerateErrorAnswer(string cmd, Exception exception) {
    DiscordEmbedBuilder e = new DiscordEmbedBuilder {
      Color = Red,
      Title = "Error in " + cmd,
      Description = exception.Message
    };
    Log("Error in " + cmd + ": " + exception.Message);
    return e.Build();
  }

  /// <summary>
  /// Logs an embed as a message in the relevant channel
  /// </summary>
  /// <param name="builder">Embed builder with the embed template</param>
  /// <param name="ctx">CommandContext, required to send a message</param>
  /// <param name="respond">Respond to original message or send an independent message?</param>
  public static async Task<DiscordMessage> LogEmbed(DiscordEmbedBuilder builder, CommandContext ctx, bool respond)
  {
    if (respond)
      return await ctx.RespondAsync(builder.Build());

    return await ctx.Channel.SendMessageAsync(builder.Build());
  } 

  private static DiscordEmoji[] emojis;
  private static ulong[] emojiIDs;
  private static DiscordEmoji thinkingAsError;

  /// <summary>
  /// This function gets the Emoji object corresponding to the id fromthe server.
  /// </summary>
  /// <param name="id">The id of the emoji to get</param>
  /// <returns>The requested emoji or the Thinking emoji in case something went wrong</returns>
  public static DiscordEmoji GetEmoji(ulong id) {
    try {
      DiscordEmoji emoji = guild.GetEmojiAsync(id).Result;
      return emoji;
    } catch (Exception) { }
    return thinkingAsError;
  }

  /// <summary>
  /// This function gets the Emoji object corresponding to the emojis of the server.
  /// They are cached to improve performance (this command will not work on other servers.)
  /// </summary>
  /// <param name="emoji">The emoji to get, specified from the enum</param>
  /// <returns>The requested emoji or the Thinking emoji in case something went wrong</returns>
  public static DiscordEmoji GetEmoji(EmojiEnum emoji) {
    if (emojis == null) emojis = new DiscordEmoji[13];

    int index = (int)emoji;
    if (index < 0 || index >= emojis.Length) {
      Console.WriteLine("WARNING: Requested wrong emoji");
      return thinkingAsError;
    }
    if (emojis[index] == null) {
      if (!DiscordEmoji.TryFromGuildEmote(client, emojiIDs[index], out DiscordEmoji res)) {
        Console.WriteLine("WARNING: Cannot get requested emoji: " + emoji.ToString());
        return thinkingAsError;
      }
      emojis[index] = res;
    }
    return emojis[index];
  }

  /// <summary>
  /// Used to get the <:UnitedProgramming:831407996453126236> format of an emoji object
  /// </summary>
  /// <param name="emoji">The emoji to convert</param>
  /// <returns>A string representation of the emoji that can be used in a message</returns>
  public static string GetEmojiSnowflakeID(DiscordEmoji emoji) {
    return "<" + emoji.GetDiscordName() + emoji.Id.ToString() + ">";
  }

  /// <summary>
  /// Used to get the <:UnitedProgramming:831407996453126236> format of an emoji object
  /// </summary>
  /// <param name="emoji">The emoji enum for the emoji to convert</param>
  /// <returns>A string representation of the emoji that can be used in a message</returns>
  public static string GetEmojiSnowflakeID(EmojiEnum emoji) {
    DiscordEmoji em = GetEmoji(emoji);
    return "<" + em.GetDiscordName() + em.Id.ToString() + ">";
  }

  /// <summary>
  /// Adds a line in the logs telling which user used what command
  /// </summary>
  /// <param name="ctx"></param>
  /// <returns></returns>
  internal static void LogUserCommand(CommandContext ctx) {
    Log(DateTime.Now.ToString(sortableDateTimeFormat.SortableDateTimePattern) + "=> " + ctx.Command.Name + " FROM " + ctx.Member.DisplayName);
  }

  /// <summary>
  /// Logs a text in the console
  /// </summary>
  /// <param name="msg"></param>
  /// <returns></returns>
  internal static void Log(string msg) {
    Console.WriteLine(msg);
    try {
      logs.WriteLine(msg);
      logs.FlushAsync();
    } catch (Exception e) {
      _ = e.Message;
    }
  }
  
  internal static async Task ErrorCallback(CommandErrors error, CommandContext ctx, params object[] additionalParams)
  {
    DiscordColor red = Red;
    string message = string.Empty;
    bool respond = false;
    switch (error)
    {
      case CommandErrors.CommandExists:
        respond = true;
        if (additionalParams[0] is string name)
          message = $"There is already a command containing the alias {additionalParams[0]}";
        else
          throw new System.ArgumentException("This error type 'CommandErrors.CommandExists' requires a string");
        break;
      case CommandErrors.UnknownError:
        message = "Unknown error!";
        respond = false;
        break;
      case CommandErrors.InvalidParams:
        message = "The given parameters are invalid. Enter `\\help [commandName]` to get help with the usage of the command.";
        respond = true;
        break;
      case CommandErrors.InvalidParamsDelete:
        if (additionalParams[0] is int count)
          message = $"You can't delete {count} messages. Try to eat {count} apples, does that make sense?";
        else
          goto case CommandErrors.InvalidParams;
        break;
      case CommandErrors.MissingCommand:
        message = "There is no command with this name! If it's a CC, please don't use an alias, use the original name!";
        respond = true;
        break;
      case CommandErrors.NoCustomCommands:
        message = "There are no CC's currently.";
        respond = false;
        break;
      case CommandErrors.CommandNotSpecified:
        message = "No command name was specified. Enter `\\help ccnew` to get help with the usage of the command.";
        respond = true;
        break;
    }
        
    await Utils.BuildEmbedAndExecute("Error", message, red, ctx, respond);
  }

  /// <summary>
  /// Used to delete some messages after a while
  /// </summary>
  /// <param name="msg1"></param>
  public static Task DeleteDelayed(int seconds, DiscordMessage msg1) {
    Task.Run(() => DelayAfterAWhile(msg1, seconds * 1000));
    return Task.FromResult(0);
  }

  /// <summary>
  /// Used to delete some messages after a while
  /// </summary>
  /// <param name="msg1"></param>
  /// <param name="msg2"></param>
  public static Task DeleteDelayed(int seconds, DiscordMessage msg1, DiscordMessage msg2) {
    Task.Run(() => DelayAfterAWhile(msg1, seconds * 1000));
    Task.Run(() => DelayAfterAWhile(msg2, seconds * 1000));
    return Task.FromResult(0);
  }

  /// <summary>
  /// Used to delete some messages after a while
  /// </summary>
  /// <param name="msg1"></param>
  /// <param name="msg2"></param>
  /// <param name="msg3"></param>
  public static Task DeleteDelayed(int seconds, DiscordMessage msg1, DiscordMessage msg2, DiscordMessage msg3) {
    Task.Run(() => DelayAfterAWhile(msg1, seconds * 1000));
    Task.Run(() => DelayAfterAWhile(msg2, seconds * 1000));
    Task.Run(() => DelayAfterAWhile(msg3, seconds * 1000));
    return Task.FromResult(0);
  }

  static void DelayAfterAWhile(DiscordMessage msg, int delay) {
    try {
      Task.Delay(delay).Wait();
      msg.DeleteAsync().Wait();
    } catch (Exception) { }
  }

  public static bool IsAdmin(DiscordMember m) {
    if (m.Permissions.HasFlag(Permissions.Administrator)) return true;
    if (m.Permissions.HasFlag(Permissions.ManageMessages)) return true;
    foreach (DiscordRole r in m.Roles)
      if (r.Id == 830901562960117780ul /* Owner */ || r.Id == 830901743624650783ul /* Mod */ || r.Id == 831050318171078718ul /* Helper */) return true;
    return false;
  }

}

public enum EmojiEnum {
  None = -1,
  OK = 0,
  KO = 1,
  WhatThisGuySaid = 2,
  StrongSmile = 3,
  Cpp = 4,
  CSharp = 5,
  Java = 6,
  Javascript = 7,
  Python = 8,
  UnitedProgramming = 9,
  Unity = 10,
  Godot = 11,
  AutoRefactored = 12,
}

public enum CommandErrors {
    InvalidParams,
    InvalidParamsDelete,
    CommandExists,
    UnknownError,
    MissingCommand,
    NoCustomCommands,
    CommandNotSpecified
}
