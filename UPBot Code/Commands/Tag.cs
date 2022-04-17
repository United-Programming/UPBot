using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;

/// <summary>
/// Command that allows helpers, admins, etc. Add more information in "Help Language" script.
/// Author: J0nathan550
/// </summary>

public class Tag : BaseCommandModule {
  public DiscordInteraction interaction; // for waiting user


  [Command("tag")]
  public async Task TagMainCommand(CommandContext ctx) {
    if (!Setup.Permitted(ctx.Guild, Config.ParamType.TagsUse, ctx) && !Setup.Permitted(ctx.Guild, Config.ParamType.TagsDefine, ctx)) return;
    try {
      DiscordEmbedBuilder embed = new DiscordEmbedBuilder {
        Title = "Error in usage!",
        Color = DiscordColor.Red,
        Description = $"Use: `tag add <topic>` - to start registration of topic\nUse: `tag remove <topic>` - to remove topic and included information\nUse: `tag list` - to see all list of topics\nUse: `tag alias <tag> <alias>` - to add an alias for an exisitng topic\nUse: `tag <topic>` - to show a topic.\nUse: `tag edit <topic>` - edit information of topic\nUse:`tag edit-tag <tag>` - to change name of tag`",
        Timestamp = DateTime.Now
      };
      var builder = new DiscordMessageBuilder();
      await Utils.DeleteDelayed(30, ctx.RespondAsync(builder.AddEmbed(embed.Build())));
    } catch (Exception ex) {
      await ctx.RespondAsync(Utils.GenerateErrorAnswer(ctx.Guild.Name, "tag", ex));
    }
  }

  [Command("tag")]
  public async Task TagMainCommand(CommandContext ctx, [Description("Topic to be shown.")] string topic) {
    if (!Setup.Permitted(ctx.Guild, Config.ParamType.TagsUse, ctx)) return;
    try {
      Utils.LogUserCommand(ctx);
      topic = topic.Trim().ToLowerInvariant();
      if (topic == "list") {
        if (Setup.Permitted(ctx.Guild, Config.ParamType.TagsDefine, ctx)) await ShowAllInformation(ctx);
        return;
      }
      await ShowTopic(ctx, topic);
    } catch (Exception ex) {
      await ctx.RespondAsync(Utils.GenerateErrorAnswer(ctx.Guild.Name, "tag", ex));
    }
  }

  [Command("tag")]
  public async Task TagMainCommand(CommandContext ctx, [Description("Command to execute (add, remove, list, alias, edit, edit-tag)")] string command, [Description("Topic to be shown.")] string topic) {
    if (!Setup.Permitted(ctx.Guild, Config.ParamType.TagsDefine, ctx)) return;
    try {
      Utils.LogUserCommand(ctx);
      DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
      topic = topic.Trim().ToLowerInvariant();
      command = command.Trim().ToLowerInvariant();

      if (command == "add") {
        await AddCommand(ctx, topic);
        return;
      }
      else if (command == "remove") {
        await RemoveCommand(ctx, topic);
        return;
      }
      else if (command == "edit") {
        await EditCommand(ctx, topic);
        return;
      }
      else if(command == "edit-tag") {
        await EditTagCommand(ctx, topic);
        return;
      }
      else if (command == "alias") {
        embed.Title = "Error in usage!";
        embed.Color = DiscordColor.Red;
        embed.Description = ($"Alias requires the topic to be aliased and the name of the alias");
        embed.Timestamp = DateTime.Now;
        await Utils.DeleteDelayed(30, ctx.RespondAsync(new DiscordMessageBuilder().AddEmbed(embed.Build())));
        return;
      }
      embed.Title = "Error in usage!";
      embed.Color = DiscordColor.Red;
      embed.Description = ($"Possible commands are: `Add`, `Remove`, and `Edit`");
      embed.Timestamp = DateTime.Now;
      var builder = new DiscordMessageBuilder();
      await Utils.DeleteDelayed(30, ctx.RespondAsync(builder.AddEmbed(embed.Build())));
    } catch (Exception ex) {
      await ctx.RespondAsync(Utils.GenerateErrorAnswer(ctx.Guild.Name, "tag", ex));
    }
  }

