using DSharpPlus.Entities;

namespace UPBot.UPBot_Code.DataClasses;

public class TrackChannel : Entity {
  [Key] public ulong Guild;
  public ulong ChannelId;
  public bool trackJoin;
  public bool trackLeave;
  public bool trackRoles;

  [NotPersistent] public DiscordChannel channel;



  public TrackChannel() { }

  public TrackChannel(ulong guild, ulong channel) {
    Guild = guild;
    ChannelId = channel;
  }

}