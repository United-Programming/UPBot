using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;

namespace UPBot {
  class Program {

    static void Main(string[] args) {
      if (args.Length != 2) {
        Utils.Log("You have to specify the bot token as first parameter and the logs path as second parameter!", null);
        return;
      }
      Utils.LogsFolder = args[1];
      Utils.Log("Log Started. Woho.", null);

      try {
        MainAsync(args[0]).GetAwaiter().GetResult();
      } catch (TaskCanceledException) {
        Utils.Log("Exit for critical failure", null);
      } catch (Exception ex) {
        Utils.Log("Exit by error: " + ex.Message, null);
      }
    }

    readonly private static CancellationTokenSource exitToken = new CancellationTokenSource();

    static async Task MainAsync(string token) {
      try {
        Utils.Log("Init Main", null);
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
          typeof(SpamProtection), typeof(Timezone), typeof(AdminRole), typeof(TrackChannel), typeof(TagBase) 
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
            Utils.Log("CRITICAL ERROR: We are not connecting! (guilds count is zero)", null);
            Console.WriteLine("CRITICAL ERROR: The bot seems to be in no guild");
            return;
          }
        }


      } catch (Exception ex) {
        Utils.Log("with exception: " + ex.Message, null);
      }

      // Wait forever
      await Task.Delay(-1, exitToken.Token);
    }

    private static async Task Discord_Ready(DiscordClient client, DSharpPlus.EventArgs.ReadyEventArgs e) {
      Utils.Log("connected", null);

      Utils.Log("Logging [re]Started at: " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:dd") + " --------------------------------", null);

      await Task.Delay(500);

      Utils.Log("Setup complete, waiting guilds to be ready", null);
      _ = WaitForGuildsTask(client);
    }

    private static async Task WaitForGuildsTask(DiscordClient client) {
      Dictionary<ulong, bool> guilds = new Dictionary<ulong, bool>();
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
        if (times % 21 == 20) Utils.Log("Tried " + times + " got only " + num + "/" + toGet, null);

        if (times > 300) {
          if (num > 0) {
            Utils.Log("Stopping the wait, got only " + num + " over " + toGet, null);
            break;
          }
          else {
            Utils.Log("[CRITICAL] Stopping. We cannot find any valid Discord server.", null);
            exitToken.Cancel();
            return;
          }
        }
      }
      // Remove guild that are no more valid
      if (cleanOldGuilds) {
        foreach (var g in client.Guilds.Values) {
          if (g.IsUnavailable || string.IsNullOrEmpty(g.Name)) {
            Utils.Log("Leaving guild with id: " + g.Id, null);
            try {
              _ = g.LeaveAsync();
            } catch (Exception e) {
              Utils.Log("Error in Leaving guild: " + e.Message, null);
            }
          }
        }
      }


      Utils.Log("Got all guilds after " + times, null);
      foreach (var g in client.Guilds.Values)
        Utils.Log(">> " + g.Name + (g.IsUnavailable ? " (NOT WORKING)" : ""), null);

      Utils.Log("LoadingParams", null);
      Configs.LoadParams();

      Utils.Log("Adding action events", null);
      client.GuildMemberAdded += MembersTracking.DiscordMemberAdded;
      client.GuildMemberRemoved += MembersTracking.DiscordMemberRemoved;
      client.GuildMemberUpdated += MembersTracking.DiscordMemberUpdated;

      client.MessageCreated += async (s, e) => { await CheckSpam.CheckMessage(s, e); };
      Utils.Log("Tracking", null);

      client.GuildCreated += Configs.NewGuildAdded;

      Utils.Log("--->>> Bot ready <<<---", null);
    }
  }

}
