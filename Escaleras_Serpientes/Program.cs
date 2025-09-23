using Escaleras_Serpientes.Hubs;
using Escaleras_Serpientes.Services.Auth;
using Escaleras_Serpientes.Services.Player;
using Escaleras_Serpientes.Services.Resume;
using Escaleras_Serpientes.Services.Room;
using Escaleras_Serpientes.SnakesLaddersDataBase;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────────────────────────────────────
// Services
// ─────────────────────────────────────────────────────────────────────────────
builder.Services.AddSignalR(o => o.EnableDetailedErrors = true);
builder.Services.AddSingleton<ConnectionRegistryGame>();

// DbContext (usa el provider real que tengas: UseSqlServer / UseMySql / etc.)
// Si ya lo configuras dentro del DbContext, puedes dejar AddDbContext sin opciones.
// builder.Services.AddDbContext<SnakesLaddersDbContext>(opt =>
//     opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<SnakesLaddersDbContext>();

builder.Services.AddScoped<IPlayerService, PlayerService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IResumeService, ResumeService>();
builder.Services.AddSingleton<IGameService, GameService>();


builder.Services
    .AddControllers()
    .AddNewtonsoftJson(x =>
        x.SerializerSettings.ReferenceLoopHandling =
            Newtonsoft.Json.ReferenceLoopHandling.Ignore);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS: para desarrollo, lista de orígenes que SÍ pueden conectarse con credenciales.
// IMPORTANTE: AllowCredentials NO puede usarse con AllowAnyOrigin.
// Agrega aquí los orígenes de tu front (Vite) y otros que necesites.
builder.Services.AddCors(options =>
{
    options.AddPolicy(MyAllowSpecificOrigins, policy =>
    {
        policy
            .WithOrigins(
                "http://26.249.132.191:5173",
                "http://localhost:5173",
                "http://127.0.0.1:5173",
                "http://26.9.80.46:5173"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Auth: JWT + soporte para SignalR (token por querystring `access_token`)
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "yourdomain.com",
            ValidAudience = "yourdomain.com",
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("your_super_secret_key_your_super_secret_key"))
        };

        // 🧩 Necesario para WebSockets de SignalR: leer el token desde la query
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];

                // Si la petición es para el hub, usa el token de la query
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/gameHub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ─────────────────────────────────────────────────────────────────────────────
// App
// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Orden recomendado de middlewares
app.UseHttpsRedirection();

app.UseRouting();

app.UseCors(MyAllowSpecificOrigins); // <- antes de auth para que aplique a WS

app.UseAuthentication();
app.UseAuthorization();

// Endpoints
app.MapControllers();
app.MapHub<GameHub>("/gameHub");

app.Run();
