using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Reflection;

namespace UPBot.UPBot_Code;

public class Database {
  static SQLiteConnection connection = null;
  const string DbName = "BotDb";
  static Dictionary<Type, EntityDef> entities;


  public static void InitDb(List<Type> tables) {
    try {
      // Do we have the db?
      if (File.Exists("Database/" + DbName + ".db"))
        connection = new SQLiteConnection("Data Source=Database/" + DbName + ".db; Version=3; Journal Mode=Off; UTF8Encoding=True;"); // Open the database
      else {
        if (!Directory.Exists("Database")) Directory.CreateDirectory("Database");
        connection = new SQLiteConnection("Data Source=Database/" + DbName + ".db; Version=3; Journal Mode=Off; New=True; UTF8Encoding=True;"); // Create a new database
      }

      // Open the connection
      connection.Open();
      Console.WriteLine("DB connection open");

      foreach (Type t in tables) {
        if (!typeof(Entity).IsAssignableFrom(t))
          throw new Exception("The class " + t + " does not derive from Entity and cannot be used as database table!");
      }

      SQLiteCommand cmd = new SQLiteCommand("SELECT name FROM sqlite_schema WHERE type = 'table'", connection);
      SQLiteDataReader reader = cmd.ExecuteReader();
      List<string> dbTables = new List<string>();
      while (reader.Read()) {
        dbTables.Add(reader.GetString(0));
      }

      foreach (var table in dbTables) {
        bool delete = true;
        foreach (Type t in tables) {
          if (t.ToString() == table) {
            delete = false;
            break;
          }
        }
        if (delete) {
          Console.WriteLine("Removing old Table " + table + ".");
          try {
            SQLiteCommand command = new SQLiteCommand(connection) {
              CommandText = "DROP TABLE IF EXISTS " + table
            };
            command.ExecuteNonQuery();
          } catch (Exception ex) {
            Console.WriteLine(ex.Message);
          }
        }
      }

      entities = new Dictionary<Type, EntityDef>();

      // Ensure creation
      foreach (Type t in tables) {
        AddTable(t);
      }

    } catch (Exception ex) {
      throw new Exception("Cannot open the database: " + ex.Message);
    }
  }

