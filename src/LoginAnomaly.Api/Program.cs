using LoginAnomaly.Infrastructure.Persistence;
using LoginAnomaly.Api.Auth;
using LoginAnomaly.Api.Hubs;
using LoginAnomaly.Domain.Detection;
using LoginAnomaly.Domain.Detection.Rules;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicy = "AngularDev";

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<IDetectionRule, BruteForceRule>();
builder.Services.AddScoped<IDetectionRule, ImpossibleTravelRule>();
builder.Services.AddScoped<IDetectionRule, VelocityRule>();
builder.Services.AddScoped<IDetectionRule, NewDeviceRule>();
builder.Services.AddScoped<IDetectionRule, UnusualTimeRule>();
builder.Services.AddScoped<AttackSimulator>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<RiskScorer>();
builder.Services.AddScoped<LoginPipelineService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
    options.AddPolicy(CorsPolicy, p => p
        .WithOrigins("http://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

var jwt = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!))
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(CorsPolicy);

app.UseAuthentication();

app.UseAuthorization();

app.MapHub<LoginAnomaly.Api.Hubs.MonitoringHub>("/hubs/monitoring");
app.MapControllers();

app.Run();
