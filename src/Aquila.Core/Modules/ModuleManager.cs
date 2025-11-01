using System.Collections.Immutable;
using Serilog;

namespace Aquila.Core.Modules;

/// <summary>
/// Manages the lifecycle of modules including initialization, configuration, and shutdown.
/// </summary>
public class ModuleManager
{
    private readonly IModuleContext _context;
    private readonly Dictionary<string, IModule> _modules;
    private readonly ILogger _logger;

    public ModuleManager(IModuleContext context)
    {
        _context = context;
        _modules = new Dictionary<string, IModule>();
        _logger = Log.ForContext<ModuleManager>();
    }

    /// <summary>
    /// Register a module to be loaded.
    /// </summary>
    public void RegisterModule(IModule module)
    {
        if (_modules.ContainsKey(module.Name))
        {
            throw new InvalidOperationException($"Module '{module.Name}' is already registered.");
        }

        _modules[module.Name] = module;
        _logger.Debug("Module registered: {ModuleName} v{Version}", module.Name, module.Version);
    }

    /// <summary>
    /// Initialize all registered modules in dependency order.
    /// </summary>
    public async Task InitializeAsync()
    {
        var initOrder = ResolveDependencies();

        foreach (var moduleName in initOrder)
        {
            var module = _modules[moduleName];
            try
            {
                _logger.Information("Initializing module: {ModuleName}", module.Name);
                await module.Initialize(_context);
                _logger.Information("Module initialized: {ModuleName}", module.Name);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to initialize module: {ModuleName}", module.Name);
                throw;
            }
        }
    }

    /// <summary>
    /// Configure all registered modules.
    /// </summary>
    public async Task ConfigureAsync()
    {
        var configOrder = ResolveDependencies();

        foreach (var moduleName in configOrder)
        {
            var module = _modules[moduleName];
            try
            {
                _logger.Information("Configuring module: {ModuleName}", module.Name);
                await module.Configure(_context);
                _logger.Information("Module configured: {ModuleName}", module.Name);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to configure module: {ModuleName}", module.Name);
                throw;
            }
        }
    }

    /// <summary>
    /// Shutdown all modules in reverse dependency order.
    /// </summary>
    public async Task ShutdownAsync()
    {
        var shutdownOrder = ResolveDependencies().Reverse();

        foreach (var moduleName in shutdownOrder)
        {
            var module = _modules[moduleName];
            try
            {
                _logger.Information("Shutting down module: {ModuleName}", module.Name);
                await module.Shutdown();
                _logger.Information("Module shutdown complete: {ModuleName}", module.Name);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error during module shutdown: {ModuleName}", module.Name);
            }
        }
    }

    /// <summary>
    /// Get a registered module by name.
    /// </summary>
    public IModule? GetModule(string name)
    {
        _modules.TryGetValue(name, out var module);
        return module;
    }

    /// <summary>
    /// Get all registered modules.
    /// </summary>
    public IReadOnlyDictionary<string, IModule> GetModules() => _modules.AsReadOnly();

    private List<string> ResolveDependencies()
    {
        var result = new List<string>();
        var visited = new HashSet<string>();
        var visiting = new HashSet<string>();

        foreach (var moduleName in _modules.Keys)
        {
            VisitDependency(moduleName, result, visited, visiting);
        }

        return result;
    }

    private void VisitDependency(string moduleName, List<string> result, HashSet<string> visited, HashSet<string> visiting)
    {
        if (visited.Contains(moduleName))
            return;

        if (visiting.Contains(moduleName))
            throw new InvalidOperationException($"Circular dependency detected involving module '{moduleName}'.");

        if (!_modules.TryGetValue(moduleName, out var module))
            throw new InvalidOperationException($"Module '{moduleName}' not found.");

        visiting.Add(moduleName);

        foreach (var dep in module.Dependencies)
        {
            VisitDependency(dep, result, visited, visiting);
        }

        visiting.Remove(moduleName);
        visited.Add(moduleName);
        result.Add(moduleName);
    }
}
