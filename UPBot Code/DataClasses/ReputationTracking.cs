using System;
using System.Collections.Generic;

public class ReputationTracking {
  readonly Dictionary<ulong, Reputation> dic;


  public ReputationTracking() {
    dic = new Dictionary<ulong, Reputation>();
    if (Utils.db.Reputations != null) {
      int num = 0;
      foreach (Reputation rep in Utils.db.Reputations) {
        num++;
        dic[rep.User] = rep;
      }
      Utils.Log("Found " + num + " reputation entries");
    }
  }

  public void AlterRep(ulong id, bool add) {
    if (add) {
      if (dic.ContainsKey(id)) {
        Reputation r = dic[id];
        r.Rep++;
        Utils.db.Reputations.Update(r);
        Utils.db.SaveChanges();
      }
      else {
        Reputation r = new Reputation { User = id, Rep = 1, Fun = 0, Tnk = 0, DateAdded = DateTime.Now };
        dic.Add(id, r);
        Utils.db.Reputations.Add(r);
        Utils.db.SaveChanges();
      }
    }
    else {
      if (dic.ContainsKey(id) && dic[id].Rep > 0) {
        Reputation r = dic[id];
        r.Rep--;
        Utils.db.Reputations.Update(r);
        Utils.db.SaveChanges();
      }
    }
  }
  public void AlterFun(ulong id, bool add) {
    if (add) {
      if (dic.ContainsKey(id)) {
        Reputation r = dic[id];
        r.Fun++;
        Utils.db.Reputations.Update(r);
        Utils.db.SaveChanges();
      }
      else {
        Reputation r = new Reputation { User = id, Rep = 0, Fun = 1, Tnk = 0, DateAdded = DateTime.Now };
        dic.Add(id, r);
        Utils.db.Reputations.Add(r);
        Utils.db.SaveChanges();
      }
    }
    else {
      if (dic.ContainsKey(id) && dic[id].Fun > 0) {
        Reputation r = dic[id];
        r.Fun--;
        Utils.db.Reputations.Update(r);
        Utils.db.SaveChanges();
      }
    }
  }

  internal void AlterThankYou(ulong id) {
    if (dic.ContainsKey(id)) {
      Reputation r = dic[id];
      r.Tnk++;
      Utils.db.Reputations.Update(r);
      Utils.db.SaveChanges();
    }
    else {
      Reputation r = new Reputation { User = id, Rep = 0, Fun = 0, Tnk = 1, DateAdded = DateTime.Now };
      dic.Add(id, r);
      Utils.db.Reputations.Add(r);
      Utils.db.SaveChanges();
    }
  }

  internal IEnumerable<Reputation> GetReputation() {
    return dic.Values;
  }

}