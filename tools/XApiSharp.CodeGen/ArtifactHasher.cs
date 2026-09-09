using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace XApiSharp.CodeGen;

/// <summary>
/// Spec 22.2's "SHA-256 подготовленных артефактов" (SHA-256 of the prepared artifacts).
/// Implemented in portable dotnet code (not <c>sha256sum</c>/<c>shasum</c>/<c>certutil</c>)
/// specifically so the same command works unmodified across the Linux/Windows/macOS runners spec
/// 22.2 requires - no OS-specific hashing tool to keep in sync across three platforms.
/// </summary>
internal static class ArtifactHasher
{
    /// <summary>Hashes every <c>*.nupkg</c>/<c>*.snupkg</c> file directly under
    /// <paramref name="directory"/> (non-recursive - artifacts live flat in the pack output
    /// directory) and writes a sorted "hash  filename" manifest, matching the conventional
    /// <c>sha256sum</c> output shape so it is still verifiable with standard tools by hand.</summary>
    public static void HashPackagesTo(string directory, string outputPath)
    {
        var files = PackageFilesIn(directory);

        var builder = new StringBuilder();
        foreach (var file in files)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"{ComputeHash(file)}  {Path.GetFileName(file)}");
        }

        File.WriteAllText(outputPath, builder.ToString());
    }

    /// <summary>
    /// Spec 23.2 step 4 ("fetch the release job's verified artifacts and re-check their hashes"):
    /// recomputes the hash of every package file under <paramref name="directory"/> and compares
    /// it against the recorded <paramref name="sumsFilePath"/> (from <see cref="HashPackagesTo"/>) -
    /// catches corruption or tampering between the release-candidate run that produced the
    /// artifacts and the publish run that pushes them, since publish never rebuilds.
    /// </summary>
    public static IReadOnlyList<string> VerifyAgainstSums(string directory, string sumsFilePath)
    {
        var problems = new List<string>();
        if (!File.Exists(sumsFilePath))
        {
            problems.Add($"Sums file not found: {sumsFilePath}");
            return problems;
        }

        var recorded = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in File.ReadAllLines(sumsFilePath))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var parts = line.Split("  ", 2);
            if (parts.Length != 2)
            {
                problems.Add($"Unparseable line in {sumsFilePath}: '{line}'");
                continue;
            }

            recorded[parts[1].Trim()] = parts[0].Trim();
        }

        var actualFiles = PackageFilesIn(directory);
        var actualNames = actualFiles.Select(Path.GetFileName).ToHashSet(StringComparer.Ordinal);

        foreach (var (fileName, expectedHash) in recorded)
        {
            if (!actualNames.Contains(fileName))
            {
                problems.Add($"Recorded in sums file but missing from {directory}: {fileName}");
            }
        }

        foreach (var file in actualFiles)
        {
            var fileName = Path.GetFileName(file);
            if (!recorded.TryGetValue(fileName, out var expectedHash))
            {
                problems.Add($"Present in {directory} but not recorded in sums file: {fileName}");
                continue;
            }

            var actualHash = ComputeHash(file);
            if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
            {
                problems.Add($"Hash mismatch for {fileName}: expected {expectedHash}, got {actualHash}.");
            }
        }

        return problems;
    }

    private static List<string> PackageFilesIn(string directory) =>
        Directory.EnumerateFiles(directory, "*.nupkg")
            .Concat(Directory.EnumerateFiles(directory, "*.snupkg"))
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();

    private static string ComputeHash(string file)
    {
        using var stream = File.OpenRead(file);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
