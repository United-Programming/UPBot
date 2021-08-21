using Microsoft.EntityFrameworkCore;
using System.Reflection;

public class BotDbContext : DbContext {
  public DbSet<BannedWord> BannedWords { get; set; }
  public DbSet<Reputation> Reputations { get; set; }
  public DbSet<EmojiForRoleValue> EmojiForRoles { get; set; }
  public DbSet<CustomCommand> CustomCommands { get; set; }

  protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
    optionsBuilder.UseSqlite("Filename=Database/UPBot.db", options => {
      options.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName);
    });
    base.OnConfiguring(optionsBuilder);
  }

  protected override void OnModelCreating(ModelBuilder modelBuilder) {
    modelBuilder.Entity<BannedWord>().ToTable("BannedWord", "UPBotSchema");
    modelBuilder.Entity<BannedWord>(entity => {
      entity.HasKey(e => e.Word);
      entity.HasIndex(e => e.Word).IsUnique();
      entity.Property(e => e.Creator);
      entity.Property(e => e.DateAdded).HasDefaultValueSql("CURRENT_TIMESTAMP");
    });
    modelBuilder.Entity<Reputation>().ToTable("Reputation", "UPBotSchema");
    modelBuilder.Entity<Reputation>(entity => {
      entity.HasKey(e => e.User);
      entity.HasIndex(e => e.User).IsUnique();
      entity.Property(e => e.Rep);
      entity.Property(e => e.Fun);
      entity.Property(e => e.Tnk);
      entity.Property(e => e.DateAdded).HasDefaultValueSql("CURRENT_TIMESTAMP");
    });
    modelBuilder.Entity<EmojiForRoleValue>().ToTable("EmojiForRole", "UPBotSchema");
    modelBuilder.Entity<EmojiForRoleValue>(entity => {
      entity.Property(e => e.Channel);
      entity.HasKey(e => e.Message);
      entity.Property(e => e.Role);
      entity.Property(e => e.EmojiId);
      entity.Property(e => e. EmojiName);
    });
    modelBuilder.Entity<CustomCommand>().ToTable("CustomCommand", "UPBotSchema");
    modelBuilder.Entity<CustomCommand>(entity => {
      entity.Property(e => e.Name);
      entity.HasKey(e => e.Name);
      entity.Property(e => e.Content);
      entity.Property(e => e.Alias0);
      entity.Property(e => e.Alias1);
      entity.Property(e => e.Alias2);
      entity.Property(e => e.Alias3);
    });
    base.OnModelCreating(modelBuilder);
  }
}