  public static void AddTable(Type t) {
    // Check if we have the table in the db
    string tableName = t.ToString();
    SQLiteCommand command = new SQLiteCommand(connection) {
      CommandText = "SELECT count(*) FROM " + tableName + ";"
    };
    bool exists = true; // Check if table exists
    try {
      SQLiteDataReader reader = command.ExecuteReader();
      reader.Close();
    } catch (Exception) {
      exists = false;
    }
    // Check if we have all columns or we have to upgrade
    List<FieldInfo> missing = new List<FieldInfo>();
    foreach (FieldInfo field in t.GetFields()) {
      bool skip = false;
      foreach (CustomAttributeData attr in field.CustomAttributes)
        if (attr.AttributeType == typeof(Entity.NotPersistent)) {
          skip = true;
          break;
        }
      if (skip) continue;

      command.CommandText = "SELECT count(" + field.Name + ") FROM " + tableName + ";";
      try {
        SQLiteDataReader reader = command.ExecuteReader();
        reader.Close();
      } catch (Exception) {
        missing.Add(field);
      }
    }

    if (exists) {
      if (missing.Count != 0)
        Console.WriteLine("Table " + tableName + " exists but some columns are missing.");
      else
        Console.WriteLine("Table " + tableName + " exists.");
    }
    else
      Console.WriteLine("Table " + tableName + " does NOT exist!");

    string theKey = null;
    if (!exists) {
      string sql = "create table " + tableName + " (";
      string index = null;
      foreach (FieldInfo field in t.GetFields()) {
        bool comment = false;
        bool blob = false;
        bool notnull = false;
        bool ignore = false;
        foreach (CustomAttributeData attr in field.CustomAttributes) {
          if (attr.AttributeType == typeof(Entity.Blob)) blob = true;
          if (attr.AttributeType == typeof(Entity.Comment)) comment = true;
          if (attr.AttributeType == typeof(Entity.Key)) {
            notnull = true;
            if (theKey == null) theKey = field.Name;
            else theKey += ", " + field.Name;
          }
          if (attr.AttributeType == typeof(Entity.NotNull)) notnull = true;
          if (attr.AttributeType == typeof(Entity.Index)) {
            if (index == null) index = "CREATE INDEX idx_" + tableName + " ON " + tableName + "(" + field.Name;
            else index += ", " + field.Name;
          }
          if (attr.AttributeType == typeof(Entity.NotPersistent)) ignore = true;
        }
        if (ignore) continue;

        if (blob) sql += field.Name + " BLOB";
        else switch (field.FieldType.Name.ToLowerInvariant()) {
            case "int8": sql += field.Name + " SMALLINT"; break;
            case "uint8": sql += field.Name + " SMALLINT"; break;
            case "byte": sql += field.Name + " SMALLINT"; break;
            case "int32": sql += field.Name + " INT"; break;
            case "int64": sql += field.Name + " BIGINT"; break;
            case "uint64": sql += field.Name + " UNSIGNED BIG INT"; break;
            case "string": {
                if (comment) sql += field.Name + " TEXT";
                else sql += field.Name + " VARCHAR(256)";
                break;
              }
            case "bool": sql += field.Name + " TINYINT"; break;
            case "boolean": sql += field.Name + " TINYINT"; break;
            case "datetime": sql += field.Name + " NUMERIC"; break;
            case "single": sql += field.Name + " REAL"; break;
            case "double": sql += field.Name + " REAL"; break;
            case "byte[]": sql += field.Name + " BLOB"; break;
            default:
              throw new Exception("Unmanaged type: " + field.FieldType.Name + " for class " + t.Name);
          }
        if (notnull) sql += " NOT NULL";
        sql += ", ";
      }
      if (theKey == null) throw new Exception("Missing [Key] for class " + t);
      sql += " PRIMARY KEY (" + theKey + "));";
      command.CommandText = sql;
      command.ExecuteNonQuery();

      if (index != null) {
        command.CommandText = index;
        command.ExecuteNonQuery();
      }
    }
    else if (missing.Count != 0) { // Existing but with missing columns
      foreach (FieldInfo field in missing) {
        string sql = "ALTER TABLE " + tableName + " ADD COLUMN ";
        bool comment = false;
        bool blob = false;
        bool notnull = false;
        bool ignore = false;
        foreach (CustomAttributeData attr in field.CustomAttributes) {
          if (attr.AttributeType == typeof(Entity.Blob)) blob = true;
          if (attr.AttributeType == typeof(Entity.Comment)) comment = true;
          if (attr.AttributeType == typeof(Entity.Key)) {
            notnull = true;
            if (theKey == null) theKey = field.Name;
            else theKey += ", " + field.Name;
          }
          if (attr.AttributeType == typeof(Entity.NotNull)) notnull = true;
          if (attr.AttributeType == typeof(Entity.NotPersistent)) ignore = true;
        }
        if (ignore) continue;

        if (blob) sql += field.Name + " BLOB";
        else switch (field.FieldType.Name.ToLowerInvariant()) {
            case "int8": sql += field.Name + " SMALLINT"; break;
            case "uint8": sql += field.Name + " SMALLINT"; break;
            case "byte": sql += field.Name + " SMALLINT"; break;
            case "int32": sql += field.Name + " INT"; break;
            case "int64": sql += field.Name + " BIGINT"; break;
            case "uint64": sql += field.Name + " UNSIGNED BIG INT"; break;
            case "string": {
                if (comment) sql += field.Name + " TEXT";
                else sql += field.Name + " VARCHAR(256)";
                break;
              }
            case "bool": sql += field.Name + " TINYINT"; break;
            case "boolean": sql += field.Name + " TINYINT"; break;
            case "datetime": sql += field.Name + " NUMERIC"; break;
            case "single": sql += field.Name + " REAL"; break;
            case "double": sql += field.Name + " REAL"; break;
            case "byte[]": sql += field.Name + " BLOB"; break;
            default:
              throw new Exception("Unmanaged type: " + field.FieldType.Name + " for class " + t.Name);
          }
        if (notnull) sql += " NOT NULL";
        sql += ";";
        command.CommandText = sql;
        command.ExecuteNonQuery();

        // We need to fill the default value

        switch (field.FieldType.Name.ToLowerInvariant()) {
          case "int8":
          case "uint8":
          case "byte":
          case "int32":
          case "int64":
          case "uint64":
          case "bool":
          case "boolean":
          case "datetime":
          case "single":
          case "double":
            sql = "UPDATE " + t + " SET " + field.Name + "= 0;"; break;

          case "string":
          case "byte[]":
            sql = "UPDATE " + t + " SET " + field.Name + "= NULL;"; break;

          default:
            throw new Exception("Unmanaged type: " + field.FieldType.Name + " for class " + t.Name);
        }
        if (sql != null) {
          command.CommandText = sql;
          command.ExecuteNonQuery();
        }

      }
    }

    // Construct the entity
    EntityDef ed = new EntityDef { type = t };
    List<FieldInfo> keygens = new List<FieldInfo>();
    foreach (FieldInfo field in t.GetFields()) {
      bool blob = false;
      bool ignore = false;
      foreach (CustomAttributeData attr in field.CustomAttributes) {
        if (attr.AttributeType == typeof(Entity.Key)) { keygens.Add(field); }
        if (attr.AttributeType == typeof(Entity.Blob)) { blob = true; }
        if (attr.AttributeType == typeof(Entity.NotPersistent)) { ignore = true; }
      }
      if (ignore) {
        ed.fields[field.Name] = new ColDef { name = field.Name, ft = FieldType.IGNORE, index = -1 };
        continue;
      }
      if (blob) ed.fields[field.Name] = new ColDef { name = field.Name, ft = FieldType.ByteArray, index = -1 };
      else switch (field.FieldType.Name.ToLowerInvariant()) {
          case "int8": ed.fields[field.Name] = new ColDef { name = field.Name, ft = FieldType.Byte, index = -1 }; break;
          case "uint8": ed.fields[field.Name] = new ColDef { name = field.Name, ft = FieldType.Byte, index = -1 }; break;
          case "byte": ed.fields[field.Name] = new ColDef { name = field.Name, ft = FieldType.Byte, index = -1 }; break;
          case "int32": ed.fields[field.Name] = new ColDef { name = field.Name, ft = FieldType.Int, index = -1 }; break;
          case "int64": ed.fields[field.Name] = new ColDef { name = field.Name, ft = FieldType.Long, index = -1 }; break;
          case "uint64": ed.fields[field.Name] = new ColDef { name = field.Name, ft = FieldType.ULong, index = -1 }; break;
          case "string": {
              if (blob) ed.fields[field.Name] = new ColDef { name = field.Name, ft = FieldType.Blob, index = -1 };
              else ed.fields[field.Name] = new ColDef { name = field.Name, ft = FieldType.String, index = -1 };
              break;
            }
          case "bool": ed.fields[field.Name] = new ColDef { name = field.Name, ft = FieldType.Bool, index = -1 }; break;
          case "boolean": ed.fields[field.Name] = new ColDef { name = field.Name, ft = FieldType.Bool, index = -1 }; break;
          case "datetime": ed.fields[field.Name] = new ColDef { name = field.Name, ft = FieldType.Date, index = -1 }; break;
          case "single": ed.fields[field.Name] = new ColDef { name = field.Name, ft = FieldType.Float, index = -1 }; break;
          case "double": ed.fields[field.Name] = new ColDef { name = field.Name, ft = FieldType.Double, index = -1 }; break;
          case "byte[]": ed.fields[field.Name] = new ColDef { name = field.Name, ft = FieldType.ByteArray, index = -1 }; break;
          default:
            throw new Exception("Unmanaged type: " + field.FieldType.Name + " for class " + t.Name);
        }
    }
    if (keygens.Count == 0) throw new Exception("Missing key for class " + t);
    ed.keys = keygens.ToArray();

    // Build the query strings
    theKey = "";
    int keynum = 1;
    foreach (var key in keygens) {
      if (theKey.Length > 0) theKey += " and ";
      theKey += key.Name + "=@param" + keynum;
      keynum++;
    }

    ed.count = "SELECT Count(*) FROM " + t + " WHERE " + theKey;
    ed.select = "SELECT * FROM " + t;
    ed.delete = "DELETE FROM " + t.Name + " WHERE " + theKey;
    ed.selectOne = "SELECT * FROM " + t + " WHERE " + theKey;

    // Insert, Update
    string insert = "INSERT INTO " + t + " (";
    string insertpost = ") VALUES (";
    string update = "UPDATE " + t + " SET ";
    bool donefirst = false;
    foreach (FieldInfo field in t.GetFields()) {
      bool ignore = false;
      foreach (CustomAttributeData attr in field.CustomAttributes) {
        if (attr.AttributeType == typeof(Entity.NotPersistent)) { ignore = true; break; }
      }
      if (ignore) continue;

      if (donefirst) { insert += ", "; insertpost += ", "; update += ", "; } else donefirst = true;
      insert += field.Name;
      insertpost += "@p" + field.Name;
      update += field.Name + "=@p" + field.Name;
    }
    ed.insert = insert + insertpost + ");";
    ed.update = update + " WHERE " + theKey;
    entities.Add(t, ed);

    // Find the position of all columns
    try {
      command.CommandText = "SELECT * FROM " + t.Name + " LIMIT 1;";
      SQLiteDataReader reader = command.ExecuteReader();
      int cols = reader.FieldCount;
      for (int i = 0; i < cols; i++) {
        string name = reader.GetName(i);
        foreach (ColDef cd in ed.fields.Values) {
          if (cd.name.Equals(name, StringComparison.InvariantCultureIgnoreCase)) {
            cd.index = i;
            break;
          }
        }
      }
    } catch (Exception ex) {
      Console.WriteLine(ex.Message);
    }
  }