  [Command("tag")]
  public async Task TagMainCommand(CommandContext ctx, [Description("Command to execute (add, remove, list, alias, edit.)")] string command, [Description("Topic to be shown.")] string topic, [Description("Alias for the topic")] string alias) {
    if (!Setup.Permitted(ctx.Guild, Config.ParamType.TagsDefine, ctx)) return;
    try {
      Utils.LogUserCommand(ctx);
      DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
      topic = topic.Trim().ToLowerInvariant();
      alias = alias.Trim().ToLowerInvariant();
      command = command.Trim().ToLowerInvariant();

      if (command == "alias") {
        await AliasCommand(ctx, topic, alias);
        return;
      }

      embed.Title = "Error in usage!";
      embed.Color = DiscordColor.Red;
      embed.Description = ($"Possible commands are: `Add` or `Remove`, `Remove`, `Edit`, and `Alias`");
      embed.Timestamp = DateTime.Now;
      var builder = new DiscordMessageBuilder();
      await Utils.DeleteDelayed(30, ctx.RespondAsync(builder.AddEmbed(embed.Build())));
    } catch (Exception ex) {
      await ctx.RespondAsync(Utils.GenerateErrorAnswer(ctx.Guild.Name, "tag", ex));
    }
  }

  TagBase FindTag(ulong gid, string name, bool getClosest) {
    foreach (TagBase tag in Setup.Tags[gid]) {
      if (tag.Topic == name || tag.Alias1 == name || tag.Alias2 == name || tag.Alias3 == name) {
        return tag;
      }
    }
    if (getClosest) {
      // Try to find the closest one

      int min = int.MaxValue;
      TagBase res = null;
      foreach (TagBase tag in Setup.Tags[gid]) {
        int dist = StringDistance.Distance(name, tag.Topic);
        if (min > dist) {
          min = dist;
          res = tag;
        }
        if (tag.Alias1 != null) {
          dist = StringDistance.Distance(name, tag.Alias1);
          if (min > dist) {
            min = dist;
            res = tag;
          }
        }
        if (tag.Alias2 != null) {
          dist = StringDistance.Distance(name, tag.Alias2);
          if (min > dist) {
            min = dist;
            res = tag;
          }
        }
        if (tag.Alias3 != null) {
          dist = StringDistance.Distance(name, tag.Alias3);
          if (min > dist) {
            min = dist;
            res = tag;
          }
        }
      }
      if (min < 100) {
        return res;
      }
    }

    return null;
  }


  public async Task AddCommand(CommandContext ctx, string topic) {
    DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
    var builder = new DiscordMessageBuilder();

    foreach (var topics in Setup.Tags[ctx.Guild.Id]) {
      if (topic == topics.Topic) {
        embed.Title = "The Topic exists already!";
        embed.Color = DiscordColor.Red;
        embed.Description = ($"You are trying to add topic that already exists!\nIf you want to edit the topic use: `tag edit <topic>` - to edit");
        embed.Timestamp = DateTime.Now;
        await Utils.DeleteDelayed(30, ctx.RespondAsync(builder.AddEmbed(embed.Build())));
        return;
      }
    }

    embed.Title = "Add topic with information";
    embed.Color = DiscordColor.Green;
    embed.Description = ($"Type the content of the {topic}.");
    embed.Timestamp = DateTime.Now;
    DiscordMessage question = await ctx.RespondAsync(builder.AddEmbed(embed.Build()));

    var interact = ctx.Client.GetInteractivity();
    var answer = await interact.WaitForMessageAsync((dm) => {
      return (dm.Channel == ctx.Channel && dm.Author.Id == ctx.Member.Id);
    }, TimeSpan.FromMinutes(5));
    await question.DeleteAsync();

    if (answer.Result == null) {
      embed.Title = "Time expired!";
      embed.Color = DiscordColor.Red;
      embed.Description = $"You took too much time to type the tag. :KO:";
      embed.Timestamp = DateTime.Now;
      await Utils.DeleteDelayed(30, ctx.RespondAsync(builder.AddEmbed(embed.Build())));
      return;
    }

    TagBase tagBase = new TagBase(ctx.Guild.Id, topic, answer.Result.Content); // creating line inside of database
    Database.Add(tagBase); // adding information to base
    Setup.Tags[ctx.Guild.Id].Add(tagBase);

    embed.Title = "Topic added";
    embed.Color = DiscordColor.Green;
    embed.Description = ($"The topic: {topic}, has been created");
    embed.Timestamp = DateTime.Now;
    await Utils.DeleteDelayed(30, ctx.RespondAsync(builder.AddEmbed(embed.Build())));

  }

