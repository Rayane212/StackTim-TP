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
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;
using Newtonsoft.Json;
using StackTim_TP;

var builder = WebApplication.CreateBuilder(args);
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.WithOrigins("*").AllowAnyHeader().AllowAnyMethod();

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
app.UseCors(MyAllowSpecificOrigins);
// SignIn 
app.MapPost("/signIn", async (IConfiguration _config, HttpContext http) =>
{
    try
    {
        // Récupération des identifiants de l'utilisateur
        string username = http.Request.Form["nomUtilisateur"];
        string password = http.Request.Form["MotDePasse"];

        // Vérifier que les identifiants sont valides
        using var connection = new SqlConnection(builder.Configuration.GetConnectionString("SQL"));
        var user = await connection.QuerySingleOrDefaultAsync<UtilisateursEntity>(
            "SELECT * FROM Utilisateurs WHERE EmailUtilisateur = @EmailUtilisateur OR nomUtilisateur = @nomUtilisateur AND MotDePasse = @MotDePasse", new { EmailUtilisateur = username, nomUtilisateur = username, MotDePasse = password });
        if (user == null)
        {
            http.Response.StatusCode = 401; // Code HTTP 401 Unauthorized
            await http.Response.WriteAsync("Adresse email ou mot de passe incorrect.");
            return;
        }

        // Création du token d'authentification
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_config["JwtConfig:Secret"]);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.codeUtilisateur.ToString()),
                new Claim(ClaimTypes.Name, user.nomUtilisateur),
                new Claim(ClaimTypes.Email, user.emailUtilisateur),
            }),
            Expires = DateTime.UtcNow.AddHours(2),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

      
        // Retourner le token d'authentification dans la réponse du serveur
        http.Response.StatusCode = 200;
        http.Response.ContentType = "application/json";
        await http.Response.WriteAsync(JsonConvert.SerializeObject(new { Token = tokenString }));
    }
    catch (Exception ex)
    {
        http.Response.StatusCode = 400; // Code HTTP 400 Bad Request
        await http.Response.WriteAsync(ex.Message);
        return;
    }
});



app.MapPut("/signUp", async (IConfiguration _config, HttpContext http, UtilisateursEntity utilisateur) =>
{
    var userId = utilisateur.nomUtilisateur.ToUpper() + new Guid(Guid.NewGuid().ToString());
    using var connection = new SqlConnection(builder.Configuration.GetConnectionString("SQL"));
    var existingUser = await connection.QuerySingleOrDefaultAsync<UtilisateursEntity>(
        "SELECT * FROM Utilisateurs WHERE EmailUtilisateur = @EmailUtilisateur OR nomUtilisateur = @nomUtilisateur", new { utilisateur.emailUtilisateur, utilisateur.nomUtilisateur });
    if (existingUser != null)
    {
        http.Response.StatusCode = 409; // Code HTTP 409 Conflict
        await http.Response.WriteAsync("Un utilisateur avec cette adresse email ou ce nom d'utilisateur existe déjà.");
        return;
    }
    await connection.QueryAsync<UtilisateursEntity>(
        "INSERT INTO Utilisateurs (nomUtilisateur, EmailUtilisateur, MotDePasse, codeUtilisateur) VALUES (@nomUtilisateur, @EmailUtilisateur, @MotDePasse, @codeUtilisateur)", new { utilisateur.nomUtilisateur, utilisateur.emailUtilisateur, utilisateur.motDePasse, codeUtilisateur = userId });


    http.Response.StatusCode = 200; // Code HTTP 200 OK
    await http.Response.WriteAsync($"Utilisateur {userId} créé avec succès.");
});


// logout
app.MapPost("/logout", async (HttpContext http, IConfiguration _config) =>
{
    var token = http.Request.Headers["Authorization"].ToString().Split(" ")[1];
    var claims = JwtUtils.DecodeJwt(token, _config["JwtConfig:Secret"]);
    var userId = claims[ClaimTypes.NameIdentifier];
    if (token != "")
    {
        http.Request.Headers.Remove("Authorization");
        http.Response.Redirect("/");
        http.Response.StatusCode = 200;
        await http.Response.WriteAsync($"Utilisateur {userId} a été déconnecté.");
    }
    else
    {
        http.Response.StatusCode = 409;
        await http.Response.WriteAsync("Il y'a eu un problème.");
        return;
    }

});


