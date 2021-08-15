using System;
using System.IO;

public class CustomCommand
{
    public CustomCommand(string[] names, string content)
    {
        this.Names = names;
        this.FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, names[0].Trim().ToLowerInvariant() + ".txt");
        this.Content = content;
    }
    
    public string[] Names { get; private set; }
    public string FilePath { get; private set; }
    public string Content { get; private set; }
}