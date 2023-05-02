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
using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.WithOrigins("http://localhost:3000").AllowAnyOrigin().AllowAnyHeader().AllowCredentials().AllowAnyMethod();

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
    IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());
    cache.Set("UserId", user.codeUtilisateur);



    // Stocker l'identifiant de l'utilisateur dans la session
    http.Session.SetString("UserId", user.codeUtilisateur);


    http.Response.StatusCode = 200; // Code HTTP 200 OK
    await http.Response.WriteAsync($"Utilisateur {user.codeUtilisateur} connecté avec succès.");
});

app.MapPut("/signUp", async (IConfiguration _config, HttpContext http, string nom, string email ,string mdp) =>
{
    var userId = nom.ToUpper() + new Guid(Guid.NewGuid().ToString()); 
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
        "INSERT INTO Utilisateurs (nomUtilisateur, EmailUtilisateur, MotDePasse, codeUtilisateur) VALUES (@nomUtilisateur, @EmailUtilisateur, @MotDePasse, @codeUtilisateur)", new { nomUtilisateur = nom, EmailUtilisateur = email, MotDePasse = mdp, codeUtilisateur = userId });    


    http.Response.StatusCode = 200; // Code HTTP 200 OK
    await http.Response.WriteAsync($"Utilisateur {userId} créé avec succès.");

   // http.Response.Redirect("/signIn");

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
app.MapPut("/CreateConnaissance",async (IConfiguration _config, ConnaissanceEntity connaissance, HttpContext http) =>
{
    var userId = http.Session.GetString("UserId");
    var code = connaissance.codeConnaissance.ToUpper();
    var existingConnaissance = await new ConnaissanceRepos(_config).ExistingConnaissance(code, userId);
    if ( (userId != null || userId != "") && existingConnaissance == false)
    {
        connaissance.codeUtilisateur = userId;
        connaissance.codeConnaissance = code;
        var ok = new ConnaissanceRepos(_config).InsertConnaissance(connaissance);
        return (ok != -1) ? Results.Created($"/{ok}", connaissance) : Results.Problem(new ProblemDetails { Detail = "L'insert n'a pas marché", Status = 500 });
    }
    else
    {
        if(userId == null || userId == "")
        {
            return Results.Problem(new ProblemDetails { Detail = "Utilisateur non connecté.", Status = 401 });

        }
        return Results.Problem(new ProblemDetails { Detail = "Code connaissance déjà existant", Status = 401 });
    }
    

});

// Read Connaissances
app.MapGet("/GetAllConnaissance", async (IConfiguration _config, HttpContext http) =>
{
    var userId = http.Session.GetString("UserId");
    if (userId == null || userId == "")
    {
        http.Response.StatusCode = 401; 
        await http.Response.WriteAsync("Utilisateur non connecté.");
        return;
    }

    var ok = new ConnaissanceRepos(_config).GetAllConnaissance(userId);

    http.Response.StatusCode = 200; 
    await http.Response.WriteAsJsonAsync(ok);

   
});


app.MapGet("/GetByIdConnaissance/{idConnaissance}", async (IConfiguration _config, int id, HttpContext http) =>
{
    var userId = http.Session.GetString("UserId");
    if (userId == null)
    {
        http.Response.StatusCode = 401;
        await http.Response.WriteAsync("Utilisateur non connecté.");
        return;
    }

    var ok = new ConnaissanceRepos(_config).GetByIdConnaissance(id,userId);

    http.Response.StatusCode = 200;
    await http.Response.WriteAsJsonAsync(ok);
});

app.MapGet("/GetByCodeConnaissance/{codeConnaissance}", async (IConfiguration _config, string codeConnaissance, HttpContext http) =>
{
    var userId = http.Session.GetString("UserId");
    if (userId == null)
    {
        http.Response.StatusCode = 401;
        await http.Response.WriteAsync("Utilisateur non connecté.");
        return;
    }
    var ok = new ConnaissanceRepos(_config).GetByCodeConnaissance(codeConnaissance, userId);

    http.Response.StatusCode = 200;
    await http.Response.WriteAsJsonAsync(ok);

});

// Update Connaissances 
app.MapPost("/UpdateConnaissance/{idConnaissance}", async (IConfiguration _config, ConnaissanceEntity connaissance, HttpContext http) =>
{
    var userId = http.Session.GetString("UserId");
    var code = connaissance.codeConnaissance.ToUpper();
    var existingConnaissance = await new ConnaissanceRepos(_config).ExistingConnaissance(code, userId);
    if ((userId != null || userId != "") && existingConnaissance == false)
    {
        connaissance.codeUtilisateur = userId;
        connaissance.codeConnaissance = code;
        var ok = new ConnaissanceRepos(_config).UpdateConnaissance(connaissance);
        return ok > 0 ? Results.NoContent() : Results.Problem(new ProblemDetails { Detail = "L'update n'a pas marché", Status = 500 });
    }
    else
    {
        if (userId == null || userId == "")
        {
            return Results.Problem(new ProblemDetails { Detail = "Utilisateur non connecté.", Status = 401 });

        }
        return Results.Problem(new ProblemDetails { Detail = "Code connaissance déjà existant", Status = 401 });
    }


});

// Delete Connaissance 
app.MapDelete("/DeleteConnaissance/{idConnaissance}", (IConfiguration _config, int id, HttpContext http) =>
{
    var userId = http.Session.GetString("UserId");
    if (userId != null || userId != "")
    {
        var ok = new ConnaissanceRepos(_config).DeleteConnaissance(id, userId);
        return ok > 0 ? Results.Ok() : Results.Problem(new ProblemDetails { Detail = "Le delete n'a pas marché", Status = 500 });
    }
    else
    {
        return Results.Problem(new ProblemDetails { Detail = "Utilisateur non connecté.", Status = 401 });
    }

});

// Create Categorie 
app.MapPut("CreateCategorie", async (IConfiguration _config, CategorieEntity categorie, HttpContext http) =>
{
    var userId = http.Session.GetString("UserId");
    var code = categorie.codeCategorie.ToUpper();
    var existingCategorie = await new CategorieRepos(_config).ExistingCategorie(code, userId);

    if ((userId != null || userId != "") && existingCategorie == false)
    {
        categorie.codeUtilisateur = userId;
        categorie.codeCategorie = code;
        var ok = new CategorieRepos(_config).InsertCategorie(categorie);
        return (ok != -1) ? Results.Created($"/{ok}", categorie) : Results.Problem(new ProblemDetails { Detail = "L'insert n'a pas marché", Status = 500 });
    }
    else
    {
        if (userId == null || userId == "")
        {
            return Results.Problem(new ProblemDetails { Detail = "Utilisateur non connecté.", Status = 401 });

        }
        return Results.Problem(new ProblemDetails { Detail = "Code categorie déjà existant", Status = 401 });
    }

        
});

// Read Categorie 
app.MapGet("/GetAllCategorie", async (IConfiguration _config, HttpContext http) =>
{
    var userId = http.Session.GetString("UserId");
    if (userId == null || userId == "")
    {
        http.Response.StatusCode = 401;
        await http.Response.WriteAsync("Utilisateur non connecté.");
        return;
    }

    var ok = new CategorieRepos(_config).GetAllCategorie(userId);

    http.Response.StatusCode = 200;
    await http.Response.WriteAsJsonAsync(ok);

});

app.MapGet("/GetByIdCategorie/{idCategorie}", async (IConfiguration _config, int id, HttpContext http) =>
{

    var userId = http.Session.GetString("UserId");
    if (userId == null || userId == "")
    {
        http.Response.StatusCode = 401;
        await http.Response.WriteAsync("Utilisateur non connecté.");
        return;
    }

    var ok = new CategorieRepos(_config).GetByIdCategorie(id, userId);
    http.Response.StatusCode = 200;
    await http.Response.WriteAsJsonAsync(ok);
});

app.MapGet("/GetByCodeCategorie/{codeCategorie}", async (IConfiguration _config, string codeCategorie, HttpContext http) =>
{

    var userId = http.Session.GetString("UserId");
    if (userId == null || userId == "")
    {
        http.Response.StatusCode = 401;
        await http.Response.WriteAsync("Utilisateur non connecté.");
        return;
    }

    var ok = new CategorieRepos(_config).GetByCodeCategorie(codeCategorie, userId);
    
    http.Response.StatusCode = 200;
    await http.Response.WriteAsJsonAsync(ok);

});

// Update Categorie 
app.MapPost("/UpdateCategorie/{idCategorie}",async  (IConfiguration _config, CategorieEntity categorie, HttpContext http) =>
{
    var userId = http.Session.GetString("UserId");
    var code = categorie.codeCategorie.ToUpper();
    var existingCategorie = await new CategorieRepos(_config).ExistingCategorie(code, userId);

    if ((userId != null || userId != "") && existingCategorie == false)
    {
        categorie.codeUtilisateur = userId;
        var ok = new CategorieRepos(_config).UpdateCategorie(categorie);
        return ok > 0 ? Results.NoContent() : Results.Problem(new ProblemDetails { Detail = "L'update n'a pas marché", Status = 500 });
    }
    else
    {
        return Results.Problem(new ProblemDetails { Detail = "Utilisateur non connecté.", Status = 401 });
    }

});

// Delete Categorie 
app.MapDelete("/DeleteCategorie/{idCategorie}",  (IConfiguration _config, int id, HttpContext http) =>
{
    var userId = http.Session.GetString("UserId");
    if (userId != null || userId != "")
    {
        var ok = new CategorieRepos(_config).DeleteCategorie(id, userId);
        return ok > 0 ? Results.Ok() : Results.Problem(new ProblemDetails { Detail = "Le delete n'a pas marché", Status = 500 });
    }
    else
    {
        return Results.Problem(new ProblemDetails { Detail = "Utilisateur non connecté.", Status = 401 });
    }

});

// Create Ressource
app.MapPut("/CreateRessource", async (IConfiguration _config, RessourcesEntity Ressource, HttpContext http) =>
{
    var userId = http.Session.GetString("UserId");
    var code = Ressource.codeRessource.ToUpper();
    var existingRessource = await new RessourcesRepos(_config).ExistingRessource(code, userId);
    var oSqlConnection = new SqlConnection(_config?.GetConnectionString("SQL"));

    if ((userId != null || userId != "") && existingRessource == false)
    {
        Ressource.codeUtilisateur = userId;
        Ressource.creerPar = oSqlConnection.QueryFirstOrDefault<string>("Select nomUtilisateur from Utilisateurs where codeUtilisateur = @CodeUtilisateur", new {CodeUtilisateur = userId}) ;
        Ressource.codeRessource = code;
        Ressource.datePublication = DateTime.Today;
        var ok = new RessourcesRepos(_config).InsertRessource(Ressource);
        return (ok != -1) ? Results.Created($"/{ok}", Ressource) : Results.Problem(new ProblemDetails { Detail = "L'insert n'a pas marché", Status = 500 });
    }
    else
    {
        if (userId == null || userId == "")
        {
            return Results.Problem(new ProblemDetails { Detail = "Utilisateur non connecté.", Status = 401 });

        }
        return Results.Problem(new ProblemDetails { Detail = "Code Ressource déjà existant", Status = 401 });
    }


});

// Read Ressources
app.MapGet("/GetAllRessource", async (IConfiguration _config, HttpContext http) =>
{
    var userId = http.Session.GetString("UserId");
    if (userId == null || userId == "")
    {
        http.Response.StatusCode = 401;
        await http.Response.WriteAsync("Utilisateur non connecté.");
        return;
    }

    var ok = new RessourcesRepos(_config).GetAllRessource(userId);

    http.Response.StatusCode = 200;
    await http.Response.WriteAsJsonAsync(ok);


});


app.MapGet("/GetByIdRessource/{idRessource}", async (IConfiguration _config, int id, HttpContext http) =>
{
    var userId = http.Session.GetString("UserId");
    if (userId == null)
    {
        http.Response.StatusCode = 401;
        await http.Response.WriteAsync("Utilisateur non connecté.");
        return;
    }

    var ok = new RessourcesRepos(_config).GetByIdRessource(id, userId);

    http.Response.StatusCode = 200;
    await http.Response.WriteAsJsonAsync(ok);
});

app.MapGet("/GetByCodeRessource/{codeRessource}", async (IConfiguration _config, string codeRessource, HttpContext http) =>
{
    var userId = http.Session.GetString("UserId");
    if (userId == null)
    {
        http.Response.StatusCode = 401;
        await http.Response.WriteAsync("Utilisateur non connecté.");
        return;
    }
    var ok = new RessourcesRepos(_config).GetByCodeRessource(codeRessource, userId);

    http.Response.StatusCode = 200;
    await http.Response.WriteAsJsonAsync(ok);

});

// Update Ressources 
app.MapPost("/UpdateRessource/{idRessource}", async (IConfiguration _config, RessourcesEntity Ressource, HttpContext http) =>
{
    var userId = http.Session.GetString("UserId");
    var code = Ressource.codeRessource.ToUpper();
    var existingRessource = await new RessourcesRepos(_config).ExistingRessource(code, userId);
    if ((userId != null || userId != "") && existingRessource == false)
    {
        Ressource.codeUtilisateur = userId;
        Ressource.codeRessource = code;
        var ok = new RessourcesRepos(_config).UpdateRessource(Ressource);
        return ok > 0 ? Results.NoContent() : Results.Problem(new ProblemDetails { Detail = "L'update n'a pas marché", Status = 500 });
    }
    else
    {
        if (userId == null || userId == "")
        {
            return Results.Problem(new ProblemDetails { Detail = "Utilisateur non connecté.", Status = 401 });

        }
        return Results.Problem(new ProblemDetails { Detail = "Code Ressource déjà existant", Status = 401 });
    }


});

// Delete Ressource 
app.MapDelete("/DeleteRessource/{idRessource}", (IConfiguration _config, int id, HttpContext http) =>
{
    var userId = http.Session.GetString("UserId");
    if (userId != null || userId != "")
    {
        var ok = new RessourcesRepos(_config).DeleteRessource(id, userId);
        return ok > 0 ? Results.Ok() : Results.Problem(new ProblemDetails { Detail = "Le delete n'a pas marché", Status = 500 });
    }
    else
    {
        return Results.Problem(new ProblemDetails { Detail = "Utilisateur non connecté.", Status = 401 });
    }

});

// Create Projet
app.MapPut("/CreateProjet", async (IConfiguration _config, ProjetsEntity Projet, HttpContext http) =>
{
    var userId = http.Session.GetString("UserId");
    var code = Projet.codeProjet.ToUpper();
    var existingProjet = await new ProjetsRepos(_config).ExistingProjet(code, userId);
    var oSqlConnection = new SqlConnection(_config?.GetConnectionString("SQL"));

    if ((userId != null || userId != "") && existingProjet == false)
    {
        Projet.codeUtilisateur = userId;
        Projet.creerPar = oSqlConnection.QueryFirstOrDefault<string>("Select nomUtilisateur from Utilisateurs where codeUtilisateur = @CodeUtilisateur", new { CodeUtilisateur = userId });
        Projet.codeProjet = code;
        //Projet.EtatDuProjet = "à faire";
        Projet.dateCreation = DateTime.Today;
        var ok = new ProjetsRepos(_config).InsertProjet(Projet);
        return (ok != -1) ? Results.Created($"/{ok}", Projet) : Results.Problem(new ProblemDetails { Detail = "L'insert n'a pas marché", Status = 500 });
    }
    else
    {
        if (userId == null || userId == "")
        {
            return Results.Problem(new ProblemDetails { Detail = "Utilisateur non connecté.", Status = 401 });

        }
        return Results.Problem(new ProblemDetails { Detail = "Code Projet déjà existant", Status = 401 });
    }


});

// Read Projets
app.MapGet("/GetAllProjet", async (IConfiguration _config, HttpContext http) =>
{
    var userId = http.Session.GetString("UserId");
    if (userId == null || userId == "")
    {
        http.Response.StatusCode = 401;
        await http.Response.WriteAsync("Utilisateur non connecté.");
        return;
    }

    var ok = new ProjetsRepos(_config).GetAllProjet(userId);

    http.Response.StatusCode = 200;
    await http.Response.WriteAsJsonAsync(ok);


});


app.MapGet("/GetByIdProjet/{idProjet}", async (IConfiguration _config, int id, HttpContext http) =>
{
    var userId = http.Session.GetString("UserId");
    if (userId == null)
    {
        http.Response.StatusCode = 401;
        await http.Response.WriteAsync("Utilisateur non connecté.");
        return;
    }

    var ok = new ProjetsRepos(_config).GetByIdProjet(id, userId);

    http.Response.StatusCode = 200;
    await http.Response.WriteAsJsonAsync(ok);
});

app.MapGet("/GetByCodeProjet/{codeProjet}", async (IConfiguration _config, string codeProjet, HttpContext http) =>
{
    var userId = http.Session.GetString("UserId");
    if (userId == null)
    {
        http.Response.StatusCode = 401;
        await http.Response.WriteAsync("Utilisateur non connecté.");
        return;
    }
    var ok = new ProjetsRepos(_config).GetByCodeProjet(codeProjet, userId);

    http.Response.StatusCode = 200;
    await http.Response.WriteAsJsonAsync(ok);

});

// Update Projets 
app.MapPost("/UpdateProjet/{idProjet}", async (IConfiguration _config, ProjetsEntity Projet, HttpContext http) =>
{
    var userId = http.Session.GetString("UserId");
    var code = Projet.codeProjet.ToUpper();
    var existingProjet = await new ProjetsRepos(_config).ExistingProjet(code, userId);
    if ((userId != null || userId != "") && existingProjet == false)
    {
        Projet.codeUtilisateur = userId;
        Projet.codeProjet = code;
        var ok = new ProjetsRepos(_config).UpdateProjet(Projet);
        return ok > 0 ? Results.NoContent() : Results.Problem(new ProblemDetails { Detail = "L'update n'a pas marché", Status = 500 });
    }
    else
    {
        if (userId == null || userId == "")
        {
            return Results.Problem(new ProblemDetails { Detail = "Utilisateur non connecté.", Status = 401 });

        }
        return Results.Problem(new ProblemDetails { Detail = "Code Projet déjà existant", Status = 401 });
    }


});

// Delete Projet 
app.MapDelete("/DeleteProjet/{idProjet}", (IConfiguration _config, int id, HttpContext http) =>
{
    var userId = http.Session.GetString("UserId");
    if (userId != null || userId != "")
    {
        var ok = new ProjetsRepos(_config).DeleteProjet(id, userId);
        return ok > 0 ? Results.Ok() : Results.Problem(new ProblemDetails { Detail = "Le delete n'a pas marché", Status = 500 });
    }
    else
    {
        return Results.Problem(new ProblemDetails { Detail = "Utilisateur non connecté.", Status = 401 });
    }

});



app.UseHttpsRedirection();

app.UseAuthorization();


app.Run();
