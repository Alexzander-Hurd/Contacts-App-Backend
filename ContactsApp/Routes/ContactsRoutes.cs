using System.Security.Claims;
using ContactsApp.Data;
using ContactsApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace ContactsApp.Routes;

public static class ContactRoutes
{
    public static void MapContactsRoutes(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/contacts").RequireAuthorization().WithTags("Contacts");
        group
            .MapGet(
                "/",
                async (ApplicationDbContext context, HttpContext request) =>
                {
                    var user = request.User;
                    string userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

                    List<Contact> contacts = context.contacts.ToList();

                    if (userId == null)
                        return Results.Ok(contacts);

                    User? appUser = await context
                        .users.Include(u => u.contact)
                        .FirstOrDefaultAsync(u => u.id == userId);
                    if (appUser == null || appUser.contact == null)
                        return Results.Ok(contacts);

                    contacts = contacts.Where(c => c.id != appUser.contact.id).ToList();
                    return Results.Ok(contacts);
                }
            )
            .Produces<List<Contact>>(200)
            .Produces(401);

        group
            .MapPost(
                "/",
                (ContactDTO contact, ApplicationDbContext context) =>
                {
                    if (contact == null)
                        return Results.BadRequest(new { message = "Contact is null" });
                    Contact newContact = new Contact
                    {
                        id = Guid.NewGuid().ToString(),
                        name = contact.name,
                        email = contact.email,
                        extension = contact.extension,
                    };
                    context.contacts.Add(newContact);
                    context.SaveChanges();
                    return Results.Ok(newContact);
                }
            )
            .Produces<Contact>(200)
            .Produces<MessageResponse>(400)
            .Produces(401);
        ;

        group
            .MapDelete(
                "/{id}",
                [Authorize(Roles = "Admin")]
                (string id, ApplicationDbContext context) =>
                {
                    Contact? contact = context.contacts.Find(id);
                    if (contact == null)
                        return Results.NotFound(
                            new { message = "Contact with supplied id not found" }
                        );
                    context.contacts.Remove(contact);
                    context.SaveChanges();
                    return Results.Ok(contact);
                }
            )
            .Produces<Contact>(200)
            .Produces(401)
            .Produces<MessageResponse>(404);

        group
            .MapPut(
                "/{id}",
                [Authorize(Roles = "Admin")]
                (string id, ContactDTO contact, ApplicationDbContext context) =>
                {
                    Contact? contactToUpdate = context.contacts.Find(id);
                    if (contactToUpdate == null)
                        return Results.NotFound(
                            new { message = "Contact with supplied id not found" }
                        );

                    contactToUpdate.name = contact.name;
                    contactToUpdate.email = contact.email;
                    contactToUpdate.extension = contact.extension;
                    context.SaveChanges();
                    return Results.Ok(contactToUpdate);
                }
            )
            .Produces<Contact>(200)
            .Produces(401)
            .Produces<MessageResponse>(404);

        group
            .MapGet(
                "/favorites",
                (ApplicationDbContext context, HttpContext request) =>
                {
                    var user = request.User;
                    string userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

                    if (userId == null)
                        return Results.BadRequest(new { message = "User id is null" });

                    var favoriteContacts =
                        from fav in context.favorites
                        join contact in context.contacts on fav.contactId equals contact.id
                        where fav.userId == userId
                        select contact;

                    return Results.Ok(favoriteContacts.ToList());
                }
            )
            .Produces<List<Contact>>(200)
            .Produces(401);

        group
            .MapPost(
                "/favorites/{id}",
                async (string id, ApplicationDbContext context, HttpContext request) =>
                {
                    var user = request.User;
                    string userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

                    if (id == null)
                        return Results.BadRequest(new { message = "Contact id is null" });

                    if (userId == null)
                        return Results.BadRequest(new { message = "User id is null" });

                    Contact? contact = await context.contacts.FirstOrDefaultAsync(c => c.id == id);
                    if (contact == null)
                        return Results.NotFound(
                            new { message = "Contact with supplied id not found" }
                        );

                    Favorite favorite = new Favorite
                    {
                        id = Guid.NewGuid().ToString(),
                        userId = userId,
                        contactId = id,
                    };
                    context.favorites.Add(favorite);
                    context.SaveChanges();
                    return Results.Ok(favorite);
                }
            )
            .Produces<Favorite>(200)
            .Produces(401);

        group
            .MapDelete(
                "/favorites/{id}",
                async (string id, ApplicationDbContext context, HttpContext request) =>
                {
                    var user = request.User;
                    string userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

                    if (id == null)
                        return Results.BadRequest(new { message = "Contact id is null" });

                    if (userId == null)
                        return Results.BadRequest(new { message = "User id is null" });

                    Contact? contact = await context.contacts.FirstOrDefaultAsync(c => c.id == id);
                    if (contact == null)
                        return Results.NotFound(
                            new { message = "Contact with supplied id not found" }
                        );

                    Favorite? favorite = new Favorite
                    {
                        id = Guid.NewGuid().ToString(),
                        userId = userId,
                        contactId = id,
                    };

                    Favorite? existingFavorite = await context
                        .favorites.AsNoTracking()
                        .FirstOrDefaultAsync(f => f.userId == userId && f.contactId == id);
                    if (existingFavorite == null)
                        return Results.Ok(favorite);

                    context.favorites.Remove(existingFavorite);
                    context.SaveChanges();
                    return Results.Ok(existingFavorite);
                }
            )
            .Produces<Favorite>(200)
            .Produces(401);

        app.MapGet(
                "/me",
                [Authorize]
                async (ApplicationDbContext context, HttpContext request) =>
                {
                    var user = request.User;
                    string userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

                    if (userId == null)
                        return Results.BadRequest(new { message = "User id is null" });

                    User? appUser = await context
                        .users.Include(u => u.contact)
                        .FirstOrDefaultAsync(u => u.id == userId);
                    if (appUser == null || appUser.contact == null)
                        return Results.NotFound(new { message = "User not found" });

                    return Results.Ok(appUser.contact);
                }
            )
            .Produces<Contact>(200)
            .Produces(401)
            .WithTags("Contacts");
    }
}
