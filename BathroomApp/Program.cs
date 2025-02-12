using BathroomApp.Service;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Inicializa el builder con los argumentos de la l�nea de comandos.
var builder = WebApplication.CreateBuilder(args);

// Agregar servicios al contenedor.

// Registra un servicio singleton para almacenar el estado del ba�o.
// (La interfaz y la implementaci�n se encuentran en BathroomApp.Service)
builder.Services.AddSingleton<IBathroomService, BathroomService>();

// Agrega SignalR para notificaciones en tiempo real.
builder.Services.AddSignalR();

// Configura CORS para permitir solicitudes desde cualquier origen (ajusta seg�n necesites).
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("http://localhost:5173", builder.Configuration.GetValue<string>("BackEndURL") ?? "https://localhost:7131")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Agrega controladores (Controllers).
builder.Services.AddControllers();

// Configuraci�n de Swagger/OpenAPI.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Construye la aplicaci�n.
var app = builder.Build();

// Configura el pipeline HTTP.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Habilita CORS con la pol�tica definida.
app.UseCors("AllowAll");

app.UseAuthorization();

// Mapea los controladores.
app.MapControllers();

// Mapea el hub de SignalR en la ruta /bathroomHub.
app.MapHub<BathroomHub>("/bathroomHub");

// Ejecuta la aplicaci�n.
app.Run();


// Si a�n no has movido BathroomHub a otro archivo, puedes definirlo aqu�:
public class BathroomHub : Hub
{
    // M�todo para que el cliente se una a un grupo usando su userId
    public async Task JoinGroup(string userId)
    {
        // Agrega la conexi�n actual al grupo con el nombre del userId
        await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        Console.WriteLine($"Connection {Context.ConnectionId} joined group {userId}");
    }
}