// Create Connaissance
app.MapPut("/CreateConnaissance",async (IConfiguration _config, ConnaissanceEntity connaissance, HttpContext http) =>
{
    var token = http.Request.Headers["Authorization"].ToString().Split(" ")[1];
    var claims = JwtUtils.DecodeJwt(token, _config["JwtConfig:Secret"]);
    var userId = claims[ClaimTypes.NameIdentifier]; 
    var code = connaissance.codeConnaissance.ToUpper();
    var existingConnaissance = await new ConnaissanceRepos(_config).ExistingConnaissance(code, userId);
    if ( (userId != null || userId != "") && existingConnaissance == false)
    {
        connaissance.codeUtilisateur = userId;
        connaissance.codeConnaissance = code.ToUpper();
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
    //var userId = http.Session.GetString("UserId");
    var token = http.Request.Headers["Authorization"].ToString().Split(" ")[1]; 

    var claims = JwtUtils.DecodeJwt(token, _config["JwtConfig:Secret"]);
    var userId = claims[ClaimTypes.NameIdentifier]; 

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


app.MapGet("/GetByIdConnaissance/{idConnaissance}", async (IConfiguration _config,  HttpContext http, int idConnaissance) =>
{
     var token = http.Request.Headers["Authorization"].ToString().Split(" ")[1];
    var claims = JwtUtils.DecodeJwt(token, _config["JwtConfig:Secret"]);
    var userId = claims[ClaimTypes.NameIdentifier];

    if (userId == null)
    {
        http.Response.StatusCode = 401;
        await http.Response.WriteAsync("Utilisateur non connecté.");
        return;
    }
    
    var ok = new ConnaissanceRepos(_config).GetByIdConnaissance(idConnaissance, userId);

    http.Response.StatusCode = 200;
    await http.Response.WriteAsJsonAsync(ok);
});

app.MapGet("/GetByCodeConnaissance/{codeConnaissance}", async (IConfiguration _config, string codeConnaissance, HttpContext http) =>
{
    var token = http.Request.Headers["Authorization"].ToString().Split(" ")[1];
    var claims = JwtUtils.DecodeJwt(token, _config["JwtConfig:Secret"]);
    var userId = claims[ClaimTypes.NameIdentifier]; if (userId == null)
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
app.MapPost("/UpdateConnaissance/{idConnaissance}", async (IConfiguration _config, ConnaissanceEntity connaissance, HttpContext http, int idConnaissance) =>
{
    var token = http.Request.Headers["Authorization"].ToString().Split(" ")[1];
    var claims = JwtUtils.DecodeJwt(token, _config["JwtConfig:Secret"]);
    var userId = claims[ClaimTypes.NameIdentifier]; 
    var code = connaissance.codeConnaissance.ToUpper();
    var existingConnaissance = await new ConnaissanceRepos(_config).RedondanceConnaissance(code, userId);
    Console.WriteLine(connaissance.idConnaissance);

    if ((userId != null || userId != "") && existingConnaissance == false)
    {
        connaissance.codeUtilisateur = userId;
        connaissance.codeConnaissance = code;
        connaissance.idConnaissance = idConnaissance;
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

// Get Code Utilisateur 
app.MapGet("/GetCodeUtilisateur", async (IConfiguration _config, HttpContext http) =>
{
    var token = http.Request.Headers["Authorization"].ToString().Split(" ")[1];
    var claims = JwtUtils.DecodeJwt(token, _config["JwtConfig:Secret"]);
    var userId = claims[ClaimTypes.NameIdentifier];

    var response = new { codeUtilisateur = userId };
    var jsonResponse = JsonConvert.SerializeObject(response);

    http.Response.ContentType = "application/json";
    await http.Response.WriteAsync(jsonResponse);
});

// Delete Connaissance 
app.MapDelete("/DeleteConnaissance/{idConnaissance}", (IConfiguration _config, HttpContext http, int idConnaissance) =>
{
    var token = http.Request.Headers["Authorization"].ToString().Split(" ")[1];
    var claims = JwtUtils.DecodeJwt(token, _config["JwtConfig:Secret"]);
    var userId = claims[ClaimTypes.NameIdentifier]; 
    if ((userId != null || userId != "") && idConnaissance != 0)
    {
        var ok = new ConnaissanceRepos(_config).DeleteConnaissance(idConnaissance, userId);
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
    var token = http.Request.Headers["Authorization"].ToString().Split(" ")[1];
    var claims = JwtUtils.DecodeJwt(token, _config["JwtConfig:Secret"]);
    var userId = claims[ClaimTypes.NameIdentifier]; var code = categorie.codeCategorie.ToUpper();

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
    var token = http.Request.Headers["Authorization"].ToString().Split(" ")[1];
    var claims = JwtUtils.DecodeJwt(token, _config["JwtConfig:Secret"]);
    var userId = claims[ClaimTypes.NameIdentifier]; 
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

app.MapGet("/GetByIdCategorie/{idCategorie}", async (IConfiguration _config, int idCategorie, HttpContext http) =>
{

    var token = http.Request.Headers["Authorization"].ToString().Split(" ")[1];
    var claims = JwtUtils.DecodeJwt(token, _config["JwtConfig:Secret"]);
    var userId = claims[ClaimTypes.NameIdentifier]; 
    if (userId == null || userId == "")

    {
        http.Response.StatusCode = 401;
        await http.Response.WriteAsync("Utilisateur non connecté.");
        return;
    }

    var ok = new CategorieRepos(_config).GetByIdCategorie(idCategorie, userId);
    http.Response.StatusCode = 200;
    await http.Response.WriteAsJsonAsync(ok);
});

app.MapGet("/GetByCodeCategorie/{codeCategorie}", async (IConfiguration _config, string codeCategorie, HttpContext http) =>
{

    var token = http.Request.Headers["Authorization"].ToString().Split(" ")[1];
    var claims = JwtUtils.DecodeJwt(token, _config["JwtConfig:Secret"]);
    var userId = claims[ClaimTypes.NameIdentifier]; 
    if (userId == null || userId == "")

    {
        http.Response.StatusCode = 401;
        await http.Response.WriteAsync("Utilisateur non connecté.");
        return;
    }

    var ok = new CategorieRepos(_config).GetByCodeCategorie(codeCategorie);
    
    http.Response.StatusCode = 200;
    await http.Response.WriteAsJsonAsync(ok);

});

// Update Categorie 
app.MapPost("/UpdateCategorie/{idCategorie}",async  (IConfiguration _config, CategorieEntity categorie, HttpContext http, int idCategorie) =>
{
    var token = http.Request.Headers["Authorization"].ToString().Split(" ")[1];
    var claims = JwtUtils.DecodeJwt(token, _config["JwtConfig:Secret"]);
    var userId = claims[ClaimTypes.NameIdentifier]; 
    var code = categorie.codeCategorie.ToUpper();
    var existingCategorie = await new CategorieRepos(_config).ExistingCategorie(code, userId);

    if ((userId != null || userId != "") && existingCategorie == false)
    {
        categorie.codeUtilisateur = userId;
        categorie.codeCategorie = code; 
        categorie.idCategorie = idCategorie;
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
    var token = http.Request.Headers["Authorization"].ToString().Split(" ")[1];
    var claims = JwtUtils.DecodeJwt(token, _config["JwtConfig:Secret"]);
    var userId = claims[ClaimTypes.NameIdentifier]; if (userId != null || userId != "")
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
    var token = http.Request.Headers["Authorization"].ToString().Split(" ")[1];
    var claims = JwtUtils.DecodeJwt(token, _config["JwtConfig:Secret"]);
    var userId = claims[ClaimTypes.NameIdentifier]; var code = Ressource.codeRessource.ToUpper();
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
    var token = http.Request.Headers["Authorization"].ToString().Split(" ")[1];
    var claims = JwtUtils.DecodeJwt(token, _config["JwtConfig:Secret"]);
    var userId = claims[ClaimTypes.NameIdentifier]; if (userId == null || userId == "")
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
    var token = http.Request.Headers["Authorization"].ToString().Split(" ")[1];
    var claims = JwtUtils.DecodeJwt(token, _config["JwtConfig:Secret"]);
    var userId = claims[ClaimTypes.NameIdentifier]; if (userId == null)
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
    var token = http.Request.Headers["Authorization"].ToString().Split(" ")[1];
    var claims = JwtUtils.DecodeJwt(token, _config["JwtConfig:Secret"]);
    var userId = claims[ClaimTypes.NameIdentifier]; if (userId == null)

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
    var token = http.Request.Headers["Authorization"].ToString().Split(" ")[1];
    var claims = JwtUtils.DecodeJwt(token, _config["JwtConfig:Secret"]);
    var userId = claims[ClaimTypes.NameIdentifier]; var code = Ressource.codeRessource.ToUpper();
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
    var token = http.Request.Headers["Authorization"].ToString().Split(" ")[1];
    var claims = JwtUtils.DecodeJwt(token, _config["JwtConfig:Secret"]);
    var userId = claims[ClaimTypes.NameIdentifier]; if (userId != null || userId != "")
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
    var token = http.Request.Headers["Authorization"].ToString().Split(" ")[1];
    var claims = JwtUtils.DecodeJwt(token, _config["JwtConfig:Secret"]);
    var userId = claims[ClaimTypes.NameIdentifier]; var code = Projet.codeProjet.ToUpper();
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
    var token = http.Request.Headers["Authorization"].ToString().Split(" ")[1];
    var claims = JwtUtils.DecodeJwt(token, _config["JwtConfig:Secret"]);
    var userId = claims[ClaimTypes.NameIdentifier]; if (userId == null || userId == "")
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
    var token = http.Request.Headers["Authorization"].ToString().Split(" ")[1];
    var claims = JwtUtils.DecodeJwt(token, _config["JwtConfig:Secret"]);
    var userId = claims[ClaimTypes.NameIdentifier]; if (userId == null)
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
    var token = http.Request.Headers["Authorization"].ToString().Split(" ")[1];
    var claims = JwtUtils.DecodeJwt(token, _config["JwtConfig:Secret"]);
    var userId = claims[ClaimTypes.NameIdentifier]; if (userId == null)
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
    var token = http.Request.Headers["Authorization"].ToString().Split(" ")[1];
    var claims = JwtUtils.DecodeJwt(token, _config["JwtConfig:Secret"]);
    var userId = claims[ClaimTypes.NameIdentifier]; var code = Projet.codeProjet.ToUpper();
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
    var token = http.Request.Headers["Authorization"].ToString().Split(" ")[1];
    var claims = JwtUtils.DecodeJwt(token, _config["JwtConfig:Secret"]);
    var userId = claims[ClaimTypes.NameIdentifier];
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
