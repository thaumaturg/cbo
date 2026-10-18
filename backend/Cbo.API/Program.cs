using System.Text;
using System.Text.Json.Serialization;
using Cbo.API.Authorization;
using Cbo.API.Data;
using Cbo.API.Data.Interceptors;
using Cbo.API.ErrorHandling;
using Cbo.API.Models.Domain;
using Cbo.API.Repositories;
using Cbo.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

namespace Cbo.API;

public class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        string? connectionString = builder.Configuration.GetConnectionString("CboDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'CboDb' is missing or empty.");
        }

        string? jwtKey = builder.Configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            throw new InvalidOperationException("Configuration value 'Jwt:Key' is missing or empty.");
        }

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<AuditSaveChangesInterceptor>();

        builder.Services.AddDbContext<CboDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>()));

        builder.Services.AddScoped<ITournamentRepository, TournamentRepository>();
        builder.Services.AddScoped<ITournamentParticipantsRepository, TournamentParticipantsRepository>();
        builder.Services.AddScoped<ITournamentTopicRepository, TournamentTopicRepository>();
        builder.Services.AddScoped<ITopicRepository, TopicRepository>();
        builder.Services.AddScoped<ITopicAuthorRepository, TopicAuthorRepository>();
        builder.Services.AddScoped<IMatchRepository, MatchRepository>();
        builder.Services.AddScoped<IRoundRepository, RoundRepository>();
        builder.Services.AddScoped<ITokenRepository, TokenRepository>();

        builder.Services.AddScoped<IAuthorizationHandler, TopicAuthorizationHandler>();
        builder.Services.AddScoped<IAuthorizationHandler, TournamentAuthorizationHandler>();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
        builder.Services.AddScoped<ITournamentAccessService, TournamentAccessService>();
        builder.Services.AddScoped<IMatchGenerationService, MatchGenerationService>();
        builder.Services.AddScoped<ITournamentService, TournamentService>();
        builder.Services.AddScoped<IMatchService, MatchService>();
        builder.Services.AddScoped<IRoundService, RoundService>();
        builder.Services.AddScoped<ITournamentParticipantService, TournamentParticipantService>();
        builder.Services.AddScoped<ITournamentTopicService, TournamentTopicService>();
        builder.Services.AddScoped<ITopicService, TopicService>();
        builder.Services.AddScoped<ITopicValidationService, TopicValidationService>();

        builder.Services.AddControllers(options =>
            {
                // Model-validation error keys use the JSON property names (camelCase) instead of the C# names
                options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider());
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi("v1", options =>
        {
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
        });

        builder.Services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole<Guid>>()
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
                        Encoding.UTF8.GetBytes(jwtKey))
                });

        WebApplication app = builder.Build();

        app.UseExceptionHandler();

        ForwardedHeadersOptions forwardedHeadersOptions = new()
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        };
        forwardedHeadersOptions.KnownIPNetworks.Clear();
        forwardedHeadersOptions.KnownProxies.Clear();
        app.UseForwardedHeaders(forwardedHeadersOptions);

        if (app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }
        else
        {
            app.UseStaticFiles();
        }

        // Gives error responses without a body (for example the JWT challenge 401) a problem details body
        app.UseStatusCodePages();

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

        using (IServiceScope scope = app.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<CboDbContext>().Database.Migrate();
        }

        app.Run();
    }
}
