public class Timezone : Entity {
  [Key]
  public ulong User; // Timezones are not related to guilds
  public float UtcOffset;
  public string TimeZoneName;

  public Timezone() { }

  public Timezone(ulong usr, string name) {
    User = usr;
    UtcOffset = 0;
    TimeZoneName = name;
  }
}
