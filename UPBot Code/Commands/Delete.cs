using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using UPBot;

/// <summary>
/// This command will delete the last x messages
/// or the last x messages of a specific user
/// author: Duck
/// </summary>
public class Delete : BaseCommandModule
{
    private const int MessageLimit = 50;
    private readonly string callbackLimitExceeded = ", since you can't delete more than 50 messages at a time.";
    
    /// <summary>
    /// Delete the last x messages of any user
    /// </summary>
    [Command("delete")]
    public async Task DeleteCommand(CommandContext ctx, int count)
    {
        bool limitExceeded = CheckLimit(count);
        var messages = ctx.Channel.GetMessagesAsync(count + 1).Result;
        foreach (DiscordMessage m in messages)
        {
            if(m != ctx.Message)
                await m.DeleteAsync();
        }

        await Success(ctx, limitExceeded, count);
    }
    
    /// <summary>
    /// Delete the last x messages of the specified user
    /// </summary>
    [Command("delete")]
    public async Task DeleteCommand(CommandContext ctx, DiscordMember targetUser, int count)
    {
        bool limitExceeded = CheckLimit(count);
        
        // TODO: Get messages by user
        // TODO: Delete messages

        await Success(ctx, limitExceeded, count, targetUser);
    }

    /// <summary>
    /// Will be called at the end of every execution of this command and tells the user that the execution succeeded
    /// including a short summary of the command (how many messages, by which user etc.)
    /// </summary>
    public async Task Success(CommandContext ctx, bool limitExceeded, int count, DiscordMember targetUser = null)
    {
        string mentionUserStr = targetUser == null ? string.Empty : $"by {targetUser.DisplayName}";
        string overLimitStr = limitExceeded ? callbackLimitExceeded : string.Empty;
        string messagesLiteral = UtilityFunctions.PluralFormatter(count, "message", "messages");
        string hasLiteral = UtilityFunctions.PluralFormatter(count, "has", "have");
        
        await ctx.Message.DeleteAsync();
        await ctx.RespondAsync($"The last {count} {messagesLiteral} '{mentionUserStr}' {hasLiteral} been successfully deleted{overLimitStr}.");
    }

    private bool CheckLimit(int count)
    {
        return count > MessageLimit;
    }
}