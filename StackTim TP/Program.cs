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

app.MapPut("/CreateConnaissances", (IConfiguration _config,ConnaissanceEntity ce) =>
{
    var ok = new ConnaissanceRepos(_config).Insert(ce);
    return (ok != -1) ? Results.Created($"/{ok}", ce) : Results.Problem(new ProblemDetails { Detail = "L'insert n'a pas marché", Status = 500 });

});

app.MapGet("/GetAllConnaissance", (IConfiguration _config) =>
{
    var ce = new ConnaissanceEntity();
    var oSqlConnection = new SqlConnection(_config?.GetConnectionString("SQL"));
    return oSqlConnection.Query("Select * from Connaissance");
});
app.UseHttpsRedirection();

app.UseAuthorization();


app.Run();
