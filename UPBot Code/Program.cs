using DSharpPlus;
using System.Threading.Tasks;

namespace UPBot {
  class Program {
    static void Main(string[] args) {
      MainAsync().GetAwaiter().GetResult();
    }

    static async Task MainAsync() {
      var discord = new DiscordClient(new DiscordConfiguration() {
        Token = "ODc1NzAxNTQ4MzAxMjk5NzQz.YRZWng.2TufJMWrlmo3ZQM9RfnURAS8LUg",
        TokenType = TokenType.Bot,
        Intents = DiscordIntents.AllUnprivileged
      });

      discord.MessageCreated += async (s, e) => {
        if (e.Message.Content.ToLower().StartsWith("upbot"))
          await e.Message.RespondAsync("I am alive!");
      };

      await discord.ConnectAsync();
      await Task.Delay(-1);
    }
  }
}