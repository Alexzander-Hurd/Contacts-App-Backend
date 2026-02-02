using Microsoft.EntityFrameworkCore;

namespace ContactsApp.Models;

public class Group
{
    public required string id { get; set; }
    public required string name { get; set; }
    public string? description { get; set; }
}

public class GroupDetails
{
    public required string id { get; set; }
    public required string name { get; set; }
    public string? description { get; set; }
    public required List<Contact> members { get; set; }
}

public class GroupDTO
{
    public required string name { get; set; }
    public string? description { get; set; }
}

[PrimaryKey("groupId", "contactId")]
public class GroupMember
{
    public required string groupId { get; set; }
    public required string contactId { get; set; }
}
