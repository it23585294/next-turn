using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace NextTurn.API.OpenApi;

/// <summary>
/// Teaches the OpenAPI document generator about JWT Bearer authentication.
///
/// Without this, the generated spec has no security scheme and the "Authorize"
/// button in Scalar does not appear.
///
/// What this adds to the output JSON:
///   components.securitySchemes.Bearer — declares the HTTP bearer scheme
///   security (per operation) — marks every operation as requiring Bearer
///
/// Registered via: builder.Services.AddOpenApi(options =>
///     options.AddDocumentTransformer&lt;BearerSecuritySchemeTransformer&gt;())
/// </summary>
internal sealed class BearerSecuritySchemeTransformer(
    IAuthenticationSchemeProvider authenticationSchemeProvider)
    : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(
        OpenApiDocument                  document,
        OpenApiDocumentTransformerContext context,
        CancellationToken                cancellationToken)
    {
        var authenticationSchemes = await authenticationSchemeProvider
            .GetAllSchemesAsync();

        // Only add the security scheme if JWT Bearer is actually registered.
        if (!authenticationSchemes.Any(s => s.Name == JwtBearerDefaults.AuthenticationScheme))
            return;

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??=
            new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes[JwtBearerDefaults.AuthenticationScheme] =
            new OpenApiSecurityScheme
            {
                Type         = SecuritySchemeType.Http,
                Scheme       = "bearer",    // must be lowercase per spec
                BearerFormat = "JWT",
                Description  = "Paste your JWT access token from POST /api/auth/login. " +
                               "The 'Bearer ' prefix is added automatically."
            };

        // In Microsoft.OpenApi v2.0 the key in OpenApiSecurityRequirement is an
        // OpenApiSecuritySchemeReference (replaces the old OpenApiSecurityScheme { Reference = ... } pattern).
        var securityRequirement = new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme, document)] =
                new List<string>()
        };

        // Apply the requirement to every operation.
        // [AllowAnonymous] endpoints still show the padlock in UI; auth is simply
        // not enforced server-side for those endpoints.
        foreach (var path in document.Paths.Values)
        {
            if (path.Operations is null) continue;
            foreach (var operation in path.Operations.Values)
            {
                operation.Security ??= [];
                operation.Security.Add(securityRequirement);
            }
        }
    }
}
