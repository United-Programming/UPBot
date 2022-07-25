using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;

/// <summary>
/// Choosing Custom Status for bot! ("Playing CS:GO!")
/// author: J0nathan550
/// </summary>

namespace UPBot.UPBot_Code.DiscordRPC
{
    public class DiscordStatus
    {
        private List<string> activityStatusString = new List<string>();
        private List<int> activityTypeInt = new List<int>();
        public async Task DiscordUpdateStatusFunction(DiscordClient client)
        {
            activityStatusString.Add("at you!"); activityTypeInt.Add(1);
            activityStatusString.Add("for all users!"); activityTypeInt.Add(1);
            activityStatusString.Add("coding algorithms!"); activityTypeInt.Add(0);
            activityStatusString.Add("solving problems"); activityTypeInt.Add(4);
            activityStatusString.Add("programming tutorials"); activityTypeInt.Add(3);
            activityStatusString.Add("sounds in my head"); activityTypeInt.Add(2);

            Random random = new Random();
            int num = random.Next(0, activityStatusString.Count);

            if (activityTypeInt[num] == 0) await client.UpdateStatusAsync(new DiscordActivity(activityStatusString[num], ActivityType.Playing));
            else if (activityTypeInt[num] == 1) await client.UpdateStatusAsync(new DiscordActivity(activityStatusString[num], ActivityType.Watching));
            else if (activityTypeInt[num] == 2) await client.UpdateStatusAsync(new DiscordActivity(activityStatusString[num], ActivityType.ListeningTo));
            else if (activityTypeInt[num] == 3) await client.UpdateStatusAsync(new DiscordActivity(activityStatusString[num], ActivityType.Streaming));
            else if (activityTypeInt[num] == 4) await client.UpdateStatusAsync(new DiscordActivity(activityStatusString[num], ActivityType.Competing));
            else await client.UpdateStatusAsync(new DiscordActivity(activityStatusString[num], ActivityType.Playing));

            await Task.Delay(TimeSpan.FromMinutes(5));
            await DiscordUpdateStatusFunction(client);
        }
    }
}
