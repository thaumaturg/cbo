using System.Text;
using System.Text.Json.Serialization;
using Cbo.API.Authorization;
using Cbo.API.Data;
using Cbo.API.Mappings;
using Cbo.API.Models.Domain;
using Cbo.API.Repositories;
using Cbo.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
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

        builder.Services.AddScoped<ITournamentRepository, PostgresTournamentRepository>();
        builder.Services.AddScoped<ITournamentParticipantsRepository, PostgresTournamentParticipantsRepository>();
        builder.Services.AddScoped<ITournamentTopicRepository, PostgresTournamentTopicRepository>();
        builder.Services.AddScoped<ITopicRepository, PostgresTopicRepository>();
        builder.Services.AddScoped<ITopicAuthorRepository, PostgresTopicAuthorRepository>();
        builder.Services.AddScoped<IMatchRepository, PostgresMatchRepository>();
        builder.Services.AddScoped<IRoundRepository, PostgresRoundRepository>();
        builder.Services.AddScoped<ITokenRepository, TokenRepository>();

        builder.Services.AddScoped<IAuthorizationHandler, TopicAuthorizationHandler>();
        builder.Services.AddScoped<IAuthorizationHandler, TournamentAuthorizationHandler>();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
        builder.Services.AddScoped<IMatchGenerationService, MatchGenerationService>();
        builder.Services.AddScoped<IRoundService, RoundService>();

        builder.Services.AddAutoMapper(typeof(AutoMapperProfiles));

        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi("v1", options =>
        {
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
        });

        builder.Services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole<int>>()
            .AddTokenProvider<DataProtectorTokenProvider<ApplicationUser>>("Cbo")
            .AddEntityFrameworkStores<CboDbContext>()
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

        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseAuthentication();

        app.UseAuthorization();

        app.MapControllers();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapScalarApiReference();
            app.MapOpenApi();

            app.MapWhen(context =>
                !context.Request.Path.StartsWithSegments("/api") &&
                !context.Request.Path.StartsWithSegments("/scalar") &&
                !context.Request.Path.StartsWithSegments("/openapi"),
            builder =>
            {
                builder.UseSpa(spa =>
                {
                    spa.UseProxyToSpaDevelopmentServer("https://localhost:5173");
                });
            });
        }
        else
        {
            app.MapFallbackToFile("index.html");
        }

        app.Run();
    }
}
