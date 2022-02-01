using DSharpPlus.Entities;

public class TrackChannel : Entity {
  [Key] public long TrackChannelKey;
  [KeyGen] public ulong Guild;
  [KeyGen] public ulong ChannelId;
  public bool trackJoin;
  public bool trackLeave;
  public bool trackRoles;

  [NotPersistent] public DiscordChannel channel;



  public TrackChannel() { }

  public TrackChannel(ulong guild, ulong channel) {
    Guild = guild;
    ChannelId = channel;
    TrackChannelKey = GetKeyValue();
  }

}
