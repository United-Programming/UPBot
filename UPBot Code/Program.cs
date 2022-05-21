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
      if (args.Length >= 3) Utils.LogsFolder = args[2];
      Utils.Log("Log Started. Woho.", null);
      if (args.Length < 1) {
        Utils.Log("You have to specify the bot token as first parameter!", null);
        return;
      }

      try {
        MainAsync(args[0], (args.Length > 1 && args[1].Length > 0) ? args[1] : "\\").GetAwaiter().GetResult();
      } catch (TaskCanceledException) {
        Utils.Log("Exit for critical failure", null);
      } catch (Exception ex) {
        Utils.Log("Exit by error: " + ex.Message, null);
      }
    }

    readonly private static CancellationTokenSource exitToken = new CancellationTokenSource();

    static async Task MainAsync(string token, string prefix) {
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
        Database.InitDb();

        Utils.Log("Database.InitDb", null);
        Database.AddTable<SpamProtection>();
        Database.AddTable<Timezone>();
        Database.AddTable<AdminRole>();
        Database.AddTable<TrackChannel>();
        Database.AddTable<TagBase>();


        // SlashCommands
        Utils.Log("SlashCommands", null);
        var slash = client.UseSlashCommands();
        slash.RegisterCommands<SlashVersion>(830900174553481236ul);
        slash.RegisterCommands<SlashPing>(830900174553481236ul); // FIXME this is just for United programming
        slash.RegisterCommands<SlashUnityDocs>(830900174553481236ul);
        slash.RegisterCommands<SlashRefactor>(830900174553481236ul);
        slash.RegisterCommands<SlashDelete>(830900174553481236ul);
        slash.RegisterCommands<SlashWhoIs>(830900174553481236ul);
        slash.RegisterCommands<SlashGame>(830900174553481236ul);
        slash.RegisterCommands<SlashTags>(830900174553481236ul);
        slash.RegisterCommands<SlashTagsEdit>(830900174553481236ul);
        slash.RegisterCommands<SlashStats>(830900174553481236ul);
        slash.RegisterCommands<SlashTimezone>(830900174553481236ul);
        slash.RegisterCommands<SlashLogs>(830900174553481236ul);
        slash.RegisterCommands<SlashSetup>(830900174553481236ul);



        CommandsNextExtension commands = client.UseCommandsNext(new CommandsNextConfiguration() {
          StringPrefixes = new[] { prefix[0].ToString() }, // The backslash will be the default command prefix if not specified in the parameters
          CaseSensitive = false,
          EnableDms = true,
          EnableMentionPrefix = true
        });
        Utils.Log("CommandsNextExtension", null);
        commands.RegisterCommands(Assembly.GetExecutingAssembly()); // Registers all defined commands

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
