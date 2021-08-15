using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System.Threading.Tasks;

/// <summary>
/// Debug command to quickly test some new stuff
/// author: CPU
/// </summary>
public class DebugCommand : BaseCommandModule {

  [Command("qwe")]
  [Description("This is a command just to quickly debug stuff in development")]
  [RequirePermissions(Permissions.ManageMessages)] // Restrict this command to users/roles who have the "Manage Messages" permission
  [RequireRoles(RoleCheckMode.Any, "Helper", "Mod", "Owner")] // Restrict this command to "Helper", "Mod" and "Owner" roles only
  public async Task DoDebug(CommandContext ctx) { // Refactors the previous post, if it is code
    UtilityFunctions.LogUserCommand(ctx);
    DiscordMessage msg = await ctx.Channel.SendMessageAsync("Test message");

    DiscordEmoji emoji = UtilityFunctions.GetEmoji(EmojiEnum.Godot);
    await msg.CreateReactionAsync(emoji);

    await ctx.Message.RespondAsync("Done");
  }
}
