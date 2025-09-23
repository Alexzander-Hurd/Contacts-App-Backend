using System.ComponentModel.DataAnnotations;

namespace ContactsApp.Models;

public class User
{
    [Key]
    public required string id { get; set; }
    public required string username { get; set; }
    public required string password { get; set; }
    public required string salt { get; set; }
    public string? role { get; set; }
    public string? contact_id { get; set; }
    public Contact? contact { get; set; }
}

public class RefreshToken
{
    [Key]
    public required string id { get; set; }
    public required string token { get; set; }
    public required User user { get; set; }
    public required DateTime expiry { get; set; }
    public bool revoked { get; set; } = false;
}