  public static int Count<T>() {
    SQLiteCommand cmd = new SQLiteCommand("SELECT count(*) FROM " + typeof(T), connection);
    return Convert.ToInt32(cmd.ExecuteScalar());
  }



  public static void Update<T>(T val) {
    Add(val);
  }
  public static void Insert<T>(T val) {
    Add(val);
  }

  public static void Add<T>(T val) {
    try {
      Type t = val.GetType();
      EntityDef ed = entities[t];
      // Get the values with this key from the db
      SQLiteCommand cmd = new SQLiteCommand(ed.count, connection);
      AddKeyParams(ed, cmd, val);

      // Do we have our value?
      if (Convert.ToInt32(cmd.ExecuteScalar()) > 0) { // Yes -> Update
        SQLiteCommand update = new SQLiteCommand(ed.update, connection);
        foreach (FieldInfo field in t.GetFields()) {
          update.Parameters.Add(new SQLiteParameter("@p" + field.Name, field.GetValue(val)));
        }
        AddKeyParams(ed, update, val);
        update.ExecuteNonQuery();
      }
      else { // No - Insert
        SQLiteCommand insert = new SQLiteCommand(ed.insert, connection);
        foreach (FieldInfo field in t.GetFields()) {
          insert.Parameters.Add(new SQLiteParameter("@p" + field.Name, field.GetValue(val)));
        }
        insert.ExecuteNonQuery();
      }
    } catch (Exception ex) {
      Utils.Log("Error in Adding data for " + val.GetType() + ": " + ex.Message, null);
    }
  }

