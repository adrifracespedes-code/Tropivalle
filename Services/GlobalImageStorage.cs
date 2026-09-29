using System.Net.Http.Headers;

namespace EcommerceApp.Services;

/// <summary>
/// Sube imágenes a un almacenamiento público global (Supabase Storage).
/// Si no está configurado, el controlador guarda en disco y publica URL absoluta del sitio.
/// </summary>
public class GlobalImageStorage(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<GlobalImageStorage> logger)
{
    public bool IsConfigured
    {
        get
        {
            var url = GetSupabaseUrl();
            var key = GetServiceKey();
            return !string.IsNullOrWhiteSpace(url) && !string.IsNullOrWhiteSpace(key);
        }
    }

    public string Bucket =>
        config["Storage:Bucket"]
        ?? Environment.GetEnvironmentVariable("STORAGE_BUCKET")
        ?? "product-images";

    private string GetSupabaseUrl() =>
        (config["Storage:SupabaseUrl"]
         ?? Environment.GetEnvironmentVariable("SUPABASE_URL")
         ?? config["Supabase:Url"]
         ?? "").TrimEnd('/');

    private string GetServiceKey() =>
        config["Storage:ServiceKey"]
        ?? Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY")
        ?? Environment.GetEnvironmentVariable("SUPABASE_KEY")
        ?? config["Supabase:ServiceKey"]
        ?? "";

    /// <summary>
    /// Sube el archivo y devuelve la URL pública https global, o null si falla / no hay config.
    /// </summary>
    public async Task<string?> UploadAsync(IFormFile file, string fileName, CancellationToken ct = default)
    {
        if (!IsConfigured)
            return null;

        var baseUrl = GetSupabaseUrl();
        var key = GetServiceKey();
        var bucket = Bucket;

        var client = httpClientFactory.CreateClient("supabase-storage");
        var endpoint = $"{baseUrl}/storage/v1/object/{bucket}/{fileName}";

        await using var stream = file.OpenReadStream();
        using var content = new StreamContent(stream);
        content.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);

        using var req = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = content };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        req.Headers.TryAddWithoutValidation("apikey", key);
        req.Headers.TryAddWithoutValidation("x-upsert", "true");

        try
        {
            using var resp = await client.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(ct);
                logger.LogWarning("Supabase Storage upload failed {Status}: {Body}", (int)resp.StatusCode, body);
                return null;
            }

            // URL pública del objeto
            return $"{baseUrl}/storage/v1/object/public/{bucket}/{fileName}";
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Supabase Storage upload exception");
            return null;
        }
    }
}
