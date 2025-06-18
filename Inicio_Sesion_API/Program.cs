using Inicio_Sesion_API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Cors.Infrastructure; // Necesario para CORS
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Agregar servicios para los controladores
builder.Services.AddControllers();

// Agregar cliente HTTP
builder.Services.AddHttpClient();

// Agregar soporte para la documentación Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Cambiar la conexión a PostgreSQL usando Npgsql
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))); // Cambiado a PostgreSQL

// Configurar autenticación JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero,  // Tiempo exacto de expiración del token
            ValidIssuer = builder.Configuration["Jwt:Issuer"],  // Configurar el emisor del JWT
            ValidAudience = builder.Configuration["Jwt:Audience"],  // Configurar la audiencia del JWT
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])) // Clave secreta
        };
    });

// Agregar soporte para CORS (habilitar CORS)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.WithOrigins("http://localhost:4200") // Dirección de tu frontend Angular
              .AllowAnyMethod()    // Permite cualquier método (GET, POST, PUT, DELETE, etc.)
              .AllowAnyHeader();   // Permite cualquier encabezado
    });
});

var app = builder.Build();

app.Urls.Add("http://0.0.0.0:5000");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Aplicar CORS (usando la política creada)
app.UseCors("AllowLocalhost");

// Configurar los middlewares de autenticación y autorización
app.UseAuthentication();  
app.UseAuthorization();   

app.UseHttpsRedirection();

// Mapear los controladores
app.MapControllers();

app.Run();