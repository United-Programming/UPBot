public class ReputationEmoji : Entity {
  [Key]
  public long EmKey;
  public ulong Guild;
  public ulong Lid;
  public string Sid;

  public ReputationEmoji(ulong gid, ulong id) {
    Guild = gid;
    Lid = id;
    EmKey = GetTheKey(gid, id);
    Sid = null;
  }

  public ReputationEmoji(ulong gid, string id) {
    Guild = gid;
    Sid = id;
    EmKey = GetTheKey(gid, id);
    Lid = 0;
  }

  public static long GetTheKey(ulong gid, ulong id) {
    return (long)gid ^ (long)id;
  }

  public static long GetTheKey(ulong gid, string id) {
    return (long)gid ^ id.GetHashCode();
  }

}


