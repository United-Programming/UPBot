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
public class CheckSpam  {
  readonly static Regex linkRE = new Regex(@"http[s]?://([^/]+)/");
  readonly static Regex wwwRE = new Regex(@"^w[^\.]{0,3}.\.");
  readonly string[] testLinks = { "discord.com", "discordapp.com", "discord.gg",

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
      float dist = CalculateDistance(testLinks[i], true, true, true, out string probableSite);
      bool risk = false;
      int leven = 1;
      float riskval = 0;
      if (dist != 0) {
        leven = StringDistance.DLDistance(testLinks[i], probableSite);
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

  public static int CalculateDistance(string s, bool cdisc, bool csteam, bool cepic, out string siteToCheck) {
    siteToCheck = "";
    // Remove the leading www and similar (they cannot be invalid if the rest of the url is valid)
    s = wwwRE.Replace(s, "");
    // Remove the domain parts before the 2nd
    int pos = s.LastIndexOf('.');
    if (pos > 0) pos = s.LastIndexOf('.', pos - 1);
    if (pos > 0) s = s[(pos + 1)..];

    if (s == "discord.com" || s == "discord.gg"  || s == "discord.net" || s == "discordapp.com"  || s == "discordapp.net" || s == "discord.gift") return 0;
    if (s == "media.discordapp.net" || s == "media.discord.net" || s == "canary.discord.com" || s == "canary.discord.net" || s == "canary.discord.gg") return 0;
    if (s == "steamcommunity.com" || s == "store.steampowered.com" || s == "steampowered.com") return 0;
    if (s == "epicgames.com") return 0;
    if (s == "pastebin.com" || s == "github.com" || s == "controlc.com" || s == "ghostbin.co" || s == "rentry.co" || s == "codiad.com" || s == "zerobin.net" ||
        s == "toptal.com" || s == "ideone.com" || s == "jsfiddle.net" || s == "textbin.net" || s == "jsbin.com" || s == "ideone.com" || s == "pythondiscord.com") return 0;

    int extra = 0;
    if (s.IndexOf("nitro") != -1 || s.IndexOf("gift") != -1 || s.IndexOf("give") != -1) extra = 100;

    // Remove the last part of the url and any leading w??.
    if (s.IndexOf('.') != -1) s = s[..s.LastIndexOf('.')];

    // Check how many substrings of discord.com we have in the string
    int valDiscord = 0;
    if (cdisc) {
      for (int len = 4; len < 7; len++) {
        for (int strt = 0; strt < 7 - len; strt++) {
          if (s.IndexOf("discord"[strt..(strt + len)]) != -1)
            valDiscord += len;
        }
      }
      if (s.IndexOf("xyz") != -1) valDiscord += 5;
      for (int len = 4; len < 7; len++) {
        for (int strt = 0; strt < 7 - len; strt++) {
          if (s.IndexOf("diczord"[strt..(strt + len)]) != -1)
            valDiscord += len;
        }
      }
    }

    int valSteam1 = 0;
    int valSteam2 = 0;
    if (csteam) {
      for (int len = 5; len < 14; len++) {
        for (int strt = 0; strt < 14 - len; strt++) {
          if (s.IndexOf("steamcommunity"[strt..(strt+len)]) != -1) 
            valSteam1 += len;
        }
      }
      for (int len = 5; len < 12; len++) {
        for (int strt = 0; strt < 12 - len; strt++) {
          if (s.IndexOf("steampowered"[strt..(strt + len)]) != -1)
            valSteam2 += len;
        }
      }
    }
    int valEpic = 0;
    if (cepic) {
      for (int len = 4; len < 9; len++) {
        for (int strt = 0; strt < 9 - len; strt++) {
          if (s.IndexOf("epicgames"[strt..(strt + len)]) != -1)
            valEpic += len;
        }
      }
    }
    int max = valDiscord; siteToCheck = "discord.com";
    if (valSteam1 > max) { max = valSteam1; siteToCheck = "steamcommunity.com"; }
    if (valSteam2 > max) { max = valSteam2; siteToCheck = "steampowered.com"; }
    if (valEpic > max) { max = valEpic; siteToCheck = "epicgames.com"; }
    return max + extra;
  }


  internal static async Task CheckMessageUpdate(DiscordClient _, MessageUpdateEventArgs args) {
    await CheckMessage(args.Guild, args.Author, args.Message);
  }

  internal static async Task CheckMessageCreate(DiscordClient _, MessageCreateEventArgs args) {
    await CheckMessage(args.Guild, args.Author, args.Message);
  }

  static async Task CheckMessage(DiscordGuild guild, DiscordUser author, DiscordMessage message) {
    if (guild == null) return;
    if (author.Id == Utils.GetClient().CurrentUser.Id) return; // Do not consider myself
    try {
      if (!Configs.SpamProtections.ContainsKey(guild.Id)) return;
      SpamProtection sp = Configs.SpamProtections[guild.Id];
      if (sp == null) return;
      if (!sp.protectDiscord && !sp.protectSteam && !sp.protectDiscord) return;
      bool edisc = sp.protectDiscord;
      bool esteam = sp.protectSteam;
      bool eepic = sp.protectEpic;

      string msg = message.Content.ToLowerInvariant();
      Match m = linkRE.Match(msg);
      if (!m.Success) return;
      string link = m.Groups[1].Value;

      float dist = CalculateDistance(link, edisc, esteam, eepic, out string probableSite);
      if (dist != 0) {
        link = link.Replace("app", "");

        float leven = StringDistance.DLDistance(link, probableSite);
        if (link == probableSite) leven = 1;
        float riskval = dist / (float)Math.Sqrt(leven);
        if (riskval > 3) {
          Utils.Log("Removed spam link message from " + author.Username + "\nPossible counterfeit site: " + probableSite + "\noriginal link: " + msg, guild.Name);
          DiscordMessage warning = await message.Channel.SendMessageAsync("Removed spam link message from " + author.Username + " possible counterfeit site: " + probableSite + "\n" + Configs.GetAdminsMentions(guild.Id) + ", please take care.");
          await message.DeleteAsync("Spam link from " + author.Username);
          Utils.DeleteDelayed(10000, warning).Wait();
        }
      }

    } catch (NullReferenceException ex) {
      Utils.Log(Utils.sttr.ToString(), null);
      Utils.Log(ex.Message, null);
      Utils.Log(ex.ToString(), null);
    } catch (Exception ex) {
      if (ex is DSharpPlus.Exceptions.NotFoundException) return; // Timed out
      await message.RespondAsync(Utils.GenerateErrorAnswer(guild.Name, "CheckSpam.CheckMessage", ex));
    }
  }
}

