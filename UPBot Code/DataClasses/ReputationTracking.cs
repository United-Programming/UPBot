using System;
using System.Collections.Generic;
using System.IO;

public class ReputationTracking {
  readonly DateTime trackingStarted;
  readonly Dictionary<ulong, Reputation> dic;
  readonly string path = null;

  public ReputationTracking(string path) {
    try {
      this.path = path;
      dic = new Dictionary<ulong, Reputation>();
      if (!File.Exists(path)) {
        trackingStarted = DateTime.Now;
        return;
      }
      byte[] data = new byte[20];
      using (FileStream f = new FileStream(path, FileMode.Open)) {
        // 32 bits for the date (ymd)
        if (f.Read(data, 0, 4) < 4) {
          Utils.Log("ERROR: wrong Reputation file: " + path);
          try {
            if (File.Exists(path)) File.Delete(path);
          } catch (Exception e) {
            Utils.Log("ERROR: cannot delete old Reputation file: " + path + "\nException: " + e.Message);
          }
          return;
        }
        trackingStarted = GetDateFromBytes(data, 0);
        while (f.Read(data, 0, 16) == 16) {
          ulong usrid = BitConverter.ToUInt64(data);
          ushort rep = BitConverter.ToUInt16(data, 8);
          ushort fun = BitConverter.ToUInt16(data, 10);
          DateTime start = GetDateFromBytes(data, 12);
          dic[usrid] = new Reputation { user = usrid, reputation = rep, fun = fun, startTracking = start };
        }
      }
      Utils.Log("ReputationTracking: Loaded " + dic.Count + " users");
    } catch (Exception e) {
      Utils.Log("ERROR: problems in loading the Reputation file: " + e.Message);
    }
  }

  public void Save() {
    try {
      lock (dic) {
        try {
          if (File.Exists(path)) File.Delete(path);
        } catch (Exception e) {
          Utils.Log("ERROR: cannot delete old Reputation file: " + path + "\nException: " + e.Message);
          return;
        }
        using (FileStream f = new FileStream(path, FileMode.CreateNew)) {
          byte[] data = new byte[16];
          SetDateToBytes(trackingStarted, data, 0);
          f.Write(data, 0, 4);
          foreach (Reputation r in dic.Values) {
            byte[] d = BitConverter.GetBytes(r.user);
            int pos = 0;
            for (int i = 0; i < d.Length; i++)
              data[pos++] = d[i];
            d = BitConverter.GetBytes(r.reputation);
            for (int i = 0; i < d.Length; i++)
              data[pos++] = d[i];
            d = BitConverter.GetBytes(r.fun);
            for (int i = 0; i < d.Length; i++)
              data[pos++] = d[i];
            SetDateToBytes(r.startTracking, data, pos);
            f.Write(data, 0, 16);
          }
          f.Flush();
        }
      }
      Utils.Log("ReputationTracking: Saved " + dic.Count + " users");
    } catch (Exception e) {
      Utils.Log("ERROR: problems in saving the Reputation file: " + e.Message);
    }
  }

  private void SetDateToBytes(DateTime d, byte[] data, int offset) {
    data[offset + 0] = (byte)((d.Year & 0xff00) >> 8);
    data[offset + 1] = (byte)(d.Year & 0xff);
    data[offset + 2] = (byte)(d.Month & 0xff);
    data[offset + 3] = (byte)(d.Day & 0xff);
  }

  private DateTime GetDateFromBytes(byte[] data, int offset) {
    try {
      return new DateTime((data[offset + 0] << 8) + data[offset + 1], data[offset + 2], data[offset + 3]);
    } catch (Exception) {
      return DateTime.Now;
    }
  }

  public bool AlterRep(ulong id, bool add) {
    if (add) {
      if (dic.ContainsKey(id)) dic[id].reputation++;
      else {
        dic.Add(id, new Reputation { user = id, reputation = 1, fun = 0, startTracking = DateTime.Now });
      }
      return true;
    }
    else {
      if (dic.ContainsKey(id) && dic[id].reputation > 0) {
        dic[id].reputation--;
        return true;
      }
    }
    return false;
  }
  public bool AlterFun(ulong id, bool add) {
    if (add) {
      if (dic.ContainsKey(id)) dic[id].fun++;
      else {
        dic.Add(id, new Reputation { user = id, reputation = 0, fun = 1, startTracking = DateTime.Now });
      }
      return true;
    }
    else {
      if (dic.ContainsKey(id) && dic[id].fun > 0) {
        dic[id].fun--;
        return true;
      }
    }
    return false;
  }

  internal string GetStartDate() {
    return trackingStarted.ToString("yyyy/MM/dd");
  }

  internal IEnumerable<Reputation> GetReputation() {
    return dic.Values;
  }
}