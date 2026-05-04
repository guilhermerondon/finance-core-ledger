using FinanceAPI.Application.Services;
using FinanceAPI.Domain.Interfaces;
using FinanceAPI.Infrastructure.Data;
using FinanceAPI.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// --- 1. LOGS ---
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// --- 2. CONTROLLERS ---
builder.Services.AddControllers();

// --- 3. CORS (Sincronizado com Vercel) ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("VercelPolicy", policy =>
    {
        var frontendUrlEnv = Environment.GetEnvironmentVariable("URL_FRONTEND");
        var origins = new List<string> 
        { 
            "http://localhost:4200", 
            "https://guilhermerondon-interface.vercel.app" 
        };
        
        if (!string.IsNullOrEmpty(frontendUrlEnv))
        {
            origins.Add(frontendUrlEnv.TrimEnd('/'));
        }
        
        policy.WithOrigins(origins.ToArray())
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// --- 4. INJEÇÃO DE DEPENDÊNCIA ---
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<TokenService>();

// --- 5. BANCO DE DADOS (PostgreSQL Railway) ---
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrEmpty(databaseUrl))
{
    databaseUrl = databaseUrl.Replace("postgresql://", "postgres://");
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':');
    var connectionString = $"Host={uri.Host};Port={(uri.Port > 0 ? uri.Port : 5432)};Database={uri.LocalPath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SslMode=Require;TrustServerCertificate=true;";
    
    builder.Services.AddDbContext<FinanceDbContext>(options => options.UseNpgsql(connectionString));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=finance.db";
    builder.Services.AddDbContext<FinanceDbContext>(options => options.UseSqlite(connectionString));
}

// --- 6. IDENTITY E JWT ---
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

// --- 7. SWAGGER ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --- 8. AUTO-MIGRATION (A correção para o erro de tabelas faltando) ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<FinanceDbContext>();
        // Cria o banco e as tabelas (AspNetUsers, etc) se não existirem
        context.Database.Migrate(); 
        Console.WriteLine("🚀 Banco de dados sincronizado com sucesso!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Erro ao migrar banco: {ex.Message}");
    }
}

// --- 9. MIDDLEWARES (Ordem Crítica) ---
app.UseDeveloperExceptionPage(); 
app.UseRouting();
app.UseCors("VercelPolicy"); // Sempre antes da Auth
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", db = "Ready" }));

app.Run();