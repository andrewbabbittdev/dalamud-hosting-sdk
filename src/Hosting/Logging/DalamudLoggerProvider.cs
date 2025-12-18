// Licensed to the Dalamud Hosting SDK Contributors under one or more agreements.
// The Dalamud Hosting SDK Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Dalamud.Hosting.Logging;

/// <summary>
/// Provides an implementation of <see cref="ILoggerProvider"/> that creates loggers which write to the Dalamud plugin log system.
/// </summary>
/// <param name="pluginInterface">The plugin interface used to interact with the Dalamud environment.</param>
/// <param name="log">The logging service used to record log messages.</param>
public sealed class DalamudLoggerProvider(IDalamudPluginInterface pluginInterface, IPluginLog log) : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, DalamudLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new DalamudLogger(pluginInterface, log, name));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _loggers.Clear();
    }
}
