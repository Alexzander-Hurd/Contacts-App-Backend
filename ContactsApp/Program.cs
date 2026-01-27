using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ContactsApp.Data;
using ContactsApp.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "ContactsApp", Version = "v1" });
    c.AddSecurityDefinition(
        "Bearer",
        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Description =
                @"JWT Authorization header using the Bearer scheme. \r\n\r\n 
                      Enter 'Bearer' [space] and then your token in the text input below.
                      \r\n\r\nExample: 'Bearer 12345abcdef'",
            Name = "Authorization",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
        }
    );
    c.AddSecurityRequirement(
        new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer",
                    },
                },
                new string[] { }
            },
        }
    );
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))
);

var key = builder.Configuration.GetValue<string>("Auth:SecretKey");
if (key == null)
    throw new Exception("Missing Auth:SecretKey");

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin();
        builder.AllowAnyHeader();
        builder.AllowAnyMethod();
    });
});

builder
    .Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; //internal app
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.ASCII.GetBytes(key)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                context.HandleResponse();

                context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
                context.Response.Headers.Append("Access-Control-Allow-Credentials", "true");
                context.Response.Headers.Append(
                    "Access-Control-Allow-Headers",
                    "Authorization, Content-Type"
                );

                context.Response.StatusCode = 401;
                return Task.CompletedTask;
            },
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

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
                return Results.BadRequest(new { message = "Username or password is missing" });

            User? user = context.users.FirstOrDefault(u => u.username == loginRequest.username);
            if (user == null)
                return Results.Unauthorized();

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
                return Results.Unauthorized();

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
            context.SaveChanges();

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
                context.SaveChanges();
            }

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
    .Produces(401)
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
                return Results.BadRequest(new { message = "Invalid or expired refresh token" });
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
                return Results.BadRequest(new { message = "Username or password is missing" });

            User? user = context.users.FirstOrDefault(u => u.username == loginRequest.username);
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

app.MapGet(
        "/contacts",
        [Authorize]
        (ApplicationDbContext context) =>
        {
            List<Contact> contacts = context.contacts.ToList();
            return Results.Ok(contacts);
        }
    )
    .Produces<List<Contact>>(200)
    .Produces(401)
    .WithTags("Contacts");

app.MapPost(
        "/contacts",
        [Authorize(Roles = "Admin")]
        (ContactDTO contact, ApplicationDbContext context) =>
        {
            if (contact == null)
                return Results.BadRequest(new { message = "Contact is null" });
            context.contacts.Add(
                new Contact
                {
                    id = Guid.NewGuid().ToString(),
                    name = contact.name,
                    email = contact.email,
                    extension = contact.extension,
                }
            );
            context.SaveChanges();
            return Results.Ok(contact);
        }
    )
    .Produces<Contact>(200)
    .Produces<MessageResponse>(400)
    .Produces(401)
    .WithTags("Contacts");
;

app.MapDelete(
        "/contacts/{id}",
        [Authorize(Roles = "Admin")]
        (string id, ApplicationDbContext context) =>
        {
            Contact? contact = context.contacts.Find(id);
            if (contact == null)
                return Results.NotFound(new { message = "Contact with supplied id not found" });
            context.contacts.Remove(contact);
            context.SaveChanges();
            return Results.Ok(contact);
        }
    )
    .Produces<Contact>(200)
    .Produces(401)
    .Produces<MessageResponse>(404)
    .WithTags("Contacts");

app.MapPut(
        "/contacts/{id}",
        [Authorize(Roles = "Admin")]
        (string id, ContactDTO contact, ApplicationDbContext context) =>
        {
            Contact? contactToUpdate = context.contacts.Find(id);
            if (contactToUpdate == null)
                return Results.NotFound(new { message = "Contact with supplied id not found" });

            contactToUpdate.name = contact.name;
            contactToUpdate.email = contact.email;
            contactToUpdate.extension = contact.extension;
            context.SaveChanges();
            return Results.Ok(contactToUpdate);
        }
    )
    .Produces<Contact>(200)
    .Produces(401)
    .Produces<MessageResponse>(404)
    .WithTags("Contacts");

app.MapGet(
        "/contacts/favorites",
        [Authorize]
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
    .Produces(401)
    .WithTags("Contacts");

app.MapPost(
        "/contacts/favorites/{id}",
        [Authorize]
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
                return Results.NotFound(new { message = "Contact with supplied id not found" });

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
    .Produces(401)
    .WithTags("Contacts");

app.Run();

record LoginRequest(string? username, string? password);
