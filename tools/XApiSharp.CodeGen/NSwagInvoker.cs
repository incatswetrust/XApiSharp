using System.Diagnostics;

namespace XApiSharp.CodeGen;

/// <summary>
/// Invokes the pinned local NSwag tool (.config/dotnet-tools.json - GEN-03) as a subprocess with
/// a fixed settings, so two runs against the same snapshot produce byte-identical output (GEN-02).
/// No network access is used - NSwag reads only the local snapshot file (GEN-01).
/// </summary>
internal static class NSwagInvoker
{
    public static int Run(string snapshotPath, string outputPath, string @namespace, string className)
    {
        var arguments = new[]
        {
            "tool", "run", "nswag", "openapi2csclient",
            $"/input:{snapshotPath}",
            $"/output:{outputPath}",
            $"/namespace:{@namespace}",
            $"/classname:{className}",
            "/JsonLibrary:SystemTextJson",
            "/generateClientInterfaces:true",
            "/generateOptionalParameters:true",
            "/injectHttpClient:true",
            "/disposeHttpClient:false",
            "/useBaseUrl:false",
        };

        var startInfo = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var arg in arguments)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = Process.Start(startInfo)!;
        Console.Write(process.StandardOutput.ReadToEnd());
        Console.Error.Write(process.StandardError.ReadToEnd());
        process.WaitForExit();
        return process.ExitCode;
    }
}
