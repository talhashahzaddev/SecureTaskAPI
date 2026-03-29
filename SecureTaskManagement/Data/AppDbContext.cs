using Microsoft.EntityFrameworkCore;
using SecureTaskManagement.Models;

namespace SecureTaskManagement.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    { 
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                Username = "admin",
                Password = "$2a$11$ouFX8QHPkHgH7LVVHjWfIeuKCkaO.UTCWVpU4.Ro4b3XdxLcRz0AK", // BCrypt.Net.BCrypt.HashPassword("admin123")
                Role = "Admin"
            },
            new User
            {
                Id = 2,
                Username = "user",
                Password = "$2a$11$iP23j44ywJT//qcOHtAhoeWaFu08wo2jIXKQH.DeJL/BxwRx95c7a", // BCrypt.Net.BCrypt.HashPassword("user123")
                Role = "User"
            }
        );
    }
}