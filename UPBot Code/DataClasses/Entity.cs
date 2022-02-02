using System;
using System.Reflection;

public class Entity {

  public void Debug() {
    Type t = GetType();

    string msg = t.ToString() + "\n";
    foreach (var a in t.GetFields()) {
      msg += "- " + a.Name + " " + a.FieldType.Name;
      foreach (CustomAttributeData attr in a.CustomAttributes)
        msg += " " + attr.ToString();
      msg += "\n";
    }

    Console.WriteLine(msg);
  }


  public class Key : Attribute {}
  public class NotNull : Attribute {}
  public class Index : Attribute {}
  public class Comment : Attribute {}
  public class Blob : Attribute {}
  public class NotPersistent : Attribute {}

}