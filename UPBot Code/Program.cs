using System;
using System.IO;
using System.Reflection;
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

      MainAsync(args[0], (args.Length > 1 && args[1].Length > 0) ? args[1] : "\\").GetAwaiter().GetResult();
    }

    static async Task MainAsync(string token, string prefix) {
      try {
        lw?.WriteLine("Init MainAsync");
        lw.Flush();
        var discord = new DiscordClient(new DiscordConfiguration() {
          Token = token, // token has to be passed as parameter
          TokenType = TokenType.Bot, // We are a bot
          Intents = DiscordIntents.AllUnprivileged | DiscordIntents.GuildMembers
        });
        lw?.WriteLine("discord object");
        lw.Flush();



        discord.UseInteractivity(new InteractivityConfiguration() {
          Timeout = TimeSpan.FromSeconds(120),
          ButtonBehavior = DSharpPlus.Interactivity.Enums.ButtonPaginationBehavior.DeleteMessage,
          ResponseBehavior = DSharpPlus.Interactivity.Enums.InteractionResponseBehavior.Ack
        });
        lw?.WriteLine("use interactivity");
        lw.Flush();
        CustomCommandsService.DiscordClient = discord;
        lw?.WriteLine("CustomCommandsService");
        lw.Flush();

        Utils.InitClient(discord);
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

        CommandsNextExtension commands = discord.UseCommandsNext(new CommandsNextConfiguration() {
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

        BannedWords.Init();
        lw?.WriteLine("BannedWords");
        lw.Flush();
        discord.MessageCreated += async (s, e) => { await BannedWords.CheckMessage(s, e); };
        discord.MessageCreated += async (s, e) => { await CheckSpam.CheckMessage(s, e); };
        discord.MessageCreated += AppreciationTracking.ThanksAdded;
        lw?.WriteLine("Tracking");
        lw.Flush();

        CustomCommandsService.LoadCustomCommands();
        lw?.WriteLine("CustomCommandsService.LoadCustomCommands");
        lw.Flush();

        lw?.WriteLine("connecting to discord...");
        lw.Flush();
        discord.Ready += Discord_Ready;

        await Task.Delay(1000); // 1 sec

        var connections = await discord.GetConnectionsAsync();
        foreach (var connection in connections) await discord.DisconnectAsync();

        await discord.ConnectAsync(); // Connects and wait forever

      } catch (Exception ex) {
        lw?.WriteLine("with exception: " + ex.Message);
        lw.Flush();
      }
      await Task.Delay(-1);
    }

    private static async Task Discord_Ready(DiscordClient discord, DSharpPlus.EventArgs.ReadyEventArgs e) {
      lw?.WriteLine("connected");
      lw.Flush();

      Utils.Log("Logging [re]Started at: " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:dd") + " --------------------------------");

      discord.GuildMemberAdded += MembersTracking.DiscordMemberAdded;
      discord.GuildMemberRemoved += MembersTracking.DiscordMemberRemoved;
      discord.GuildMemberUpdated += MembersTracking.DiscordMemberUpdated;
      discord.MessageReactionAdded += AppreciationTracking.ReacionAdded;
      discord.MessageReactionAdded += EmojisForRole.ReacionAdded;
      discord.MessageReactionRemoved += AppreciationTracking.ReactionRemoved;
      discord.MessageReactionRemoved += EmojisForRole.ReactionRemoved;
      lw?.WriteLine("Adding action events");
      lw.Flush();

      // Wait a second and re-load some parameters (they will arrive only after a while)
      await Task.Delay(50); // 50 msec
      lw.Flush();
      lw?.WriteLine("LoadParams");
      SetupModule.LoadParams();
      lw?.WriteLine("done");
      lw.Flush();
      Utils.Log("--->>> Bot ready <<<---");

      foreach (var g in discord.Guilds.Values)
        Utils.Log(">>" + g.Name);
    }
  }

}

