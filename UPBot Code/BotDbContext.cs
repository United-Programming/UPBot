using Microsoft.EntityFrameworkCore;
using System.Reflection;

public class BotDbContext : DbContext {
  public DbSet<HelperMember> Helpers { get; set; }
  protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
    optionsBuilder.UseSqlite("Filename=Database/UPBot.db", options => {
      options.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName);
    });
    base.OnConfiguring(optionsBuilder);
  }
  protected override void OnModelCreating(ModelBuilder modelBuilder) {
    // Map table names
    modelBuilder.Entity<HelperMember>().ToTable("HelperMember", "test");
    modelBuilder.Entity<HelperMember>(entity => {
      entity.HasKey(e => e.Id);
      entity.HasIndex(e => e.Name);// .IsUnique();
      entity.Property(e => e.DateAdded).HasDefaultValueSql("CURRENT_TIMESTAMP");
    });
    base.OnModelCreating(modelBuilder);
  }
}