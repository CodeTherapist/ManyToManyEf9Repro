using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ConsoleApp2;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public static string db = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
    
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .Property(e => e.Id)
            .ValueGeneratedOnAdd();
        
        modelBuilder.Entity<User>()
            .HasKey(e => new { e.DateTime, e.Id });
       
        modelBuilder.Entity<User>()
            .HasMany(u => u.UserRoles)
            .WithMany(u => u.Users)
            .UsingEntity<UserToUserRole>();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql($"Host=localhost;Database=ManyToManyTest;Username=testuser;Password=testpass;Include Error Detail=true");
        base.OnConfiguring(optionsBuilder);
    }
}

public class BloggingContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost");

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}