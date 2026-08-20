using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
// В Microsoft.OpenApi 2.x модели переехали из Microsoft.OpenApi.Models в корневое пространство имён.
using Microsoft.OpenApi;

namespace RepairFlow.Api.OpenApi;

/// <summary>
/// Встроенный генератор OpenAPI в .NET 10 не знает про схемы аутентификации — их добавляет трансформер.
/// Благодаря нему в Scalar появляется поле для access-токена, и защищённые методы можно дёргать прямо из UI.
/// </summary>
internal sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    private const string SchemeName = "Bearer";

    private readonly IAuthenticationSchemeProvider _schemeProvider;

    public BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider schemeProvider) =>
        _schemeProvider = schemeProvider;

    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var schemes = await _schemeProvider.GetAllSchemesAsync();

        if (!schemes.Any(scheme => scheme.Name == SchemeName))
        {
            return;
        }

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
        {
            [SchemeName] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Access-токен из POST /api/auth/login или /api/auth/demo."
            }
        };

        // Требование ставится на весь документ: почти все методы закрыты, а публичные помечены [AllowAnonymous].
        document.Security =
        [
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(SchemeName, document)] = new List<string>()
            }
        ];
    }
}
