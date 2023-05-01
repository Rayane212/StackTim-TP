using StackTim_TP.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Dapper;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Session;
using static System.Net.WebRequestMethods;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.AllowAnyOrigin().AllowAnyHeader().AllowCredentials().AllowAnyMethod();
                      });
});
// Ajouter le service de cache distribué
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseSession();

}

app.UseSession();

// SignIn 
app.MapPost("/signIn", async (IConfiguration _config, HttpContext http, string username, string mdp) =>
{
    // Vérifier que les identifiants sont valides
    using var connection = new SqlConnection(builder.Configuration.GetConnectionString("SQL"));
    var user = await connection.QuerySingleOrDefaultAsync<UtilisateursEntity>(
        "SELECT * FROM Utilisateurs WHERE EmailUtilisateur = @EmailUtilisateur OR nomUtilisateur = @nomUtilisateur AND MotDePasse = @MotDePasse", new { EmailUtilisateur = username, nomUtilisateur = username, MotDePasse = mdp});
    if (user == null)
    {
        http.Response.StatusCode = 401; // Code HTTP 401 Unauthorized
        await http.Response.WriteAsync("Adresse email ou mot de passe incorrect.");
        return;
    }

    // Créez un objet MemoryCache pour stocker les informations de session
    IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());

    // Stockez les informations de session dans le cache
    cache.Set("UserId", user.codeUtilisateur);

    // Stocker l'identifiant de l'utilisateur dans la session
    http.Session.SetString("UserId", user.codeUtilisateur);


    http.Response.StatusCode = 200; // Code HTTP 200 OK
    await http.Response.WriteAsync($"Utilisateur {user.codeUtilisateur} connecté avec succès.");
});

app.MapPost("/signUp", async (IConfiguration _config, HttpContext http, string nom, string email ,string mdp) =>
{
    // Vérifier que l'utilisateur n'existe pas déjà
    using var connection = new SqlConnection(builder.Configuration.GetConnectionString("SQL"));
    var existingUser = await connection.QuerySingleOrDefaultAsync<UtilisateursEntity>(
        "SELECT * FROM Utilisateurs WHERE EmailUtilisateur = @EmailUtilisateur OR nomUtilisateur = @nomUtilisateur", new { EmailUtilisateur = email, nomUtilisateur = nom });
    if (existingUser != null)
    {
        http.Response.StatusCode = 409; // Code HTTP 409 Conflict
        await http.Response.WriteAsync("Un utilisateur avec cette adresse email ou ce nom d'utilisateur existe déjà.");
        return;
    }
    await connection.QueryAsync<UtilisateursEntity>(
        "INSERT INTO Utilisateurs (nomUtilisateur, EmailUtilisateur, MotDePasse, codeUtilisateur) VALUES (@nomUtilisateur, @EmailUtilisateur, @MotDePasse, @codeUtilisateur)", new { nomUtilisateur = nom, EmailUtilisateur = email, MotDePasse = mdp, codeUtilisateur = nom });
    // Créez un objet MemoryCache pour stocker les informations de session
    
    IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());

    // Stockez les informations de session dans le cache
    cache.Set("UserId", nom);

    // Stocker l'identifiant de l'utilisateur dans la session
    http.Session.SetString("UserId", nom);

    http.Response.StatusCode = 200; // Code HTTP 200 OK
    await http.Response.WriteAsync($"Utilisateur {nom} créé avec succès.");
});

// logout
app.MapPost("/logout", async (HttpContext http) =>
{
    // Vider la session
    http.Session.Clear();

    // Vider le cache
    http.RequestServices.GetService<IMemoryCache>().Remove(http.Session.GetString("UserId"));

    // Rediriger vers la page d'accueil ou une autre page de votre choix
    http.Response.Redirect("/");
});


// Create Connaissance
app.MapPut("/CreateConnaissance", (IConfiguration _config, string codeConnaissance, string nomConnaissance, string? descriptifConnaissance, string? codeRessource, HttpContext http) =>
{
    var ce = new ConnaissanceEntity();
    var ok = new ConnaissanceRepos(_config).InsertConnaissance(http.Session.GetString("UserId"), codeConnaissance, nomConnaissance, descriptifConnaissance, codeRessource);
    return (ok != -1) ? Results.Created($"/{ok}", ce) : Results.Problem(new ProblemDetails { Detail = "L'insert n'a pas marché", Status = 500 });

});

// Read Connaissances
app.MapGet("/GetAllConnaissance", (IConfiguration _config, HttpContext http) =>
{
    var ok = new ConnaissanceRepos(_config).GetAllConnaissance(http.Session.GetString("UserId"));
    return ok;
    
});

app.MapGet("/GetByIdConnaissance/{idConnaissance}", (IConfiguration _config, int id, HttpContext http) =>
{
    var ce = new ConnaissanceRepos(_config).GetByIdConnaissance(id, http.Session.GetString("UserId"));
    return ce;
});

app.MapGet("/GetByCodeConnaissance/{codeConnaissance}", (IConfiguration _config, string codeConnaissance, HttpContext http) =>
{
    var ce = new ConnaissanceRepos(_config).GetByCodeConnaissance(codeConnaissance, http.Session.GetString("UserId"));
    return ce;

});

// Update Connaissances 
app.MapPost("/UpdateConnaissance/{idConnaissance}", (IConfiguration _config, ConnaissanceEntity ce) =>
{
    var ok = new ConnaissanceRepos(_config).UpdateConnaissance(ce);
    return ok > 0 ? Results.NoContent() : Results.Problem(new ProblemDetails { Detail = "L'update n'a pas marché", Status = 500 });
});

// Delete Connaissance 
app.MapDelete("/DeleteConnaissance/{idConnaissance}", (IConfiguration _config, int id, HttpContext http) =>
{
    var ok = new ConnaissanceRepos(_config).DeleteConnaissance(id, http.Session.GetString("UserId"));
    return ok;

});

// Create Categorie 
app.MapPut("CreateCategorie", (IConfiguration _config, CategorieEntity categorie) =>
{
    var ok = new CategorieRepos(_config).InsertCategorie(categorie);
    return ok ;
});

// Read Categorie 
app.MapGet("/GetAllCategorie", (IConfiguration _config) =>
{

    var ok = new CategorieRepos(_config).GetAllCategorie();
    return ok;

});

app.MapGet("/GetByIdCategorie/{idCategorie}", (IConfiguration _config, int id) =>
{
    var ce = new CategorieRepos(_config).GetByIdCategorie(id);
    return ce;
});

app.MapGet("/GetByCodeCategorie/{codeCategorie}", (IConfiguration _config, string codeCategorie) =>
{
    var ce = new CategorieRepos(_config).GetByCodeCategorie(codeCategorie);
    return ce;

});

// Update Categorie 
app.MapPost("/UpdateCategorie/{idCategorie}", (IConfiguration _config, CategorieEntity categorie) =>
{
    var ok = new CategorieRepos(_config).UpdateCategorie(categorie);
    return ok > 0 ? Results.NoContent() : Results.Problem(new ProblemDetails { Detail = "L'update n'a pas marché", Status = 500 });
});

// Delete Categorie 
app.MapDelete("/DeleteCategorie/{idCategorie}", (IConfiguration _config, int id) =>
{
    var ok = new CategorieRepos(_config).DeleteCategorie(id);
    return ok;

});




app.UseHttpsRedirection();

app.UseAuthorization();


app.Run();
