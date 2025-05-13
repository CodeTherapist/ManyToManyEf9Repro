using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ConsoleApp2;

internal class Program
{
    static void Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(o =>
        {
            o.EnableSensitiveDataLogging();
            o.EnableDetailedErrors();
            o.UseSeeding((context, _) =>
            {
                 context.Database.ExecuteSqlRaw("CREATE EXTENSION IF NOT EXISTS timescaledb CASCADE;");
                 context.Database.ExecuteSqlRaw("SELECT create_hypertable('public.\"User\"', 'DateTime');");
            });
            o.UseNpgsql();
        });


        var serviceProvider = services.BuildServiceProvider();
        using (var sp = serviceProvider.CreateScope())
        {
            using (var dbContext = sp.ServiceProvider.GetRequiredService<ApplicationDbContext>())
            {
                dbContext.Database.Migrate();
            }
        }

        UserRole userRole;
        using (var sp = serviceProvider.CreateScope())
        {
            using (var dbContext = sp.ServiceProvider.GetRequiredService<ApplicationDbContext>())
            {
                userRole = new UserRole()
                {
                    Name = "Role"
                };
                dbContext.Add(userRole);
                dbContext.SaveChanges();
            }
        }

        DoesNotWork(serviceProvider, userRole);
    }

    private static void DoesNotWork(ServiceProvider serviceProvider, UserRole userRole)
    {
        using (var sp = serviceProvider.CreateScope())
        {
            using (var dbContext = sp.ServiceProvider.GetRequiredService<ApplicationDbContext>())
            {
                for (var i = 0; i < 100; i++)
                {
                    var user = new User
                    {
                        Name = "User-" + i,
                        DateTime = DateTimeOffset.Now.UtcDateTime,
                    };
                    
                    user.UserToUserRoles.Add( new UserToUserRole
                    {
                        User = user,
                        UserRoleId = userRole.Id,
                    });
                    dbContext.Add(user);
                }
                
                dbContext.SaveChanges();
            }
        }
    }
    
    
    private static void DoesWorkWhenUserIsSavedFirst(ServiceProvider serviceProvider, UserRole userRole)
    {
        using (var sp = serviceProvider.CreateScope())
        {
            using (var dbContext = sp.ServiceProvider.GetRequiredService<ApplicationDbContext>())
            {
                for (var i = 0; i < 100; i++)
                {
                    var user = new User
                    {
                        Name = "User-IsSavedFirst" + i,
                        DateTime = DateTimeOffset.Now.UtcDateTime,
                    };
                    dbContext.SaveChanges();
                    
                    user.UserToUserRoles.Add( new UserToUserRole
                    {
                        User = user,
                        UserRoleId = userRole.Id,
                    });
                    dbContext.Add(user);
                }
                dbContext.SaveChanges();
            }
        }
    }
    
        
    private static void DoesWorkWhenLinkTableIsSavedOneByOne(ServiceProvider serviceProvider, UserRole userRole)
    {
        using (var sp = serviceProvider.CreateScope())
        {
            using (var dbContext = sp.ServiceProvider.GetRequiredService<ApplicationDbContext>())
            {
                for (var i = 0; i < 100; i++)
                {
                    var user = new User
                    {
                        Name = "User-OneByOne" + i,
                        DateTime = DateTimeOffset.Now.UtcDateTime,
                    };
                    user.UserToUserRoles.Add( new UserToUserRole
                    {
                        User = user,
                        UserRoleId = userRole.Id,
                    });
                    dbContext.Add(user);
                    dbContext.SaveChanges();
                }
            }
        }
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