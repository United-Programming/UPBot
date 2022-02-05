using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using System.IO;

/// <summary>
/// Command that allows helpers, admins, etc. Add more information in "Help Language" script.
/// Author: J0nathan550
/// </summary>

public class Tag : BaseCommandModule
{
    public DiscordInteraction interaction; // for waiting user
    List<TagBase> HelpInformation; // Creating list of "Scripts"
    private char p = '.'; // prefix

    [RequireRoles(RoleCheckMode.Any, "Helper", "Mod", "Owner")] // what if user wants to get some information? What to do?
    [Command("tag")]
    public async Task TagMainCommand(CommandContext ctx)
    {
        DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
        embed.Title = "Error in usage!";
        embed.Color = DiscordColor.Red;
        embed.Description = $"`USE:{p}tag add <topic> - to start registration of topic\n USE: {p}tag remove <topic> - to remove topic and included information`\n`USE: {p}tag list  - to see all list of topics`\n`USE: {p}tag <topic> - to see information about specific topic.`";
        embed.Timestamp = DateTime.Now;
        var builder = new DiscordMessageBuilder();
        await ctx.RespondAsync(builder.AddEmbed(embed.Build()));
    }
    [Command("tag")]
    public async Task TagMainCommand(CommandContext ctx, [Description("Topic to be shown.")]string topic)
    {
        Utils.LogUserCommand(ctx);
        topic = topic.Trim().ToLowerInvariant();
        if (topic == "list")
        {
            await ShowAllInformation(ctx);
            return;
        }
        await ShowTopic(ctx, topic);

    }

    [RequireRoles(RoleCheckMode.Any, "Helper", "Mod", "Owner")] // what if user wants to get some information? What to do?
    [Command("tag")]
    public async Task TagMainCommand(CommandContext ctx, [Description("Add or remove Topic command usage.")] string command, [Description("Topic to be shown.")] string topic)
    {
        DiscordEmbedBuilder embed = new DiscordEmbedBuilder();

        Utils.LogUserCommand(ctx);
        topic = topic.Trim().ToLowerInvariant();
        command = command.Trim().ToLowerInvariant();

        if (command == "add")
        {
            await AddingCommand(ctx, topic);
            return;
        }
        else if (command == "remove")
        {
            await RemovingCommand(ctx, topic);
            return;
        }
        else if(command == "edit")
        {
            await EditCommand(ctx, topic);
            return;
        }

        embed.Title = "Error in usage!";
        embed.Color = DiscordColor.Red;
        embed.Description = ($"Possible commands are: `ADD` or `REMOVE`");
        embed.Timestamp = DateTime.Now;
        var builder = new DiscordMessageBuilder();
        await ctx.RespondAsync(builder.AddEmbed(embed.Build()));
    }

    public async Task AddingCommand(CommandContext ctx, string topic)
    {
        DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
        var builder = new DiscordMessageBuilder();

        if (HelpInformation == null)
        {
            HelpInformation = Database.GetAll<TagBase>();
        }
        foreach (var topics in HelpInformation)
        {
            if (topic == topics.Topic)
            {
                embed.Title = "The Topic is already existing!";
                embed.Color = DiscordColor.Red;
                embed.Description = ($"You are trying to add topic that are already existing!\nIf you want to edit the topic that you just type use: `{p}tag edit <topic> - to edit`");
                embed.Timestamp = DateTime.Now;
                await ctx.RespondAsync(builder.AddEmbed(embed.Build()));
                return;
            }
        }

        embed.Title = "Adding topic with information";
        embed.Color = DiscordColor.Green;
        embed.Description = ($"Type what you want type for {topic}.");
        embed.Timestamp = DateTime.Now;
        await ctx.RespondAsync(builder.AddEmbed(embed.Build()));

        var interact = ctx.Client.GetInteractivity();
        var answer = await interact.WaitForMessageAsync((dm) => {
            return (dm.Channel == ctx.Channel && dm.Author.Id == ctx.Member.Id);
        }, TimeSpan.FromMinutes(5));

        if (answer.Result == null)
        {
            embed.Title = "Time Passed!";
            embed.Color = DiscordColor.Red;
            embed.Description = ($"You have being answering for too long. :KO:");
            embed.Timestamp = DateTime.Now;
            await ctx.RespondAsync(builder.AddEmbed(embed.Build()));
            return;
        }

        TagBase tagBase = new TagBase(ctx.Guild.Id, topic, answer.Result.Content); // creating line inside of database
        Database.Add(tagBase); // adding information to base
        HelpInformation = Database.GetAll<TagBase>(); // updating the list

        embed.Title = "Topic adding successful!";
        embed.Color = DiscordColor.Green;
        embed.Description = ($"The topic: {topic}, has been created!");
        embed.Timestamp = DateTime.Now;
        await ctx.RespondAsync(builder.AddEmbed(embed.Build()));

    }

