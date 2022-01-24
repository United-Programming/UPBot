using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using System.Threading.Tasks;
using System;
using System.Text.RegularExpressions;
using DSharpPlus;
using DSharpPlus.EventArgs;
/// <summary>
/// Command used to check for false nitro links and spam links
/// author: CPU
/// </summary>
public class CheckSpam : BaseCommandModule {
  readonly static Regex linkRE = new Regex(@"http[s]?://([^/]+)/");
  readonly static Regex wwwRE = new Regex(@"^w[^\.]{0,3}.\.");

  string[] testLinks = { "discord.com", "discordapp.com", "discord.gg",

    "discrodapp.com", "discord.org", "discrodgift.com", "discordapp.gift", "humblebundle.com", "microsoft.com", "google.com",
    "discorx.gift", "dljscord.com", "disboard.org", "dischrdapp.com","discord-cpp.com", "discord-nitro.ru.com","discörd.com","disçordapp.com","dlscord.space",
    "discod.art", "discord-nitro.site", "disscord-nitro.com", "dirscod.com", "dlscord.in", "discorcl.link", "discorb.co", "discord-nitro.su", "dlscord.org", "discord-give.org",

    "steamcommunity.com","store.steampowered.com",
    "steancomunnity.ru", "streamcommunnlty.ru", "streancommunuty.ru", "streamconmunitlu.me", "streamconmunitlu.me", "stearncomminuty.ru", "steamcommunytu.ru",
    "steamcommnuitry.com", "stearncomunitu.ru", "stearncormunsity.com", "steamcommunytiu.ru", "streammcomunnity.ru", "steamcommunytiy.ru", "stearncommunytiy.ru", "strearncomuniity.ru.com",
    "steamcomminytiu.ru", "steamcconuunity.co", "steamcomminytiu.com", "store-stempowered.com", "stemcomnunity.ru.com", "steamcommynitu.ru", 
    "steamcommurnuity.com", "steamcomminutiu.ru", "steamcommunrlity.com", "steamcommytiny.com", "steamcommunityu.ru", "steamgivenitro.com","steamcommunity.link",

    "epicgames.com","www.epicgames.com","ww2.epicgames.com","epycgames.com",
    /*
    "mvncentral.net", "vladvilcu2006.tech", "verble.software", "jonathanhardwick.me", "etc.catering", "tlrepo.cc", "khonsarifamily.tech", "batonrogue.tech", "verbleisover.party", 
    "grabify.link", "bmwforum.co", "leancoding.co", "spottyfly.com", "stopify.co", "yoütu.be","minecräft.com", "freegiftcards.co", "särahah.eu", 
    "särahah.pl", "xda-developers.us", "quickmessage.us", "fortnight.space", "fortnitechat.site", "youshouldclick.us", "joinmy.site", "crabrave.pw", "lovebird.guru", "trulove.guru", 
    "dateing.club", "otherhalf.life", "shrekis.life", "datasig.io", "datauth.io", "headshot.monster", "gaming-at-my.best", "progaming.monster", "yourmy.monster", "screenshare.host", 
    "imageshare.best", "screenshot.best", "gamingfun.me", "catsnthing.com", "mypic.icu", "catsnthings.fun", "curiouscat.club", "gyazo.nl", "gaymers.ax", "ps3cfw.com", "iplogger.org",
    "operation-riptide.click", "xpro.gift","lemonchase.club","xn--yutube-iqc.com", "yȯutube.com","tournament-predator.xyz",
    */
    };

  public void Test() {
    for (int i = 0; i < testLinks.Length; i++) {
      float dist = CalculateDistance(testLinks[i], out string probableSite);
      bool risk = false;
      int leven = 1;
      float riskval = 0;
      if (dist != 0) {
        leven = GetDamerauLevenshteinDistance(testLinks[i], probableSite);
        riskval = dist / (float)Math.Sqrt(leven);
        risk = riskval > 3;
      }
      string rvs = riskval.ToString("f2");
      while (rvs.Length < 6) rvs = "0" + rvs;
      Console.WriteLine(rvs + " / " + dist.ToString("000") + " / " + leven.ToString("00") +
        (risk ? "   RISK! " + probableSite : "") + " <= " + testLinks[i]
        );
    }

  }