  public async Task EditCommand(CommandContext ctx, string topic) {
    DiscordEmbedBuilder embed = new DiscordEmbedBuilder();

    TagBase toEdit = FindTag(ctx.Guild.Id, topic, false);
    if (toEdit == null) {
      embed.Title = "The Topic does not exist!";
      embed.Color = DiscordColor.Red;
      embed.Description = $"The tag `{topic}` does not exist";
      embed.Timestamp = DateTime.Now;
      await Utils.DeleteDelayed(30, ctx.RespondAsync(new DiscordMessageBuilder().AddEmbed(embed.Build())));
      return;
    }

    embed.Title = $"Editing {topic}";
    embed.Color = DiscordColor.Purple;
    embed.Description = ($"You are editing the {topic.ToUpperInvariant()}.\nBetter to copy previous text, and edit inside of message.");
    embed.Timestamp = DateTime.Now;
    var builder = new DiscordMessageBuilder();
    DiscordMessage question = await ctx.RespondAsync(builder.AddEmbed(embed.Build()));

    var interact = ctx.Client.GetInteractivity();
    var answer = await interact.WaitForMessageAsync((dm) => {
      return (dm.Channel == ctx.Channel && dm.Author.Id == ctx.Member.Id);
    }, TimeSpan.FromMinutes(5));
    await question.DeleteAsync();

    if (answer.Result == null || string.IsNullOrWhiteSpace(answer.Result.Content)) {
      embed.Title = "Time Passed!";
      embed.Color = DiscordColor.Red;
      embed.Description = ($"You have being answering for too long. :KO:");
      embed.Timestamp = DateTime.Now;
      await Utils.DeleteDelayed(30, ctx.RespondAsync(builder.AddEmbed(embed.Build())));
      return;
    }

    toEdit.Information = answer.Result.Content;
    Database.Add(toEdit); // adding information to base

    embed.Title = "Changes accepted";
    embed.Color = DiscordColor.Green;
    embed.Description = $"New information for {topic.ToUpperInvariant()}, is:\n\n{answer.Result.Content}\n";
    embed.Timestamp = DateTime.Now;
    await Utils.DeleteDelayed(30, ctx.RespondAsync(builder.AddEmbed(embed.Build())));
  }
  public async Task EditTagCommand(CommandContext ctx, string topic)
  {
        DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
        TagBase toEdit = FindTag(ctx.Guild.Id, topic, false);
        if (toEdit == null)
        {
            embed.Title = "Tag does not exist!";
            embed.Color = DiscordColor.Red;
            embed.Description = $"The tag `{topic}` does not exist!";
            embed.Timestamp = DateTime.Now;
            await Utils.DeleteDelayed(30, ctx.RespondAsync(new DiscordMessageBuilder().AddEmbed(embed.Build())));
            return;
        }
        embed.Title = $"Editing name of tag: {topic}";
        embed.Color = DiscordColor.Purple;
        embed.Description = ($"You are editing the {topic.ToUpperInvariant()}.");
        embed.Timestamp = DateTime.Now;
        var builder = new DiscordMessageBuilder();
        DiscordMessage question = await ctx.RespondAsync(builder.AddEmbed(embed.Build()));

        var interact = ctx.Client.GetInteractivity();
        var answer = await interact.WaitForMessageAsync((dm) => {
            return (dm.Channel == ctx.Channel && dm.Author.Id == ctx.Member.Id);
        }, TimeSpan.FromMinutes(5));
        await question.DeleteAsync();

        if (answer.Result == null || string.IsNullOrWhiteSpace(answer.Result.Content))
        {
            embed.Title = "Time Passed!";
            embed.Color = DiscordColor.Red;
            embed.Description = ($"You have being answering for too long. :KO:");
            embed.Timestamp = DateTime.Now;
            await Utils.DeleteDelayed(30, ctx.RespondAsync(builder.AddEmbed(embed.Build())));
            return;
        }

        toEdit.Topic = answer.Result.Content;
        Database.Add(toEdit); // adding information to base

        embed.Title = "Changes accepted";
        embed.Color = DiscordColor.Green;
        embed.Description = $"New name for {topic.ToUpperInvariant()}, changed to:\n\n{answer.Result.Content}\n";
        embed.Timestamp = DateTime.Now;
        await Utils.DeleteDelayed(30, ctx.RespondAsync(builder.AddEmbed(embed.Build())));
  }

