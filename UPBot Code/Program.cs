using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using TimeZoneConverter;

namespace UPBot {
  class Program {

    static void Main(string[] args) {
      if (args.Length != 2) {
        Console.ForegroundColor = ConsoleColor.Red;
        Utils.Log("You have to specify the bot token as first parameter and the logs path as second parameter!", null);
        return;
      }
      Utils.LogsFolder = args[1];
      Console.ForegroundColor = ConsoleColor.Green;
      Utils.Log("Log Started. Woho.", null);
      Console.ForegroundColor = ConsoleColor.White;

      try {
        MainAsync(args[0]).GetAwaiter().GetResult();
      } catch (TaskCanceledException) {
        Console.ForegroundColor = ConsoleColor.Red;
        Utils.Log("Exit for critical failure", null);
        Console.ForegroundColor = ConsoleColor.White;
      } catch (Exception ex) {
        Console.ForegroundColor = ConsoleColor.Red;
        Utils.Log("Exit by error: " + ex.Message, null);
        Console.ForegroundColor = ConsoleColor.White;
      }
    }

    readonly private static CancellationTokenSource exitToken = new();

    static async Task MainAsync(string token) {
      try {
        Console.ForegroundColor = ConsoleColor.Green;
        Utils.Log("Init Main", null);
        Console.ForegroundColor = ConsoleColor.Yellow;
        Utils.Log("Version: " + Utils.GetVersion(), null);
        Console.ForegroundColor = ConsoleColor.White;

        var client = new DiscordClient(new DiscordConfiguration() {
          Token = token, // token has to be passed as parameter
          TokenType = TokenType.Bot, // We are a bot
          Intents = DiscordIntents.AllUnprivileged | DiscordIntents.GuildMembers
        });

        Utils.Log("Use interactivity", null);
        client.UseInteractivity(new InteractivityConfiguration() {
          Timeout = TimeSpan.FromSeconds(120),
          ButtonBehavior = DSharpPlus.Interactivity.Enums.ButtonPaginationBehavior.DeleteMessage,
          ResponseBehavior = DSharpPlus.Interactivity.Enums.InteractionResponseBehavior.Ack
        });

        Utils.Log("Utils.InitClient", null);
        Utils.InitClient(client);

        Database.InitDb(new List<Type>{
          typeof(SpamProtection), typeof(Timezone), typeof(AdminRole), typeof(TrackChannel), typeof(TagBase), typeof(SpamLink)
        });
        Utils.Log("Database.InitDb", null);


        // SlashCommands
        Utils.Log("SlashCommands", null);
        var slash = client.UseSlashCommands();
        slash.RegisterCommands<SlashVersion>();
        slash.RegisterCommands<SlashPing>();
        slash.RegisterCommands<SlashUnityDocs>();
        slash.RegisterCommands<SlashRefactor>();
        slash.RegisterCommands<SlashDelete>();
        slash.RegisterCommands<SlashWhoIs>();
        slash.RegisterCommands<SlashGame>();
        slash.RegisterCommands<SlashTags>();
        slash.RegisterCommands<SlashTagsEdit>();
        slash.RegisterCommands<SlashStats>();
        slash.RegisterCommands<SlashTimezone>();
        slash.RegisterCommands<SlashLogs>();
        slash.RegisterCommands<SlashSetup>();



        Utils.Log("Connecting to discord...", null);
        client.Ready += Discord_Ready;

        await Task.Delay(50);
        await client.ConnectAsync(); // Connect

        // Check for a while if we have any guild
        int t = 0;
        while (Utils.GetClient() == null) { // 10 secs max for client
          await Task.Delay(1000);
          if (t++ > 10) {
            Console.ForegroundColor = ConsoleColor.Red;
            Utils.Log("CRITICAL ERROR: We are not connecting! (no client)", null);
            Console.WriteLine("CRITICAL ERROR: No discord client");
            return;
          }
        }

        // 10 secs max for guilds
        t = 0;
        while (Utils.GetClient().Guilds == null) {
          await Task.Delay(1000);
          if (t++ > 10) {
            Console.ForegroundColor = ConsoleColor.Red;
            Utils.Log("CRITICAL ERROR: We are not connecting! (no guilds)", null);
            Console.WriteLine("CRITICAL ERROR: No guilds avilable");
            return;
          }
        }

        // 30 secs max for guilds coubnt
        t = 0;
        while (Utils.GetClient().Guilds.Count == 0) {
          await Task.Delay(1000);
          if (t++ > 30) {
            Console.ForegroundColor = ConsoleColor.Red;
            Utils.Log("CRITICAL ERROR: We are not connecting! (guilds count is zero)", null);
            Console.WriteLine("CRITICAL ERROR: The bot seems to be in no guild");
            return;
          }
        }


      } catch (Exception ex) {
        Console.ForegroundColor = ConsoleColor.Red;
        Utils.Log("with exception: " + ex.Message, null);
        Console.ForegroundColor = ConsoleColor.White;
      }

      // Wait forever
      await Task.Delay(-1, exitToken.Token);
    }

