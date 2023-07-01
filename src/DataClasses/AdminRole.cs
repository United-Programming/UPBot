public class AdminRole : Entity {
  [Key] public ulong Guild;
  [Key] public ulong Role;

  public AdminRole() { }

  public AdminRole(ulong guild, ulong role) {
    Guild = guild;
    Role = role;
  }

}
