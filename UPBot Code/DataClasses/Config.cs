using System;

public class Config : Entity {
  [Key]
  public long ConfigKey; // Guild+ParamType+Value
  public ulong Guild;
  public int Param;
  public ulong IdVal;
  public string StrVal;

  [NotPersistent]

  public Config() { }

  public Config(ulong guild, ParamType param, ulong val) {
    Guild = guild;
    Param = (int)param;
    IdVal = val;
    StrVal = null;
    ConfigKey = TheKey();
  }

  public Config(ulong guild, ParamType param, string val) {
    Guild = guild;
    Param = (int)param;
    IdVal = 0;
    StrVal = val;
    ConfigKey = TheKey();
  }

  public long TheKey() {
    if (IdVal == 0) return (long)Guild ^ Param ^ StrVal.GetHashCode();
    else return (long)Guild ^ Param ^ (long)IdVal;
  }

  public static long TheKey(ulong g, ParamType p, ulong v) {
    return (long)g ^ (int)p ^ (long)v;
  }
  public static long TheKey(ulong g, ParamType p, string v) {
    return (long)g ^ (int)p ^ v.GetHashCode();
  }

  public enum ParamType {
    AdminRole,
    TrackingChannel,
    Ping,
    WhoIs,
    MassDel,
    Games,
    Refactor,
    TimezoneS,
    TimezoneG,
    UnityDocs,
    CppDocs,
    CSharpDocs,
    PhytonDocs,
    JavaDocs,
    JScriptDocs,
    SpamProtection,
    //VideosAbout
  }

  public enum ConfVal {
    NotAllowed=0,
    OnlyAdmins=1,
    Everybody=2
  }



  public override string ToString() {
    if (IdVal == 0) return (ParamType)Param + "\t " + Param + "\t " + StrVal + "\n ";
    else return (ParamType)Param + "\t " + Param + "\t " + IdVal;
  }

  internal bool IsParam(ParamType t) {
    return (ParamType)Param == t;
  }

  internal void SetVal(ConfVal v) {
    IdVal = (ulong)v;
  }
}
