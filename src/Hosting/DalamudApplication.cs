// Licensed to the Dalamud Hosting SDK Contributors under one or more agreements.
// The Dalamud Hosting SDK Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Dalamud.Plugin;
using Microsoft.Extensions.Hosting;

namespace Dalamud.Hosting;

/// <summary>
/// Provides a host for running a Dalamud-based application, managing its lifetime and services.
/// </summary>
public sealed class DalamudApplication : IHost
{
    private readonly IHost _host;

    /// <inheritdoc/>
    public IServiceProvider Services => _host.Services;

    internal DalamudApplication(IHost host)
    {
        _host = host;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="DalamudApplicationBuilder"/> using the specified plugin interface.
    /// </summary>
    /// <param name="pluginInterface">The plugin interface used to initialize the application builder. Cannot be null.</param>
    /// <returns>A <see cref="DalamudApplicationBuilder"/> instance configured with the provided plugin interface.</returns>
    public static DalamudApplicationBuilder CreateBuilder(IDalamudPluginInterface pluginInterface)
    {
        return new(pluginInterface);
    }

    /// <summary>
    /// Starts the application synchronously.
    /// </summary>
    public void Start()
    {
        StartAsync().GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        return _host.StartAsync(cancellationToken);
    }

    /// <summary>
    /// Stops the application synchronously.
    /// </summary>
    public void Stop()
    {
        StopAsync().GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        return _host.StopAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _host.Dispose();
    }
}
