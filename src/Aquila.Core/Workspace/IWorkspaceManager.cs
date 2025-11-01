namespace Aquila.Core.Workspace;

/// <summary>
/// Manages workspaces which contain user-specific layouts, settings, and state.
/// </summary>
public interface IWorkspaceManager
{
    /// <summary>
    /// Get the currently active workspace.
    /// </summary>
    WorkspaceInfo? CurrentWorkspace { get; }

    /// <summary>
    /// Load a workspace by name or path.
    /// </summary>
    Task<WorkspaceInfo> LoadWorkspaceAsync(string nameOrPath);

    /// <summary>
    /// Save the current workspace.
    /// </summary>
    Task SaveWorkspaceAsync();

    /// <summary>
    /// Create a new workspace.
    /// </summary>
    Task<WorkspaceInfo> CreateWorkspaceAsync(string name);

    /// <summary>
    /// Delete a workspace.
    /// </summary>
    Task DeleteWorkspaceAsync(string name);

    /// <summary>
    /// Get list of recently accessed workspaces.
    /// </summary>
    IReadOnlyList<WorkspaceInfo> RecentWorkspaces { get; }

    /// <summary>
    /// Get all available workspaces.
    /// </summary>
    Task<IReadOnlyList<WorkspaceInfo>> GetAllWorkspacesAsync();
}

/// <summary>
/// Metadata about a workspace.
/// </summary>
public class WorkspaceInfo
{
    public required string Name { get; init; }
    public required string Path { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime LastAccessedAt { get; init; }

    /// <summary>
    /// Settings store path for this workspace.
    /// </summary>
    public string SettingsPath => System.IO.Path.Combine(Path, "settings");

    /// <summary>
    /// Cache path for this workspace.
    /// </summary>
    public string CachePath => System.IO.Path.Combine(Path, "cache");

    /// <summary>
    /// Layouts path for this workspace.
    /// </summary>
    public string LayoutsPath => System.IO.Path.Combine(Path, "layouts");
}
