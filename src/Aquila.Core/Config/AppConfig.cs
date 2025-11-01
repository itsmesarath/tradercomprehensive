namespace Aquila.Core.Config;

/// <summary>
/// Application-level configuration settings.
/// </summary>
public class AppConfig
{
    /// <summary>
    /// Application name.
    /// </summary>
    public string AppName { get; set; } = "Aquila";

    /// <summary>
    /// Application version.
    /// </summary>
    public string Version { get; set; } = "0.1.0";

    /// <summary>
    /// Default theme name.
    /// </summary>
    public string DefaultTheme { get; set; } = "Light";

    /// <summary>
    /// Default UI language code (e.g., "en", "zh-CN").
    /// </summary>
    public string DefaultLanguage { get; set; } = "en";

    /// <summary>
    /// Directory for plugin DLLs.
    /// </summary>
    public string PluginDirectory { get; set; } = "plugins";

    /// <summary>
    /// List of enabled brokers/connectors.
    /// </summary>
    public List<string> EnabledConnectors { get; set; } = new() { "Binance", "CQG" };

    /// <summary>
    /// Enable gRPC out-of-process plugins.
    /// </summary>
    public bool EnableGrpcPlugins { get; set; } = true;

    /// <summary>
    /// Logging level (Verbose, Debug, Information, Warning, Error, Fatal).
    /// </summary>
    public string LogLevel { get; set; } = "Information";

    /// <summary>
    /// Window state on last close (for restoration).
    /// </summary>
    public WindowState? LastWindowState { get; set; }
}

/// <summary>
/// Saved window state for restoration on startup.
/// </summary>
public class WindowState
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; } = 1200;
    public int Height { get; set; } = 800;
    public bool IsMaximized { get; set; }
    public int MonitorIndex { get; set; }
}
