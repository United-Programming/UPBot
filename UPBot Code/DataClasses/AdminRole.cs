public class AdminRole : Entity {
  [Key] public long AdminRolesKey;
  [KeyGen] public ulong Guild;
  [KeyGen] public ulong Role;

  public AdminRole() { }

  public AdminRole(ulong guild, ulong role) {
    Guild = guild;
    Role = role;
    AdminRolesKey = GetKeyValue();
  }

}
