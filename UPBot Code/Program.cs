using DSharpPlus;
using DSharpPlus.CommandsNext;
using System.Reflection;
using System.Threading.Tasks;

namespace UPBot {
  class Program {
    static void Main(string[] args) {
      MainAsync(args[0]).GetAwaiter().GetResult();
    }

    static async Task MainAsync(string token) {
      var discord = new DiscordClient(new DiscordConfiguration() {
        Token = token, // token has to be passed as parameter
        TokenType = TokenType.Bot, // We are a bot
        Intents = DiscordIntents.AllUnprivileged // But very limited right now
      });

      var commands = discord.UseCommandsNext(new CommandsNextConfiguration() {
        StringPrefixes = new[] { "/" } // The slash will be the command prefix
      });
      commands.RegisterCommands(Assembly.GetExecutingAssembly()); // Registers all defined commands

      await discord.ConnectAsync(); // Connects and wait forever
      await Task.Delay(-1);
    }
  }
}