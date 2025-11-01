using Autofac;

namespace Aquila.Core.DI;

/// <summary>
/// Wraps Autofac DI container for Aquila.
/// </summary>
public class DIContainer : IDisposable
{
    private readonly IContainer _container;

    public DIContainer(IContainer container)
    {
        _container = container;
    }

    /// <summary>
    /// Get a registered service.
    /// </summary>
    public T Resolve<T>() where T : notnull
    {
        return _container.Resolve<T>();
    }

    /// <summary>
    /// Get a registered service by type.
    /// </summary>
    public object Resolve(Type type)
    {
        return _container.Resolve(type);
    }

    /// <summary>
    /// Try to get a registered service.
    /// </summary>
    public bool TryResolve<T>(out T instance) where T : notnull
    {
        return _container.TryResolve<T>(out instance);
    }

    /// <summary>
    /// Create a new scope for scoped services.
    /// </summary>
    public ILifetimeScope BeginLifetimeScope()
    {
        return _container.BeginLifetimeScope();
    }

    public void Dispose()
    {
        _container.Dispose();
    }
}

/// <summary>
/// Builder for DIContainer with fluent API.
/// </summary>
public class DIContainerBuilder
{
    private readonly ContainerBuilder _builder;

    public DIContainerBuilder()
    {
        _builder = new ContainerBuilder();
    }

    /// <summary>
    /// Register a singleton service.
    /// </summary>
    public DIContainerBuilder RegisterSingleton<TInterface, TImplementation>()
        where TInterface : notnull
        where TImplementation : notnull, TInterface
    {
        _builder.RegisterType<TImplementation>().As<TInterface>().SingleInstance();
        return this;
    }

    /// <summary>
    /// Register a singleton service instance.
    /// </summary>
    public DIContainerBuilder RegisterSingletonInstance<T>(T instance) where T : notnull
    {
        _builder.RegisterInstance(instance).As<T>();
        return this;
    }

    /// <summary>
    /// Register a scoped service.
    /// </summary>
    public DIContainerBuilder RegisterScoped<TInterface, TImplementation>()
        where TInterface : notnull
        where TImplementation : notnull, TInterface
    {
        _builder.RegisterType<TImplementation>().As<TInterface>().InstancePerLifetimeScope();
        return this;
    }

    /// <summary>
    /// Register a transient service.
    /// </summary>
    public DIContainerBuilder RegisterTransient<TInterface, TImplementation>()
        where TInterface : notnull
        where TImplementation : notnull, TInterface
    {
        _builder.RegisterType<TImplementation>().As<TInterface>().InstancePerDependency();
        return this;
    }

    /// <summary>
    /// Register a factory delegate.
    /// </summary>
    public DIContainerBuilder RegisterFactory<T>(Func<ILifetimeScope, T> factory) where T : notnull
    {
        _builder.Register(c => factory(c)).As<T>();
        return this;
    }

    /// <summary>
    /// Build the container.
    /// </summary>
    public DIContainer Build()
    {
        return new DIContainer(_builder.Build());
    }
}
