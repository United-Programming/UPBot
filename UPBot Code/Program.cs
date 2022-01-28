using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;

namespace UPBot {
  class Program {
    static StreamWriter lw = null;
    static void Main(string[] args) {
      lw = File.CreateText(args.Length >= 3 ?  args[2] : "debug.log");
      lw.WriteLine("Log Started. Woho.");
      lw.Flush();
      if (args.Length < 1) {
        lw.WriteLine("You have to specify the bot token as first parameter!");
        lw.Flush();
        return;
      }

      try {
        MainAsync(args[0], (args.Length > 1 && args[1].Length > 0) ? args[1] : "\\").GetAwaiter().GetResult();
      } catch (TaskCanceledException) {
        lw?.WriteLine("Exit for critical failure");
        lw.Flush();
      } catch (Exception ex) {
        lw?.WriteLine("Exit by error: " + ex.Message);
        lw.Flush();
      }
    }

    private static CancellationTokenSource exitToken = new CancellationTokenSource();

    static async Task MainAsync(string token, string prefix) {
      try {
        lw?.WriteLine("Init MainAsync");
        lw.Flush();
        var client = new DiscordClient(new DiscordConfiguration() {
          Token = token, // token has to be passed as parameter
          TokenType = TokenType.Bot, // We are a bot
          Intents = DiscordIntents.AllUnprivileged | DiscordIntents.GuildMembers
        });
        lw?.WriteLine("discord object");
        lw.Flush();



        client.UseInteractivity(new InteractivityConfiguration() {
          Timeout = TimeSpan.FromSeconds(120),
          ButtonBehavior = DSharpPlus.Interactivity.Enums.ButtonPaginationBehavior.DeleteMessage,
          ResponseBehavior = DSharpPlus.Interactivity.Enums.InteractionResponseBehavior.Ack
        });
        lw?.WriteLine("use interactivity");
        lw.Flush();
        CustomCommandsService.DiscordClient = client;
        lw?.WriteLine("CustomCommandsService");
        lw.Flush();

        Utils.InitClient(client);
        lw?.WriteLine("Utils.InitClient");
        lw.Flush();
        Database.InitDb();
        lw?.WriteLine("Database.InitDb");
        lw.Flush();
        Database.AddTable<BannedWord>();
        Database.AddTable<Reputation>();
        Database.AddTable<EmojiForRoleValue>();
        Database.AddTable<CustomCommand>();
        Database.AddTable<Config>();
        Database.AddTable<Timezone>();
        lw?.WriteLine("Added Tables");
        lw.Flush();

        CommandsNextExtension commands = client.UseCommandsNext(new CommandsNextConfiguration() {
          StringPrefixes = new[] { prefix[0].ToString() }, // The backslash will be the default command prefix if not specified in the parameters

          CaseSensitive = false,
          EnableDms = true,
          EnableMentionPrefix = true
        });
        lw?.WriteLine("CommandsNextExtension");
        lw.Flush();
        commands.CommandErrored += CustomCommandsService.CommandError;
        commands.RegisterCommands(Assembly.GetExecutingAssembly()); // Registers all defined commands
        lw?.WriteLine("RegisterCommands");
        lw.Flush();

        lw?.WriteLine("BannedWords");
        lw.Flush();
        client.MessageCreated += async (s, e) => { await BannedWords.CheckMessage(s, e); };
        client.MessageCreated += async (s, e) => { await CheckSpam.CheckMessage(s, e); };
        client.MessageCreated += AppreciationTracking.ThanksAdded;
        lw?.WriteLine("Tracking");
        lw.Flush();

        CustomCommandsService.LoadCustomCommands();
        lw?.WriteLine("CustomCommandsService.LoadCustomCommands");
        lw.Flush();

        lw?.WriteLine("connecting to discord...");
        lw.Flush();
        client.Ready += Discord_Ready;

        await Task.Delay(50);
        await client.ConnectAsync(); // Connect and wait forever

      } catch (Exception ex) {
        lw?.WriteLine("with exception: " + ex.Message);
        lw.Flush();
      }

      // Wait forever
      await Task.Delay(-1, exitToken.Token);
    }

    private static async Task Discord_Ready(DiscordClient client, DSharpPlus.EventArgs.ReadyEventArgs e) {
      lw?.WriteLine("connected");
      lw.Flush();

      Utils.Log("Logging [re]Started at: " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:dd") + " --------------------------------");

      lw?.WriteLine("Adding action events");
      lw.Flush();
      client.GuildMemberAdded += MembersTracking.DiscordMemberAdded;
      client.GuildMemberRemoved += MembersTracking.DiscordMemberRemoved;
      client.GuildMemberUpdated += MembersTracking.DiscordMemberUpdated;
      client.MessageReactionAdded += AppreciationTracking.ReacionAdded;
      client.MessageReactionAdded += EmojisForRole.ReacionAdded;
      client.MessageReactionRemoved += AppreciationTracking.ReactionRemoved;
      client.MessageReactionRemoved += EmojisForRole.ReactionRemoved;

      await Task.Delay(500);

      Utils.Log("Setup complete, waiting guilds to be ready");
      lw?.WriteLine("Setup complete, waiting guilds to be ready");
      lw.Flush();
      _ = WaitForGuildsTask(client);
    }

    private static async Task WaitForGuildsTask(DiscordClient client) {
      Dictionary<ulong, bool> guilds = new Dictionary<ulong, bool>();
      int toGet = client.Guilds.Count;
      foreach (ulong key in client.Guilds.Keys) 
        guilds[key] = false;

      int times = 0;
      while (true) {
        times++;
        foreach (var g in client.Guilds.Values) {
          guilds[g.Id] = !g.IsUnavailable && !string.IsNullOrEmpty(g.Name);
        }
        int num = 0;
        foreach (bool b in guilds.Values) if (b) num++;

        if (num == toGet) break;
        await Task.Delay(500);
        if (times % 21 == 20) Utils.Log("Tried " + times + " got only " + num + "/" + toGet);

        if (times > 120) {
          if (num > 0) Utils.Log("Stopping the wait, got only " + num + " over " + toGet);
          else {
            Utils.Log("[CRITICAL] Stopping. We cannot find any valid Discord server.");
            exitToken.Cancel();
            return;
          }
        }
      }

      Utils.Log("Got all guilds after " + times);
      foreach (var g in client.Guilds.Values)
        Utils.Log(">> " + g.Name + (g.IsUnavailable ? " (NOT WORKING)" : ""));

      lw?.WriteLine("LoadingParams");
      lw.Flush();
      SetupModule.LoadParams();

      lw?.WriteLine("done");
      lw.Flush();
      Utils.Log("--->>> Bot ready <<<---");
    }
  }

}

