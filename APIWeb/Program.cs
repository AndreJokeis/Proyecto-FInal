using APIWeb.Data;
using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);

// Configuración de la Base de Datos
builder.Services.AddDbContext<ContextoBD>(options =>
    options.UseMySQL(builder.Configuration.GetConnectionString("ConexionMySQL")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()   // Peticiones desde cualquier origen
                  .AllowAnyHeader()   // Cabeceras HTTP permitidas
                  .AllowAnyMethod();  // GET, POST, PUT, DELETE
        });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseStaticFiles();


app.MapControllers();

app.Run();

