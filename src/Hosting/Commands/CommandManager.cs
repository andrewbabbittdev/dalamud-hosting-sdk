// Licensed to the Dalamud Hosting SDK Contributors under one or more agreements.
// The Dalamud Hosting SDK Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Dalamud.Plugin.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dalamud.Hosting.Commands;

/// <summary>
/// Provides management and coordination of plugin commands.
/// </summary>
/// <param name="serviceProvider">The service provider used to resolve window instances for registration and management.</param>
/// <param name="commandManager">The command manager used to register and manage commands.</param>
public class CommandManager(IServiceProvider serviceProvider, ICommandManager commandManager) : IHostedService
{
    private List<Command> _commands = [];

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _commands = [.. serviceProvider.GetServices<Command>()];

        foreach (var command in _commands)
        {
            commandManager.AddHandler(command.Name, new(command.OnExecute)
            {
                HelpMessage = command.HelpMessage,
            });
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var command in _commands)
        {
            commandManager.RemoveHandler(command.Name);
        }

        return Task.CompletedTask;
    }
}
