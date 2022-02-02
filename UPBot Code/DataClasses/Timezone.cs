using System;
using System.Text.RegularExpressions;

public class Timezone : Entity {
  [Key]
  public ulong User; // Timezones are not related to guilds
  public float UtcOffset;
  public string TimeZoneName;

  public Timezone() { }

  public Timezone(ulong usr, float tz, string name) {
    User = usr;
    UtcOffset = tz;
    TimeZoneName = name;
  }

  public static float GetOffset(string tz, out string name) {
    float res = 0;
    tz = tz.ToLowerInvariant().Replace(" ", "");
    Match m = utcRE.Match(tz);
    bool byName = false;
    if (!m.Success) {
      byName = true;
      string utz;
      switch (tz.ToUpperInvariant()) {
        case "A": utz = "utc+1"; break;
        case "ACDT": utz = "utc+10:30"; break;
        case "ACST": utz = "utc+9:30"; break;
        case "ACT": utz = "utc+9:30"; break;
        case "ACWST": utz = "utc+8:45"; break;
        case "ADT": utz = "utc+4"; break;
        case "AEDT": utz = "utc+11"; break;
        case "AEST": utz = "utc+10"; break;
        case "AET": utz = "utc+10:00"; break;
        case "AFT": utz = "utc+4:30"; break;
        case "AKDT": utz = "utc-8"; break;
        case "AKST": utz = "utc-9"; break;
        case "ALMT": utz = "utc+6"; break;
        case "AMST": utz = "utc+5"; break;
        case "AMT": utz = "utc+4"; break;
        case "ANAST": utz = "utc+12"; break;
        case "ANAT": utz = "utc+12"; break;
        case "AQTT": utz = "utc+5"; break;
        case "ART": utz = "utc-3"; break;
        case "AST": utz = "utc+3"; break;
        case "AT": utz = "utc-4:00"; break;
        case "AWDT": utz = "utc+9"; break;
        case "AWST": utz = "utc+8"; break;
        case "AZOST": utz = "utc+0"; break;
        case "AZOT": utz = "utc-1"; break;
        case "AZST": utz = "utc+5"; break;
        case "AZT": utz = "utc+4"; break;
        case "AoE": utz = "utc-12"; break;
        case "B": utz = "utc+2"; break;
        case "BNT": utz = "utc+8"; break;
        case "BOT": utz = "utc-4"; break;
        case "BRST": utz = "utc-2"; break;
        case "BRT": utz = "utc-3"; break;
        case "BST": utz = "utc+1"; break;
        case "BTT": utz = "utc+6"; break;
        case "C": utz = "utc+3"; break;
        case "CAST": utz = "utc+8"; break;
        case "CAT": utz = "utc+2"; break;
        case "CCT": utz = "utc+6:30"; break;
        case "CDT": utz = "utc-5"; break;
        case "CEST": utz = "utc+2"; break;
        case "CET": utz = "utc+1"; break;
        case "CHADT": utz = "utc+13:45"; break;
        case "CHAST": utz = "utc+12:45"; break;
        case "CHOST": utz = "utc+9"; break;
        case "CHOT": utz = "utc+8"; break;
        case "CHUT": utz = "utc+10"; break;
        case "CIDST": utz = "utc-4"; break;
        case "CIST": utz = "utc-5"; break;
        case "CKT": utz = "utc-10"; break;
        case "CLST": utz = "utc-3"; break;
        case "CLT": utz = "utc-4"; break;
        case "COT": utz = "utc-5"; break;
        case "CST": utz = "utc-6"; break;
        case "CT": utz = "utc-6:00"; break;
        case "CVT": utz = "utc-1"; break;
        case "CXT": utz = "utc+7"; break;
        case "ChST": utz = "utc+10"; break;
        case "D": utz = "utc+4"; break;
        case "DAVT": utz = "utc+7"; break;
        case "DDUT": utz = "utc+10"; break;
        case "E": utz = "utc+5"; break;
        case "EASST": utz = "utc-5"; break;
        case "EAST": utz = "utc-6"; break;
        case "EAT": utz = "utc+3"; break;
        case "ECT": utz = "utc-5"; break;
        case "EDT": utz = "utc-4"; break;
        case "EEST": utz = "utc+3"; break;
        case "EET": utz = "utc+2"; break;
        case "EGST": utz = "utc+0"; break;
        case "EGT": utz = "utc-1"; break;
        case "EST": utz = "utc-5"; break;
        case "ET": utz = "utc-5:00"; break;
        case "F": utz = "utc+6"; break;
        case "FET": utz = "utc+3"; break;
        case "FJST": utz = "utc+13"; break;
        case "FJT": utz = "utc+12"; break;
        case "FKST": utz = "utc-3"; break;
        case "FKT": utz = "utc-4"; break;
        case "FNT": utz = "utc-2"; break;
        case "G": utz = "utc+7"; break;
        case "GALT": utz = "utc-6"; break;
        case "GAMT": utz = "utc-9"; break;
        case "GET": utz = "utc+4"; break;
        case "GFT": utz = "utc-3"; break;
        case "GILT": utz = "utc+12"; break;
        case "GMT": utz = "utc+0"; break;
        case "GST": utz = "utc+4"; break;
        case "GYT": utz = "utc-4"; break;
        case "H": utz = "utc+8"; break;
        case "HDT": utz = "utc-9"; break;
        case "HKT": utz = "utc+8"; break;
        case "HOVST": utz = "utc+8"; break;
        case "HOVT": utz = "utc+7"; break;
        case "HST": utz = "utc-10"; break;
        case "I": utz = "utc+9"; break;
        case "ICT": utz = "utc+7"; break;
        case "IDT": utz = "utc+3"; break;
        case "IOT": utz = "utc+6"; break;
        case "IRDT": utz = "utc+4:30"; break;
        case "IRKST": utz = "utc+9"; break;
        case "IRKT": utz = "utc+8"; break;
        case "IRST": utz = "utc+3:30"; break;
        case "IST": utz = "utc+5:30"; break;
        case "JST": utz = "utc+9"; break;
        case "K": utz = "utc+10"; break;
        case "KGT": utz = "utc+6"; break;
        case "KOST": utz = "utc+11"; break;
        case "KRAST": utz = "utc+8"; break;
        case "KRAT": utz = "utc+7"; break;
        case "KST": utz = "utc+9"; break;
        case "KUYT": utz = "utc+4"; break;
        case "L": utz = "utc+11"; break;
        case "LHDT": utz = "utc+11"; break;
        case "LHST": utz = "utc+10:30"; break;
        case "LINT": utz = "utc+14"; break;
        case "M": utz = "utc+12"; break;
        case "MAGST": utz = "utc+12"; break;
        case "MAGT": utz = "utc+11"; break;
        case "MART": utz = "utc-9:30"; break;
        case "MAWT": utz = "utc+5"; break;
        case "MDT": utz = "utc-6"; break;
        case "MHT": utz = "utc+12"; break;
        case "MMT": utz = "utc+6:30"; break;
        case "MSD": utz = "utc+4"; break;
        case "MSK": utz = "utc+3"; break;
        case "MST": utz = "utc-7"; break;
        case "MT": utz = "utc-7:00"; break;
        case "MUT": utz = "utc+4"; break;
        case "MVT": utz = "utc+5"; break;
        case "MYT": utz = "utc+8"; break;
        case "N": utz = "utc-1"; break;
        case "NCT": utz = "utc+11"; break;
        case "NDT": utz = "utc-2:30"; break;
        case "NFDT": utz = "utc+12"; break;
        case "NFT": utz = "utc+11"; break;
        case "NOVST": utz = "utc+7"; break;
        case "NOVT": utz = "utc+7"; break;
        case "NPT": utz = "utc+5:45"; break;
        case "NRT": utz = "utc+12"; break;
        case "NST": utz = "utc-3:30"; break;
        case "NUT": utz = "utc-11"; break;
        case "NZDT": utz = "utc+13"; break;
        case "NZST": utz = "utc+12"; break;
        case "O": utz = "utc-2"; break;
        case "OMSST": utz = "utc+7"; break;
        case "OMST": utz = "utc+6"; break;
        case "ORAT": utz = "utc+5"; break;
        case "P": utz = "utc-3"; break;
        case "PDT": utz = "utc-7"; break;
        case "PET": utz = "utc-5"; break;
        case "PETST": utz = "utc+12"; break;
        case "PETT": utz = "utc+12"; break;
        case "PGT": utz = "utc+10"; break;
        case "PHOT": utz = "utc+13"; break;
        case "PHT": utz = "utc+8"; break;
        case "PKT": utz = "utc+5"; break;
        case "PMDT": utz = "utc-2"; break;
        case "PMST": utz = "utc-3"; break;
        case "PONT": utz = "utc+11"; break;
        case "PST": utz = "utc-8"; break;
        case "PT": utz = "utc-8:00 / -7:00"; break;
        case "PWT": utz = "utc+9"; break;
        case "PYST": utz = "utc-3"; break;
        case "PYT": utz = "utc-4"; break;
        case "Q": utz = "utc-4"; break;
        case "QYZT": utz = "utc+6"; break;
        case "R": utz = "utc-5"; break;
        case "RET": utz = "utc+4"; break;
        case "ROTT": utz = "utc-3"; break;
        case "S": utz = "utc-6"; break;
        case "SAKT": utz = "utc+11"; break;
        case "SAMT": utz = "utc+4"; break;
        case "SAST": utz = "utc+2"; break;
        case "SBT": utz = "utc+11"; break;
        case "SCT": utz = "utc+4"; break;
        case "SGT": utz = "utc+8"; break;
        case "SRET": utz = "utc+11"; break;
        case "SRT": utz = "utc-3"; break;
        case "SST": utz = "utc-11"; break;
        case "SYOT": utz = "utc+3"; break;
        case "T": utz = "utc-7"; break;
        case "TAHT": utz = "utc-10"; break;
        case "TFT": utz = "utc+5"; break;
        case "TJT": utz = "utc+5"; break;
        case "TKT": utz = "utc+13"; break;
        case "TLT": utz = "utc+9"; break;
        case "TMT": utz = "utc+5"; break;
        case "TOST": utz = "utc+14"; break;
        case "TOT": utz = "utc+13"; break;
        case "TRT": utz = "utc+3"; break;
        case "TVT": utz = "utc+12"; break;
        case "U": utz = "utc-8"; break;
        case "ULAST": utz = "utc+9"; break;
        case "ULAT": utz = "utc+8"; break;
        case "UTC": utz = "UTC"; break;
        case "UYST": utz = "utc-2"; break;
        case "UYT": utz = "utc-3"; break;
        case "UZT": utz = "utc+5"; break;
        case "V": utz = "utc-9"; break;
        case "VET": utz = "utc-4"; break;
        case "VLAST": utz = "utc+11"; break;
        case "VLAT": utz = "utc+10"; break;
        case "VOST": utz = "utc+6"; break;
        case "VUT": utz = "utc+11"; break;
        case "W": utz = "utc-10"; break;
        case "WAKT": utz = "utc+12"; break;
        case "WARST": utz = "utc-3"; break;
        case "WAST": utz = "utc+2"; break;
        case "WAT": utz = "utc+1"; break;
        case "WEST": utz = "utc+1"; break;
        case "WET": utz = "utc+0"; break;
        case "WFT": utz = "utc+12"; break;
        case "WGST": utz = "utc-2"; break;
        case "WGT": utz = "utc-3"; break;
        case "WIB": utz = "utc+7"; break;
        case "WIT": utz = "utc+9"; break;
        case "WITA": utz = "utc+8"; break;
        case "WST": utz = "utc+1"; break;
        case "WT": utz = "utc+0"; break;
        case "X": utz = "utc-11"; break;
        case "Y": utz = "utc-12"; break;
        case "YAKST": utz = "utc+10"; break;
        case "YAKT": utz = "utc+9"; break;
        case "YAPT": utz = "utc+10"; break;
        case "YEKST": utz = "utc+6"; break;
        case "YEKT": utz = "utc+5"; break;
        case "Z": utz = "utc+0"; break;
        default: name = "unknown"; return -999;
      }
      m = utcRE.Match(utz);
    }

    if (m.Success) {
      if (byName) name = tz.ToUpperInvariant() + " (UTC" + m.Groups[1].Value + ")";
      else name = m.Groups[1].Value;
      if (!m.Groups[1].Success) { // is 1 false then just utc
        name = "UTC";
        return 0; // No offset, it is UTC
      } else if (m.Groups[2].Success) { // if 2 is true
        name = "UTC" + m.Groups[2].Value;
        int.TryParse(m.Groups[2].Value, out int hours);
        res += hours;
        if (m.Groups[3].Success) { // if 3 is true then also minutes
          name += m.Groups[3].Value;
          int.TryParse(m.Groups[3].Value[1..], out int mins);
          res += MathF.Sign(hours) * mins / 60f;
        }
      }
    } else {
      name = "unknown";
      return -999;
    }
    return res;
  }

  static Regex utcRE = new Regex(@"^utc(([+\-]\s*[0-9]{0,2})(:[0-9][0-9])?)?$");
}
