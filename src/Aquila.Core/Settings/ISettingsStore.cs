namespace Aquila.Core.Settings;

/// <summary>
/// Provides key-value settings storage with change notifications.
/// </summary>
public interface ISettingsStore
{
    /// <summary>
    /// Get a setting value by key.
    /// </summary>
    /// <returns>The setting value or null if not found.</returns>
    object? Get(string key);

    /// <summary>
    /// Get a typed setting value.
    /// </summary>
    /// <returns>The setting value or null if not found or type mismatch.</returns>
    T? Get<T>(string key) where T : class;

    /// <summary>
    /// Get a setting value with a default.
    /// </summary>
    T GetOrDefault<T>(string key, T defaultValue) where T : notnull;

    /// <summary>
    /// Set a setting value.
    /// </summary>
    void Set(string key, object? value);

    /// <summary>
    /// Check if a setting exists.
    /// </summary>
    bool Contains(string key);

    /// <summary>
    /// Remove a setting.
    /// </summary>
    bool Remove(string key);

    /// <summary>
    /// Clear all settings.
    /// </summary>
    void Clear();

    /// <summary>
    /// Watch for changes to a specific setting.
    /// </summary>
    IDisposable Watch(string key, Action<object?> onChanged);

    /// <summary>
    /// Persist all pending changes to storage.
    /// </summary>
    Task SaveAsync();

    /// <summary>
    /// Load all settings from storage.
    /// </summary>
    Task LoadAsync();
}
