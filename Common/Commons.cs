using System.Text.Json;
using System.Text.Json.Serialization;

namespace AzureDevopsUtils.Common;

public sealed class Commons
{
    private const string OutputDirectory = @"D:\Dev\.NET\AzureDevopsUtils\Output";

    private static readonly Dictionary<string, string> BranchesMapping = new()
    {
        { "feature", "develop" },
        {  "dev", "develop" },
        { "qa", "quality" },
    };

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static string GetParentBranch(string branchName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(branchName, nameof(branchName));
        string prefix = branchName.Split('/')[0];
        if (!BranchesMapping.TryGetValue(prefix, out string? value))
        {
            throw new ArgumentException($"Invalid brach name {branchName}");
        }
        return value;
    }

    public static async Task WriteJsonObj(string filename, object obj)
    {
        await File.WriteAllTextAsync(OutputDirectory + $"/{filename}-{obj.GetHashCode()}.json",
            JsonSerializer.Serialize(obj,
                JsonSerializerOptions));
    }
}
