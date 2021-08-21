using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;

/// <summary>
/// Holds information about a CustomCommand
/// and contains functions which execute or edit the command
/// </summary>
public class CustomCommand {

  public CustomCommand() {
  }

  public CustomCommand(string[] names, string content) {
    Name = names[0];
    Content = content;
    if (names.Length > 1) Alias0 = names[1];
    if (names.Length > 2) Alias1 = names[2];
    if (names.Length > 3) Alias2 = names[3];
    if (names.Length >= 4) Alias3 = names[4];
  }

  [Key]
  [Required]
  public string Name { get; set; }
  public string Alias0 { get; set; }
  public string Alias1 { get; set; }
  public string Alias2 { get; set; }
  public string Alias3 { get; set; }
  [Required]
  public string Content { get; set; }

  internal async Task ExecuteCommand(CommandContext ctx) {
    await ctx.Channel.SendMessageAsync(Content);
  }

  internal void EditCommand(string newContent) {
    Content = newContent;
  }

  internal void EditCommand(string[] newNames) {
    Name = newNames[0];
    if (newNames.Length > 1) Alias0 = newNames[1];
    if (newNames.Length > 2) Alias1 = newNames[2];
    if (newNames.Length > 3) Alias2 = newNames[3];
    if (newNames.Length >= 4) Alias3 = newNames[4];
  }

  internal bool Contains(string name) {
    return (Name.Equals(name) || Alias0.Equals(name) || Alias1.Equals(name) || Alias2.Equals(name) || Alias3.Equals(name));
  }

  internal string GetNames() {
    string res = "";
    if (string.IsNullOrWhiteSpace(Name)) return string.Empty;
    res += Name;
    if (string.IsNullOrWhiteSpace(Alias0)) return res;
    res += ", " + Alias0;
    if (string.IsNullOrWhiteSpace(Alias1)) return res;
    res += ", " + Alias1;
    if (string.IsNullOrWhiteSpace(Alias2)) return res;
    res += ", " + Alias2;
    if (string.IsNullOrWhiteSpace(Alias3)) return res;
    return res + ", " + Alias3;
  }
}