using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;

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
        Utils.Log("Init MainAsync", null);
        var client = new DiscordClient(new DiscordConfiguration() {
          Token = token, // token has to be passed as parameter
          TokenType = TokenType.Bot, // We are a bot
          Intents = DiscordIntents.AllUnprivileged | DiscordIntents.GuildMembers
        });
        Utils.Log("Discord object", null);

        client.UseInteractivity(new InteractivityConfiguration() {
          Timeout = TimeSpan.FromSeconds(120),
          ButtonBehavior = DSharpPlus.Interactivity.Enums.ButtonPaginationBehavior.DeleteMessage,
          ResponseBehavior = DSharpPlus.Interactivity.Enums.InteractionResponseBehavior.Ack
        });
        Utils.Log("Use interactivity", null);

        Utils.InitClient(client);
        Utils.Log("Utils.InitClient", null);
        Database.InitDb();
        Utils.Log("Database.InitDb", null);
        Database.AddTable<BannedWord>();
        Database.AddTable<Reputation>();
        Database.AddTable<ReputationEmoji>();
        Database.AddTable<EmojiForRoleValue>();
        Database.AddTable<Config>();
        Database.AddTable<Timezone>();
        Database.AddTable<AdminRole>();
        Database.AddTable<TrackChannel>();
        Database.AddTable<TagBase>();
        Utils.Log("Added Tables", null);

        CommandsNextExtension commands = client.UseCommandsNext(new CommandsNextConfiguration() {
          StringPrefixes = new[] { prefix[0].ToString() }, // The backslash will be the default command prefix if not specified in the parameters

          CaseSensitive = false,
          EnableDms = true,
          EnableMentionPrefix = true
        });
        Utils.Log("CommandsNextExtension", null);
        commands.RegisterCommands(Assembly.GetExecutingAssembly()); // Registers all defined commands
        Utils.Log("RegisterCommands", null);

        Utils.Log("Connecting to discord...", null);
        client.Ready += Discord_Ready;

        await Task.Delay(50);
        await client.ConnectAsync(); // Connect and wait forever

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

        if (times > 10) { // FIXME
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
      Setup.LoadParams();

      Utils.Log("Adding action events", null);
      client.GuildMemberAdded += MembersTracking.DiscordMemberAdded;
      client.GuildMemberRemoved += MembersTracking.DiscordMemberRemoved;
      client.GuildMemberUpdated += MembersTracking.DiscordMemberUpdated;
      client.MessageReactionAdded += AppreciationTracking.ReactionAdded;
      client.MessageReactionAdded += EmojisForRole.ReacionAdded;
      client.MessageReactionRemoved += EmojisForRole.ReactionRemoved;

      client.MessageCreated += async (s, e) => { await BannedWords.CheckMessage(s, e); };
      client.MessageCreated += async (s, e) => { await CheckSpam.CheckMessage(s, e); };
      client.MessageCreated += AppreciationTracking.ThanksAdded;
      Utils.Log("Tracking", null);

      client.GuildCreated += Setup.NewGuildAdded;

      Utils.Log("--->>> Bot ready <<<---", null);
    }
  }

}
