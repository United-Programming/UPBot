public class ExampleEntity : Entity {
  [Key]
  public int id;
  public string name;
  [Comment]
  public string comment;
  [Blob]
  public byte[] blob;
  public long l;
  public ulong ul;
}
