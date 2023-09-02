using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;

/// <summary>
/// Choosing Custom Status for bot! ("Playing CS:GO!")
/// author: J0nathan550
/// </summary>

namespace UPBot.DiscordRPC {
  public class DiscordStatus {
    struct ActivityStatus {
      public string status;
      public ActivityType type;
    }

    static async void DiscordUpdateStatusFunction(DiscordClient client, CancellationToken token) {
      List<ActivityStatus> activityStatusString = new() {
        new ActivityStatus { type = ActivityType.Playing, status = "Visual Studio to code algorithms!" },
        new ActivityStatus { type = ActivityType.Playing, status = "a random game" },
        new ActivityStatus { type = ActivityType.Playing, status = "happily with my toys" },
        new ActivityStatus { type = ActivityType.Streaming, status = "the whole life" },
        new ActivityStatus { type = ActivityType.Streaming, status = "a bunch of solution" },
        new ActivityStatus { type = ActivityType.Streaming, status = "programming tutorials" },
        new ActivityStatus { type = ActivityType.Streaming, status = "some lights in the channels" },
        new ActivityStatus { type = ActivityType.ListeningTo, status = "Ode to Joy" },
        new ActivityStatus { type = ActivityType.ListeningTo, status = "your complaints" },
        new ActivityStatus { type = ActivityType.ListeningTo, status = "sounds in my head" },
        new ActivityStatus { type = ActivityType.ListeningTo, status = "the falling rain" },
        new ActivityStatus { type = ActivityType.Watching, status = "you!" },
        new ActivityStatus { type = ActivityType.Watching, status = "all users" },
        new ActivityStatus { type = ActivityType.Watching, status = "for nitro fakes" },
        new ActivityStatus { type = ActivityType.Watching, status = "to reformat code" },
        new ActivityStatus { type = ActivityType.Watching, status = "water boil" },
        new ActivityStatus { type = ActivityType.Watching, status = "grass grow" },
        new ActivityStatus { type = ActivityType.Competing, status = "with other bots" },
        new ActivityStatus { type = ActivityType.Competing, status = "performance review" },
        new ActivityStatus { type = ActivityType.Competing, status = "performance optimization" }
      };

      Random random = new();
      while (!token.IsCancellationRequested) {
        int activity = random.Next(0, activityStatusString.Count);
        ActivityStatus activityStatus = activityStatusString[activity];

        await client.UpdateStatusAsync(new DiscordActivity(activityStatus.status, activityStatus.type));

        await Task.Delay(TimeSpan.FromSeconds(60 + random.Next(0, 180)), token);
      }
    }

    internal static void Start(DiscordClient client) {
      Task statusUpdateTask = new(() => DiscordUpdateStatusFunction(client, new CancellationToken()));
      statusUpdateTask.Start();
    }
  }
}