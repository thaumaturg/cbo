using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using Cbo.Api.Authentication;
using Cbo.Api.Endpoints;
using Cbo.Api.ErrorHandling;
using Cbo.Api.Validation;
using Cbo.Core;
using Cbo.Core.Accounts;
using Cbo.Core.Data;
using Cbo.Core.Domain;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

namespace Cbo.Api;

public class Program
{
    /// <summary>The environment the integration tests run the API in.</summary>
    public const string TestingEnvironmentName = "Testing";

    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        bool isGeneratingOpenApiDocument = Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";
        if (isGeneratingOpenApiDocument)
        {
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:CboDb"] = "Host=localhost",
                ["Jwt:Key"] = "openapi-document-generation",
                ["Jwt:Issuer"] = "openapi-document-generation",
                ["Jwt:Audience"] = "openapi-document-generation"
            });
        }

        string? connectionString = builder.Configuration.GetConnectionString("CboDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'CboDb' is missing or empty.");
        }

        builder.Services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        JwtOptions jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        if (string.IsNullOrWhiteSpace(jwtOptions.Key))
        {
            throw new InvalidOperationException($"Configuration value '{JwtOptions.SectionName}:Key' is missing or empty.");
        }

        builder.Services.AddCore(connectionString);

        builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
        builder.Services.AddScoped<IJwtSecurityStampValidator, JwtSecurityStampValidator>();

        builder.Services.AddAuthorization();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            // An explicit null for a non-nullable property is a bad request
            // A null in a non-nullable response property fails with a 500
            options.SerializerOptions.RespectNullableAnnotations = true;
        });

        // Malformed requests reach GlobalExceptionHandler as BadHttpRequestException in every environment
        builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);

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
            .AddErrorDescriber<AccountErrorDescriber>()
            .AddDefaultTokenProviders();

        builder.Services.Configure<IdentityOptions>(AccountIdentityOptions.Configure);

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudiences = new[] { jwtOptions.Audience },
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.Key))
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        IJwtSecurityStampValidator validator =
                            context.HttpContext.RequestServices.GetRequiredService<IJwtSecurityStampValidator>();
                        if (context.Principal is null || !await validator.IsValidAsync(context.Principal))
                            context.Fail("The token has been revoked.");
                    }
                };
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

        // Request validators run before every handler, after authentication
        RouteGroupBuilder api = app.MapGroup("/api").RequireAuthorization().AddEndpointFilter<ValidationFilter>();
        api.MapAuthEndpoints();
        api.MapFriendEndpoints();
        api.MapTopicEndpoints();
        api.MapTournamentEndpoints();
        api.MapTournamentParticipantEndpoints();
        api.MapTournamentMatchEndpoints();
        api.MapTournamentRoundEndpoints();
        api.MapTournamentTopicEndpoints();

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

        // Integration tests create their throwaway database from the model instead
        if (!isGeneratingOpenApiDocument && !app.Environment.IsEnvironment(TestingEnvironmentName))
        {
            using IServiceScope scope = app.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<CboDbContext>().Database.Migrate();
        }

        app.Run();
    }
}
