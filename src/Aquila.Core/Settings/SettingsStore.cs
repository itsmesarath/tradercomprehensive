using System.Text.Json;
using Serilog;

namespace Aquila.Core.Settings;

/// <summary>
/// File-based settings store using JSON for persistence.
/// </summary>
public class SettingsStore : ISettingsStore
{
    private readonly string _settingsPath;
    private readonly Dictionary<string, object?> _settings;
    private readonly Dictionary<string, List<Action<object?>>> _watchers;
    private readonly object _lockObj = new();
    private readonly ILogger _logger;
    private bool _isDirty;

    public SettingsStore(string settingsDirectory)
    {
        _settingsPath = Path.Combine(settingsDirectory, "settings.json");
        _settings = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        _watchers = new Dictionary<string, List<Action<object?>>>(StringComparer.OrdinalIgnoreCase);
        _logger = Log.ForContext<SettingsStore>();

        // Ensure directory exists
        Directory.CreateDirectory(settingsDirectory);
    }

    public object? Get(string key)
    {
        lock (_lockObj)
        {
            _settings.TryGetValue(key, out var value);
            return value;
        }
    }

    public T? Get<T>(string key) where T : class
    {
        var value = Get(key);
        return value as T;
    }

    public T GetOrDefault<T>(string key, T defaultValue) where T : notnull
    {
        var value = Get(key);
        return (value as T) ?? defaultValue;
    }

    public void Set(string key, object? value)
    {
        lock (_lockObj)
        {
            var oldValue = _settings.TryGetValue(key, out var existing) ? existing : null;

            if (Equals(oldValue, value))
                return;

            _settings[key] = value;
            _isDirty = true;

            if (_watchers.TryGetValue(key, out var watchers))
            {
                foreach (var watcher in watchers.ToList())
                {
                    try
                    {
                        watcher(value);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Error in settings watcher for key {Key}", key);
                    }
                }
            }
        }
    }

    public bool Contains(string key)
    {
        lock (_lockObj)
        {
            return _settings.ContainsKey(key);
        }
    }

    public bool Remove(string key)
    {
        lock (_lockObj)
        {
            var removed = _settings.Remove(key);
            if (removed)
            {
                _isDirty = true;
            }
            return removed;
        }
    }

    public void Clear()
    {
        lock (_lockObj)
        {
            if (_settings.Count > 0)
            {
                _settings.Clear();
                _isDirty = true;
            }
        }
    }

    public IDisposable Watch(string key, Action<object?> onChanged)
    {
        lock (_lockObj)
        {
            if (!_watchers.ContainsKey(key))
            {
                _watchers[key] = new List<Action<object?>>();
            }

            _watchers[key].Add(onChanged);

            return new WatcherDisposable(() =>
            {
                lock (_lockObj)
                {
                    if (_watchers.TryGetValue(key, out var list))
                    {
                        list.Remove(onChanged);
                    }
                }
            });
        }
    }

    public async Task SaveAsync()
    {
        lock (_lockObj)
        {
            if (!_isDirty)
                return;

            _isDirty = false;
        }

        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_settings, options);

            // Write to temp file first, then rename for atomicity
            var tempPath = _settingsPath + ".tmp";
            await File.WriteAllTextAsync(tempPath, json);
            File.Move(tempPath, _settingsPath, overwrite: true);

            _logger.Debug("Settings saved to {SettingsPath}", _settingsPath);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to save settings");
            throw;
        }
    }

    public async Task LoadAsync()
    {
        lock (_lockObj)
        {
            _settings.Clear();
            _isDirty = false;
        }

        if (!File.Exists(_settingsPath))
        {
            _logger.Debug("Settings file does not exist: {SettingsPath}", _settingsPath);
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_settingsPath);
            var loaded = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)
                ?? new Dictionary<string, JsonElement>();

            lock (_lockObj)
            {
                foreach (var kvp in loaded)
                {
                    _settings[kvp.Key] = DeserializeJsonElement(kvp.Value);
                }
            }

            _logger.Information("Settings loaded from {SettingsPath}", _settingsPath);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load settings from {SettingsPath}", _settingsPath);
            throw;
        }
    }

    private object? DeserializeJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Array => element.EnumerateArray().Select(DeserializeJsonElement).ToList(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(
                p => p.Name,
                p => DeserializeJsonElement(p.Value)
            ),
            _ => element.GetRawText()
        };
    }

    private class WatcherDisposable : IDisposable
    {
        private readonly Action _dispose;
        private bool _disposed;

        public WatcherDisposable(Action dispose)
        {
            _dispose = dispose;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _dispose();
        }
    }
}
