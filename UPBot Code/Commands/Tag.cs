using System;
using System.Threading.Tasks;
using DSharpPlus.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;

/// <summary>
/// Command that allows helpers, admins, etc. Add more information in "Help Language" script.
/// Author: J0nathan550
/// In commands i comment the condition where bot are checking if user is helper. Please uncomment lines 19,61,127,162,203,264,323,359,394. To bring back this condition
/// New features: Picking color of text, Author of tag, Date of creation, List sorting. <- (buggy)
/// </summary>

[SlashCommandGroup("tags", "Define and use tags on the server")]
public class SlashTags : ApplicationCommandModule {


  [SlashCommand("tag", "Show the contents of a specific tag")]
  public async Task TagCommand(InteractionContext ctx, [Option("tagname", "Tag to be shown")] string tagname) {
    if (!Setup.Permitted(ctx.Guild, Config.ParamType.TagsUse, ctx)) { Utils.DefaultNotAllowed(ctx); return; }
    Utils.LogUserCommand(ctx);

    try {
      DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
      int randomnumber = rand.Next(0, randColor.Length);
      embed.Color = randColor[randomnumber];
      embed.Timestamp = DateTime.Now;

            if (tag != null)
            {
                embed.Title = tag.Topic;
                string descr = "";
                if (tag.Alias3 != null) descr += $"Aliases: _**{tag.Alias1}**_, _**{tag.Alias2}**_, _**{tag.Alias3}**_\n";
                else if (tag.Alias2 != null) descr += $"Aliases: _**{tag.Alias1}**_, _**{tag.Alias2}**_\n";
                else if (tag.Alias1 != null) descr += $"Alias: _**{tag.Alias1}**_\n";
                descr += $"Author: **{tag.Author}**\n";
                descr += tag.Information;
                await ctx.CreateResponseAsync(embed.WithDescription(descr));
            }
            else
            {
                await ctx.CreateResponseAsync(embed.WithDescription($"{tagname} tag does not exist."), true);
            }
        }
        catch (Exception ex)
        {
            await ctx.CreateResponseAsync(Utils.GenerateErrorAnswer(ctx.Guild.Name, "Tag", ex));
        }
    }

