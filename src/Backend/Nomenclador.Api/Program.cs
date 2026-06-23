using NHibernate;
using Nomenclador.Api.Data;
using Nomenclador.Api.Mappers;
using Nomenclador.Api.Middleware;
using Nomenclador.Api.Repositories;
using Nomenclador.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("VueClient", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173",
                "http://localhost:4173",
                "http://127.0.0.1:4173")
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

var app = builder.Build();

app.UseMiddleware<ApiExceptionMiddleware>();
app.UseCors("VueClient");
app.MapControllers();

app.Run();
