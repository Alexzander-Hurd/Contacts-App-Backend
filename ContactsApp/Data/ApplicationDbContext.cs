using ContactsApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ContactsApp.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Contact> contacts { get; set; }
    public DbSet<User> users { get; set; }
    public DbSet<RefreshToken> refresh_tokens { get; set; }
    public DbSet<Favorite> favorites { get; set; }
    public DbSet<Group> groups { get; set; }
    public DbSet<GroupMember> group_members { get; set; }
}
