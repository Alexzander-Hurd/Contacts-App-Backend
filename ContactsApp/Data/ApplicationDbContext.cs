
using Microsoft.EntityFrameworkCore;
using ContactsApp.Models;

namespace ContactsApp.Data;


public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Contact> contacts { get; set; }
    public DbSet<User> users { get; set; }
    public DbSet<RefreshToken> refresh_tokens { get; set; }
}