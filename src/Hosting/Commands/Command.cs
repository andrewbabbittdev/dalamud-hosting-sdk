// Licensed to the Dalamud Hosting SDK Contributors under one or more agreements.
// The Dalamud Hosting SDK Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Dalamud.Hosting.Commands;

/// <summary>
/// Represents an abstract base class for defining executable commands with a specified name.
/// </summary>
/// <param name="name">The name of the command. Cannot be null or empty.</param>
/// <param name="helpMessage">The help message for the command. Cannot be null or empty.</param>
public abstract class Command(string name, string helpMessage)
{
    /// <summary>
    /// Gets the command name.
    /// </summary>
    public string Name { get; init; } = name;

    /// <summary>
    /// Gets the command help message.
    /// </summary>
    public string HelpMessage { get; init; } = helpMessage;

    /// <summary>
    /// Executes the command using the specified argument string.
    /// </summary>
    /// <param name="command">The command name.</param>
    /// <param name="args">A string containing the arguments to be used during command execution.</param>
    public abstract void OnExecute(string command, string args);
}
