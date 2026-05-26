using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace ArchRender.Revit.Services;

/// <summary>
/// Stores the API key encrypted via DPAPI in %APPDATA%\ArchRender\credentials.json.
/// Encryption is per-user and per-machine, so the file cannot be decrypted by another user.
/// </summary>
public static class CredentialStore
{
    private static readonly string CredentialsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ArchRender",
        "credentials.json");

    public static void SaveApiKey(string apiKey)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(CredentialsPath)!);

        var plainBytes = System.Text.Encoding.UTF8.GetBytes(apiKey);
        var encrypted = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);

        var payload = new { key = Convert.ToBase64String(encrypted) };
        File.WriteAllText(CredentialsPath, JsonSerializer.Serialize(payload));
    }

    public static string? LoadApiKey()
    {
        if (!File.Exists(CredentialsPath)) return null;

        try
        {
            var json = File.ReadAllText(CredentialsPath);
            using var doc = JsonDocument.Parse(json);
            var base64 = doc.RootElement.GetProperty("key").GetString();
            if (base64 is null) return null;

            var encrypted = Convert.FromBase64String(base64);
            var plain = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            return System.Text.Encoding.UTF8.GetString(plain);
        }
        catch
        {
            return null;
        }
    }

    public static void Clear()
    {
        if (File.Exists(CredentialsPath))
            File.Delete(CredentialsPath);
    }
}
