using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

/// <summary>
/// This command will delete the last x messages
/// or the last x messages of a specific user
/// author: Duck
/// </summary>
public class Delete : BaseCommandModule
{
    private const int MessageLimit = 50;
    
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
        
        if(limitExceeded)
            await ctx.RespondAsync($"The last 50 messages have been removed, since you can't delete more than 50 messages at a time.");
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

        if(limitExceeded)
            await ctx.RespondAsync($"The last 50 messages have been removed, since you can't delete more than 50 messages at a time.");
    }

    private bool CheckLimit(int count)
    {
        return count > MessageLimit;
    }
}