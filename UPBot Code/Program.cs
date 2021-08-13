using DSharpPlus;
using System.Threading.Tasks;

namespace UPBot {
  class Program {
    static void Main(string[] args) {
      MainAsync(args[0]).GetAwaiter().GetResult();
    }

    static async Task MainAsync(string token) {
      var discord = new DiscordClient(new DiscordConfiguration() {
        Token = token,
        TokenType = TokenType.Bot,
        Intents = DiscordIntents.AllUnprivileged
      });

      discord.MessageCreated += async (s, e) => {
        if (e.Message.Content.ToLower().StartsWith("/upbot"))
          await e.Message.RespondAsync("I am alive!");
      };

      await discord.ConnectAsync();
      await Task.Delay(-1);
    }
  }
}