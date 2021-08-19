using System;
using System.Reflection;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;

namespace UPBot {
  class Program {
    static void Main(string[] args) {
      MainAsync(args[0], (args.Length > 1 && args[1].Length > 0) ? args[1] : "\\").GetAwaiter().GetResult();
    }

    static async Task MainAsync(string token, string prefix) {
      var discord = new DiscordClient(new DiscordConfiguration() {
        Token = token, // token has to be passed as parameter
        TokenType = TokenType.Bot, // We are a bot
        Intents = DiscordIntents.AllUnprivileged | DiscordIntents.GuildMembers
      });
      discord.UseInteractivity(new InteractivityConfiguration() {
        Timeout = TimeSpan.FromHours(2)
      });
      CustomCommandsService.DiscordClient = discord;

      UtilityFunctions.InitClient(discord);
      var commands = discord.UseCommandsNext(new CommandsNextConfiguration() {
        StringPrefixes = new[] { prefix[0].ToString() } // The backslash will be the default command prefix if not specified in the parameters
      });
      commands.CommandErrored += CustomCommandsService.CommandError;
      commands.RegisterCommands(Assembly.GetExecutingAssembly()); // Registers all defined commands

      BannedWords.Init();
      discord.MessageCreated += async (s, e) => { await BannedWords.CheckMessage(s, e); };

      await CustomCommandsService.LoadCustomCommands();
      await discord.ConnectAsync(); // Connects and wait forever

      UtilityFunctions.Log("Logging [re]Started at: " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:dd"));

      AppreciationTracking.Init();
      discord.GuildMemberAdded += MembersTracking.DiscordMemberAdded;
      discord.GuildMemberRemoved += MembersTracking.DiscordMemberRemoved;
      discord.GuildMemberUpdated += MembersTracking.DiscordMemberUpdated;
      discord.MessageReactionAdded += AppreciationTracking.ReacionAdded;
      discord.MessageReactionRemoved += AppreciationTracking.ReactionRemoved;

      await Task.Delay(-1);
    }
  }
}