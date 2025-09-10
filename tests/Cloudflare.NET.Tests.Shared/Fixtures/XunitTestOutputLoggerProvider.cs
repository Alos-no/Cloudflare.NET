namespace Cloudflare.NET.Tests.Shared.Fixtures;

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

/// <summary>
///   ILoggerProvider that writes logs to xUnit's ITestOutputHelper. This version is
///   designed to be compatible with IClassFixture by using an AsyncLocal to store the
///   ITestOutputHelper for the currently executing test.
/// </summary>
public sealed class XunitTestOutputLoggerProvider : ILoggerProvider
{
  #region Properties & Fields - Non-Public

  private readonly ConcurrentDictionary<string, XunitLogger> _loggers = new();

  /// <summary>
  ///   This allows the ITestOutputHelper to be set on a per-test basis, making the provider
  ///   safe to use as a singleton within a test fixture.
  /// </summary>
  private readonly AsyncLocal<ITestOutputHelper?> _testOutputHelper = new();

  #endregion

  #region Constructors

  public void Dispose() => _loggers.Clear();

  #endregion

  #region Properties & Fields - Public

  /// <summary>
  ///   Gets or sets the current ITestOutputHelper. This should be set by the test class
  ///   constructor.
  /// </summary>
  public ITestOutputHelper? Current
  {
    get => _testOutputHelper.Value;
    set => _testOutputHelper.Value = value;
  }

  #endregion

  #region Methods Impl

  public ILogger CreateLogger(string categoryName) =>
    _loggers.GetOrAdd(categoryName, name => new XunitLogger(this, name));

  #endregion

  private sealed class XunitLogger(XunitTestOutputLoggerProvider provider, string category) : ILogger
  {
    #region Constants & Statics

    private static readonly object _gate = new();

    #endregion

    #region Methods Impl

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state)
      where TState : notnull =>
      default;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    /// <inheritdoc />
    public void Log<TState>(LogLevel                         logLevel,
                            EventId                          eventId,
                            TState                           state,
                            Exception?                       exception,
                            Func<TState, Exception?, string> formatter)
    {
      if (!IsEnabled(logLevel))
        return;

      var output = provider.Current;
      if (output is null)
        return;


      var message = formatter(state, exception);
      lock (_gate)
      {
        output.WriteLine($"[{DateTimeOffset.UtcNow:O}] {logLevel,-11} {category} {eventId.Id} {message}");
        if (exception is not null)
          output.WriteLine(exception.ToString());
      }
    }

    #endregion
  }
}
