using System.Text.Json;
using Serilog;

namespace Aquila.Core.Workspace;

/// <summary>
/// Manages workspace lifecycle and persistence.
/// </summary>
public class WorkspaceManager : IWorkspaceManager
{
    private readonly string _workspacesRootPath;
    private readonly List<WorkspaceInfo> _recentWorkspaces;
    private readonly ILogger _logger;
    private WorkspaceInfo? _currentWorkspace;
    private const string MetadataFileName = "workspace.json";
    private const int MaxRecentWorkspaces = 10;

    public WorkspaceManager(string appDataPath)
    {
        _workspacesRootPath = Path.Combine(appDataPath, "workspaces");
        _recentWorkspaces = new List<WorkspaceInfo>();
        _logger = Log.ForContext<WorkspaceManager>();

        Directory.CreateDirectory(_workspacesRootPath);
    }

    public WorkspaceInfo? CurrentWorkspace => _currentWorkspace;

    public IReadOnlyList<WorkspaceInfo> RecentWorkspaces => _recentWorkspaces.AsReadOnly();

    public async Task<WorkspaceInfo> LoadWorkspaceAsync(string nameOrPath)
    {
        try
        {
            WorkspaceInfo workspace;

            // Check if it's a path or a name
            if (Path.IsPathRooted(nameOrPath) && Directory.Exists(nameOrPath))
            {
                workspace = await LoadWorkspaceFromPathAsync(nameOrPath);
            }
            else
            {
                // Assume it's a name
                var workspacePath = Path.Combine(_workspacesRootPath, nameOrPath);
                workspace = await LoadWorkspaceFromPathAsync(workspacePath);
            }

            _currentWorkspace = workspace;
            UpdateRecentWorkspaces(workspace);

            _logger.Information("Workspace loaded: {WorkspaceName} from {Path}", workspace.Name, workspace.Path);
            return workspace;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load workspace: {NameOrPath}", nameOrPath);
            throw;
        }
    }

    public async Task SaveWorkspaceAsync()
    {
        if (_currentWorkspace == null)
        {
            throw new InvalidOperationException("No workspace is currently loaded.");
        }

        try
        {
            var metadata = new
            {
                _currentWorkspace.Name,
                _currentWorkspace.CreatedAt,
                LastAccessedAt = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
            var metadataPath = Path.Combine(_currentWorkspace.Path, MetadataFileName);

            await File.WriteAllTextAsync(metadataPath, json);

            _logger.Information("Workspace saved: {WorkspaceName}", _currentWorkspace.Name);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to save workspace: {WorkspaceName}", _currentWorkspace.Name);
            throw;
        }
    }

    public async Task<WorkspaceInfo> CreateWorkspaceAsync(string name)
    {
        try
        {
            var workspacePath = Path.Combine(_workspacesRootPath, name);

            if (Directory.Exists(workspacePath))
            {
                throw new InvalidOperationException($"Workspace '{name}' already exists.");
            }

            Directory.CreateDirectory(workspacePath);
            Directory.CreateDirectory(Path.Combine(workspacePath, "settings"));
            Directory.CreateDirectory(Path.Combine(workspacePath, "cache"));
            Directory.CreateDirectory(Path.Combine(workspacePath, "layouts"));

            var workspace = new WorkspaceInfo
            {
                Name = name,
                Path = workspacePath,
                CreatedAt = DateTime.UtcNow,
                LastAccessedAt = DateTime.UtcNow
            };

            await SaveWorkspaceMetadataAsync(workspace);

            _logger.Information("Workspace created: {WorkspaceName}", name);
            return workspace;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to create workspace: {WorkspaceName}", name);
            throw;
        }
    }

    public async Task DeleteWorkspaceAsync(string name)
    {
        try
        {
            var workspacePath = Path.Combine(_workspacesRootPath, name);

            if (!Directory.Exists(workspacePath))
            {
                throw new InvalidOperationException($"Workspace '{name}' not found.");
            }

            if (_currentWorkspace?.Name == name)
            {
                throw new InvalidOperationException("Cannot delete the currently active workspace.");
            }

            Directory.Delete(workspacePath, recursive: true);
            _recentWorkspaces.RemoveAll(w => w.Name == name);

            _logger.Information("Workspace deleted: {WorkspaceName}", name);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to delete workspace: {WorkspaceName}", name);
            throw;
        }
    }

    public async Task<IReadOnlyList<WorkspaceInfo>> GetAllWorkspacesAsync()
    {
        try
        {
            var workspaces = new List<WorkspaceInfo>();

            if (!Directory.Exists(_workspacesRootPath))
            {
                return workspaces.AsReadOnly();
            }

            var directories = Directory.GetDirectories(_workspacesRootPath);

            foreach (var dir in directories)
            {
                try
                {
                    var workspace = await LoadWorkspaceFromPathAsync(dir);
                    workspaces.Add(workspace);
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Failed to load workspace metadata from {Path}", dir);
                }
            }

            return workspaces.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to enumerate workspaces");
            throw;
        }
    }

    private async Task<WorkspaceInfo> LoadWorkspaceFromPathAsync(string workspacePath)
    {
        var metadataPath = Path.Combine(workspacePath, MetadataFileName);

        if (!File.Exists(metadataPath))
        {
            throw new InvalidOperationException($"Workspace metadata not found at {metadataPath}");
        }

        var json = await File.ReadAllTextAsync(metadataPath);
        var metadata = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json) ?? new();

        var workspace = new WorkspaceInfo
        {
            Name = metadata.TryGetValue("Name", out var nameEl) ? nameEl.GetString() ?? "Unknown" : "Unknown",
            Path = workspacePath,
            CreatedAt = metadata.TryGetValue("CreatedAt", out var createdEl) ? createdEl.GetDateTime() : DateTime.UtcNow,
            LastAccessedAt = metadata.TryGetValue("LastAccessedAt", out var accessedEl) ? accessedEl.GetDateTime() : DateTime.UtcNow
        };

        return workspace;
    }

    private async Task SaveWorkspaceMetadataAsync(WorkspaceInfo workspace)
    {
        var metadata = new
        {
            workspace.Name,
            workspace.CreatedAt,
            workspace.LastAccessedAt
        };

        var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
        var metadataPath = Path.Combine(workspace.Path, MetadataFileName);

        await File.WriteAllTextAsync(metadataPath, json);
    }

    private void UpdateRecentWorkspaces(WorkspaceInfo workspace)
    {
        _recentWorkspaces.RemoveAll(w => w.Name == workspace.Name);
        _recentWorkspaces.Insert(0, workspace);

        if (_recentWorkspaces.Count > MaxRecentWorkspaces)
        {
            _recentWorkspaces.RemoveAt(_recentWorkspaces.Count - 1);
        }
    }
}
