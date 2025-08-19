using AzureDevopsUtils.Common;
using AzureDevopsUtils.Services;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace AzureDevopsUtils;

public abstract class Program
{
    private static async Task Main(string[] args)
    {
        // start setup
        var organizationUrl = GetEnvironmentVariable("AZ_DEVOPS_ORGANIZATION_URL");
        var personalAccessToken = GetEnvironmentVariable("AZ_DEVOPS_PERSONAL_ACCESS_TOKEN");
        var projectName = GetEnvironmentVariable("AZ_DEVOPS_PROJECT_NAME");

        var credentials = new VssBasicCredential(string.Empty, personalAccessToken);

        using var connection = new VssConnection(new Uri(organizationUrl), credentials);
        using var service = new AzureDevOpsService(connection, projectName);
        // end setup

        var work = await service.GetAssignmentRelatedWorkAsync(6);
        await Commons.WriteJsonObj($"work-{work.WorkItemId}", work);

        Console.WriteLine("Done.");
    }

    private static string GetEnvironmentVariable(string name)
    {
        var value = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User);
        ArgumentException.ThrowIfNullOrWhiteSpace(value, name);
        return value;
    }
}