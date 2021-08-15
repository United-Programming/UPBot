using System.Threading.Tasks;
using DSharpPlus.CommandsNext;

/// <summary>
/// Holds information about a CustomCommand
/// and contains functions which execute or edit the command
/// </summary>
public class CustomCommand
{
    public CustomCommand(string[] names, string content)
    {
        this.Names = names;
        this.FilePath = UtilityFunctions.ConstructPath(names[0], ".txt");
        this.Content = content;
    }
    
    internal string[] Names { get; private set; }
    internal string FilePath { get; }
    internal string Content { get; private set; }

    internal async Task ExecuteCommand(CommandContext ctx)
    {
        await ctx.Channel.SendMessageAsync(Content);
    }

    internal void EditCommand(string newContent)
    {
        this.Content = newContent;
    }
    
    internal void EditCommand(string[] newNames)
    {
        this.Names = newNames;
    }
}