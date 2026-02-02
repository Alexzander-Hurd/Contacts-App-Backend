using System.Linq.Expressions;
using System.Security.Claims;
using ContactsApp.Data;
using ContactsApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace ContactsApp.Routes;

public static class GroupsRoutes
{
    public static void MapGroupsRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/groups").RequireAuthorization().WithTags("Groups");

        group
            .MapGet(
                "/groups",
                async (ApplicationDbContext context) =>
                {
                    var groups = await context.groups.ToListAsync();

                    if (groups == null || groups.Count == 0)
                        return TypedResults.NoContent();
                    return Results.Ok(groups);
                }
            )
            .Produces<List<Group>>(200)
            .Produces(204)
            .Produces(401)
            .Produces(500);

        group
            .MapPost(
                "/groups",
                async (GroupDTO data, ApplicationDbContext context) =>
                {
                    Group group = new Group()
                    {
                        id = Guid.NewGuid().ToString(),
                        name = data.name,
                        description = data.description,
                    };
                    context.groups.Add(group);
                    await context.SaveChangesAsync();
                    return TypedResults.Ok(group);
                }
            )
            .Produces<Group>(201)
            .Produces<MessageResponse>(400)
            .Produces(401)
            .Produces(500);

        group
            .MapGet(
                "/groups/{id}",
                async (string id, ApplicationDbContext context) =>
                {
                    Group? group = await context.groups.FindAsync(id);
                    if (group == null)
                        return Results.Json(new { message = "Group not found" }, statusCode: 404);

                    List<GroupMember> members = await context
                        .group_members.Where(c => c.groupId == id)
                        .ToListAsync();
                    var contactIds = members.Select(m => m.contactId).ToList();

                    List<Contact> contacts = await context
                        .contacts.Where(c => contactIds.Contains(c.id))
                        .ToListAsync();

                    GroupDetails groupDetails = new GroupDetails
                    {
                        id = group.id,
                        name = group.name,
                        description = group.description ?? "",
                        members = contacts,
                    };
                    return TypedResults.Ok(groupDetails);
                }
            )
            .Produces<GroupDetails>(200)
            .Produces<MessageResponse>(404)
            .Produces(401)
            .Produces(500);

        group
            .MapDelete(
                "/groups/{id}",
                async (string id, ApplicationDbContext context) =>
                {
                    Group? group = await context.groups.FindAsync(id);
                    if (group == null)
                        return Results.Json(new { message = "Group not found" }, statusCode: 404);

                    context.group_members.RemoveRange(
                        context.group_members.Where(g => g.groupId == id)
                    );
                    context.groups.Remove(group);
                    await context.SaveChangesAsync();
                    return TypedResults.Ok(group);
                }
            )
            .Produces<Group>(200)
            .Produces<MessageResponse>(404)
            .Produces(401)
            .Produces(500);

        group
            .MapPut(
                "/groups/{id}",
                async (string id, GroupDTO data, ApplicationDbContext context) =>
                {
                    Group? group = await context.groups.FindAsync(id);
                    if (group == null)
                        return Results.Json(new { message = "Group not found" }, statusCode: 404);
                    group.name = data.name;
                    group.description = data.description;
                    await context.SaveChangesAsync();
                    return TypedResults.Ok(group);
                }
            )
            .Produces<Group>(200)
            .Produces<MessageResponse>(404)
            .Produces(401)
            .Produces(500);

        group
            .MapPost(
                "/groups/{id}/members/{contactId}",
                async (string id, string contactId, ApplicationDbContext context) =>
                {
                    Group? group = await context.groups.FindAsync(id);
                    if (group == null)
                        return Results.Json(new { message = "Group not found" }, statusCode: 404);

                    Contact? contact = await context.contacts.FirstOrDefaultAsync(c =>
                        c.id == contactId
                    );
                    if (contact == null)
                        return Results.Json(new { message = "Contact not found" }, statusCode: 404);

                    GroupMember data = new GroupMember() { groupId = id, contactId = contactId };
                    context.group_members.Add(data);
                    await context.SaveChangesAsync();
                    return TypedResults.Ok(data);
                }
            )
            .Produces<GroupMember>(201)
            .Produces<MessageResponse>(400)
            .Produces(401)
            .Produces(500);

        group
            .MapDelete(
                "/groups/{id}/members/{contactId}",
                async (string id, string contactId, ApplicationDbContext context) =>
                {
                    GroupMember? member = await context.group_members.FirstOrDefaultAsync(c =>
                        c.groupId == id && c.contactId == contactId
                    );
                    if (member == null)
                        return Results.Json(new { message = "Member not found" }, statusCode: 404);
                    context.group_members.Remove(member);
                    await context.SaveChangesAsync();
                    return TypedResults.Ok(member);
                }
            )
            .Produces<GroupMember>(200)
            .Produces<MessageResponse>(404)
            .Produces(401)
            .Produces(500);
    }
}
