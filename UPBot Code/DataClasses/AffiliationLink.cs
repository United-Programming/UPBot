public class AffiliationLink : Entity {
  [Key] public ulong Guild;
  public string IconURL;
  public string AffiliationID;
  [Comment]public string Message;
  public string Title;

  public AffiliationLink() { }

  public AffiliationLink(ulong guild, string icon, string id, string msg, string title) {
    Guild = guild;
    IconURL = icon;
    AffiliationID = id;
    Message = msg;
    Title = title;
  }
}
