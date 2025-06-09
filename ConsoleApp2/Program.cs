using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ConsoleApp2;

internal class Program
{
    static void Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddDbContextPool<ApplicationDbContext>(o =>
        {
            o.EnableSensitiveDataLogging().EnableDetailedErrors().UseNpgsql();
            o.UseSeeding((context, _) =>
            {
                // Removing the hypertable resolves the issue.
                context.Database.ExecuteSqlRaw("CREATE EXTENSION IF NOT EXISTS timescaledb CASCADE;");
                context.Database.ExecuteSqlRaw("SELECT create_hypertable('public.\"User\"', 'DateTime');");
            });
        });
        services.AddDbContextFactory<ApplicationDbContext>(o =>
        {
            o.EnableSensitiveDataLogging().EnableDetailedErrors().UseNpgsql();
        });

        var serviceProvider = services.BuildServiceProvider();

        var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        using (var dbContext = dbContextFactory.CreateDbContext())
        {
            dbContext.Database.EnsureDeleted();
            dbContext.Database.Migrate();
        }

        var userRole = new UserRole { Name = "Role" };
        using (var dbContext = dbContextFactory.CreateDbContext())
        {
            dbContext.Add(userRole);
            dbContext.SaveChanges();
        }
        //return;
        DoesNotWork(serviceProvider, userRole.Id);
    }

    private static void DoesNotWork(ServiceProvider serviceProvider, int userRoleId)
    {
        var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        using var dbContext = dbContextFactory.CreateDbContext();

        for (var i = 1; i < 11; i++)
        {
            var user = new User
            {
                Name = "User-" + i,
                DateTime = DateTimeOffset.Now.UtcDateTime,
            };
            user.UserToUserRoles.Add(new UserToUserRole
            {
                User = user,
                UserRoleId = userRoleId,
            });
            dbContext.Add(user);
        }

        dbContext.SaveChanges();
    }
}

public class User
{
    public int Id { get; set; }

    public DateTimeOffset DateTime { get; set; }

    public string Name { get; set; }

    public List<UserRole> UserRoles { get; set; } = [];

    public List<UserToUserRole> UserToUserRoles { get; set; } = [];
}

/// <summary>
/// The link table User -> UserToUserRole -> UserRole
/// </summary>
public class UserToUserRole
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public DateTimeOffset UserDateTime { get; set; }
    public User User { get; set; }

    public int UserRoleId { get; set; }

    public UserRole UserRole { get; set; }
}

public class UserRole
{
    public int Id { get; set; }

    public string Name { get; set; }

    public List<User> Users { get; set; } = [];

    public List<UserToUserRole> UserToUserRoles { get; set; } = [];
}