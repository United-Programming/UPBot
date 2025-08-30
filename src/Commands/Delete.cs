using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UPBot.UPBot_Code;

namespace UPBot
{
    public class SlashDelete : ApplicationCommandModule
    {
        [SlashCommand("massdel", "Deletes all the last messages (massdel 10) or from a user (massdel @User 10) in the channel")]
        public async Task DeleteCommand(InteractionContext ctx,
            [Option("count", "How many messages to delete")][Minimum(1)][Maximum(50)] long count,
            [Option("user", "What user's messages to delete")] DiscordUser user = null)
        {
            // Check permissions
            if (!Configs.HasAdminRole(ctx.Guild.Id, ctx.Member.Roles, false))
            {
                Utils.DefaultNotAllowed(ctx);
                return;
            }

            Utils.LogUserCommand(ctx);

            // Validate count
            if (count <= 0 || count > 50)
            {
                await ctx.CreateResponseAsync(Utils.GenerateErrorAnswer(ctx.Guild.Name, "WhatLanguage",
                    $"Invalid message count: {count}. Must be between 1 and 50."));
                return;
            }

            // Acknowledge the command
            await ctx.CreateResponseAsync("🗑️ Starting deletion process...");

            try
            {
                // Fetch messages from the channel
                var allMessages = new List<DiscordMessage>();
                var messagesToDelete = new List<DiscordMessage>();

                // Get more messages than needed to account for filtering
                int fetchLimit = user == null ? (int)count + 10 : Math.Min(200, (int)count * 3);
                allMessages.AddRange(await ctx.Channel.GetMessagesAsync(fetchLimit));

                // Filter messages based on criteria
                var filteredMessages = allMessages.Where(m =>
                {
                    // Skip the bot's own response message
                    if (m.Author.Id == ctx.Client.CurrentUser.Id &&
                        m.Content.Contains("Starting deletion process")) return false;

                    // If user is specified, only include their messages
                    if (user != null && m.Author.Id != user.Id) return false;

                    return true;
                }).Take((int)count).ToList();

                if (filteredMessages.Count == 0)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent("❌ No messages found to delete."));
                    return;
                }

                // Separate messages by age (Discord bulk delete limitation)
                var cutoffTime = DateTimeOffset.UtcNow.AddDays(-14);
                var recentMessages = filteredMessages.Where(m => m.Timestamp > cutoffTime).ToList();
                var oldMessages = filteredMessages.Where(m => m.Timestamp <= cutoffTime).ToList();

                int totalDeleted = 0;

                // Delete recent messages in bulk (more efficient)
                if (recentMessages.Count > 0)
                {
                    if (recentMessages.Count == 1)
                    {
                        // Single message - use individual delete
                        await recentMessages[0].DeleteAsync();
                        totalDeleted++;
                    }
                    else
                    {
                        // Multiple messages - use bulk delete
                        await ctx.Channel.DeleteMessagesAsync(recentMessages);
                        totalDeleted += recentMessages.Count;
                    }
                }

                // Delete old messages individually (Discord requirement)
                foreach (var oldMessage in oldMessages)
                {
                    try
                    {
                        await oldMessage.DeleteAsync();
                        totalDeleted++;

                        // Small delay to avoid rate limiting
                        if (oldMessages.Count > 5)
                            await Task.Delay(250);
                    }
                    catch (Exception ex)
                    {
                        Utils.Log($"Failed to delete old message: {ex.Message}", ctx.Guild.Name);
                        // Continue with other messages
                    }
                }

                // Update response with results
                string resultMessage = user != null
                    ? $"✅ Deleted {totalDeleted} messages from {user.Username}"
                    : $"✅ Deleted {totalDeleted} messages";

                // Try to edit the response, if it fails, send a new message
                try
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(resultMessage));

                    // Delete the success message after a few seconds
                    await Task.Delay(3000);
                    try
                    {
                        var response = await ctx.GetOriginalResponseAsync();
                        await response.DeleteAsync();
                    }
                    catch { } // Ignore if already deleted
                }
                catch
                {
                    // If editing fails, send a new message
                    var successMsg = await ctx.Channel.SendMessageAsync(resultMessage);
                    await Task.Delay(3000);
                    try
                    {
                        await successMsg.DeleteAsync();
                    }
                    catch { } // Ignore if already deleted
                }
            }
            catch (Exception ex)
            {
                Utils.Log($"Delete command error: {ex.Message}", ctx.Guild.Name);

                try
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent($"❌ Error during deletion: {ex.Message}"));
                }
                catch
                {
                    await ctx.Channel.SendMessageAsync($"❌ Error during deletion: {ex.Message}");
                }
            }
        }
    }
}