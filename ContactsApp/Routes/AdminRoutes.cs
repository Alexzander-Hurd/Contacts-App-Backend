using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ContactsApp.Data;
using ContactsApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ContactsApp.Routes;

public static class AdminRoutes
{
    public static void MapAdminRoutes(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin")
            .RequireAuthorization(policy => policy.RequireRole(["Admin"]))
            .WithTags("Admin");
        group
            .MapGet(
                "/",
                async (ApplicationDbContext context, HttpContext request) =>
                {
                    var user = request.User;
                    string userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

                    var users = await context
                        .users.Include(u => u.contact)
                        .Where(u => u.id != userId)
                        .ToListAsync();
                    return Results.Ok(users);
                }
            )
            .Produces<List<User>>(200)
            .Produces(401)
            .WithTags("Admin");

        group
            .MapPost(
                "/reset-password/{id}",
                (string id, ApplicationDbContext context) =>
                {
                    User? user = context.users.FirstOrDefault(u => u.id == id);
                    if (user == null)
                        return Results.BadRequest(new { message = "User not found" });
                    string password = Convert
                        .ToHexString(RandomNumberGenerator.GetBytes(5))
                        .ToUpperInvariant();
                    password = password.Replace("I", "1");
                    password = password.Replace("O", "0");
                    password = password.Replace("L", "1");
                    password = password.Replace(
                        "U",
                        RandomNumberGenerator.GetInt32(0, 9).ToString()
                    );
                    string salt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
                    string passwordHash = Convert.ToBase64String(
                        Rfc2898DeriveBytes.Pbkdf2(
                            Encoding.UTF8.GetBytes(password),
                            Encoding.UTF8.GetBytes(salt),
                            10000,
                            HashAlgorithmName.SHA256,
                            32
                        )
                    );
                    user.password = passwordHash;
                    user.salt = salt;
                    context.SaveChanges();
                    return Results.Ok(
                        new { message = $"Password updated successfully: {password}" }
                    );
                }
            )
            .Produces<MessageResponse>(200)
            .Produces<MessageResponse>(400)
            .WithTags("Admin");

        app.MapDelete(
                "/users/{id}",
                async (string id, ApplicationDbContext context) =>
                {
                    User? appUser = await context
                        .users.Include(u => u.contact)
                        .FirstOrDefaultAsync(u => u.id == id);
                    if (appUser == null)
                        return Results.BadRequest(new { message = "User not found" });

                    if (appUser.contact != null)
                    {
                        List<GroupMember>? groupMembers = await context
                            .group_members.Where(gm => gm.contactId == appUser.contact.id)
                            .ToListAsync();
                        if (groupMembers != null && groupMembers.Count > 0)
                            context.group_members.RemoveRange(groupMembers);

                        List<Favorite>? favorites = await context
                            .favorites.Where(f => f.contactId == appUser.contact.id)
                            .ToListAsync();
                        if (favorites != null && favorites.Count > 0)
                            context.favorites.RemoveRange(favorites);

                        Console.WriteLine(appUser.contact.id);
                        Contact? contact = appUser.contact;
                        appUser.contact = null;
                        appUser.contact_id = null;
                        context.contacts.Remove(contact);
                    }

                    List<RefreshToken>? refreshTokens = await context
                        .refresh_tokens.Where(rt => rt.user.id == appUser.id)
                        .ToListAsync();
                    if (refreshTokens != null && refreshTokens.Count > 0)
                        context.refresh_tokens.RemoveRange(refreshTokens);

                    context.users.Remove(appUser);
                    await context.SaveChangesAsync();
                    return Results.Ok(new { message = "User deleted successfully" });
                }
            )
            .Produces<MessageResponse>(200)
            .Produces<MessageResponse>(400)
            .WithTags("Admin");
    }
}
