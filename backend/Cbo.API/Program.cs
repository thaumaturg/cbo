using System.Text;
using Cbo.API.Data;
using Cbo.API.Mappings;
using Cbo.API.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

namespace Cbo.API;

public class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        string connectionString = builder.Configuration.GetConnectionString("CboDb")
            ?? throw new InvalidOperationException("Connection string" + "'CboDb' not found.");

        builder.Services.AddDbContext<CboDbContext>(options =>
            options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());

        string connectionStringAuth = builder.Configuration.GetConnectionString("CboAuthDb")
            ?? throw new InvalidOperationException("Connection string" + "'CboAuthDb' not found.");

        builder.Services.AddDbContext<CboAuthDbContext>(options =>
            options.UseNpgsql(connectionStringAuth).UseSnakeCaseNamingConvention());

        builder.Services.AddScoped<ITournamentRepository, PostgresTournamentRepository>();
        builder.Services.AddScoped<ITopicRepository, PostgresTopicRepository>();
        builder.Services.AddScoped<IMatchRepository, PostgresMatchRepository>();
        builder.Services.AddScoped<IRoundRepository, PostgresRoundRepository>();
        builder.Services.AddScoped<IRoundAnswerRepository, PostgresRoundAnswerRepository>();
        builder.Services.AddScoped<ITokenRepository, TokenRepository>();

        builder.Services.AddAutoMapper(typeof(AutoMapperProfiles));

        builder.Services.AddControllers();
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi("v1", options =>
        {
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
        });

        builder.Services.AddIdentityCore<IdentityUser>()
            .AddRoles<IdentityRole>()
            .AddTokenProvider<DataProtectorTokenProvider<IdentityUser>>("Cbo")
            .AddEntityFrameworkStores<CboAuthDbContext>()
            .AddDefaultTokenProviders();

        builder.Services.Configure<IdentityOptions>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 8;
            options.Password.RequiredUniqueChars = 1;
        });

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudiences = new[] { builder.Configuration["Jwt:Audience"] },
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
                });

        WebApplication app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapScalarApiReference();
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        app.UseAuthentication();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