  private static void AddKeyParams(EntityDef ed, SQLiteCommand cmd, object val) {
    int num = 1;
    foreach (var key in ed.keys) {
      object kv = key.GetValue(val);
      cmd.Parameters.Add(new SQLiteParameter("@param" + num, kv));
      num++;
    }
  }

  public static void Delete<T>(T val) {
    try {
      EntityDef ed = entities[val.GetType()];
      SQLiteCommand cmd = new SQLiteCommand(ed.delete, connection);
      AddKeyParams(ed, cmd, val);
      cmd.ExecuteNonQuery();
    } catch (Exception ex) {
      Utils.Log("Error in Deleting data for " + val.GetType() + ": " + ex.Message, null);
    }
  }
  public static void DeleteByKeys<T>(params object[] keys) {
    try {
      EntityDef ed = entities[typeof(T)];
      SQLiteCommand cmd = new SQLiteCommand(ed.delete, connection);
      if (ed.keys.Length != keys.Length) throw new Exception("Inconsistent number of keys for: " + typeof(T).FullName);
      int num = 0;
      foreach (var key in ed.keys) {
        cmd.Parameters.Add(new SQLiteParameter("@param" + (num + 1), keys[num]));
        num++;
      }
      cmd.ExecuteNonQuery();
    } catch (Exception ex) {
      Utils.Log("Error in Deleting data for " + typeof(T) + ": " + ex.Message, null);
    }
  }

