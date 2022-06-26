using System;
using System.Threading.Tasks;
using DSharpPlus.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;

/// <summary>
/// Command that allows helpers, admins, etc. Add more information in "Help Language" script.
/// Author: J0nathan550, CPU
/// </summary>

public class SlashTags : ApplicationCommandModule {
  [SlashCommand("tag", "Show the contents of a specific tag (shows all the tags in case no tag is specified)")]
  public async Task TagCommand(InteractionContext ctx, [Option("tagname", "Tag to be shown")] string tagname = null) {
    Utils.LogUserCommand(ctx);
    if (tagname != null) {
      try {
        TagBase tag = FindTag(ctx.Guild.Id, tagname.Trim(), true);
        //DiscordEmbedBuilder embed = new();
        var builder = new DiscordEmbedBuilder();
        if (tag.ColorOfTheme == discordColors.Length) {
          int randomnumber = rand.Next(0, discordColors.Length);
          builder.Color = discordColors[randomnumber];
        }
        else {
          builder.Color = discordColors[tag.ColorOfTheme];
        }
        builder.Timestamp = tag.timeOfCreation;
        if(tag.thumbnailLink != null) { 
            //builder.Thumbnail.Url = tag.thumbnailLink;
            builder.Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail
            {
                Url = $"{tag.thumbnailLink}"
            };
        }
        else { }
        if (tag != null) {
          builder.Title = tag.Topic;
          string descr = "";
          if (tag.Alias3 != null) descr += $"Aliases: _**{CleanName(tag.Alias1)}**_, _**{CleanName(tag.Alias2)}**_, _**{CleanName(tag.Alias3)}**_\n";
          else if (tag.Alias2 != null) descr += $"Aliases: _**{CleanName(tag.Alias1)}**_, _**{CleanName(tag.Alias2)}**_\n";
          else if (tag.Alias1 != null) descr += $"Alias: _**{CleanName(tag.Alias1)}**_\n";
          if (!string.IsNullOrWhiteSpace(tag.Author))
            descr += $"Author: **{tag.Author}**\n\n";
          descr += tag.Information;
          await ctx.CreateResponseAsync(builder.WithDescription(descr));
        }
        else {
          await ctx.CreateResponseAsync(builder.WithDescription($"{tagname} tag does not exist."), true);
        }
      } catch (Exception ex) {
        await ctx.CreateResponseAsync(Utils.GenerateErrorAnswer(ctx.Guild.Name, "Tag", ex));
      }
    }
    else {
      try {
        DiscordEmbedBuilder embed = new();
        string result = "";
        if (Configs.Tags[ctx.Guild.Id].Count == 0) {
          result = "No tags are defined. ";
        }
        else {
          int count = 0;
          foreach (TagBase tag in Configs.Tags[ctx.Guild.Id]) {
            count++;
            result += $"**{CleanName(tag.Topic)}**";
            if (tag.Alias3 != null) result += $"Aliases: _**{CleanName(tag.Alias1)}**_, _**{CleanName(tag.Alias2)}**_, _**{CleanName(tag.Alias3)}**_";
            else if (tag.Alias2 != null) result += $"Aliases: _**{CleanName(tag.Alias1)}**_, _**{CleanName(tag.Alias2)}**_";
            else if (tag.Alias1 != null) result += $"Alias: _**{CleanName(tag.Alias1)}**_";
            if (count < Configs.Tags[ctx.Guild.Id].Count - 1) result += ", \n";
            else result += ".";
          }
          count = 0;
        }
        embed.Title = "List of tags";
        embed.Color = DiscordColor.Blurple;
        embed.Description = result[0..^2];
        embed.Timestamp = DateTime.Now;
        await ctx.CreateResponseAsync(embed);
      } catch (Exception ex) {
        await ctx.CreateResponseAsync(Utils.GenerateErrorAnswer(ctx.Guild.Name, "TagList", ex));
      }
    }
  }

  static string CleanName(string name) {
    return name.Replace("*", "\\*").Replace("_", "\\_").Replace("`", "\\`");
  }

