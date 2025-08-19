using AzureDevopsUtils.Common;
using AzureDevopsUtils.Domain;
using Microsoft.Azure.DevOps.Comments.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System.Collections.Immutable;

namespace AzureDevopsUtils.Services;

public class AzureDevOpsService : IDisposable
{
    private readonly WorkItemTrackingHttpClient _vssWorkitemTrackingClient;
    private readonly GitHttpClient _vssGitClient;
    private readonly string _projectName;

    public AzureDevOpsService(VssConnection connection, string projectName)
    {
        _vssGitClient = connection.GetClient<GitHttpClient>();
        _vssWorkitemTrackingClient = connection.GetClient<WorkItemTrackingHttpClient>();
        _projectName = projectName;
    }

    public async Task<AssignmentRelatedWork> GetAssignmentRelatedWorkAsync(int workItemId)
    {
        var workItem = await _vssWorkitemTrackingClient.GetWorkItemAsync(workItemId, expand: WorkItemExpand.Relations);
        var assignedToDetails = (IdentityRef)workItem.Fields["System.AssignedTo"];

        var reposWork = await GetRepositoryWork(workItem.Relations, workItemId);

        return new AssignmentRelatedWork
        {
            WorkItemId = workItemId,
            Title = workItem.Fields["System.Title"].ToString()!,
            WorkItemType = workItem.Fields["System.WorkItemType"].ToString()!,
            IterationPath = workItem.Fields["System.IterationPath"].ToString()!,
            TeamProject = workItem.Fields["System.TeamProject"].ToString()!,
            AreaPath = workItem.Fields["System.AreaPath"].ToString()!,
            State = workItem.Fields["System.State"].ToString()!,
            AssignedTo = new AssignedTo(assignedToDetails.DisplayName, assignedToDetails.UniqueName),
            GitReposWork = reposWork
        };
    }

    private async Task<IEnumerable<GitRepositoryWork>> GetRepositoryWork(IList<WorkItemRelation> workItemRelations, int workItemId)
    {
        var gitReposWork = new List<GitRepositoryWork>();

        var relBranches = workItemRelations
            .Where(rel => string.Equals("Branch", rel.Attributes["name"].ToString()))
            .Select(rel => new GitBranchInformation
            {
                Name = GitBranchInformation.ExtractBranchName(rel.Url),
                ProjectId = GitBranchInformation.ExtractProjectId(rel.Url),
                RepositoryId = GitBranchInformation.ExtractRepositoryId(rel.Url)
            });


        var gitBranchesXRepository = from rel in relBranches group rel by rel.RepositoryId;
        foreach (var gitRepo in gitBranchesXRepository)
        {
            var gitRepositoryId = gitRepo.Key;
            var gitChanges = new HashSet<GitCommit>();
            foreach (var gitBranch in gitRepo)
            {
                var allGitChanges = await GetBranchDiffs(gitRepositoryId, gitBranch.Name, Commons.GetParentBranch(gitBranch.Name));
                gitChanges.AddRange(allGitChanges);
            }
            gitReposWork.Add(await BuildRepositoryWork(gitChanges, gitRepositoryId, workItemId));
        }

        return gitReposWork;
    }

    private async Task<GitRepositoryWork> BuildRepositoryWork(HashSet<GitCommit> gitChanges, Guid repositoryId, int workItemId)
    {
        var repository = await _vssGitClient.GetRepositoryAsync(repositoryId: repositoryId);
        return new GitRepositoryWork
        {
            RepositoryName = repository.Name,
            Commits = BuildCommmitsWork(gitChanges, workItemId)
        };
    }

    private static IEnumerable<Commit> BuildCommmitsWork(HashSet<GitCommit> gitChanges, int workItemId)
    {
        return gitChanges
            .Where(commit => commit.Comment.Contains($"#{workItemId}"))
            .Select(commit =>
        {
            return new Commit
            {
                CommitId = commit.CommitId,
                ChangeCounts = commit.ChangeCounts,
                Comment = commit.Comment,
                Date = commit.Committer.Date,
                Changes = commit.Changes.Where(change => change.Item.GitObjectType == GitObjectType.Blob)
                            .Select(change =>
                            {
                                return new Change
                                {
                                    FileName = Path.GetFileName(change.Item.Path),
                                    FilePath = Path.GetDirectoryName(change.Item.Path),
                                    Type = change.ChangeType
                                };
                            })
            };
        });
    }

    private async Task<ImmutableHashSet<GitCommit>> GetBranchDiffs(Guid repositoryId, string sourceBranch, string targetBranch)
    {
        var changes = new HashSet<GitCommit>();

        int skip = 0;
        while (true)
        {
            var gitCommits = await _vssGitClient.GetCommitsAsync(repositoryId, searchCriteria: new GitQueryCommitsCriteria
            {
                ItemVersion = new GitVersionDescriptor
                {
                    Version = targetBranch,
                    VersionType = GitVersionType.Branch
                },
                CompareVersion = new GitVersionDescriptor
                {
                    Version = sourceBranch,
                    VersionType = GitVersionType.Branch
                },
                ShowOldestCommitsFirst = true,

                Skip = skip
            });
            skip += 100;


            var gitChanges = await Task.WhenAll(gitCommits.Select(async (commit, _) =>
                await _vssGitClient.GetCommitAsync(repositoryId: repositoryId, commitId: commit.CommitId, changeCount: 500)
            ));

            changes.AddRange(gitChanges);

            if (gitCommits.Count == 0)
            {
                break;
            }
        }

        return [.. changes];
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _vssGitClient.Dispose();
        _vssWorkitemTrackingClient.Dispose();
    }

}