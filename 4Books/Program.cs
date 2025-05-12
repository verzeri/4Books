using _4Books.Pages.Data;
using _4Books.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddHttpClient();
builder.Services.AddScoped<_4Books.Services.BookApiService>();

// Aggiungi il DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
    // Imposta il comportamento di tracciamento qui, sull'oggetto options principale
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll);
});

builder.Services.AddHttpClient("GeminiAPI", client =>
{
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddScoped<IBookSummaryService, GeminiSummaryService>();

// Aggiungi il servizio UserService
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<IPasswordHasher<_4Books.Pages.Models.User>, PasswordHasher<_4Books.Pages.Models.User>>();

// Configurazione autenticazione con cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddScoped<_4Books.Services.FavoriteService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Importante: Aggiungi middleware per autenticazione e autorizzazione
app.UseAuthentication();
app.UseAuthorization();

// Reindirizza la pagina root all'accesso o registrazione
app.MapGet("/", context =>
{
    context.Response.Redirect("/Account/Login");
    return Task.CompletedTask;
});

app.MapRazorPages();

// Migrazione automatica del database all'avvio
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}

app.Run();