  [SlashCommand("tagadd", "Adds a new tag")]
  public async Task TagAddCommand(InteractionContext ctx, [Option("tagname", "Tag to be added")] string tagname) {
    if (!Setup.Permitted(ctx.Guild, Config.ParamType.TagsDefine, ctx)) { Utils.DefaultNotAllowed(ctx); return; }
    Utils.LogUserCommand(ctx);

        try
        {
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
            tagname = tagname.Trim();

      foreach (var topics in Setup.Tags[ctx.Guild.Id]) {
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
            var answer = await interact.WaitForMessageAsync((dm) =>
            {
                return (dm.Channel == ctx.Channel && dm.Author.Id == ctx.Member.Id);
            }, TimeSpan.FromMinutes(5));

            if (answer.Result == null)
            {
                embed.Title = "Time expired!";
                embed.Color = DiscordColor.Red;
                embed.Description = $"You took too much time to type the tag. :KO:";
                embed.Timestamp = DateTime.Now;
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed));
                return;
            }

      TagBase tagBase = new TagBase(ctx.Guild.Id, tagname, answer.Result.Content); // creating line inside of database
      Database.Add(tagBase); // adding information to base
      Setup.Tags[ctx.Guild.Id].Add(tagBase);

            embed.Title = "Tag added";
            embed.Color = DiscordColor.Green;
            embed.Description = ($"The topic: {tagname}, has been created");
            embed.Timestamp = DateTime.Now;
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed));
        }
        catch (Exception ex)
        {
            await ctx.CreateResponseAsync(Utils.GenerateErrorAnswer(ctx.Guild.Name, "TagAdd", ex));
        }
    }


  [SlashCommand("tagremove", "Removes an existing tag")]
  public async Task TagRemoveCommand(InteractionContext ctx, [Option("tagname", "Tag to be removed")] string tagname) {
    if (!Setup.Permitted(ctx.Guild, Config.ParamType.TagsDefine, ctx)) { Utils.DefaultNotAllowed(ctx); return; }
    Utils.LogUserCommand(ctx);

        try
        {
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();

      TagBase toRemove = FindTag(ctx.Guild.Id, tagname, false);
      if (toRemove == null) {
        embed.Title = "The Tag does not exist!";
        embed.Color = DiscordColor.Red;
        embed.Description = $"The tag `{tagname}` does not exist";
        embed.Timestamp = DateTime.Now;
        await ctx.CreateResponseAsync(embed, true);
        return;
      }
      Setup.Tags[ctx.Guild.Id].Remove(toRemove);
      Database.DeleteByKeys<TagBase>(ctx.Guild.Id, toRemove);

            embed.Title = "Topic deleted";
            embed.Color = DiscordColor.DarkRed;
            embed.Description = ($"Tag `{tagname}` has been deleted by {ctx.Member.DisplayName}");
            embed.Timestamp = DateTime.Now;
            await ctx.CreateResponseAsync(embed, true);
        }
        catch (Exception ex)
        {
            await ctx.CreateResponseAsync(Utils.GenerateErrorAnswer(ctx.Guild.Name, "TagRemove", ex));
        }
    }

  [SlashCommand("taglist", "Shows all tags")]
  public async Task TagListCommand(InteractionContext ctx) {
    if (!Setup.Permitted(ctx.Guild, Config.ParamType.TagsUse, ctx)) { Utils.DefaultNotAllowed(ctx); return; }
    Utils.LogUserCommand(ctx);

    try {
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
      await ctx.CreateResponseAsync(embed);
    } catch (Exception ex) {
      await ctx.CreateResponseAsync(Utils.GenerateErrorAnswer(ctx.Guild.Name, "TagList", ex));
    }
  }

  [SlashCommand("tagalias", "Define aliases for a tag")]
  public async Task TagAliasCommand(InteractionContext ctx, [Option("tagname", "Tag to be aliased")] string tagname, [Option("alias1", "First alias")] string alias1, [Option("alias2", "Second alias")] string alias2 = null, [Option("alias3", "Third alias")] string alias3 = null) {
    if (!Setup.Permitted(ctx.Guild, Config.ParamType.TagsDefine, ctx)) { Utils.DefaultNotAllowed(ctx); return; }
    Utils.LogUserCommand(ctx);

        try
        {
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();

      // Find it, can be an alias
      TagBase toAlias = FindTag(ctx.Guild.Id, tagname, false);
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
        }
        catch (Exception ex)
        {
            await ctx.CreateResponseAsync(Utils.GenerateErrorAnswer(ctx.Guild.Name, "TagAlias", ex));
        }
    }

  [SlashCommand("tagedit", "Edit an existing tag")]
  public async Task TagEditCommand(InteractionContext ctx, [Option("tagname", "Tag to be modified")] string tagname) {
    if (!Setup.Permitted(ctx.Guild, Config.ParamType.TagsDefine, ctx)) { Utils.DefaultNotAllowed(ctx); return; }
    Utils.LogUserCommand(ctx);

        try
        {
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();

      TagBase toEdit = FindTag(ctx.Guild.Id, tagname, false);
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
            var answer = await interact.WaitForMessageAsync((dm) =>
            {
                return (dm.Channel == ctx.Channel && dm.Author.Id == ctx.Member.Id);
            }, TimeSpan.FromMinutes(5));

            if (answer.Result == null || string.IsNullOrWhiteSpace(answer.Result.Content))
            {
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
        }
        catch (Exception ex)
        {
            await ctx.CreateResponseAsync(Utils.GenerateErrorAnswer(ctx.Guild.Name, "TagEdit", ex));
        }
    }


  [SlashCommand("tagrename", "Rename a tag")]
  public async Task TagRenameCommand(InteractionContext ctx, [Option("tagname", "Tag to be modified")] string oldname, [Option("newname", "The new name for the tag")] string newname) {
    if (!Setup.Permitted(ctx.Guild, Config.ParamType.TagsDefine, ctx)) { Utils.DefaultNotAllowed(ctx); return; }
    Utils.LogUserCommand(ctx);

    try {
      DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
      TagBase toEdit = FindTag(ctx.Guild.Id, oldname, false);
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


  TagBase FindTag(ulong gid, string name, bool getClosest) {
    foreach (TagBase tag in Setup.Tags[gid]) {
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
