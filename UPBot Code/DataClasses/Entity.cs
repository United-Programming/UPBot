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
  public class KeyGen : Attribute {}
  public class NotNull : Attribute {}
  public class Index : Attribute {}
  public class Comment : Attribute {}
  public class Blob : Attribute {}
  public class NotPersistent : Attribute {}

  public long GetKeyValue() {
    Type t = GetType();

    foreach (FieldInfo field in t.GetFields()) {
      foreach (CustomAttributeData attr in field.CustomAttributes) {
        if (attr.AttributeType == typeof(Key)) {
          long? val = field.GetValue(this) as long?;
          if (val == null) return 0;
          return (long)val;
        }
      }
    }
    // Not found, calculate
    long res = 0; // Calculate from the fields
    foreach (FieldInfo field in t.GetFields()) {
      foreach (CustomAttributeData attr in field.CustomAttributes) {
        if (attr.AttributeType == typeof(KeyGen)) {
          object val = field.GetValue(this);
          if (val != null) res ^= val.GetHashCode();
        }
      }
    }
    return res;
  }

  public static long GetKeyValue(params object[] args) {
    long res = 0;
    foreach (object o in args) {
      if (o != null) res ^= o.GetHashCode();
    }
    return res;
  }
}