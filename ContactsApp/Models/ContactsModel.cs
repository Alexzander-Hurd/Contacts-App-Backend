using System.ComponentModel.DataAnnotations;

namespace ContactsApp.Models;

public class Contact
{
    [Key]
    public required string id { get; set; }
    public required string name { get; set; }
    public required string email { get; set; }
    public required string extension { get; set; }
}

public class ContactDTO
{
    public required string name { get; set; }
    public required string email { get; set; }
    public required string extension { get; set; }
}