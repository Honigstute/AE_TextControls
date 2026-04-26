using System.Security.Cryptography;

namespace TextControlsDependencies.Core;

public static class FileHash
{
    public static async Task<string> Sha256Async(string path)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream).ConfigureAwait(false);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
