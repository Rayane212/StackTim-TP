using StackTim_TP.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Dapper;

var builder = WebApplication.CreateBuilder(args);
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.AllowAnyOrigin();
                      });
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
}

// Create Connaissance
app.MapPut("/CreateConnaissance", (IConfiguration _config,ConnaissanceEntity ce) =>
{
    var ok = new ConnaissanceRepos(_config).Insert(ce);
    return (ok != -1) ? Results.Created($"/{ok}", ce) : Results.Problem(new ProblemDetails { Detail = "L'insert n'a pas marché", Status = 500 });

});

// Read Connaissances
app.MapGet("/GetAllConnaissance", (IConfiguration _config) =>
{
    
        var ok = new ConnaissanceRepos(_config).GetAllConnaissance();
        return ok;
    
});

app.MapGet("/GetByIdConnaissance/{idConnaissance}", (IConfiguration _config, int id) =>
{
    var ce = new ConnaissanceRepos(_config).GetByIdConnaissance(id);
    return ce;
});

app.MapGet("/GetByCodeConnaissance/{codeConnaissance}", (IConfiguration _config, string codeConnaissance) =>
{
    var ce = new ConnaissanceRepos(_config).GetByCodeConnaissance(codeConnaissance);
    return ce;

});



// Update Connaissances 
app.MapPost("/UpdateConnaissance/{idConnaissance}", (IConfiguration _config, ConnaissanceEntity ce) =>
{
    var ok = new ConnaissanceRepos(_config).UpdateConnaissance(ce);
    return ok > 0 ? Results.NoContent() : Results.Problem(new ProblemDetails { Detail = "L'update n'a pas marché", Status = 500 });
});

// Delete Connaissance 
app.MapDelete("/DeleteConnaissance/{idConnaissance}", (IConfiguration _config, int id) =>
{
    var ok = new ConnaissanceRepos(_config).DeleteConnaissance(id);
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
