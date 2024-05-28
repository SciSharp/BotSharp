using BotSharp.Abstraction.Messaging.JsonConverters;
using BotSharp.Core.Users.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.JsonWebTokens;
using BotSharp.OpenAPI.BackgroundServices;

namespace BotSharp.OpenAPI;

public static class BotSharpOpenApiExtensions
{
    private static string policy = "BotSharpCorsPolicy";
    /// <summary>
    /// Add Swagger/OpenAPI
    /// </summary>
    /// <param name="services"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    public static IServiceCollection AddBotSharpOpenAPI(this IServiceCollection services,
        IConfiguration config,
        string[] origins,
        IHostEnvironment env,
        bool enableValidation)
    {
        services.AddScoped<IUserIdentity, UserIdentity>();
        services.AddHostedService<ConversationTimeoutService>();

        // Add bearer authentication
        var schema = "MIXED_SCHEME";
        var builder = services.AddAuthentication(options =>
        {
            // custom scheme defined in .AddPolicyScheme() below
            // inspired from https://weblog.west-wind.com/posts/2022/Mar/29/Combining-Bearer-Token-and-Cookie-Auth-in-ASPNET
            options.DefaultScheme = schema;
            options.DefaultChallengeScheme = schema;
            options.DefaultAuthenticateScheme = schema;
        }).AddJwtBearer(o =>
        {
            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = config["Jwt:Issuer"],
                ValidAudience = config["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"])),
                ValidateIssuer = enableValidation,
                ValidateAudience = enableValidation,
                ValidateLifetime = enableValidation && !env.IsDevelopment(),
                ValidateIssuerSigningKey = enableValidation
            };

            if (!enableValidation)
            {
                o.TokenValidationParameters.SignatureValidator = (string token, TokenValidationParameters parameters) =>
                    new JsonWebToken(token);
            }
        }).AddCookie(options =>
        {
        }).AddPolicyScheme(schema, "Mixed authentication", options =>
        {
            // runs on each request
            options.ForwardDefaultSelector = context =>
            {
                // filter by auth type
                string authorization = context.Request.Headers[HeaderNames.Authorization];
                if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer "))
                    return JwtBearerDefaults.AuthenticationScheme;
                else if (context.Request.Cookies.ContainsKey(".AspNetCore.Cookies"))
                    return CookieAuthenticationDefaults.AuthenticationScheme;
                else if (context.Request.Path.StartsWithSegments("/sso") && context.Request.Method == "GET")
                    return CookieAuthenticationDefaults.AuthenticationScheme;
                else if (context.Request.Path.ToString().StartsWith("/signin-") && context.Request.Method == "GET")
                    return CookieAuthenticationDefaults.AuthenticationScheme;

                // otherwise always check for cookie auth
                return JwtBearerDefaults.AuthenticationScheme;
            };
        });

        // GitHub OAuth
        if (!string.IsNullOrWhiteSpace(config["OAuth:GitHub:ClientId"]) && !string.IsNullOrWhiteSpace(config["OAuth:GitHub:ClientSecret"]))
        {
            builder = builder.AddGitHub(options =>
             {
                 options.ClientId = config["OAuth:GitHub:ClientId"];
                 options.ClientSecret = config["OAuth:GitHub:ClientSecret"];
                 options.Scope.Add("user:email");
             });
        }

        // Google Identiy OAuth
        if (!string.IsNullOrWhiteSpace(config["OAuth:Google:ClientId"]) && !string.IsNullOrWhiteSpace(config["OAuth:Google:ClientSecret"]))
        {
            builder = builder.AddGoogle(options =>
            {
                options.ClientId = config["OAuth:Google:ClientId"];
                options.ClientSecret = config["OAuth:Google:ClientSecret"];
            });
        }

        // Keycloak Identiy OAuth
        if (!string.IsNullOrWhiteSpace(config["OAuth:Keycloak:ClientId"]) && !string.IsNullOrWhiteSpace(config["OAuth:Keycloak:ClientSecret"]))
        {
            builder = builder.AddKeycloak(options =>
            {
                options.BaseAddress = new Uri(config["OAuth:Keycloak:BaseAddress"]);
                options.Realm = config["OAuth:Keycloak:Realm"];
                options.ClientId = config["OAuth:Keycloak:ClientId"];
                options.ClientSecret = config["OAuth:Keycloak:ClientSecret"];
                options.AccessType = AspNet.Security.OAuth.Keycloak.KeycloakAuthenticationAccessType.Confidential;
                int version = Convert.ToInt32(config["OAuth:Keycloak:Version"]??"22") ;
                options.Version = new Version(version,0);
            });
        }

        // Wexin OAuth
        if (!string.IsNullOrWhiteSpace(config["OAuth:Wexin:ClientId"]) && !string.IsNullOrWhiteSpace(config["OAuth:Wexin:ClientSecret"]))
        {
            builder = builder.AddWeixin(options =>
            {
                options.ClientId = config["OAuth:GitHub:ClientId"];
                options.ClientSecret = config["OAuth:GitHub:ClientSecret"];
                options.Scope.Add("user:email");
                options.Backchannel = builder.Services.BuildServiceProvider()
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient();
            });
        }

        // Add services to the container.
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new RichContentJsonConverter());
                options.JsonSerializerOptions.Converters.Add(new TemplateMessageJsonConverter());
            });

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(
            c =>
            {
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Please insert JWT with Bearer into field",
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement {
                   {
                     new OpenApiSecurityScheme
                     {
                       Reference = new OpenApiReference
                       {
                         Type = ReferenceType.SecurityScheme,
                         Id = "Bearer"
                       }
                      },
                      Array.Empty<string>()
                   }
                });
            }
        );

        services.AddHttpContextAccessor();

        services.AddCors(options =>
        {
            options.AddPolicy(policy,
                builder => builder.WithOrigins(origins)
                           .AllowAnyMethod()
                           .AllowAnyHeader()
                           .AllowCredentials());
        });

        return services;
    }

    /// <summary>
    /// Use Swagger/OpenAPI
    /// </summary>
    /// <param name="services"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseBotSharpOpenAPI(this IApplicationBuilder app, IHostEnvironment env)
    {
        if (app == null)
        {
            throw new ArgumentNullException(nameof(app));
        }

        app.UseCors(policy);

        app.UseSwagger();

        if (env.IsDevelopment())
        {
            IdentityModelEventSource.ShowPII = true;
            app.UseSwaggerUI();
            app.UseDeveloperExceptionPage();
        }

        app.UseAuthentication();

        app.UseRouting();

        app.UseAuthorization();

        app.UseEndpoints(
            endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });

        return app;
    }

    /// <summary>
    /// Host BotSharp UI built in adapter-static
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static IApplicationBuilder UseBotSharpUI(this IApplicationBuilder app, bool isDevelopment = false)
    {
        if (app == null)
        {
            throw new ArgumentNullException(nameof(app));
        }

        // app.UseFileServer();
        app.UseDefaultFiles();
        app.UseStaticFiles();

        app.UseEndpoints(
            endpoints =>
            {
                // For SPA static file routing
                endpoints.MapFallbackToFile("/index.html");
            });

        app.UseSpa(config =>
        {
            if (isDevelopment)
            {
                config.UseProxyToSpaDevelopmentServer("http://localhost:5015");
            }
        });

        return app;
    }
}

