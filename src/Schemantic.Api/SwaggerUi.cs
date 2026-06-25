namespace Schemantic.Api;

/// <summary>
/// Serves a minimal Swagger UI page that loads the hand-built OpenAPI document.
/// </summary>
public static class SwaggerUi
{
    /// <summary>HTML for Swagger UI pointing at <c>/openapi.json</c>.</summary>
    public const string Html =
        """
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="utf-8" />
          <title>Schemantic API</title>
          <link rel="stylesheet" href="https://unpkg.com/swagger-ui-dist@5/swagger-ui.css" />
        </head>
        <body>
          <div id="swagger-ui"></div>
          <script src="https://unpkg.com/swagger-ui-dist@5/swagger-ui-bundle.js"></script>
          <script>
            SwaggerUIBundle({ url: '/openapi.json', dom_id: '#swagger-ui' });
          </script>
        </body>
        </html>
        """;
}
