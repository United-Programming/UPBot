public class SetupParam : Entity {
  [Key]
  public ulong Guild;
  public string Param;
  public ulong IdVal;
  public string StrVal;

  public SetupParam() { }

  public SetupParam(ulong guild, string p, ulong val) {
    Guild = guild;
    Param = p;
    IdVal = val;
    StrVal = null;
  }

  public SetupParam(ulong guild, string p, string val) {
    Guild = guild;
    Param = p;
    IdVal = 0;
    StrVal = val;
  }

  public override string ToString() {
    return Guild + "\t" + Param + "\t" + IdVal + "\n" + StrVal + "\n";
  }
}
