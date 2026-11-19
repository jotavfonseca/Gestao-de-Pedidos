using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Orders.Tests.Golden;

/// <summary>
/// Compares JSON with the versioned file in tests/Orders.Tests/Golden/files.
/// To regenerate after an intentional contract change:
/// <c>UPDATE_GOLDEN=1 dotnet test</c>.
/// </summary>
internal static class GoldenFile
{
    private static readonly JsonSerializerOptions Indented = new()
    {
        WriteIndented = true,
        // Readable diffs (no escaping of non-ASCII or "+"); test-only.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static void Assert(string name, string actualJson, [CallerFilePath] string callerPath = "")
    {
        var path = Path.Combine(Path.GetDirectoryName(callerPath)!, "files", $"{name}.json");
        var actual = Normalize(actualJson);

        if (Environment.GetEnvironmentVariable("UPDATE_GOLDEN") == "1" || !File.Exists(path))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, actual + Environment.NewLine);
            if (Environment.GetEnvironmentVariable("UPDATE_GOLDEN") != "1")
            {
                Xunit.Assert.Fail($"Golden file created at {path}. Review it and run the tests again.");
            }

            return;
        }

        var expected = Normalize(File.ReadAllText(path));
        Xunit.Assert.True(expected == actual,
            $"""
            The '{name}' contract changed. If this is intentional, run: UPDATE_GOLDEN=1 dotnet test
            --- expected ({path})
            {expected}
            --- actual
            {actual}
            """);
    }

    private static string Normalize(string json) =>
        JsonNode.Parse(json)!.ToJsonString(Indented).ReplaceLineEndings("\n");
}