    public async Task EditCommand(CommandContext ctx, string topic)
    {
        DiscordEmbedBuilder embed = new DiscordEmbedBuilder();

        embed.Title = $"Editting {topic}";
        embed.Color = DiscordColor.Purple;
        embed.Description = ($"You are now editting the {topic.ToUpperInvariant()}, be aware that if you insert empty, or unfinished message changes will be applied.\n" +
            $"Better to copy previous text, and edit inside of message.");
        embed.Timestamp = DateTime.Now;
        var builder = new DiscordMessageBuilder();
        await ctx.RespondAsync(builder.AddEmbed(embed.Build()));

        var interact = ctx.Client.GetInteractivity();
        var answer = await interact.WaitForMessageAsync((dm) => {
            return (dm.Channel == ctx.Channel && dm.Author.Id == ctx.Member.Id);
        }, TimeSpan.FromMinutes(20));

        if (answer.Result == null)
        {
            embed.Title = "Time Passed!";
            embed.Color = DiscordColor.Red;
            embed.Description = ($"You have being answering for too long. :KO:");
            embed.Timestamp = DateTime.Now;
            await ctx.RespondAsync(builder.AddEmbed(embed.Build()));
            return;
        }

        TagBase tagBase = new TagBase(ctx.Guild.Id, topic, answer.Result.Content); // creating line inside of database
        Database.Add(tagBase); // adding information to base
        HelpInformation = Database.GetAll<TagBase>(); // updating the list

        embed.Title = "Changes Accepted!";
        embed.Color = DiscordColor.Green;
        embed.Description = ($"New information for {topic.ToUpperInvariant()}, is:\n\n" +
            $"{answer.Result.Content}\n\n");
        embed.Timestamp = DateTime.Now;
        await ctx.RespondAsync(builder.AddEmbed(embed.Build()));

    }

    public async Task RemovingCommand(CommandContext ctx, string topic)
    {
        DiscordEmbedBuilder embed = new DiscordEmbedBuilder();

        Database.DeleteByKeys<TagBase>(ctx.Guild.Id, topic); // deleting line inside of database
        HelpInformation = Database.GetAll<TagBase>(); // updating information

        embed.Title = "Deleted topic";
        embed.Color = DiscordColor.DarkRed;
        embed.Description = ($"Topic:{topic}, has been deleted by {ctx.Member.Mention}");
        embed.Timestamp = DateTime.Now;
        var builder = new DiscordMessageBuilder();
        await ctx.RespondAsync(builder.AddEmbed(embed.Build()));


    }

    public async Task ShowAllInformation(CommandContext ctx) {
        DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
        // list command need the data base.
        if (HelpInformation == null)
        {
            HelpInformation = Database.GetAll<TagBase>();
        }
        string result = "";
        if (HelpInformation.Count == 0)
        {
            result = "No help information inside of list.";
        }
        else
        {
            foreach (TagBase tag in HelpInformation)
            {
                result += $"**{tag.Topic}**  ";
            }
        }
        embed.Title = "List of information";
        embed.Color = DiscordColor.Blurple;
        embed.Description = ($"{result}");
        embed.Timestamp = DateTime.Now;
        var builder = new DiscordMessageBuilder();
        await ctx.RespondAsync(builder.AddEmbed(embed.Build()));
        
    }

    public async Task ShowTopic(CommandContext ctx, string topic)
    {
        DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
        // list command need the data base.
        if (HelpInformation == null)
        {
            HelpInformation = Database.GetAll<TagBase>();
        }
        string result = $"Topic: {topic}, does not exist.";

        foreach (TagBase tag in HelpInformation)
        {
            if (tag.Topic == topic)
            {
                result = tag.Information;
                break;
            }
        }
        embed.Title = $"{topic}";
        Random rand = new Random();

        DiscordColor[] randColor = { DiscordColor.Aquamarine, DiscordColor.Azure, DiscordColor.Black, DiscordColor.Blue, DiscordColor.Blurple, 
            DiscordColor.Brown, DiscordColor.Chartreuse, DiscordColor.CornflowerBlue, DiscordColor.Cyan, DiscordColor.DarkBlue, 
            DiscordColor.DarkButNotBlack, DiscordColor.DarkGray, DiscordColor.DarkGreen, DiscordColor.DarkRed, DiscordColor.Gold, 
            DiscordColor.Goldenrod, DiscordColor.Gray, DiscordColor.Grayple, DiscordColor.Green, DiscordColor.HotPink, DiscordColor.IndianRed, 
            DiscordColor.LightGray, DiscordColor.Lilac, DiscordColor.Magenta, DiscordColor.MidnightBlue, DiscordColor.NotQuiteBlack, DiscordColor.Orange, 
            DiscordColor.PhthaloBlue, DiscordColor.PhthaloGreen, DiscordColor.Purple, DiscordColor.Red, DiscordColor.Rose, DiscordColor.SapGreen, 
            DiscordColor.Sienna, DiscordColor.SpringGreen, DiscordColor.Teal, DiscordColor.Turquoise, DiscordColor.VeryDarkGray, DiscordColor.Violet, 
            DiscordColor.Wheat, DiscordColor.Yellow, DiscordColor.White};

        int randomnumber = rand.Next(0, randColor.Length);

        embed.Color = randColor[randomnumber];
        embed.Description = ($"{result}");
        embed.Timestamp = DateTime.Now;
        var builder = new DiscordMessageBuilder();
        await ctx.RespondAsync(builder.AddEmbed(embed.Build()));

    }

}