    private static async Task Discord_Ready(DiscordClient client, DSharpPlus.EventArgs.ReadyEventArgs e) {
      Console.ForegroundColor = ConsoleColor.Green;
      Utils.Log("connected", null);
      Console.ForegroundColor = ConsoleColor.White;
      Utils.Log("Logging [re]Started at: " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:dd") + " --------------------------------", null);

      await Task.Delay(500);
      Console.ForegroundColor = ConsoleColor.Green;
      Utils.Log("Setup complete, waiting guilds to be ready", null);
      Console.ForegroundColor = ConsoleColor.White;
      _ = WaitForGuildsTask(client);
    }

    private static async Task WaitForGuildsTask(DiscordClient client) {
      Dictionary<ulong, bool> guilds = new();
      int toGet = client.Guilds.Count;
      foreach (ulong key in client.Guilds.Keys)
        guilds[key] = false;

      int times = 0;
      bool cleanOldGuilds = true;
      while (true) {
        times++;
        foreach (var g in client.Guilds.Values) {
          guilds[g.Id] = !g.IsUnavailable && !string.IsNullOrEmpty(g.Name);
        }
        int num = 0;
        foreach (bool b in guilds.Values) if (b) num++;

        if (num == toGet) {
          cleanOldGuilds = false;
          break;
        }
        await Task.Delay(500);
        if (times % 21 == 20) {
          Console.ForegroundColor = ConsoleColor.Yellow;
          Utils.Log("Tried " + times + " got only " + num + "/" + toGet, null);
          Console.ForegroundColor = ConsoleColor.White;
        }

        if (times > 300) {
          if (num > 0) {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Utils.Log("Stopping the wait, got only " + num + " over " + toGet, null);
            Console.ForegroundColor = ConsoleColor.White;
            break;
          }
          else {
            Console.ForegroundColor = ConsoleColor.Red;
            Utils.Log("[CRITICAL] Stopping. We cannot find any valid Discord server.", null);
            Console.ForegroundColor = ConsoleColor.White;
            exitToken.Cancel();
            return;
          }
        }
      }
      // Remove guild that are no more valid
      if (cleanOldGuilds) {
        foreach (var g in client.Guilds.Values) {
          if (g.IsUnavailable || string.IsNullOrEmpty(g.Name)) {
            Console.ForegroundColor = ConsoleColor.White;
            Utils.Log("Leaving guild with id: " + g.Id, null);
            try {
              _ = g.LeaveAsync();
            } catch (Exception e) {
              Console.ForegroundColor = ConsoleColor.Red;
              Utils.Log("Error in Leaving guild: " + e.Message, null);
              Console.ForegroundColor = ConsoleColor.White;
            }
          }
        }
      }

      Console.ForegroundColor = ConsoleColor.Green;
      Utils.Log($"Got all guilds after '{times}'", null);
      Console.ForegroundColor = ConsoleColor.White;
      foreach (var g in client.Guilds.Values) {
        if (g.IsUnavailable) {
          Console.ForegroundColor = ConsoleColor.Red;
          Utils.Log($">> {g.Name} (NOT WORKING)", null);
        }
        else
          Utils.Log($">> {g.Name}", null);
        Console.ForegroundColor = ConsoleColor.White;
      }
      Console.ForegroundColor = ConsoleColor.Green;
      Utils.Log("LoadingParams", null);
      Configs.LoadParams();
      Console.ForegroundColor = ConsoleColor.White;

      Console.ForegroundColor = ConsoleColor.Green;
      Utils.Log("Adding action events", null);
      client.GuildMemberAdded += MembersTracking.DiscordMemberAdded;
      client.GuildMemberRemoved += MembersTracking.DiscordMemberRemoved;
      client.GuildMemberUpdated += MembersTracking.DiscordMemberUpdated;

      client.MessageCreated += async (s, e) => { await CheckSpam.CheckMessageCreate(s, e); };
      client.MessageUpdated += async (s, e) => { await CheckSpam.CheckMessageUpdate(s, e); };
      Console.ForegroundColor = ConsoleColor.White;

      Console.ForegroundColor = ConsoleColor.Yellow;
      Utils.Log("Tracking", null);
      Console.ForegroundColor = ConsoleColor.White;

      client.GuildCreated += Configs.NewGuildAdded;
      Console.ForegroundColor = ConsoleColor.Green;
      Utils.Log("--->>> Bot ready <<<---", null);
      Console.ForegroundColor = ConsoleColor.White;
    }
  }
}
