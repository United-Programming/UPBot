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
      MainAsync(args[0], (args.Length > 1 && args[1].Length > 0) ? args[1] : "\\", args[2]).GetAwaiter().GetResult();
    }

    static async Task MainAsync(string token, string prefix, string logPath) {
      try {
        lw?.WriteLine("Init MainAsync");
        var discord = new DiscordClient(new DiscordConfiguration() {
          Token = token, // token has to be passed as parameter
          TokenType = TokenType.Bot, // We are a bot
          Intents = DiscordIntents.AllUnprivileged | DiscordIntents.GuildMembers
        });
        lw?.WriteLine("discord object");
        discord.UseInteractivity(new InteractivityConfiguration() {
          Timeout = TimeSpan.FromHours(2)
        });
        lw?.WriteLine("use interactivity");
        CustomCommandsService.DiscordClient = discord;
        lw?.WriteLine("CustomCommandsService");

        Utils.InitClient(discord);
        lw?.WriteLine("Utils.InitClient");
        Database.InitDb();
        lw?.WriteLine("Database.InitDb");
        Database.AddTable<BannedWord>();
        Database.AddTable<Reputation>();
        Database.AddTable<EmojiForRoleValue>();
        Database.AddTable<CustomCommand>();
        lw?.WriteLine("Added Tables");

        CommandsNextExtension commands = discord.UseCommandsNext(new CommandsNextConfiguration() {
          StringPrefixes = new[] { prefix[0].ToString() } // The backslash will be the default command prefix if not specified in the parameters
        });
        lw?.WriteLine("CommandsNextExtension");
        commands.CommandErrored += CustomCommandsService.CommandError;
        commands.RegisterCommands(Assembly.GetExecutingAssembly()); // Registers all defined commands
        lw?.WriteLine("RegisterCommands");

        BannedWords.Init();
        lw?.WriteLine("BannedWords");
        discord.MessageCreated += async (s, e) => { await BannedWords.CheckMessage(s, e); };
        discord.MessageCreated += AppreciationTracking.ThanksAdded;

        CustomCommandsService.LoadCustomCommands();
        lw?.WriteLine("CustomCommandsService.LoadCustomCommands");
        await discord.ConnectAsync(); // Connects and wait forever

        lw?.WriteLine("connecting to discord");
        Utils.Log("Logging [re]Started at: " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:dd") + " --------------------------------");

        discord.GuildMemberAdded += MembersTracking.DiscordMemberAdded;
        discord.GuildMemberRemoved += MembersTracking.DiscordMemberRemoved;
        discord.GuildMemberUpdated += MembersTracking.DiscordMemberUpdated;
        discord.MessageReactionAdded += AppreciationTracking.ReacionAdded;
        discord.MessageReactionAdded += EmojisForRole.ReacionAdded;
        discord.MessageReactionRemoved += AppreciationTracking.ReactionRemoved;
        discord.MessageReactionRemoved += EmojisForRole.ReactionRemoved;
        lw?.WriteLine("done");

      } catch (Exception ex) {
        lw?.WriteLine("with exception: " + ex.Message);
      }
      await Task.Delay(-1);
    }

  }
}