  public static TagBase FindTag(ulong gid, string name, bool getClosest) {
    foreach (TagBase tag in Configs.Tags[gid]) {
      if (name.Equals(tag.Topic, StringComparison.InvariantCultureIgnoreCase) ||
        name.Equals(tag.Alias1, StringComparison.InvariantCultureIgnoreCase) ||
        name.Equals(tag.Alias2, StringComparison.InvariantCultureIgnoreCase) ||
        name.Equals(tag.Alias3, StringComparison.InvariantCultureIgnoreCase)) {
        return tag;
      }
    }
    if (getClosest) {
      // Try to find the closest one

      int min = int.MaxValue;
      TagBase res = null;
      foreach (TagBase tag in Configs.Tags[gid]) {
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

  readonly Random rand = new();
  readonly public static DiscordColor[] discordColors = {
      DiscordColor.Aquamarine,
      DiscordColor.Azure,
      DiscordColor.Blurple,
      DiscordColor.Chartreuse,
      DiscordColor.CornflowerBlue,
      DiscordColor.DarkBlue,
      DiscordColor.DarkButNotBlack,
      DiscordColor.Gold,
      DiscordColor.Grayple,
      DiscordColor.Green,
      DiscordColor.IndianRed,
      DiscordColor.Lilac,
      DiscordColor.MidnightBlue,
      DiscordColor.NotQuiteBlack,
      DiscordColor.Orange,
      DiscordColor.PhthaloBlue,
      DiscordColor.PhthaloGreen,
      DiscordColor.Red,
      DiscordColor.Rose,
      DiscordColor.SapGreen,
      DiscordColor.Teal,
      DiscordColor.Yellow
  };
}

public enum TagColorValue {
  [ChoiceName("Aquamarine")] Aquamarine = 0,
  [ChoiceName("Azure")] Azure = 1,
  [ChoiceName("Blurple")] Blurple = 2,
  [ChoiceName("Chartreuse")] Chartreuse = 3,
  [ChoiceName("CornflowerBlue")] CornflowerBlue = 4,
  [ChoiceName("DarkBlue")] DarkBlue = 5,
  [ChoiceName("DarkButNotBlack")] DarkButNotBlack = 6,
  [ChoiceName("Gold")] Gold = 7,
  [ChoiceName("Grayple")] Grayple = 8,
  [ChoiceName("Green")] Green = 9,
  [ChoiceName("IndianRed")] IndianRed = 10,
  [ChoiceName("Lilac")] Lilac = 11,
  [ChoiceName("MidnightBlue")] MidnightBlue = 12,
  [ChoiceName("NotQuiteBlack")] NotQuiteBlack = 13,
  [ChoiceName("Orange")] Orange = 14,
  [ChoiceName("PhthaloBlue")] PhthaloBlue = 15,
  [ChoiceName("PhthaloGreen")] PhthaloGreen = 16,
  [ChoiceName("Red")] Red = 17,
  [ChoiceName("Rose")] Rose = 18,
  [ChoiceName("SapGreen")] SapGreen = 19,
  [ChoiceName("Teal")] Teal = 20,
  [ChoiceName("Yellow")] Yellow = 21,
  [ChoiceName("Random")] Random = 22
//  [ChoiceName("Sienna")] Sienna = 33,
//  [ChoiceName("HotPink")] HotPink = 19,
//  [ChoiceName("Black")] Black = 2,
//  [ChoiceName("Blue")] Blue = 3,
//  [ChoiceName("Brown")] Brown = 5,
//  [ChoiceName("Cyan")] Cyan = 8,
//  [ChoiceName("DarkGray")] DarkGray = 11,
//  [ChoiceName("DarkGreen")] DarkGreen = 12,
//  [ChoiceName("DarkRed")] DarkRed = 13,
//  [ChoiceName("Goldenrod")] Goldenrod = 15,
//  [ChoiceName("Gray")] Gray = 16,
//  [ChoiceName("LightGray")] LightGray = 21,
//  [ChoiceName("Magenta")] Magenta = 23,
//  [ChoiceName("Purple")] Purple = 29,
//  [ChoiceName("SpringGreen")] SpringGreen = 34,
//  [ChoiceName("Turquoise")] Turquoise = 36,
//  [ChoiceName("VeryDarkGray")] VeryDarkGray = 37,
//  [ChoiceName("Violet,")] Violet = 38,
//  [ChoiceName("Wheat")] Wheat = 39,
//  [ChoiceName("White")] White = 41,
}



[SlashCommandGroup("tags", "Define and manage your tags")]
public class SlashTagsEdit : ApplicationCommandModule {
  [SlashCommand("addtag", "Adds a new tag")]
  public async Task TagAddCommand(InteractionContext ctx, [Option("tagname", "Tag to be added")] string tagname) {
    Utils.LogUserCommand(ctx);

    try {
      DiscordEmbedBuilder embed = new();
      tagname = tagname.Trim();

      foreach (var topics in Configs.Tags[ctx.Guild.Id]) {
        if (tagname.Equals(topics.Topic, StringComparison.InvariantCultureIgnoreCase) ||
            tagname.Equals(topics.Alias1, StringComparison.InvariantCultureIgnoreCase) ||
            tagname.Equals(topics.Alias2, StringComparison.InvariantCultureIgnoreCase) ||
            tagname.Equals(topics.Alias3, StringComparison.InvariantCultureIgnoreCase)) {
          embed.Title = "The Tag exists already!";
          embed.Color = DiscordColor.Red;
          embed.Description = ($"You are trying to add Tag {tagname} that already exists!\nIf you want to edit the Tag use: `tagedit <topic>` - to edit");
          embed.Timestamp = DateTime.Now;
          await ctx.CreateResponseAsync(embed, true);
          return;
        }
      }

      embed.Title = "Adding a Tag";
      embed.Color = DiscordColor.Green;
      embed.Description = $"Type the content of the Tag {tagname}.";
      embed.Timestamp = DateTime.Now;
      await ctx.CreateResponseAsync(embed);

      var interact = ctx.Client.GetInteractivity();
      var answer = await interact.WaitForMessageAsync((dm) => {
        return (dm.Channel == ctx.Channel && dm.Author.Id == ctx.Member.Id);
      }, TimeSpan.FromMinutes(5));

      if (answer.Result == null) {
        embed.Title = "Time expired!";
        embed.Color = DiscordColor.Red;
        embed.Description = $"You took too much time to type the tag.";
        embed.Timestamp = DateTime.Now;
        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed));
        return;
      }

      TagBase tagBase = new(ctx.Guild.Id, tagname, answer.Result.Content, "Unknown", 22, DateTime.Now, null); // creating line inside of database
      Database.Add(tagBase); // adding information to base
      Configs.Tags[ctx.Guild.Id].Add(tagBase);

      embed.Title = "Tag added";
      embed.Color = DiscordColor.Green;
      embed.Description = ($"The topic: {tagname}, has been created");
      embed.Timestamp = DateTime.Now;
      await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed));
    } catch (Exception ex) {
      await ctx.CreateResponseAsync(Utils.GenerateErrorAnswer(ctx.Guild.Name, "TagAdd", ex));
    }
  }


  [SlashCommand("removetag", "Removes an existing tag")]
  public async Task TagRemoveCommand(InteractionContext ctx, [Option("tagname", "Tag to be removed")] string tagname) {
    Utils.LogUserCommand(ctx);

    try {
      DiscordEmbedBuilder embed = new();

      TagBase toRemove = SlashTags.FindTag(ctx.Guild.Id, tagname, false);
      if (toRemove == null) {
        embed.Title = "The Tag does not exist!";
        embed.Color = DiscordColor.Red;
        embed.Description = $"The tag `{tagname}` does not exist";
        embed.Timestamp = DateTime.Now;
        await ctx.CreateResponseAsync(embed, true);
        return;
      }
      Configs.Tags[ctx.Guild.Id].Remove(toRemove);
      Database.DeleteByKeys<TagBase>(ctx.Guild.Id, toRemove);

      embed.Title = "Topic deleted";
      embed.Color = DiscordColor.DarkRed;
      embed.Description = ($"Tag `{tagname}` has been deleted by {ctx.Member.DisplayName}");
      embed.Timestamp = DateTime.Now;
      await ctx.CreateResponseAsync(embed, true);
    } catch (Exception ex) {
      await ctx.CreateResponseAsync(Utils.GenerateErrorAnswer(ctx.Guild.Name, "TagRemove", ex));
    }
  }

  [SlashCommand("listtags", "Shows all tags")]
  public async Task TagListCommand(InteractionContext ctx) {
    Utils.LogUserCommand(ctx);
    try {
      DiscordEmbedBuilder embed = new();
      string result = "";
      if (Configs.Tags[ctx.Guild.Id].Count == 0) {
        result = "No tags are defined.";
      }
      else {
        foreach (TagBase tag in Configs.Tags[ctx.Guild.Id]) {
          result += $"**{tag.Topic}**";
          if (tag.Alias3 != null) result += $" (_**{tag.Alias1}**_, _**{tag.Alias2}**_, _**{tag.Alias3}**_)";
          else if (tag.Alias2 != null) result += $" (_**{tag.Alias1}**_, _**{tag.Alias2}**_)";
          else if (tag.Alias1 != null) result += $" (_**{tag.Alias1}**_)";
          result += $",\n";
        }
      }
      embed.Title = "List of tags";
      embed.Color = DiscordColor.Blurple;
      embed.Description = result[0..^2];
      embed.Timestamp = DateTime.Now;
      await ctx.CreateResponseAsync(embed);
    } catch (Exception ex) {
      await ctx.CreateResponseAsync(Utils.GenerateErrorAnswer(ctx.Guild.Name, "TagList", ex));
    }
  }

  [SlashCommand("aliastag", "Define aliases for a tag")]
  public async Task TagAliasCommand(InteractionContext ctx, [Option("tagname", "Tag to be aliased")] string tagname, [Option("alias1", "First alias")] string alias1, [Option("alias2", "Second alias")] string alias2 = null, [Option("alias3", "Third alias")] string alias3 = null) {
    Utils.LogUserCommand(ctx);

    try {
      DiscordEmbedBuilder embed = new();

      // Find it, can be an alias
      TagBase toAlias = SlashTags.FindTag(ctx.Guild.Id, tagname, false);
      if (toAlias == null) {
        embed.Title = "The Topic does not exist!";
        embed.Color = DiscordColor.Red;
        embed.Description = $"The tag `{tagname}` does not exist";
        embed.Timestamp = DateTime.Now;
        await ctx.CreateResponseAsync(embed, true);
        return;
      }
      // Check if we do not have the alias already
      if (alias1.Equals(toAlias.Topic, StringComparison.InvariantCultureIgnoreCase) || alias1.Equals(toAlias.Alias1, StringComparison.InvariantCultureIgnoreCase) ||
          alias1.Equals(toAlias.Alias2, StringComparison.InvariantCultureIgnoreCase) || alias1.Equals(toAlias.Alias3, StringComparison.InvariantCultureIgnoreCase) ||
          (alias2 != null && (alias2.Equals(toAlias.Topic, StringComparison.InvariantCultureIgnoreCase) || alias2.Equals(toAlias.Alias1, StringComparison.InvariantCultureIgnoreCase) ||
                              alias2.Equals(toAlias.Alias2, StringComparison.InvariantCultureIgnoreCase) || alias2.Equals(toAlias.Alias3, StringComparison.InvariantCultureIgnoreCase))) ||
          (alias3 != null && (alias3.Equals(toAlias.Topic, StringComparison.InvariantCultureIgnoreCase) || alias3.Equals(toAlias.Alias1, StringComparison.InvariantCultureIgnoreCase) ||
                              alias3.Equals(toAlias.Alias2, StringComparison.InvariantCultureIgnoreCase) || alias3.Equals(toAlias.Alias3, StringComparison.InvariantCultureIgnoreCase)))) {
        embed.Title = "Alias already existing";
        embed.Color = DiscordColor.Yellow;
        embed.Description = $"Aliases for {toAlias.Topic.ToUpperInvariant()}:\n";
        if (toAlias.Alias3 != null) embed.Description += $" (_**{toAlias.Alias1}**_, _**{toAlias.Alias2}**_, _**{toAlias.Alias3}**_)";
        else if (toAlias.Alias2 != null) embed.Description += $" (_**{toAlias.Alias1}**_, _**{toAlias.Alias2}**_)";
        else if (toAlias.Alias1 != null) embed.Description += $" (_**{toAlias.Alias1}**_)";
        embed.Timestamp = DateTime.Now;
        await ctx.CreateResponseAsync(embed, true);
        return;
      }

      // Find the first empty alias slot
      toAlias.Alias1 = alias1;
      toAlias.Alias2 = alias2;
      toAlias.Alias3 = alias3;
      Database.Add(toAlias);

      embed.Title = "Alias accepted";
      embed.Color = DiscordColor.Green;
      embed.Description = $"Aliases for {toAlias.Topic.ToUpperInvariant()}:\n";
      if (toAlias.Alias3 != null) embed.Description += $" (_**{toAlias.Alias1}**_, _**{toAlias.Alias2}**_, _**{toAlias.Alias3}**_)";
      else if (toAlias.Alias2 != null) embed.Description += $" (_**{toAlias.Alias1}**_, _**{toAlias.Alias2}**_)";
      else if (toAlias.Alias1 != null) embed.Description += $" (_**{toAlias.Alias1}**_)";
      embed.Timestamp = DateTime.Now;
      await ctx.CreateResponseAsync(embed);
    } catch (Exception ex) {
      await ctx.CreateResponseAsync(Utils.GenerateErrorAnswer(ctx.Guild.Name, "TagAlias", ex));
    }
  }

  [SlashCommand("edittag", "Edit an existing tag")]
  public async Task TagEditCommand(InteractionContext ctx, [Option("tagname", "Tag to be modified")] string tagname) {
    Utils.LogUserCommand(ctx);

    try {
      DiscordEmbedBuilder embed = new();

      TagBase toEdit = SlashTags.FindTag(ctx.Guild.Id, tagname, false);
      if (toEdit == null) {
        embed.Title = "The Topic does not exist!";
        embed.Color = DiscordColor.Red;
        embed.Description = $"The tag `{tagname}` does not exist";
        embed.Timestamp = DateTime.Now;
        await ctx.CreateResponseAsync(embed, true);
        return;
      }

      embed.Title = $"Editing {tagname}";
      embed.Color = DiscordColor.Purple;
      embed.Description = ($"You are editing the {tagname.ToUpperInvariant()}.\nBetter to copy previous text, and edit inside of message.");
      embed.Timestamp = DateTime.Now;
      await ctx.CreateResponseAsync(embed);

      var interact = ctx.Client.GetInteractivity();
      var answer = await interact.WaitForMessageAsync((dm) => {
        return (dm.Channel == ctx.Channel && dm.Author.Id == ctx.Member.Id);
      }, TimeSpan.FromMinutes(5));

      if (answer.Result == null || string.IsNullOrWhiteSpace(answer.Result.Content)) {
        embed.Title = "Time expired!";
        embed.Color = DiscordColor.Red;
        embed.Description = ($"You took too much time to answer. :KO:");
        embed.Timestamp = DateTime.Now;
        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed));
        return;
      }

      toEdit.Information = answer.Result.Content;
      Database.Add(toEdit); // adding information to base

      embed.Title = "Changes accepted";
      embed.Color = DiscordColor.Green;
      embed.Description = $"New information for {tagname.ToUpperInvariant()}, is:\n\n{answer.Result.Content}\n";
      embed.Timestamp = DateTime.Now;
      await ctx.CreateResponseAsync(embed);
    } catch (Exception ex) {
      await ctx.CreateResponseAsync(Utils.GenerateErrorAnswer(ctx.Guild.Name, "TagEdit", ex));
    }
  }


  [SlashCommand("renametag", "Rename a tag")]
  public async Task TagRenameCommand(InteractionContext ctx, [Option("tagname", "Tag to be modified")] string oldname, [Option("newname", "The new name for the tag")] string newname) {
    Utils.LogUserCommand(ctx);

    try {
      DiscordEmbedBuilder embed = new();
      TagBase toEdit = SlashTags.FindTag(ctx.Guild.Id, oldname, false);
      if (toEdit == null) {
        embed.Title = "Tag does not exist!";
        embed.Color = DiscordColor.Red;
        embed.Description = $"The tag `{oldname}` does not exist!";
        embed.Timestamp = DateTime.Now;
        await ctx.CreateResponseAsync(embed, true);
        return;
      }

      toEdit.Topic = newname.Trim();
      Database.Add(toEdit); // adding information to base

      embed.Title = "Changes accepted";
      embed.Color = DiscordColor.Green;
      embed.Description = $"New name for {oldname.ToUpperInvariant()}, changed to:\n\n{newname}\n";
      embed.Timestamp = DateTime.Now;

      await ctx.CreateResponseAsync(embed);
    } catch (Exception ex) {
      await ctx.CreateResponseAsync(Utils.GenerateErrorAnswer(ctx.Guild.Name, "TagRename", ex));
    }
  }

  [SlashCommand("addauthor", "Add author to the tag")]
  public async Task TagAddAuthor(InteractionContext ctx, [Option("tagname", "Tag to change the author")] string tagName, [Option("authorname", "Pick author of tag")] string authorName) {
    Utils.LogUserCommand(ctx);
    try {
      DiscordEmbedBuilder embed = new();
      TagBase toEdit = SlashTags.FindTag(ctx.Guild.Id, tagName, false);
      if (toEdit == null) {
        embed.Title = "Tag does not exist!";
        embed.Color = DiscordColor.Red;
        embed.Description = $"The tag `{tagName}` does not exist!";
        embed.Timestamp = DateTime.Now;
        await ctx.CreateResponseAsync(embed, true);
        return;
      }

      toEdit.Author = authorName.Trim();
      Database.Add(toEdit); // adding information to base

      embed.Title = "Changes accepted!";
      embed.Color = DiscordColor.Green;
      embed.Description = $"New author of tag: {tagName.ToUpperInvariant()}, is \n\n{authorName}\n";
      embed.Timestamp = DateTime.Now;

      await ctx.CreateResponseAsync(embed);

    } catch (Exception ex) {
      await ctx.CreateResponseAsync(Utils.GenerateErrorAnswer(ctx.Guild.Name, "TagAddAuthor", ex));
    }
  }

  [SlashCommand("addcolor", "Add color scheme to tag")]
  public async Task TagColorPicking(InteractionContext ctx, [Option("tagname", "Tag to set the color")] string tagName, [Option("colorName", "just a comment")] TagColorValue? colorName = null) {
    Utils.LogUserCommand(ctx);
    try {
      DiscordEmbedBuilder embed = new();
      TagBase toEdit = SlashTags.FindTag(ctx.Guild.Id, tagName, false);
      if (toEdit == null) {
        embed.Title = "Tag does not exist!";
        embed.Color = DiscordColor.Red;
        embed.Description = $"The tag `{tagName}` does not exist!";
        embed.Timestamp = DateTime.Now;
        await ctx.CreateResponseAsync(embed, true);
        return;
      }
      int colorNumber = (int)colorName;
      if (colorNumber <= SlashTags.discordColors.Length) {
        toEdit.ColorOfTheme = colorNumber;
        Database.Add(toEdit); // adding information to base

        embed.Title = "Changes accepted!";
        embed.Color = DiscordColor.Green;
        embed.Description = $"New color for tag: {tagName.ToUpperInvariant()}, is \n{colorName} {SlashTags.discordColors[colorNumber]} - id {colorNumber}.";
        if (colorNumber == SlashTags.discordColors.Length)
          embed.Description = $"New color for tag: {tagName.ToUpperInvariant()}, is \n_random color_ (id {colorNumber}).";
        else
          embed.Timestamp = DateTime.Now;

        await ctx.CreateResponseAsync(embed);
      }
      else {
        embed.Title = "Color id does not exist!";
        embed.Color = DiscordColor.Red;
        embed.Description = $"Color id: {colorNumber} does not exist. Pick onve of the dropdown values!";
        embed.Timestamp = DateTime.Now;
        await ctx.CreateResponseAsync(embed, true);
        return;
      }
    } catch (Exception ex) {
      await ctx.CreateResponseAsync(Utils.GenerateErrorAnswer(ctx.Guild.Name, "TagColor", ex));
    }
  }
  [SlashCommand("addthumbnail", "Add a thumbnail image to the tag")]
  public async Task TagThumbnailPicking(InteractionContext ctx, [Option("tagname", "Tag to add the thumbnail")] string tagName, [Option("Thumbnail", "Link to image")] string thumbnailLink) {
    Utils.LogUserCommand(ctx);
    try {
      DiscordEmbedBuilder embed = new();
      TagBase toEdit = SlashTags.FindTag(ctx.Guild.Id, tagName, false);
      if (toEdit == null) {
        embed.Title = "Tag does not exist!";
        embed.Color = DiscordColor.Red;
        embed.Description = $"The tag `{tagName}` does not exist!";
        embed.Timestamp = DateTime.Now;
        await ctx.CreateResponseAsync(embed, true);
        return;
      }
      toEdit.thumbnailLink = thumbnailLink;
      Database.Add(toEdit); // adding information to base

      var builder = new DiscordEmbedBuilder {
        Title = "Changes accepted!",
        Color = DiscordColor.Green,
        Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail {
          Url = $"{thumbnailLink}"
        },
        Description = $"New Thumbnail link for tag: {tagName}, is \n{thumbnailLink}.",
        Timestamp = DateTime.Now
      };
      await ctx.CreateResponseAsync(builder);
    } catch (Exception ex) {
      await ctx.CreateResponseAsync(Utils.GenerateErrorAnswer(ctx.Guild.Name, "TagThumbnail", ex));
    }
  }

  [SlashCommand("removethumbnail", "Remove the thumbnail image from the tag")]
  public async Task TagThumbnailRemoving(InteractionContext ctx, [Option("tagname", "Tag with thumbnail")] string tagName) {
    Utils.LogUserCommand(ctx);
    try {
      DiscordEmbedBuilder embed = new();
      TagBase toEdit = SlashTags.FindTag(ctx.Guild.Id, tagName, false);
      if (toEdit == null) {
        embed.Title = "Tag does not exist!";
        embed.Color = DiscordColor.Red;
        embed.Description = $"The tag `{tagName}` does not exist!";
        embed.Timestamp = DateTime.Now;
        await ctx.CreateResponseAsync(embed, true);
        return;
      }
      if (toEdit.thumbnailLink == null || toEdit.thumbnailLink == "") {
        embed.Title = "Tag does not have any Thumbnail!";
        embed.Color = DiscordColor.Red;
        embed.Description = $"Tag does not have any Thumbnail!";
        embed.Timestamp = DateTime.Now;
        await ctx.CreateResponseAsync(embed, true);
        return;
      }
      toEdit.thumbnailLink = null;
      Database.Add(toEdit); // adding information to base

      var builder = new DiscordEmbedBuilder() {
        Title = "Thumbnail Removed!",
        Color = DiscordColor.Green,
        Description = $"Removed Thumbnail from: **'{tagName}'**!",
        Timestamp = DateTime.Now,
      };
      await ctx.CreateResponseAsync(builder);
    } catch (Exception ex) {
      await ctx.CreateResponseAsync(Utils.GenerateErrorAnswer(ctx.Guild.Name, "TagThumbnail", ex));
    }
  }

}
