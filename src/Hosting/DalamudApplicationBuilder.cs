// Licensed to the Dalamud Hosting SDK Contributors under one or more agreements.
// The Dalamud Hosting SDK Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Dalamud.Hosting.Commands;
using Dalamud.Hosting.Logging;
using Dalamud.Hosting.Windowing;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dalamud.Hosting;

/// <summary>
/// Provides a builder for configuring and constructing a Dalamud host application, including services, configuration, logging, and environment settings.
/// </summary>
public sealed class DalamudApplicationBuilder : IHostApplicationBuilder
{
    private readonly HostApplicationBuilder _hostApplicationBuilder;

    /// <inheritdoc/>
    public IDictionary<object, object> Properties => ((IHostApplicationBuilder)_hostApplicationBuilder).Properties;

    /// <inheritdoc/>
    public IConfigurationManager Configuration => _hostApplicationBuilder.Configuration;

    /// <inheritdoc/>
    public IHostEnvironment Environment => _hostApplicationBuilder.Environment;

    /// <inheritdoc/>
    public ILoggingBuilder Logging => _hostApplicationBuilder.Logging;

    /// <inheritdoc/>
    public IMetricsBuilder Metrics => _hostApplicationBuilder.Metrics;

    /// <inheritdoc/>
    public IServiceCollection Services => _hostApplicationBuilder.Services;

    internal DalamudApplicationBuilder(IDalamudPluginInterface pluginInterface)
    {
        var settings = new HostApplicationBuilderSettings
        {
            ApplicationName = pluginInterface.InternalName,
            ContentRootPath = pluginInterface.AssemblyLocation.DirectoryName,
            EnvironmentName = pluginInterface.IsDev ? Environments.Development : Environments.Production
        };

        _hostApplicationBuilder = Host.CreateEmptyApplicationBuilder(settings);

        ApplyDefaultAppConfiguration(_hostApplicationBuilder, _hostApplicationBuilder.Configuration, pluginInterface);
        AddDefaultServices(_hostApplicationBuilder, _hostApplicationBuilder.Services);
        AddDalamudServices(_hostApplicationBuilder, pluginInterface);

        _hostApplicationBuilder.Services.AddHostedService<WindowManager>();
        _hostApplicationBuilder.Services.AddHostedService<CommandManager>();
    }

    /// <inheritdoc/>
    public void ConfigureContainer<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory, Action<TContainerBuilder>? configure = null) where TContainerBuilder : notnull
    {
        _hostApplicationBuilder.ConfigureContainer(factory, configure);
    }

    /// <summary>
    /// Registers all windows in the assembly containing the specified type.
    /// </summary>
    /// <typeparam name="TAssembly">A type from the assembly whose Window-derived classes will be registered.</typeparam>
    public void AddWindows<TAssembly>()
    {
        var windows = typeof(TAssembly).Assembly
            .GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(Window)));

        foreach (var window in windows)
        {
            _hostApplicationBuilder.Services.AddSingleton(window);
            _hostApplicationBuilder.Services.AddSingleton(typeof(Window), services => services.GetRequiredService(window));
        }
    }

    /// <summary>
    /// Registers all commands in the assembly containing the specified type.
    /// </summary>
    /// <typeparam name="TAssembly">A type from the assembly whose Command-derived classes will be registered.</typeparam>
    public void AddCommands<TAssembly>()
    {
        var commands = typeof(TAssembly).Assembly
            .GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(Command)));

        foreach (var command in commands)
        {
            _hostApplicationBuilder.Services.AddSingleton(command);
            _hostApplicationBuilder.Services.AddSingleton(typeof(Command), services => services.GetRequiredService(command));
        }
    }

    /// <summary>
    /// Builds and returns a configured instance of the <see cref="DalamudApplication"/> based on the current application builder settings.
    /// </summary>
    /// <returns>A <see cref="DalamudApplication"/> instance initialized with the configured application services and settings.</returns>
    public DalamudApplication Build()
    {
        return new(_hostApplicationBuilder.Build());
    }

    private static void ApplyDefaultAppConfiguration(HostApplicationBuilder builder, IConfigurationBuilder appConfigBuilder, IDalamudPluginInterface pluginInterface)
    {
        appConfigBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddYamlFile("appsettings.yaml", optional: true, reloadOnChange: true)
            .AddYamlFile($"appsettings.{builder.Environment.EnvironmentName}.yaml", optional: true, reloadOnChange: true)
            .AddJsonFile(pluginInterface.ConfigFile.FullName, optional: true, reloadOnChange: true);
    }

    private static void AddDefaultServices(IHostApplicationBuilder builder, IServiceCollection services)
    {
        services.AddLogging(logging =>
        {
            logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

            logging.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, DalamudLoggerProvider>());

            logging.Configure(options =>
            {
                options.ActivityTrackingOptions =
                    ActivityTrackingOptions.SpanId |
                    ActivityTrackingOptions.TraceId |
                    ActivityTrackingOptions.ParentId;
            });
        });

        services.AddMetrics(metrics =>
        {
            metrics.AddConfiguration(builder.Configuration.GetSection("Metrics"));
        });
    }

    private static void AddDalamudServices(HostApplicationBuilder builder, IDalamudPluginInterface pluginInterface)
    {
        builder.Services.AddSingleton(pluginInterface);

        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<IAddonEventManager>());
        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<IAddonLifecycle>());
        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<IAetheryteList>());
        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<IBuddyList>());
        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<IChatGui>());
        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<IClientState>());
        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<ICommandManager>());
        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<ICondition>());
        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<IConsole>());
        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<IContextMenu>());
        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<IDalamudService>());
        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<IDataManager>());
        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<IDtrBar>());
        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<IDutyState>());
        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<IFateTable>());
        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<IFlyTextGui>());
        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<IFramework>());
        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<IGameConfig>());
        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<IGameGui>());
        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<IGameInteropProvider>());
        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<IGameInventory>());
        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<IGameLifecycle>());
        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<IGamepadState>());
        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<IJobGauges>());
        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<IKeyState>());
        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<IMarketBoard>());
        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<INamePlateGui>());
        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<INotificationManager>());
        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<IObjectTable>());
        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<IPartyFinderGui>());
        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<IPartyList>());
        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<IPlayerState>());
        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<IPluginLinkHandler>());
        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<IPluginLog>());
        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<IReliableFileStorage>());
        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<ISelfTestRegistry>());
        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<ISeStringEvaluator>());
        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<ISigScanner>());
        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<ITargetManager>());
        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<ITextureProvider>());
        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<ITextureReadbackProvider>());
        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<ITextureSubstitutionProvider>());
        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<ITitleScreenMenu>());
        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<IToastGui>());
        builder.Services.AddSingleton(_ => pluginInterface.GetRequiredService<IUnlockState>());
    }
}
