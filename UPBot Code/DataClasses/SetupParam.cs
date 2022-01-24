public class SetupParam : Entity {
  [Key]
  public string ParamTypeVal;
  public string Param;
  public ulong IdVal;
  public string StrVal;

  public SetupParam() { }

  public SetupParam(string p, ulong val) {
    ParamTypeVal = p + val;
    Param = p;
    IdVal = val;
    StrVal = null;
  }

  public SetupParam(string p, string val) {
    ParamTypeVal = p + val;
    Param = p;
    IdVal = 0;
    StrVal = val;
  }

  public override string ToString() {
    if (IdVal == 0) return ParamTypeVal + "\t " + Param + "\t " + StrVal + "\n ";
    else return ParamTypeVal + "\t " + Param + "\t " + IdVal;
  }
}
