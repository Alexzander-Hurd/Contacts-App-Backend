using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ContactsApp.Data;
using ContactsApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ContactsApp.Routes;

public static class AuthRoutes
{
    public static void MapAuthRoutes(this IEndpointRouteBuilder app, IConfiguration config)
    {
        string? key = config["Auth:SecretKey"];
        if (key == null)
            throw new Exception("Missing Auth:SecretKey");

        app.MapPost(
                "/login",
                async (LoginRequest loginRequest, ApplicationDbContext context) =>
                {
                    if (loginRequest == null)
                        return Results.BadRequest(new { message = "Login request is null" });
                    if (
                        string.IsNullOrWhiteSpace(loginRequest.username)
                        || string.IsNullOrWhiteSpace(loginRequest.password)
                    )
                        return Results.BadRequest(
                            new { message = "Username or password is missing" }
                        );

                    User? user = context
                        .users.Include(u => u.contact)
                        .FirstOrDefault(u => u.username == loginRequest.username);
                    if (user == null)
                        return Results.Json(
                            new { message = "Invalid username or password" },
                            statusCode: 401
                        );

                    string passwordHash = Convert.ToBase64String(
                        Rfc2898DeriveBytes.Pbkdf2(
                            Encoding.UTF8.GetBytes(loginRequest.password),
                            Encoding.UTF8.GetBytes(user.salt),
                            10000,
                            HashAlgorithmName.SHA256,
                            32
                        )
                    );
                    if (string.IsNullOrWhiteSpace(passwordHash))
                        return Results.StatusCode(500);

                    if (user.password != passwordHash)
                        return Results.Json(
                            new { message = "Invalid username or password" },
                            statusCode: 401
                        );

                    JwtSecurityTokenHandler token_handler = new JwtSecurityTokenHandler();
                    SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
                    {
                        Subject = new ClaimsIdentity([
                            new Claim(JwtRegisteredClaimNames.UniqueName, user.username),
                            new Claim(ClaimTypes.Role, user.role ?? "User"),
                            new Claim(JwtRegisteredClaimNames.Sub, user.id),
                            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                            new Claim(
                                JwtRegisteredClaimNames.Iat,
                                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()
                            ),
                        ]),
                        Expires = DateTime.UtcNow.AddMinutes(15),
                        SigningCredentials = new SigningCredentials(
                            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                            SecurityAlgorithms.HmacSha256Signature
                        ),
                        Issuer = "ContactsApp",
                    };

                    SecurityToken token = token_handler.CreateToken(tokenDescriptor);
                    string jwt = token_handler.WriteToken(token);

                    string refreshToken = Guid.NewGuid().ToString();
                    RefreshToken newRefreshToken = new RefreshToken
                    {
                        id = Guid.NewGuid().ToString(),
                        token = refreshToken,
                        user = user,
                        expiry = DateTime.UtcNow.AddDays(1),
                    };

                    context.refresh_tokens.Add(newRefreshToken);

                    if (user.contact == null)
                    {
                        Contact? contact = await context.contacts.FirstOrDefaultAsync(c =>
                            c.email == user.username
                        );
                        if (contact == null)
                        {
                            contact = new Contact
                            {
                                id = Guid.NewGuid().ToString(),
                                name = user.username.Split('@')[0],
                                email = user.username,
                                extension = "---",
                            };
                            context.contacts.Add(contact);
                            user.contact_id = contact.id;
                            user.contact = contact;
                        }
                        else
                        {
                            user.contact_id = contact.id;
                            user.contact = contact;
                        }
                    }
                    context.SaveChanges();

                    return Results.Ok(
                        new TokenResponse
                        {
                            token = jwt,
                            expiry = TimeSpan.FromMinutes(15).TotalSeconds,
                            refresh = refreshToken,
                        }
                    );
                }
            )
            .Produces<TokenResponse>(200)
            .Produces<MessageResponse>(400)
            .Produces<MessageResponse>(401)
            .Produces(500)
            .WithTags("Auth");

        app.MapPost(
                "/refresh",
                (string refreshToken, ApplicationDbContext context) =>
                {
                    RefreshToken? token = context
                        .refresh_tokens.Include(t => t.user)
                        .FirstOrDefault(t => t.token == refreshToken);
                    if (token == null)
                        return Results.BadRequest(
                            new { message = "Invalid or expired refresh token" }
                        );
                    if (token.expiry < DateTime.UtcNow)
                    {
                        context.refresh_tokens.Remove(token);
                        context.SaveChanges();
                        return Results.BadRequest(new { message = "Expired refresh token" });
                    }
                    JwtSecurityTokenHandler token_handler = new JwtSecurityTokenHandler();
                    SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
                    {
                        Subject = new ClaimsIdentity([
                            new Claim(JwtRegisteredClaimNames.UniqueName, token.user.username),
                            new Claim(ClaimTypes.Role, token.user.role ?? "User"),
                            new Claim(JwtRegisteredClaimNames.Sub, token.user.id),
                            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                            new Claim(
                                JwtRegisteredClaimNames.Iat,
                                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()
                            ),
                        ]),
                        Expires = DateTime.UtcNow.AddMinutes(15),
                        SigningCredentials = new SigningCredentials(
                            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                            SecurityAlgorithms.HmacSha256Signature
                        ),
                        Issuer = "ContactsApp",
                    };

                    SecurityToken token2 = token_handler.CreateToken(tokenDescriptor);
                    string jwt = token_handler.WriteToken(token2);

                    string newRefreshToken = Guid.NewGuid().ToString();
                    token.token = newRefreshToken;
                    token.expiry = DateTime.UtcNow.AddDays(1);
                    context.SaveChanges();
                    return Results.Ok(
                        new
                        {
                            token = jwt,
                            expiry = TimeSpan.FromMinutes(15).TotalSeconds,
                            refresh = newRefreshToken,
                        }
                    );
                }
            )
            .Produces<TokenResponse>(200)
            .Produces<MessageResponse>(400)
            .WithTags("Auth");

        app.MapGet(
                "/logout",
                [Authorize]
                (ApplicationDbContext context, HttpContext request) =>
                {
                    var user = request.User;
                    List<RefreshToken>? tokens = context
                        .refresh_tokens.Include(t => t.user)
                        .Where(t => t.user.id == user.FindFirstValue(JwtRegisteredClaimNames.Sub))
                        .ToList();
                    if (tokens != null)
                    {
                        context.refresh_tokens.RemoveRange(tokens);
                        context.SaveChanges();
                    }
                    return Results.Ok(new { message = "Logout successful" });
                }
            )
            .Produces<MessageResponse>(200)
            .Produces(401)
            .WithTags("Auth");

        app.MapPost(
                "/register",
                (LoginRequest loginRequest, ApplicationDbContext context) =>
                {
                    if (loginRequest == null)
                        return Results.BadRequest(new { message = "Request is empty" });
                    if (
                        string.IsNullOrWhiteSpace(loginRequest.username)
                        || string.IsNullOrWhiteSpace(loginRequest.password)
                    )
                        return Results.BadRequest(
                            new { message = "Username or password is missing" }
                        );

                    User? user = context.users.FirstOrDefault(u =>
                        u.username == loginRequest.username
                    );
                    if (user != null)
                        return Results.BadRequest(new { message = "User already exists" });

                    string role = context.users.Any() ? "User" : "Admin";
                    string salt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
                    string passwordHash = Convert.ToBase64String(
                        Rfc2898DeriveBytes.Pbkdf2(
                            Encoding.UTF8.GetBytes(loginRequest.password),
                            Encoding.UTF8.GetBytes(salt),
                            10000,
                            HashAlgorithmName.SHA256,
                            32
                        )
                    );
                    user = new User
                    {
                        id = Guid.NewGuid().ToString(),
                        username = loginRequest.username,
                        password = passwordHash,
                        salt = salt,
                        role = role,
                    };
                    context.users.Add(user);
                    context.SaveChanges();
                    return Results.Ok(new { loginRequest.username, role });
                }
            )
            .Produces<registerResponse>(200)
            .Produces<MessageResponse>(400)
            .WithTags("Auth");
    }
}
