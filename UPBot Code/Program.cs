using System;
using System.Reflection;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;

namespace UPBot
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync(args[0]).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string token)
        {
            var discord = new DiscordClient(new DiscordConfiguration()
            {
                Token = token, // token has to be passed as parameter
                TokenType = TokenType.Bot, // We are a bot
                Intents = DiscordIntents.AllUnprivileged // But very limited right now
            });
            discord.UseInteractivity(new InteractivityConfiguration()
            {
                PollBehaviour = PollBehaviour.KeepEmojis,
                Timeout = TimeSpan.FromSeconds(30)
            });
            CustomCommandsService.DiscordClient = discord;

            UtilityFunctions.InitClient(discord);
            var commands = discord.UseCommandsNext(new CommandsNextConfiguration()
            {
                StringPrefixes = new[] { "\\" } // The backslash will be the command prefix
            });
            commands.CommandErrored += CustomCommandsService.CommandError;
            commands.RegisterCommands(Assembly.GetExecutingAssembly()); // Registers all defined commands

            await CustomCommandsService.LoadCustomCommands();
            await discord.ConnectAsync(); // Connects and wait forever
            await Task.Delay(-1);
        }
    }
}