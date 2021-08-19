using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

/// <summary>
/// This command will delete the last x messages
/// or the last x messages of a specific user
/// author: Duck
/// </summary>
public class Delete : BaseCommandModule {
  private const int MessageLimit = 50;
  private const string CallbackLimitExceeded = ", since you can't delete more than 50 messages at a time.";

  /// <summary>
  /// Delete the last x messages of any user
  /// </summary>
  [Command("delete")]
  [Aliases("clear", "purge")]
  [Description("Deletes the last x messages in the channel, the command was invoked in (e.g. `\\delete 10`)." +
               "\nIt contains an overload to delete the last x messages of a specified user (e.g. `\\delete @User 10`)." +
               "\nThis command can only be invoked by a Helper or Mod.")]
  [RequirePermissions(Permissions.ManageMessages)] // Restrict this command to users/roles who have the "Manage Messages" permission
  [RequireRoles(RoleCheckMode.Any, "Helper", "Mod", "Owner")] // Restrict this command to "Helper", "Mod" and "Owner" roles only
  public async Task DeleteCommand(CommandContext ctx, [Description("How many messages should be deleted?")] int count) {
    Utils.LogUserCommand(ctx);
    if (count <= 0) {
      await Utils.ErrorCallback(CommandErrors.InvalidParamsDelete, ctx, count);
      return;
    }

    bool limitExceeded = CheckLimit(count);

    var messages = ctx.Channel.GetMessagesAsync(count + 1).Result;
    await DeleteMessages(ctx.Message, messages);

    await Success(ctx, limitExceeded, count);
  }

  /// <summary>
  /// Delete the last x messages of the specified user
  /// </summary>
  [Command("delete")]
  [RequirePermissions(Permissions.ManageMessages)] // Restrict this command to users/roles who have the "Manage Messages" permission
  [RequireRoles(RoleCheckMode.Any, "Helper", "Mod", "Owner")] // Restrict this command to "Helper", "Mod" and "Owner" roles only
  public async Task DeleteCommand(CommandContext ctx, [Description("Whose last x messages should get deleted?")] DiscordMember targetUser,
      [Description("How many messages should get deleted?")] int count) {
    Utils.LogUserCommand(ctx);
    if (count <= 0) {
      await Utils.ErrorCallback(CommandErrors.InvalidParamsDelete, ctx, count);
      return;
    }

    bool limitExceeded = CheckLimit(count);

    var allMessages = ctx.Channel.GetMessagesAsync().Result; // Get last 100 messages
    var userMessages = allMessages.Where(x => x.Author == targetUser).Take(count + 1);
    await DeleteMessages(ctx.Message, userMessages);

    await Success(ctx, limitExceeded, count, targetUser);
  }

  /// <summary>
  /// The core-process of deleting the messages
  /// </summary>
  public async Task DeleteMessages(DiscordMessage request, IEnumerable<DiscordMessage> messages) {
    try{
    List<DiscordMessage> toDelete = new List<DiscordMessage>();
    foreach (DiscordMessage m in messages) {
      if (m != request) toDelete.Add(m);
    }
    await request.Channel.DeleteMessagesAsync(toDelete);
    } catch (Exception ex) {
      await request.RespondAsync(Utils.GenerateErrorAnswer("DeleteMessages", ex));
    }

  }

  /// <summary>
  /// Will be called at the end of every execution of this command and tells the user that the execution succeeded
  /// including a short summary of the command (how many messages, by which user etc.)
  /// </summary>
  private async Task Success(CommandContext ctx, bool limitExceeded, int count, DiscordMember targetUser = null) {
    try{
    string mentionUserStr = targetUser == null ? string.Empty : $"by '{targetUser.DisplayName}'";
    string overLimitStr = limitExceeded ? CallbackLimitExceeded : string.Empty;
    string messagesLiteral = Utils.PluralFormatter(count, "message", "messages");
    string hasLiteral = Utils.PluralFormatter(count, "has", "have");

    await ctx.Message.DeleteAsync();
    string embedMessage = $"The last {count} {messagesLiteral} {mentionUserStr} {hasLiteral} been successfully deleted{overLimitStr}.";

    var message = await Utils.BuildEmbedAndExecute("Success", embedMessage, Utils.Green, ctx, true);
    await Utils.DeleteDelayed(10, message);
    } catch (Exception ex) {
      await ctx.RespondAsync(Utils.GenerateErrorAnswer("Delete", ex));
    }
  }

  private bool CheckLimit(int count) {
    return count > MessageLimit;
  }
}