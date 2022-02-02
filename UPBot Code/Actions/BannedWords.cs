using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Text.RegularExpressions;
using DSharpPlus;
using DSharpPlus.EventArgs;
/// <summary>
/// Task to handle the words considered as banned.
/// The actual check is done using an event from the DiscordClient
/// author: CPU
/// </summary>
public class BannedWords : BaseCommandModule {
  readonly static Regex letters = new Regex(@"[a-zA-Z0-9]");

  internal static async Task CheckMessage(DiscordClient client, MessageCreateEventArgs args) {
    try {
      // Is it from a guild we care?
      if (!Setup.BannedWords.ContainsKey(args.Guild.Id)) return;
      List<string> bannedWords = Setup.BannedWords[args.Guild.Id];
      if (bannedWords.Count == 0) return;


      // Who is the author? If the bot or a mod then ignore
      if (args.Author.IsBot) return;
      DiscordUser user = args.Author;
      DiscordMember member = await args.Guild.GetMemberAsync(user.Id);
      foreach (DiscordRole role in member.Roles) {
        if (!Setup.IsAdminRole(args.Guild.Id, role)) return;
      }

      string msg = args.Message.Content.ToLowerInvariant();
      foreach (string word in bannedWords) {
        int pos = msg.IndexOf(word);
        if (pos == -1) continue;
        if (pos > 0 && letters.IsMatch(msg[pos - 1].ToString())) continue;
        if (pos + word.Length < msg.Length && letters.IsMatch(msg[pos + word.Length].ToString())) continue;

        Utils.Log("Removed word \"" + word + "\" from " + user.Username + " in: " + msg, args.Guild.Name);
        DiscordMessage warning = await args.Message.Channel.SendMessageAsync("Moderate your language, " + user.Mention + ".");
        await args.Message.DeleteAsync("Bad words: " + word);
        Utils.DeleteDelayed(10000, warning).Wait();
        return;
      }
    } catch (Exception ex) {
      await args.Message.RespondAsync(Utils.GenerateErrorAnswer(args.Guild.Name, "BannedWords.CheckMessage", ex));
    }
  }

}
