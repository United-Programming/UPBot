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


  private FieldInfo _key = null;
  public FieldInfo GetKey() {
    if (_key != null) return _key;
    foreach (FieldInfo field in GetType().GetFields()) {
      foreach (CustomAttributeData attr in field.CustomAttributes)
        if (attr.AttributeType.Equals(typeof(Key))) {
          _key = field;
          return _key;
        }
    }
    return null;
  }

}