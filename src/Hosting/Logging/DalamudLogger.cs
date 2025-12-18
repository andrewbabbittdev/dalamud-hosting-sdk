// Licensed to the Dalamud Hosting SDK Contributors under one or more agreements.
// The Dalamud Hosting SDK Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.Logging;
using Serilog.Events;
using Serilog.Parsing;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;

namespace Dalamud.Hosting.Logging;

/// <summary>
/// Provides an implementation of <see cref="ILogger"/> that logs messages using the Dalamud plugin logging infrastructure.
/// </summary>
/// <param name="pluginInterface">The Dalamud plugin interface used to access plugin services.</param>
/// <param name="log">The plugin log instance used to write log messages.</param>
/// <param name="name">The name of the logger.</param>
public sealed class DalamudLogger(IDalamudPluginInterface pluginInterface, IPluginLog log, string name) : ILogger
{
    private static readonly ConcurrentDictionary<string, string> s_destructureDictionary = new();
    private static readonly ConcurrentDictionary<string, string> s_stringifyDictionary = new();
    private readonly MessageTemplateParser _messageTemplateParser = new();

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None && log.Logger.IsEnabled(ToSerilogLevel(logLevel));
    }

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (logLevel == LogLevel.None)
        {
            return;
        }

        var level = ToSerilogLevel(logLevel);

        if (!log.Logger.IsEnabled(level))
        {
            return;
        }

        var logEvent = PrepareWrite(level, eventId, state, exception, formatter);

        if (logEvent != null)
        {
            log.Logger.Write(logEvent);
        }
    }

    private LogEvent PrepareWrite<TState>(LogEventLevel level, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        string? messageTemplate = null;
        var properties = new Dictionary<string, LogEventPropertyValue>();

        if (state is IEnumerable<KeyValuePair<string, object?>> structure)
        {
            foreach (var property in structure)
            {
                if (property is { Key: "{OriginalFormat}", Value: string value })
                {
                    messageTemplate = value;
                }
                else if (property.Key.StartsWith('@'))
                {
                    if (log.Logger.BindProperty(GetKeyWithoutFirstSymbol(s_destructureDictionary, property.Key), property.Value, true, out var destructured))
                    {
                        properties[destructured.Name] = destructured.Value;
                    }
                }
                else if (property.Key.StartsWith('$'))
                {
                    if (log.Logger.BindProperty(GetKeyWithoutFirstSymbol(s_stringifyDictionary, property.Key), property.Value?.ToString(), true, out var stringified))
                    {
                        properties[stringified.Name] = stringified.Value;
                    }
                }
                else
                {
                    if (property.Value is null or string or int or long && LogEventProperty.IsValidName(property.Key))
                    {
                        properties[property.Key] = new ScalarValue(property.Value);
                    }
                    else if (log.Logger.BindProperty(property.Key, property.Value, false, out var bound))
                    {
                        properties[bound.Name] = bound.Value;
                    }
                }
            }

            var stateType = state.GetType();
            var stateTypeInfo = stateType.GetTypeInfo();

            if (messageTemplate == null && !stateTypeInfo.IsGenericType)
            {
                messageTemplate = "{" + stateType.Name + ":l}";

                if (log.Logger.BindProperty(stateType.Name, AsLoggableValue(state, formatter), false, out var stateTypeProperty))
                {
                    properties[stateTypeProperty.Name] = stateTypeProperty.Value;
                }
            }
        }

        if (messageTemplate == null)
        {
            string? propertyName = null;

            if (state != null)
            {
                propertyName = "State";
                messageTemplate = "{State:l}";
            }
            else if (formatter != null!)
            {
                propertyName = "Message";
                messageTemplate = "{Message:l}";
            }

            if (propertyName != null)
            {
                if (log.Logger.BindProperty(propertyName, AsLoggableValue(state, formatter!), false, out var property))
                {
                    properties[property.Name] = property.Value;
                }
            }
        }

        if (eventId.Id != 0 || eventId.Name != null)
        {
            properties["EventId"] = new StructureValue(
            [
                new("Id", new ScalarValue(eventId.Id)),
                new("Name", new ScalarValue(eventId.Name))
            ]);
        }

        var (traceId, spanId) = Activity.Current is { } activity
            ? (activity.TraceId, activity.SpanId)
            : (default(ActivityTraceId), default(ActivitySpanId));

        messageTemplate = $"[{pluginInterface.InternalName}] [{name}] {messageTemplate}";

        var parsedTemplate = _messageTemplateParser.Parse(messageTemplate);
        return LogEvent.UnstableAssembleFromParts(DateTimeOffset.Now, level, exception, parsedTemplate, properties, traceId, spanId);
    }

    private static LogEventLevel ToSerilogLevel(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.None => LevelAlias.Off,
            LogLevel.Critical => LogEventLevel.Fatal,
            LogLevel.Error => LogEventLevel.Error,
            LogLevel.Warning => LogEventLevel.Warning,
            LogLevel.Information => LogEventLevel.Information,
            LogLevel.Debug => LogEventLevel.Debug,
            LogLevel.Trace => LogEventLevel.Verbose,
            _ => LogEventLevel.Verbose,
        };
    }

    private static string GetKeyWithoutFirstSymbol(ConcurrentDictionary<string, string> source, string key)
    {
        if (source.TryGetValue(key, out var value))
        {
            return value;
        }

        if (source.Count < 1000)
        {
            return source.GetOrAdd(key, k => k[1..]);
        }

        return key[1..];
    }

    private static object? AsLoggableValue<TState>(TState state, Func<TState, Exception?, string> formatter)
    {
        object? stateObj = null;

        if (formatter != null)
        {
            stateObj = formatter(state, null);
        }

        return stateObj ?? state;
    }
}
