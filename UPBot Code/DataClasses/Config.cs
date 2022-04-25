
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
    Ping = 1,
    WhoIs = 2,
    Stats = 3,
    MassDel = 4,
    Games = 5,
    Refactor = 6,
    TimezoneS = 7,
    TimezoneG = 8,
    UnityDocs = 9,
    SpamProtection = 10,
    BannedWords = 11,
    Scores = 12,
    TagsUse = 13,
    TagsDefine = 14,
    Emoji4Role = 15,
    Emoji4RoleList = 16,
    Affiliation = 17,
    //VideosAbout
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
