using System;
using System.Collections.Generic;

public class ReputationTracking {
  readonly Dictionary<ulong, Reputation> dic;


  public ReputationTracking() {
    dic = new Dictionary<ulong, Reputation>();
    List<Reputation> all = Database.GetAll<Reputation>();
    foreach (Reputation rep in all) {
      dic[rep.User] = rep;
    }
      Utils.Log("Found " + all.Count + " reputation entries");
  }

  public void AlterRep(ulong id, bool add) {
    if (add) {
      if (dic.ContainsKey(id)) {
        Reputation r = dic[id];
        r.Rep++;
        Database.Update(r);
      }
      else {
        Reputation r = new Reputation { User = id, Rep = 1, Fun = 0, Tnk = 0, DateAdded = DateTime.Now };
        dic.Add(id, r);
        Database.Add(r);
      }
    }
    else {
      if (dic.ContainsKey(id) && dic[id].Rep > 0) {
        Reputation r = dic[id];
        r.Rep--;
        Database.Update(r);
      }
    }
  }
  public void AlterFun(ulong id, bool add) {
    if (add) {
      if (dic.ContainsKey(id)) {
        Reputation r = dic[id];
        r.Fun++;
        Database.Update(r);
      }
      else {
        Reputation r = new Reputation { User = id, Rep = 0, Fun = 1, Tnk = 0, DateAdded = DateTime.Now };
        dic.Add(id, r);
        Database.Add(r);
      }
    }
    else {
      if (dic.ContainsKey(id) && dic[id].Fun > 0) {
        Reputation r = dic[id];
        r.Fun--;
        Database.Update(r);
      }
    }
  }

  internal void AlterThankYou(ulong id) {
    if (dic.ContainsKey(id)) {
      Reputation r = dic[id];
      r.Tnk++;
      Database.Update(r);
    }
    else {
      Reputation r = new Reputation { User = id, Rep = 0, Fun = 0, Tnk = 1, DateAdded = DateTime.Now };
      dic.Add(id, r);
      Database.Add(r);
    }
  }

  internal IEnumerable<Reputation> GetReputation() {
    return dic.Values;
  }

}