
public class Config : Entity {
  [Key] public ulong Guild;
  [Key] public int Param;
  public ulong IdVal;

  [NotPersistent]

  public Config() { }

  public Config(ulong guild, ParamType param, ulong val) {
    Guild = guild;
    Param = (int)param;
    IdVal = val;
  }



  public enum ParamType {

    MassDel = 4,

    SpamProtection = 10,

    Scores = 12,
    TagsUse = 13,
    TagsDefine = 14,
    Emoji4Role = 15,
    Emoji4RoleList = 16,
    Affiliation = 17,
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