  public async Task AliasCommand(CommandContext ctx, string topic, string alias) {
    DiscordEmbedBuilder embed = new DiscordEmbedBuilder();

    // Find it, can be an alias
    TagBase toAlias = FindTag(ctx.Guild.Id, topic, false);
    if (toAlias == null) {
      embed.Title = "The Topic does not exist!";
      embed.Color = DiscordColor.Red;
      embed.Description = $"The tag `{topic}` does not exist";
      embed.Timestamp = DateTime.Now;
      await Utils.DeleteDelayed(30, ctx.RespondAsync(new DiscordMessageBuilder().AddEmbed(embed.Build())));
      return;
    }
    // Check if we do nto have the alias already
    if (toAlias.Topic == alias || toAlias.Alias1 == alias || toAlias.Alias2 == alias || toAlias.Alias3 == alias) {
      embed.Title = "Alias already existing";
      embed.Color = DiscordColor.Yellow;
      embed.Description = $"Aliases for {toAlias.Topic.ToUpperInvariant()}:\n";
      if (toAlias.Alias3 != null) embed.Description += $" (_**{toAlias.Alias1}**_, _**{toAlias.Alias2}**_, _**{toAlias.Alias3}**_)";
      else if (toAlias.Alias2 != null) embed.Description += $" (_**{toAlias.Alias1}**_, _**{toAlias.Alias2}**_)";
      else if (toAlias.Alias1 != null) embed.Description += $" (_**{toAlias.Alias1}**_)";
      embed.Timestamp = DateTime.Now;
      await Utils.DeleteDelayed(30, ctx.RespondAsync(new DiscordMessageBuilder().AddEmbed(embed.Build())));
      return;
    }

    // Find the first empty alias slot
    if (toAlias.Alias1 == null) toAlias.Alias1 = alias;
    else if (toAlias.Alias2 == null) toAlias.Alias2 = alias;
    else if (toAlias.Alias3 == null) toAlias.Alias3 = alias;
    else { // Shift and replace the last one
      toAlias.Alias1 = toAlias.Alias2;
      toAlias.Alias2 = toAlias.Alias3;
      toAlias.Alias3 = alias;
    }
    Database.Add(toAlias);

    embed.Title = "Alias accepted";
    embed.Color = DiscordColor.Green;
    embed.Description = $"Aliases for {toAlias.Topic.ToUpperInvariant()}:\n";
    if (toAlias.Alias3 != null) embed.Description += $" (_**{toAlias.Alias1}**_, _**{toAlias.Alias2}**_, _**{toAlias.Alias3}**_)";
    else if (toAlias.Alias2 != null) embed.Description += $" (_**{toAlias.Alias1}**_, _**{toAlias.Alias2}**_)";
    else if (toAlias.Alias1 != null) embed.Description += $" (_**{toAlias.Alias1}**_)";
    embed.Timestamp = DateTime.Now;
    await Utils.DeleteDelayed(30, ctx.RespondAsync(new DiscordMessageBuilder().AddEmbed(embed.Build())));
  }

