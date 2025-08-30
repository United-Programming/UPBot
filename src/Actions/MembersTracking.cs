﻿using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UPBot.UPBot_Code;

namespace UPBot
{
    public class MembersTracking
    {
        private static Dictionary<ulong, DateTime> tracking = null; // Use one from COnfig, add nonserializable datetime if we need one

        public static async Task DiscordMemberRemoved(DiscordClient _, DSharpPlus.EventArgs.GuildMemberRemoveEventArgs args)
        {
            try
            {
                TrackChannel trackChannel = Configs.TrackChannels[args.Guild.Id];
                if (trackChannel == null || trackChannel.channel == null || !trackChannel.trackLeave) return;
                tracking ??= [];

                int daysJ = (int)(DateTime.Now - args.Member.JoinedAt.DateTime).TotalDays;
                if (daysJ > 10000) daysJ = -1; // User is probably destroyed. So the value will be not valid

                if (tracking.ContainsKey(args.Member.Id) || (daysJ >= 0 && daysJ < 2))
                {
                    tracking.Remove(args.Member.Id);
                    string msg = "User " + args.Member.DisplayName + " did a kiss and go. (" + args.Guild.MemberCount + " members total)";
                    await trackChannel.channel.SendMessageAsync(msg);
                    Utils.Log(msg, args.Guild.Name);
                }
                else
                {
                    string msgC = daysJ >= 0
                        ? Utils.GetEmojiSnowflakeID(EmojiEnum.KO) + "  User " + args.Member.Mention + " (" + args.Member.DisplayName + ") left on " + DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss") + " after " + daysJ + " days (" + args.Guild.MemberCount + " members total)"
                        : Utils.GetEmojiSnowflakeID(EmojiEnum.KO) + "  User " + args.Member.Mention + " (" + args.Member.DisplayName + ") left on " + DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss") + " (" + args.Guild.MemberCount + " members total)";
                    string msgL = "- User " + args.Member.DisplayName + " left on " + DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss") + " (" + args.Guild.MemberCount + " members total)";
                    await trackChannel.channel.SendMessageAsync(msgC);
                    Utils.Log(msgL, args.Guild.Name);
                }
            }
            catch (Exception ex)
            {
                if (ex is DSharpPlus.Exceptions.NotFoundException) return; // Timed out
                Utils.Log("Error in DiscordMemberRemoved: " + ex.Message, args.Guild.Name);
            }

            await Task.Delay(50);
        }

#pragma warning disable IDE0060 // Remove unused parameter
        public static async Task DiscordMemberAdded(DiscordClient _client, DSharpPlus.EventArgs.GuildMemberAddEventArgs args)
        {
            try
            {
                TrackChannel trackChannel = Configs.TrackChannels[args.Guild.Id];
                if (trackChannel == null || trackChannel.channel == null || !trackChannel.trackJoin) return;
                tracking ??= [];

                tracking[args.Member.Id] = DateTime.Now;
                _ = SomethingAsync(trackChannel.channel, args.Member.Id, args.Member.DisplayName, args.Member.Mention, args.Guild.MemberCount);
            }
            catch (Exception ex)
            {
                if (ex is DSharpPlus.Exceptions.NotFoundException) return; // Timed out
                Utils.Log("Error in DiscordMemberAdded: " + ex.Message, args.Guild.Name);
            }
            await Task.Delay(10);
        }
#pragma warning restore IDE0060 // Remove unused parameter

        public static async Task DiscordMemberUpdated(DiscordClient _, DSharpPlus.EventArgs.GuildMemberUpdateEventArgs args)
        {
            try
            {
                TrackChannel trackChannel = Configs.TrackChannels[args.Guild.Id];
                if (trackChannel == null || trackChannel.channel == null || !trackChannel.trackRoles) return;
                tracking ??= [];

                IReadOnlyList<DiscordRole> rolesBefore = args.RolesBefore;
                IReadOnlyList<DiscordRole> rolesAfter = args.RolesAfter;
                List<DiscordRole> rolesAdded = [];
                // Changed role? We can track only additions. Removals are not really sent

                foreach (DiscordRole r1 in rolesAfter)
                {
                    bool addedRole = true;
                    foreach (DiscordRole r2 in rolesBefore)
                    {
                        if (r1.Equals(r2))
                        {
                            addedRole = false;
                            break;
                        }
                    }
                    if (addedRole) rolesAdded.Add(r1);
                }

                if (rolesBefore.Count > 0 && rolesAdded.Count > 0)
                {
                    var msgC = "User " + args.Member.Mention + " has the new role" + (rolesAdded.Count > 1 ? "s:" : ":");
                    var msgL = "User \"" + args.Member.DisplayName + "\" has the new role" + (rolesAdded.Count > 1 ? "s:" : ":");
                    foreach (DiscordRole r in rolesAdded)
                    {
                        msgC += r.Mention;
                        msgL += r.Name;
                    }
                    await trackChannel.channel.SendMessageAsync(msgC);
                    Utils.Log(msgL, args.Guild.Name);
                }
            }
            catch (Exception ex)
            {
                if (ex is DSharpPlus.Exceptions.NotFoundException) return; // Timed out
                Utils.Log("Error in DiscordMemberUpdated: " + ex.Message, args.Guild.Name);
            }

            await Task.Delay(10);
        }


        private static async Task SomethingAsync(DiscordChannel trackChannel, ulong id, string name, string mention, int numMembers)
        {
            await Task.Delay(25000);
            if (tracking.ContainsKey(id))
            {
                string msgC = Utils.GetEmojiSnowflakeID(EmojiEnum.OK) + "  User " + mention + " joined on " + DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss") + " (" + numMembers + " members total)";
                string msgL = "+ User " + name + " joined on " + DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss") + " (" + numMembers + " members total)";
                try
                {
                    await trackChannel.SendMessageAsync(msgC);
                }
                catch (Exception e)
                {
                    Utils.Log("Cannot post in tracking channel: " + e.Message, trackChannel.Guild.Name);
                }
                Utils.Log(msgL, trackChannel.Guild.Name);
                tracking.Remove(id);
            }
        }
    }
}