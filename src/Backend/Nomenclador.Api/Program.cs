using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using NHibernate;
using Nomenclador.Api.Data;
using Nomenclador.Api.Mappers;
using Nomenclador.Api.Middleware;
using Nomenclador.Api.Repositories;
using Nomenclador.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// No-op cuando se corre con `dotnet run`/consola; activa el hosting como Windows Service
// (Event Log, content root correcto) cuando el ejecutable se registra con sc.exe/services.msc.
builder.Host.UseWindowsService(options => options.ServiceName = "NomencladorApi");

builder.Services.AddControllers();

// Orígenes fijos para desarrollo local + orígenes adicionales desde config
// (appsettings.Production.json) para el/los frontend(s) desplegados.
var corsOrigins = new[]
{
    "http://localhost:5173",
    "http://127.0.0.1:5173",
    "http://localhost:4173",
    "http://127.0.0.1:4173",
}.Concat(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("VueClient", policy =>
    {
        policy
            .WithOrigins(corsOrigins)
            .AllowCredentials()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("La cadena de conexión 'DefaultConnection' no está configurada.");

// ISessionFactory como singleton (costoso de construir, se crea una sola vez)
builder.Services.AddSingleton<ISessionFactory>(_ =>
    NHibernateSessionFactory.Build(connectionString));

// ISession como scoped (una sesión por request HTTP)
builder.Services.AddScoped(provider =>
    provider.GetRequiredService<ISessionFactory>().OpenSession());

builder.Services.AddScoped<ConfiguracionNomencladorRepository>();
builder.Services.AddScoped<ConceptoRepository>();
builder.Services.AddScoped<CatalogRepository>();
builder.Services.AddScoped<ConfiguracionNomencladorMapper>();
builder.Services.AddScoped<ValidacionConfiguracionService>();
builder.Services.AddScoped<ClonadoConfiguracionService>();
builder.Services.AddScoped<ConfiguracionNomencladorService>();
builder.Services.AddHttpClient<SeguridadService>(c => c.Timeout = TimeSpan.FromSeconds(30))
    // svr-v-patri es interno; el proxy corporativo por defecto del sistema lo intercepta y lo bloquea (Squid ERR_ACCESS_DENIED).
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseProxy = false });

// El token lo emitimos nosotros (SeguridadService) tras validar credenciales contra el servicio
// externo; esta clave es puramente nuestra, no depende de nada compartido con svr-v-patri.
var jwtSigningKey = builder.Configuration["Jwt:SigningKey"];
if (string.IsNullOrWhiteSpace(jwtSigningKey))
{
    throw new InvalidOperationException("La configuración 'Jwt:SigningKey' no está definida.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                context.Token = context.Request.Cookies["nomenclador.auth"];
                return Task.CompletedTask;
            },
        };
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseMiddleware<ApiExceptionMiddleware>();
app.UseCors("VueClient");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers().RequireAuthorization();

app.Run();
