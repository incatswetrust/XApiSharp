using System.Security.Cryptography;

namespace XApiSharp.CodeGen;

internal static class FileHash
{
    public static string Sha256(string path)
    {
        using var stream = File.OpenRead(path);
        var hash = System.Security.Cryptography.SHA256.HashData(stream);
        return Convert.ToHexStringLower(hash);
    }
}
