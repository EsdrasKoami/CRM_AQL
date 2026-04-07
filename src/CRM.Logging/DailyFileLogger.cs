using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace CRM.Logging;

/// <summary>
/// Logger custom qui écrit dans un fichier BEYYYYMMDD.log quotidien.
/// Chaque ligne suit le format : [HH:mm:ss] [LEVEL] [Source] Message
/// Thread-safe via ConcurrentQueue et flush synchrone.
/// </summary>
public sealed class DailyFileLogger : ILogger, IDisposable
{
    private readonly string _categoryName;
    private readonly DailyFileLoggerOptions _options;
    private readonly DailyFileLoggerProvider _provider;

    public DailyFileLogger(string categoryName, DailyFileLoggerOptions options, DailyFileLoggerProvider provider)
    {
        _categoryName = categoryName;
        _options      = options;
        _provider     = provider;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= _options.MinLevel;

    public void Log<TState>(
        LogLevel logLevel, EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var time     = DateTime.Now.ToString("HH:mm:ss");
        var level    = logLevel.ToString().ToUpperInvariant()[..4]; // INFO, WARN, ERRO, CRIT
        var source   = _categoryName.Split('.').LastOrDefault() ?? _categoryName;
        var message  = formatter(state, exception);
        var line     = $"[{time}] [{level}] [{source}] {message}";

        if (exception is not null)
            line += Environment.NewLine + $"         Exception: {exception}";

        _provider.Enqueue(line);
    }

    public void Dispose() { }
}

/// <summary>Options du logger quotidien.</summary>
public class DailyFileLoggerOptions
{
    public string LogDirectory { get; set; } = "logs";
    public LogLevel MinLevel { get; set; } = LogLevel.Information;
}

/// <summary>
/// Provider qui crée les loggers et écrit les lignes dans le fichier BE{yyyyMMdd}.log.
/// Un seul fichier par jour ; un nouveau fichier est créé à minuit.
/// </summary>
public sealed class DailyFileLoggerProvider : ILoggerProvider
{
    private readonly DailyFileLoggerOptions _options;
    private readonly ConcurrentQueue<string> _queue = new();
    private readonly ConcurrentDictionary<string, DailyFileLogger> _loggers = new();

    private StreamWriter? _writer;
    private string? _currentFilePath;
    private readonly object _lock = new();

    public DailyFileLoggerProvider(DailyFileLoggerOptions options)
    {
        _options = options;
        Directory.CreateDirectory(options.LogDirectory);
    }

    public ILogger CreateLogger(string categoryName)
        => _loggers.GetOrAdd(categoryName, name => new DailyFileLogger(name, _options, this));

    public void Enqueue(string line)
    {
        _queue.Enqueue(line);
        Flush();
    }

    private void Flush()
    {
        lock (_lock)
        {
            EnsureFileWriter();
            while (_queue.TryDequeue(out var line))
            {
                _writer!.WriteLine(line);
            }
            _writer!.Flush();
        }
    }

    private void EnsureFileWriter()
    {
        var expectedPath = Path.Combine(
            _options.LogDirectory,
            $"BE{DateTime.Now:yyyyMMdd}.log");

        if (_currentFilePath == expectedPath && _writer is not null) return;

        // Nouveau jour → nouveau fichier
        _writer?.Dispose();
        _currentFilePath = expectedPath;
        _writer = new StreamWriter(expectedPath, append: true, encoding: System.Text.Encoding.UTF8);
        _writer.AutoFlush = false;
    }

    public void Dispose()
    {
        lock (_lock)
        {
            Flush();
            _writer?.Dispose();
        }
    }
}

/// <summary>Extension pour enregistrer le logger dans le DI.</summary>
public static class DailyFileLoggerExtensions
{
    public static ILoggingBuilder AddDailyFileLogger(
        this ILoggingBuilder builder,
        Action<DailyFileLoggerOptions>? configure = null)
    {
        var options = new DailyFileLoggerOptions();
        configure?.Invoke(options);
        builder.AddProvider(new DailyFileLoggerProvider(options));
        return builder;
    }
}