  public static int CalculateDistance(string s, out string siteToCheck) {
    siteToCheck = "";
    // Remove the leading www and similar (they cannot be invalid if the rest of the url is valid)
    s = wwwRE.Replace(s, "");
    // Remove the domain parts before the 2nd
    int pos = s.LastIndexOf('.');
    if (pos > 0) pos = s.LastIndexOf('.', pos - 1);
    if (pos > 0) s = s[(pos + 1)..];

    if (s == "discord.com" || s == "discord.gg" || s == "discordapp.com" || s == "discordapp.net" || s == "discord.gift") return 0;
    if (s == "steamcommunity.com" || s == "store.steampowered.com" || s == "steampowered.com") return 0;
    if (s == "epicgames.com") return 0;
    int extra = 0;
    if (s.IndexOf("nitro") != -1 || s.IndexOf("gift") != -1 || s.IndexOf("give") != -1) extra = 100;

    // Remove the last part of the url and any leading w??.
    if (s.IndexOf('.') != -1) s = s[..s.LastIndexOf('.')];

    // Check how many substrings of discord.com we have in the string
    int valDiscord = 0;
    for (int j = 0; j < 7; j++) {
      for (int k = 1; k < 7 - j; k++) {
        if (s.IndexOf("discord".Substring(j, k)) != -1) valDiscord += k;
      }
    }
    int valSteam1 = 0;
    for (int j = 0; j < 7; j++) {
      for (int k = 1; k < 7 - j; k++) {
        if (s.IndexOf("steamcommunity".Substring(j, k)) != -1) valSteam1 += k;
      }
    }
    int valSteam2 = 0;
    for (int j = 0; j < 7; j++) {
      for (int k = 1; k < 7 - j; k++) {
        if (s.IndexOf("steampowered".Substring(j, k)) != -1) valSteam2 += k;
      }
    }
    int valEpic = 0;
    for (int j = 0; j < 7; j++) {
      for (int k = 1; k < 7 - j; k++) {
        if (s.IndexOf("epicgames".Substring(j, k)) != -1) valEpic += k;
      }
    }
    int max = valDiscord; siteToCheck = "discord.com";
    if (valSteam1 > max) { max = valSteam1; siteToCheck = "steamcommunity.com"; }
    if (valSteam2 > max) { max = valSteam2; siteToCheck = "steampowered.com"; }
    if (valEpic > max) { max = valEpic; siteToCheck = "epicgames.com"; }
    return max + extra;
  }

  public static int GetDamerauLevenshteinDistance(string s, string t) {
    if (s.IndexOf("app") != -1) s = s.Replace("app", "");

    var bounds = new { Height = s.Length + 1, Width = t.Length + 1 };

    int[,] matrix = new int[bounds.Height, bounds.Width];

    for (int height = 0; height < bounds.Height; height++) { matrix[height, 0] = height; };
    for (int width = 0; width < bounds.Width; width++) { matrix[0, width] = width; };

    for (int height = 1; height < bounds.Height; height++) {
      for (int width = 1; width < bounds.Width; width++) {
        int cost = (s[height - 1] == t[width - 1]) ? 0 : 1;
        int insertion = matrix[height, width - 1] + 1;
        int deletion = matrix[height - 1, width] + 1;
        int substitution = matrix[height - 1, width - 1] + cost;

        int distance = Math.Min(insertion, Math.Min(deletion, substitution));

        if (height > 1 && width > 1 && s[height - 1] == t[width - 2] && s[height - 2] == t[width - 1]) {
          distance = Math.Min(distance, matrix[height - 2, width - 2] + cost);
        }

        matrix[height, width] = distance;
      }
    }

    return matrix[bounds.Height - 1, bounds.Width - 1];
  }


  internal static async Task CheckMessage(DiscordClient client, MessageCreateEventArgs args) {
    try {
      string msg = args.Message.Content.ToLowerInvariant();
      Match m = linkRE.Match(msg);
      if (!m.Success) return;
      string link = m.Groups[1].Value;

      float dist = CalculateDistance(link, out string probableSite);
      if (dist != 0) {
        float leven = GetDamerauLevenshteinDistance(link, probableSite);
        if (link == probableSite) leven = 1;
        float riskval = dist / (float)Math.Sqrt(leven);
        if (riskval > 3) {
          Utils.Log("Removed spam link message from " + args.Author.Username + "\nPossible counterfeit site: " + probableSite + "\noriginal link: " + msg);
          DiscordMessage warning = await args.Message.Channel.SendMessageAsync("Removed spam link message from " + args.Author.Username + " possible counterfeit site: " + probableSite + " @Mod please take care");
          await args.Message.DeleteAsync("Spam link from " + args.Author.Username);
          Utils.DeleteDelayed(10000, warning).Wait();
        }
      }

    } catch (Exception ex) {
      await args.Message.RespondAsync(Utils.GenerateErrorAnswer("CheckSpam.CheckMessage", ex));
    }
  }
}

