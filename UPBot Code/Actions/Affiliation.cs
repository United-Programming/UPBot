using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using System.Threading.Tasks;
using System;
using System.Text.RegularExpressions;
using DSharpPlus;
using DSharpPlus.EventArgs;
/// <summary>
/// Task to promote affiliation links if any
/// author: CPU
/// </summary>
public class Affiliation : BaseCommandModule {
  readonly static Regex assetStoreLink = new Regex(@"https://assetstore.unity.com/packages/([^\s\?]*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
  readonly static Regex assetStoreLinkId = new Regex(@"https://assetstore.unity.com/packages/([^\s\?]*)\?[^\s]*aid=([^\s&]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

  internal static async Task CheckMessage(DiscordClient _, MessageCreateEventArgs args) {
    if (args.Guild == null) return;
    try {
      if (args.Author.IsBot) return;
      // Is it from a guild we care?
      if (!Setup.Affiliations.ContainsKey(args.Guild.Id)) return;
      // It is an asset store link?
      Match m = assetStoreLink.Match(args.Message.Content);
      if (!m.Success) return;

      var af = Setup.Affiliations[args.Guild.Id];
      Match mid = assetStoreLinkId.Match(args.Message.Content);
      if (mid.Success && mid.Groups.Count > 1 && mid.Groups[2].Value == af.AffiliationID) return;

      string msg = af.Message;
      string link = "https://assetstore.unity.com/packages/" + m.Groups[1].Value + "?aid=" + af.AffiliationID;
      link = "[" + link + "](" + link + ")";
      if (msg.Contains("%l")) msg = msg.Replace("%l", link);
      else msg += "\n" + link;
      DiscordEmbedBuilder eb = new DiscordEmbedBuilder()
        .WithTitle(af.Title)
        .WithThumbnail(af.IconURL)
        .WithDescription(msg);
      await args.Channel.SendMessageAsync(eb.Build());

    } catch (Exception ex) {
      await args.Message.RespondAsync(Utils.GenerateErrorAnswer(args.Guild.Name, "Affiliation.CheckMessage", ex));
    }
  }

}
