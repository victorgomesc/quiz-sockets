using System.Text; // 🔽 ADICIONADO
using Microsoft.AspNetCore.Authentication.JwtBearer; // 🔽 ADICIONADO
using Microsoft.EntityFrameworkCore; // 🔽 ADICIONADO
using Microsoft.IdentityModel.Tokens; // 🔽 ADICIONADO
using Quiz.Api.Security; // 🔽 ADICIONADO
using Quiz.Application.Abstractions;
using Quiz.Application.Services;
using Quiz.Infrastructure.Persistence; // 🔽 ADICIONADO
using Quiz.Infrastructure.Repositories; // 🔽 ADICIONADO

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// 🔽 ADICIONADO — DbContext (por enquanto InMemory)
builder.Services.AddDbContext<QuizDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));


// 🔽 ADICIONADO — Repositórios
builder.Services.AddScoped<IUserRepository, UserRepository>();


// 🔽 ADICIONADO — JWT Service
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddScoped<IMatchRepository, MatchRepository>();
builder.Services.AddScoped<IRankingRepository, RankingRepository>();
builder.Services.AddScoped<MatchReportingService>();


// 🔽 ADICIONADO — Autenticação JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
            )
        };
    });


// 🔽 ADICIONADO — Autorização
builder.Services.AddAuthorization();


// 🔽 ADICIONADO — Controllers (necessário para AuthController)
builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("FrontendPolicy");



// 🔽 ADICIONADO — Middleware de Auth (ORDEM IMPORTANTE)
app.UseAuthentication();
app.UseAuthorization();


var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();


// 🔽 ADICIONADO — Mapeia Controllers (AuthController, UsersController etc.)
app.MapControllers();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
