using System;

public class Reputation : Entity {
  [Key]
  public long RepKey;
  public ulong Guild;
  public ulong User;
  public int Rep;
  public int Fun;
  public int Tnk;
  public int Ran;
  public DateTime LastUpdate;

  public Reputation() { }
  public Reputation(ulong gid, ulong usr) {
    Guild = gid;
    User = usr;
    RepKey = TheKey();
    Rep = 0;
    Fun = 0;
    Tnk = 0;
    Ran = 0;
    LastUpdate = DateTime.Now;
  }

  public long TheKey() {
    return (long)Guild ^ (long)User;
  }

  public static long GetTheKey(ulong gid, ulong usr) {
    return (long)gid ^ (long)usr;
  }

}


