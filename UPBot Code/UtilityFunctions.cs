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
public static class UtilityFunctions
{
  /// <summary>
  /// Common colors
  /// </summary>
  public static readonly DiscordColor Red = new DiscordColor("#f50f48");
  public static readonly DiscordColor Green = new DiscordColor("#32a852");
  public static readonly DiscordColor LightBlue = new DiscordColor("#34cceb");
  
  // ---------------------------
  static DiscordClient client;
  static DateTimeFormatInfo sortableDateTimeFormat;
  static StreamWriter logs;

  public static void InitClient(DiscordClient c) {
    client = c;
    thinkingAsError = DiscordEmoji.FromUnicode("🤔");
    emojiIDs = new ulong[] {
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
    string logPath = ConstructPath("BotLogs " + DateTime.Now.ToString("yyyyMMdd"), ".logs");
    if (File.Exists(logPath)) logs = new StreamWriter(logPath, append: true);
    else logs = File.CreateText(logPath);
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
  public static string ConstructPath(string fileNameRaw, string fileSuffix)
  {
    return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CustomCommands", fileNameRaw.Trim().ToLowerInvariant() + fileSuffix);
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
  public static async Task<DiscordEmbedBuilder> BuildEmbedAndExecute(string title, string description, DiscordColor color, 
    CommandContext ctx, bool respond)
  {
    var embedBuilder = BuildEmbed(title, description, color);
    await LogEmbed(embedBuilder, ctx, respond);
    
    return embedBuilder;
  }

  /// <summary>
  /// Logs an embed as a message in the relevant channel
  /// </summary>
  /// <param name="builder">Embed builder with the embed template</param>
  /// <param name="ctx">CommandContext, required to send a message</param>
  /// <param name="respond">Respond to original message or send an independent message?</param>
  public static async Task LogEmbed(DiscordEmbedBuilder builder, CommandContext ctx, bool respond)
  {
    if (respond)
    {
      await ctx.RespondAsync(builder.Build());
      return;
    }

    await ctx.Channel.SendMessageAsync(builder.Build());
  } 

  static DiscordEmoji[] emojis;
  static ulong[] emojiIDs;
  static DiscordEmoji thinkingAsError;

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
    Console.WriteLine(DateTime.Now.ToString(sortableDateTimeFormat.SortableDateTimePattern) + "=> " + msg);
    try {
      logs.WriteLine(msg);
    } catch (Exception) {}
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

public enum CommandErrors
{
    InvalidParams,
    InvalidParamsDelete,
    CommandExists,
}
