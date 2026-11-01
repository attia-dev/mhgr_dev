using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OpenApi.Models;
using Nop.Core.Infrastructure;

namespace Nop.Web.Infrastructure;

/// <summary>
/// Registers Swagger / OpenAPI for the mobile customer API and serves an interactive UI at /swagger. The document is
/// scoped to the api/* endpoints, so the storefront and admin MVC are excluded. [Mahjar mobile-api]
/// </summary>
public partial class SwaggerStartup : INopStartup
{
    /// <summary>
    /// Add and configure any of the middleware
    /// </summary>
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddEndpointsApiExplorer();

        //bridge nopCommerce's conventional api/* routes into ApiExplorer so Swagger can document them
        services.TryAddEnumerable(ServiceDescriptor.Transient<IApiDescriptionProvider, MobileApiDescriptionProvider>());

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("mobile", new OpenApiInfo
            {
                Title = "MHGR Mobile API",
                Version = "v1",
                Description = "Customer mobile-app REST API for the MHGR watch marketplace " +
                              "(auth, catalog discovery, product detail, vendors, cart, wishlist, checkout, orders, account, reviews, reference data)."
            });

            //only document the mobile api/* endpoints (exclude the storefront + admin MVC)
            options.DocInclusionPredicate((_, apiDescription) =>
                !string.IsNullOrEmpty(apiDescription.RelativePath) && apiDescription.RelativePath.StartsWith("api/"));

            //an action can be reached by more than one conventional route (e.g. /api/products and
            ///api/vendors/{vendorId}/products both hit CatalogApi.Products) -> keep one description
            options.ResolveConflictingActions(descriptions => descriptions.First());

            //bearer-token (JWT) auth so "Authorize" works in the UI
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Paste the token returned by /api/auth/login (just the token, no 'Bearer' prefix)."
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    System.Array.Empty<string>()
                }
            });
        });

        //align Swagger schemas with nopCommerce's Newtonsoft (PascalCase) serializer so the docs match the real JSON
        services.AddSwaggerGenNewtonsoftSupport();
    }

    /// <summary>
    /// Configure the using of added middleware
    /// </summary>
    public void Configure(IApplicationBuilder application)
    {
        application.UseSwagger();
        application.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/mobile/swagger.json", "MHGR Mobile API");
            options.RoutePrefix = "swagger";
            options.DocumentTitle = "MHGR Mobile API";
        });
    }

    /// <summary>
    /// Gets order of this startup configuration implementation (after routing=400, before MVC endpoints=1000)
    /// </summary>
    public int Order => 500;
}
