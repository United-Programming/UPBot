
public class OLDConfig : Entity {
  [Key] public ulong Guild;
  [Key] public int Param;
  public ulong IdVal;

  [NotPersistent]

  public OLDConfig() { }

  public OLDConfig(ulong guild, ParamType param, ulong val) {
    Guild = guild;
    Param = (int)param;
    IdVal = val;
  }



  public enum ParamType {

    SpamProtection = 10,
  }

  public enum ConfVal {
    NotAllowed=0,
    OnlyAdmins=1,
    Everybody=2
  }



  public override string ToString() {
    return (ParamType)Param + "\t " + Param + "\t " + IdVal;
  }

  internal bool IsParam(ParamType t) {
    return (ParamType)Param == t;
  }

  internal void SetVal(ConfVal v) {
    IdVal = (ulong)v;
  }
}
