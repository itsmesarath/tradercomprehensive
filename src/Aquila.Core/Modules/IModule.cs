namespace Aquila.Core.Modules;

/// <summary>
/// Represents a loadable module in the Aquila platform.
/// Modules are initialized in dependency order and provide lifecycle management.
/// </summary>
public interface IModule
{
    /// <summary>
    /// Module name identifier.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Module version.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// List of module names this module depends on (by Name).
    /// </summary>
    IReadOnlyList<string> Dependencies { get; }

    /// <summary>
    /// Initialize the module. Called once on application startup.
    /// </summary>
    Task Initialize(IModuleContext context);

    /// <summary>
    /// Configure the module. Called after all modules are initialized.
    /// </summary>
    Task Configure(IModuleContext context);

    /// <summary>
    /// Shutdown the module. Called on application shutdown.
    /// </summary>
    Task Shutdown();
}

/// <summary>
/// Context provided to modules during initialization and configuration.
/// </summary>
public interface IModuleContext
{
    /// <summary>
    /// Get a registered service from the DI container.
    /// </summary>
    T Resolve<T>() where T : notnull;

    /// <summary>
    /// Get a registered service by type.
    /// </summary>
    object Resolve(Type type);

    /// <summary>
    /// Access the messaging bus for event publishing/subscription.
    /// </summary>
    IMessagingBus MessagingBus { get; }

    /// <summary>
    /// Access the settings store.
    /// </summary>
    ISettingsStore SettingsStore { get; }
}
