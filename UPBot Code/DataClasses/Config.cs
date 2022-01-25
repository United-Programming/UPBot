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

  public enum ParamType {
    AdminRole,
    TrackingChannel
  }

  public override string ToString() {
    if (IdVal == 0) return (ParamType)Param + "\t " + Param + "\t " + StrVal + "\n ";
    else return (ParamType)Param + "\t " + Param + "\t " + IdVal;
  }

  internal bool IsParam(ParamType t) {
    return (ParamType)Param == t;
  }
}
