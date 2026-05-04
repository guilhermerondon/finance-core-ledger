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

// --- 1. CONFIGURAÇÃO DE LOGS (Para debugar o Erro 500 no Railway) ---
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// --- 2. SERVIÇOS E CONTROLLERS ---
builder.Services.AddControllers();

// --- 3. CONFIGURAÇÃO DE CORS (Blindada contra erros de Origin) ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("VercelPolicy", policy =>
    {
        var frontendUrlEnv = Environment.GetEnvironmentVariable("URL_FRONTEND");
        
        var origins = new List<string> 
        { 
            "http://localhost:4200", 
            "https://guilhermerondon-interface.vercel.app" // URL Oficial Fixa
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

// --- 5. BANCO DE DADOS POSTGRESQL (Railway) ---
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrEmpty(databaseUrl))
{
    // Limpeza da URL para o formato Npgsql
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

// --- 6. IDENTITY E AUTENTICAÇÃO JWT ---
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<FinanceDbContext>()
    .AddDefaultTokenProviders();

// Sincronizando com a variável exata do seu Railway
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
        ValidateIssuer = false, // Facilitando em produção inicial
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

// --- 7. SWAGGER ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});

var app = builder.Build();

// --- 8. PIPELINE DE MIDDLEWARE (ORDEM CRÍTICA) ---

// Em produção, queremos ver o que deu errado nos logs do Railway
app.UseDeveloperExceptionPage(); 

app.UseRouting();

// O CORS precisa vir ANTES de Authentication e Authorization
app.UseCors("VercelPolicy"); 

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", time = DateTime.UtcNow }));

app.Run();