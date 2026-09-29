namespace EcommerceApp.Helpers;

/// <summary>
/// Normaliza rutas de imagen para que funcionen en cualquier dispositivo
/// (no localhost, rutas relativas con /).
/// </summary>
public static class ImageUrlHelper
{
    public const string Placeholder =
        "https://placehold.co/800x500/e8f8ef/1a5c3a?text=TropiValle";

    public static string Resolve(string? imageUrl, string? placeholder = null)
    {
        var fallback = placeholder ?? Placeholder;
        if (string.IsNullOrWhiteSpace(imageUrl))
            return fallback;

        var url = imageUrl.Trim();

        // Rutas de Windows locales → no sirven en el servidor
        if (url.Length >= 2 && url[1] == ':' && char.IsLetter(url[0]))
            return fallback;

        // http://localhost:5080/images/x.jpg → /images/x.jpg
        if (url.StartsWith("http://localhost", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("https://localhost", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("http://127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("https://127.0.0.1", StringComparison.OrdinalIgnoreCase))
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                var path = uri.AbsolutePath;
                return string.IsNullOrEmpty(path) ? fallback : path;
            }
        }

        if (url.StartsWith("~/"))
            url = url[1..];

        // Ruta relativa sin slash inicial
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("/"))
        {
            url = "/" + url;
        }

        return url;
    }
}