  public static T GetByKey<T>(params object[] keys) {
    try {
      EntityDef ed = entities[typeof(T)];
      SQLiteCommand cmd = new SQLiteCommand(ed.selectOne, connection);
      if (ed.keys.Length != keys.Length) throw new Exception("Inconsistent number of keys for: " + typeof(T).FullName);
      int num = 0;
      foreach (var key in ed.keys) {
        cmd.Parameters.Add(new SQLiteParameter("@param" + (num + 1), keys[num]));
        num++;
      }
      SQLiteDataReader reader = cmd.ExecuteReader();
      Type t = typeof(T);
      if (reader.Read()) {
        T val = (T)Activator.CreateInstance(t);
        foreach (FieldInfo field in t.GetFields()) {
          ColDef cd = ed.fields[field.Name];
          num = cd.index;
          if (num != -1 && !reader.IsDBNull(num)) {
            switch (cd.ft) {
              case FieldType.Bool: field.SetValue(val, reader.GetByte(num) != 0); break;
              case FieldType.Byte: field.SetValue(val, reader.GetByte(num)); break;
              case FieldType.Int: field.SetValue(val, reader.GetInt32(num)); break;
              case FieldType.Long: field.SetValue(val, reader.GetInt64(num)); break;
              case FieldType.ULong: field.SetValue(val, (ulong)reader.GetInt64(num)); break;
              case FieldType.String: field.SetValue(val, reader.GetString(num)); break;
              case FieldType.Comment: field.SetValue(val, reader.GetString(num)); break;
              case FieldType.Date: field.SetValue(val, reader.GetDateTime(num)); break;
              case FieldType.Float: field.SetValue(val, reader.GetFloat(num)); break;
              case FieldType.Double: field.SetValue(val, reader.GetDouble(num)); break;
              case FieldType.Blob:
              case FieldType.ByteArray:
                field.SetValue(val, (byte[])reader[field.Name]);
                break;
            }
          }
        }
        return val;
      }
    } catch (Exception ex) {
      Utils.Log("Error in Getting data for " + typeof(T) + ": " + ex.Message, null);
    }
    return default;
  }

  public static List<T> GetAll<T>() {
    try {
      Type t = typeof(T);
      EntityDef ed = entities[t];
      SQLiteCommand cmd = new SQLiteCommand(ed.select + ";", connection);
      SQLiteDataReader reader = cmd.ExecuteReader();
      List<T> res = new List<T>();
      while (reader.Read()) {
        T val = (T)Activator.CreateInstance(t);
        int num;
        foreach (FieldInfo field in t.GetFields()) {
          ColDef cd = ed.fields[field.Name];
          num = cd.index;
          if (num != -1 && !reader.IsDBNull(num)) {
            switch (cd.ft) {
              case FieldType.Bool: field.SetValue(val, reader.GetByte(num) != 0); break;
              case FieldType.Byte: field.SetValue(val, reader.GetByte(num)); break;
              case FieldType.Int: field.SetValue(val, reader.GetInt32(num)); break;
              case FieldType.Long: field.SetValue(val, reader.GetInt64(num)); break;
              case FieldType.ULong: field.SetValue(val, (ulong)reader.GetInt64(num)); break;
              case FieldType.String: field.SetValue(val, reader.GetString(num)); break;
              case FieldType.Comment: field.SetValue(val, reader.GetString(num)); break;
              case FieldType.Date: field.SetValue(val, reader.GetDateTime(num)); break;
              case FieldType.Float: field.SetValue(val, reader.GetFloat(num)); break;
              case FieldType.Double: field.SetValue(val, reader.GetDouble(num)); break;
              case FieldType.Blob:
              case FieldType.ByteArray:
                field.SetValue(val, (byte[])reader[field.Name]);
                break;
            }
          }
        }
        res.Add(val);
      }
      return res;
    } catch (Exception ex) {
      Utils.Log(" " + typeof(T) + ": " + ex.Message, null);
    }
    return null;
  }


  class EntityDef {
    public Type type;
    public FieldInfo[] keys;
    public Dictionary<string, ColDef> fields = new();
    public string count;
    public string select;
    public string insert;
    public string update;
    public string delete;
    public string selectOne;
  }

  public class ColDef {
    public FieldType ft;
    public string name;
    public int index;
  }

  public enum FieldType {
    IGNORE,
    Bool,
    Byte,
    Int,
    Long,
    ULong,
    String,
    Comment,
    Date,
    Float,
    Double,
    Blob,
    ByteArray,
  }
}