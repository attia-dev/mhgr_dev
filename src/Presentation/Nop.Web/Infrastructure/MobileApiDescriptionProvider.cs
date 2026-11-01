using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;

namespace Nop.Web.Infrastructure;

/// <summary>
/// nopCommerce maps everything with CONVENTIONAL routes (via IRouteProvider), and ASP.NET Core's ApiExplorer only
/// surfaces ATTRIBUTE-routed actions — so Swashbuckle would otherwise see no endpoints. This provider walks the live
/// endpoint table and emits an <see cref="ApiDescription"/> for every mobile <c>api/*</c> endpoint, giving Swagger
/// its paths, verbs, parameters and request bodies without touching the controllers or routes. [Mahjar mobile-api]
/// </summary>
public partial class MobileApiDescriptionProvider : IApiDescriptionProvider
{
    #region Fields

    protected readonly EndpointDataSource _endpointDataSource;
    protected readonly IModelMetadataProvider _modelMetadataProvider;

    #endregion

    #region Ctor

    public MobileApiDescriptionProvider(EndpointDataSource endpointDataSource,
        IModelMetadataProvider modelMetadataProvider)
    {
        _endpointDataSource = endpointDataSource;
        _modelMetadataProvider = modelMetadataProvider;
    }

    #endregion

    //run after the default provider so we can de-dupe against anything it already produced
    public int Order => 100;

    public void OnProvidersExecuting(ApiDescriptionProviderContext context)
    {
        var seen = new HashSet<string>(context.Results
            .Where(r => !string.IsNullOrEmpty(r.RelativePath))
            .Select(r => r.HttpMethod + " " + r.RelativePath), StringComparer.OrdinalIgnoreCase);

        foreach (var endpoint in _endpointDataSource.Endpoints.OfType<RouteEndpoint>())
        {
            var actionDescriptor = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
            if (actionDescriptor == null)
                continue;

            var rawPath = endpoint.RoutePattern.RawText;
            if (string.IsNullOrEmpty(rawPath) || !rawPath.StartsWith("api/", StringComparison.OrdinalIgnoreCase))
                continue;

            //strip route constraints/defaults from the template: "{id:min(1)}" -> "{id}"
            var path = Regex.Replace(rawPath, @"\{([^:=?}]+)[^}]*\}", "{$1}");

            var routeParamNames = endpoint.RoutePattern.Parameters
                .Select(p => p.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var methods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods?.ToList()
                ?? new List<string> { "GET" };

            foreach (var method in methods)
            {
                if (!seen.Add(method + " " + path))
                    continue;

                var apiDescription = new ApiDescription
                {
                    ActionDescriptor = actionDescriptor,
                    HttpMethod = method.ToUpperInvariant(),
                    RelativePath = path,
                    GroupName = "mobile"
                };

                foreach (var parameter in actionDescriptor.Parameters)
                {
                    BindingSource source;
                    if (routeParamNames.Contains(parameter.Name))
                        source = BindingSource.Path;
                    else if (parameter.BindingInfo?.BindingSource == BindingSource.Body)
                        source = BindingSource.Body;
                    else if (typeof(IFormFile).IsAssignableFrom(parameter.ParameterType))
                        source = BindingSource.FormFile; //[Mahjar app-batch3] → a multipart/form-data request body
                    else
                        source = BindingSource.Query;

                    apiDescription.ParameterDescriptions.Add(new ApiParameterDescription
                    {
                        Name = parameter.Name,
                        Source = source,
                        Type = parameter.ParameterType,
                        ModelMetadata = _modelMetadataProvider.GetMetadataForType(parameter.ParameterType),
                        IsRequired = source == BindingSource.Path
                    });
                }

                //prefer declared [ProducesResponseType(...)] so Swagger shows the real DTO schema; else generic object.
                //ApiResponseFormats MUST be set or Swashbuckle emits a schema-less response (and never generates the DTO).
                var responseTypes = actionDescriptor.MethodInfo
                    .GetCustomAttributes(typeof(ProducesResponseTypeAttribute), inherit: true)
                    .Cast<ProducesResponseTypeAttribute>()
                    .ToList();
                if (responseTypes.Count == 0)
                    responseTypes.Add(new ProducesResponseTypeAttribute(typeof(object), 200));

                foreach (var rt in responseTypes)
                {
                    var bodyType = rt.Type ?? typeof(object);
                    apiDescription.SupportedResponseTypes.Add(new ApiResponseType
                    {
                        StatusCode = rt.StatusCode,
                        Type = bodyType,
                        ModelMetadata = bodyType == typeof(void) ? null : _modelMetadataProvider.GetMetadataForType(bodyType),
                        ApiResponseFormats = new List<ApiResponseFormat> { new ApiResponseFormat { MediaType = "application/json" } }
                    });
                }

                context.Results.Add(apiDescription);
            }
        }
    }

    public void OnProvidersExecuted(ApiDescriptionProviderContext context)
    {
    }
}