  public async Task RemoveCommand(CommandContext ctx, string topic) {
    DiscordEmbedBuilder embed = new DiscordEmbedBuilder();

    TagBase toRemove = FindTag(ctx.Guild.Id, topic, false);
    foreach (TagBase tag in Setup.Tags[ctx.Guild.Id]) {
      if (tag.Topic == topic || tag.Alias1 == topic || tag.Alias2 == topic || tag.Alias3 == topic) {
        toRemove = tag;
        break;
      }
    }
    if (toRemove == null) {
      embed.Title = "The Topic does not exist!";
      embed.Color = DiscordColor.Red;
      embed.Description = $"The tag `{topic}` does not exist";
      embed.Timestamp = DateTime.Now;
      await Utils.DeleteDelayed(30, ctx.RespondAsync(new DiscordMessageBuilder().AddEmbed(embed.Build())));
      return;
    }
    Setup.Tags[ctx.Guild.Id].Remove(toRemove);
    Database.DeleteByKeys<TagBase>(ctx.Guild.Id, toRemove);

    embed.Title = "Topic deleted";
    embed.Color = DiscordColor.DarkRed;
    embed.Description = ($"Topic `{topic}` has been deleted by {ctx.Member.DisplayName}");
    embed.Timestamp = DateTime.Now;
    var builder = new DiscordMessageBuilder();
    await Utils.DeleteDelayed(30, ctx.RespondAsync(builder.AddEmbed(embed.Build())));
  }

  public async Task ShowAllInformation(CommandContext ctx) {
    DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
    string result = "";
    if (Setup.Tags[ctx.Guild.Id].Count == 0) {
      result = "No tags are defined.";
    }
    else {
      foreach (TagBase tag in Setup.Tags[ctx.Guild.Id]) {
        result += $"**{tag.Topic}**";
        if (tag.Alias3 != null) result += $" (_**{tag.Alias1}**_, _**{tag.Alias2}**_, _**{tag.Alias3}**_)";
        else if (tag.Alias2 != null) result += $" (_**{tag.Alias1}**_, _**{tag.Alias2}**_)";
        else if (tag.Alias1 != null) result += $" (_**{tag.Alias1}**_)";
        result += $", ";
      }
    }
    embed.Title = "List of tags";
    embed.Color = DiscordColor.Blurple;
    embed.Description = result[0..^2];
    embed.Timestamp = DateTime.Now;
    var builder = new DiscordMessageBuilder();
    await ctx.RespondAsync(builder.AddEmbed(embed.Build()));
  }

  public async Task ShowTopic(CommandContext ctx, string topic) {
    DiscordEmbedBuilder embed = new DiscordEmbedBuilder {
      Title = $"{topic}"
    };
    int randomnumber = rand.Next(0, randColor.Length);
    embed.Color = randColor[randomnumber];
    embed.Timestamp = DateTime.Now;

    TagBase tag = FindTag(ctx.Guild.Id, topic, true);
    if (tag != null) {
      await ctx.RespondAsync(new DiscordMessageBuilder().AddEmbed(embed.WithDescription(tag.Information).Build()));
    }
    else {
      await Utils.DeleteDelayed(30, ctx.RespondAsync(new DiscordMessageBuilder().AddEmbed(embed.WithDescription($"{topic} tag does not exist.").Build())));
    }
  }

  readonly Random rand = new Random();
  readonly DiscordColor[] randColor = { DiscordColor.Aquamarine, DiscordColor.Azure, DiscordColor.Black, DiscordColor.Blue, DiscordColor.Blurple,
            DiscordColor.Brown, DiscordColor.Chartreuse, DiscordColor.CornflowerBlue, DiscordColor.Cyan, DiscordColor.DarkBlue,
            DiscordColor.DarkButNotBlack, DiscordColor.DarkGray, DiscordColor.DarkGreen, DiscordColor.DarkRed, DiscordColor.Gold,
            DiscordColor.Goldenrod, DiscordColor.Gray, DiscordColor.Grayple, DiscordColor.Green, DiscordColor.HotPink, DiscordColor.IndianRed,
            DiscordColor.LightGray, DiscordColor.Lilac, DiscordColor.Magenta, DiscordColor.MidnightBlue, DiscordColor.NotQuiteBlack, DiscordColor.Orange,
            DiscordColor.PhthaloBlue, DiscordColor.PhthaloGreen, DiscordColor.Purple, DiscordColor.Red, DiscordColor.Rose, DiscordColor.SapGreen,
            DiscordColor.Sienna, DiscordColor.SpringGreen, DiscordColor.Teal, DiscordColor.Turquoise, DiscordColor.VeryDarkGray, DiscordColor.Violet,
            DiscordColor.Wheat, DiscordColor.Yellow, DiscordColor.White};

}
