// Licensed to the Dalamud Hosting SDK Contributors under one or more agreements.
// The Dalamud Hosting SDK Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dalamud.Hosting.Windowing;

/// <summary>
/// Provides management and coordination of plugin window within the plugin's UI lifecycle.
/// </summary>
/// <param name="serviceProvider">The service provider used to resolve window instances for registration and management.</param>
/// <param name="pluginInterface">The plugin interface used to integrate with the plugin's UI builder and event system.</param>
public class WindowManager(IServiceProvider serviceProvider, IDalamudPluginInterface pluginInterface) : IHostedService
{
    private readonly WindowSystem _windowSystem = new(pluginInterface.InternalName);
    private Window? _mainWindow;
    private Window? _configWindow;

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var windows = serviceProvider.GetServices<Window>();

        foreach (var window in windows)
        {
            var windowType = window.GetType().Name;

            if (windowType == "MainWindow")
            {
                _mainWindow = window;
            }
            else if (windowType == "ConfigWindow")
            {
                _configWindow = window;
            }

            _windowSystem.AddWindow(window);
        }

        pluginInterface.UiBuilder.Draw += _windowSystem.Draw;

        if (_mainWindow is not null)
        {
            pluginInterface.UiBuilder.OpenMainUi += _mainWindow.Toggle;
        }

        if (_configWindow is not null)
        {
            pluginInterface.UiBuilder.OpenConfigUi += _configWindow.Toggle;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        pluginInterface.UiBuilder.Draw -= _windowSystem.Draw;

        if (_mainWindow is not null)
        {
            pluginInterface.UiBuilder.OpenMainUi -= _mainWindow.Toggle;
        }

        if (_configWindow is not null)
        {
            pluginInterface.UiBuilder.OpenConfigUi -= _configWindow.Toggle;
        }

        _windowSystem.RemoveAllWindows();

        return Task.CompletedTask;
    }
}
