using FinanceAPI.Application.Services;
using FinanceAPI.Domain.Interfaces;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using FinanceAPI.Infrastructure.Data;
using FinanceAPI.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using FinanceCoreLedger.BackgroundServices;

System.AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<HostOptions>(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(15);
});

// --- 1. LOGS E MONITORAMENTO ---
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// --- 2. SERVIÇOS BÁSICOS ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddHttpClient();

// --- 3. CONFIGURAÇÃO DE CACHE (REDIS) ---
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? Environment.GetEnvironmentVariable("ConnectionStrings__Redis");
if (!string.IsNullOrEmpty(redisConnectionString))
{
    // Adicionamos abortConnect=false para não falhar a aplicação (Graceful Degradation)
    if (!redisConnectionString.Contains("abortConnect=false"))
    {
        redisConnectionString += ",abortConnect=false";
    }

    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "Finance_";
    });
}
else
{
    builder.Services.AddDistributedMemoryCache(); // Fallback para desenvolvimento local sem Redis
}

// --- 4. CONFIGURAÇÃO DE CORS ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("ProducaoPolicy", policy =>
    {
        var origins = new List<string>
        {
            "https://guilhermerondon.com"
        };

        if (builder.Environment.IsDevelopment())
        {
            origins.Add("http://localhost:4200");
            origins.Add("http://localhost:3000");
            origins.Add("http://localhost:5173");
            // Adicione outras portas localhost conforme necessário para dev
        }

        policy.WithOrigins(origins.ToArray())
              .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS", "PATCH")
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// --- 5. BANCO DE DADOS (PostgreSQL Parser para URLs do Supabase/Render) ---
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrEmpty(databaseUrl))
{
    // Render/Supabase podem vir como postgresql:// ou postgres://
    databaseUrl = databaseUrl.Replace("postgresql://", "postgres://");
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':');

    // Decodificar caracteres especiais (como @ encodado como %40)
    var username = Uri.UnescapeDataString(userInfo[0]);
    var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
    var host = uri.Host;
    var port = uri.Port > 0 ? uri.Port : 5432;
    var database = uri.LocalPath.TrimStart('/');

    var connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};SslMode=Require;TrustServerCertificate=true;";

    builder.Services.AddDbContext<FinanceDbContext>(options => options.UseNpgsql(connectionString));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<FinanceDbContext>(options => options.UseNpgsql(connectionString));
}

// --- 6. IDENTITY E SEGURANÇA JWT ---
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<FinanceDbContext>()
    .AddDefaultTokenProviders();

var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
                ?? Environment.GetEnvironmentVariable("JWT_SECRET")
                ?? "ChaveDeSegurancaReservaParaEvitarErros123!";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

// --- 7. INJEÇÃO DE DEPENDÊNCIA ---
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<RabbitMqPublisher>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddHostedService<DataRetentionWorker>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 60, // Máximo de requisições permitidas
                QueueLimit = 0,   // Não enfileirar, rejeitar imediatamente
                Window = TimeSpan.FromMinutes(1) // Janela de tempo
            }));
});

var app = builder.Build();

var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() =>
{
    // Aqui garantimos que o log registre exatamente o momento do shutdown
    app.Logger.LogInformation("Sinal de desligamento recebido. Iniciando Graceful Shutdown: aguardando finalização das transações e fechando conexões...");
});

// --- 8. SINCRONIZAÇÃO DE BANCO (O ajuste crítico) ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<FinanceDbContext>();

        // Aplica as migrations pendentes para manter o esquema atualizado no Supabase.
        context.Database.Migrate();

        Console.WriteLine($"Tabelas mapeadas: {string.Join(", ", context.Model.GetEntityTypes().Select(t => t.GetTableName()))}");
        Console.WriteLine("🚀 Infraestrutura PostgreSQL: Tabelas Identity e Finance sincronizadas.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Erro crítico na infraestrutura: {ex.Message}");
    }
}



// --- 9. PIPELINE DE MIDDLEWARE (Ordem de Execução) ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDeveloperExceptionPage();
app.UseRouting();

// O CORS deve vir obrigatoriamente antes da Autenticação
app.UseCors("ProducaoPolicy");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health Check oficial para monitoramento (Railway / Watchdog)
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();