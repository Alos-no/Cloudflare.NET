namespace Cloudflare.NET.Tests.Shared.Helpers;

using Xunit;

/// <summary>
///   Helpers for validating parameter/argument exceptions.
///   Use this to test null/empty string validation consistently.
/// </summary>
public static class ParameterValidationTestHelpers
{
  #region ArgumentNullException Assertions

  /// <summary>
  ///   Asserts that a method throws ArgumentNullException for a null parameter.
  /// </summary>
  /// <typeparam name="T">Return type of the async method.</typeparam>
  /// <param name="action">The async action to execute.</param>
  /// <param name="paramName">Expected parameter name in the exception.</param>
  /// <returns>The thrown ArgumentNullException for further inspection.</returns>
  public static async Task<ArgumentNullException> AssertThrowsArgumentNullAsync<T>(
    Func<Task<T>> action,
    string paramName)
  {
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () => await action());

    Assert.Equal(paramName, exception.ParamName);

    return exception;
  }

  /// <summary>
  ///   Asserts that a method throws ArgumentNullException for a null parameter (void return).
  /// </summary>
  /// <param name="action">The async action to execute.</param>
  /// <param name="paramName">Expected parameter name in the exception.</param>
  /// <returns>The thrown ArgumentNullException for further inspection.</returns>
  public static async Task<ArgumentNullException> AssertThrowsArgumentNullAsync(
    Func<Task> action,
    string paramName)
  {
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () => await action());

    Assert.Equal(paramName, exception.ParamName);

    return exception;
  }

  /// <summary>
  ///   Asserts that a synchronous method throws ArgumentNullException for a null parameter.
  /// </summary>
  /// <param name="action">The action to execute.</param>
  /// <param name="paramName">Expected parameter name in the exception.</param>
  /// <returns>The thrown ArgumentNullException for further inspection.</returns>
  public static ArgumentNullException AssertThrowsArgumentNull(
    Action action,
    string paramName)
  {
    var exception = Assert.Throws<ArgumentNullException>(action);

    Assert.Equal(paramName, exception.ParamName);

    return exception;
  }

  #endregion


  #region ArgumentException (Empty String) Assertions

  /// <summary>
  ///   Asserts that a method throws ArgumentException for an empty string parameter.
  /// </summary>
  /// <typeparam name="T">Return type of the async method.</typeparam>
  /// <param name="action">The async action to execute.</param>
  /// <param name="paramName">Expected parameter name in the exception.</param>
  /// <returns>The thrown ArgumentException for further inspection.</returns>
  public static async Task<ArgumentException> AssertThrowsArgumentEmptyAsync<T>(
    Func<Task<T>> action,
    string paramName)
  {
    var exception = await Assert.ThrowsAsync<ArgumentException>(async () => await action());

    Assert.Equal(paramName, exception.ParamName);

    return exception;
  }

  /// <summary>
  ///   Asserts that a method throws ArgumentException for an empty string parameter (void return).
  /// </summary>
  /// <param name="action">The async action to execute.</param>
  /// <param name="paramName">Expected parameter name in the exception.</param>
  /// <returns>The thrown ArgumentException for further inspection.</returns>
  public static async Task<ArgumentException> AssertThrowsArgumentEmptyAsync(
    Func<Task> action,
    string paramName)
  {
    var exception = await Assert.ThrowsAsync<ArgumentException>(async () => await action());

    Assert.Equal(paramName, exception.ParamName);

    return exception;
  }

  /// <summary>
  ///   Asserts that a synchronous method throws ArgumentException for an empty string parameter.
  /// </summary>
  /// <param name="action">The action to execute.</param>
  /// <param name="paramName">Expected parameter name in the exception.</param>
  /// <returns>The thrown ArgumentException for further inspection.</returns>
  public static ArgumentException AssertThrowsArgumentEmpty(
    Action action,
    string paramName)
  {
    var exception = Assert.Throws<ArgumentException>(action);

    Assert.Equal(paramName, exception.ParamName);

    return exception;
  }

  #endregion


  #region ArgumentOutOfRangeException Assertions

  /// <summary>
  ///   Asserts that a method throws ArgumentOutOfRangeException for an invalid value.
  /// </summary>
  /// <typeparam name="T">Return type of the async method.</typeparam>
  /// <param name="action">The async action to execute.</param>
  /// <param name="paramName">Expected parameter name in the exception.</param>
  /// <returns>The thrown ArgumentOutOfRangeException for further inspection.</returns>
  public static async Task<ArgumentOutOfRangeException> AssertThrowsArgumentOutOfRangeAsync<T>(
    Func<Task<T>> action,
    string paramName)
  {
    var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await action());

    Assert.Equal(paramName, exception.ParamName);

    return exception;
  }

  /// <summary>
  ///   Asserts that a method throws ArgumentOutOfRangeException for an invalid value (void return).
  /// </summary>
  /// <param name="action">The async action to execute.</param>
  /// <param name="paramName">Expected parameter name in the exception.</param>
  /// <returns>The thrown ArgumentOutOfRangeException for further inspection.</returns>
  public static async Task<ArgumentOutOfRangeException> AssertThrowsArgumentOutOfRangeAsync(
    Func<Task> action,
    string paramName)
  {
    var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await action());

    Assert.Equal(paramName, exception.ParamName);

    return exception;
  }

  #endregion


  #region Combined Null/Empty Validation

  /// <summary>
  ///   Asserts that a method properly validates a required string parameter (both null and empty).
  /// </summary>
  /// <typeparam name="T">Return type of the async method.</typeparam>
  /// <param name="nullAction">Action that passes null for the parameter.</param>
  /// <param name="emptyAction">Action that passes empty string for the parameter.</param>
  /// <param name="paramName">Expected parameter name in the exceptions.</param>
  public static async Task AssertRequiredStringParameterAsync<T>(
    Func<Task<T>> nullAction,
    Func<Task<T>> emptyAction,
    string paramName)
  {
    // Test null
    await AssertThrowsArgumentNullAsync(nullAction, paramName);

    // Test empty
    await AssertThrowsArgumentEmptyAsync(emptyAction, paramName);
  }

  /// <summary>
  ///   Asserts that a method properly validates a required string parameter (both null and empty, void return).
  /// </summary>
  /// <param name="nullAction">Action that passes null for the parameter.</param>
  /// <param name="emptyAction">Action that passes empty string for the parameter.</param>
  /// <param name="paramName">Expected parameter name in the exceptions.</param>
  public static async Task AssertRequiredStringParameterAsync(
    Func<Task> nullAction,
    Func<Task> emptyAction,
    string paramName)
  {
    // Test null
    await AssertThrowsArgumentNullAsync(nullAction, paramName);

    // Test empty
    await AssertThrowsArgumentEmptyAsync(emptyAction, paramName);
  }

  #endregion


  #region Generic Exception Assertions

  /// <summary>
  ///   Asserts that a method throws a specific exception type.
  /// </summary>
  /// <typeparam name="TException">The expected exception type.</typeparam>
  /// <typeparam name="TResult">Return type of the async method.</typeparam>
  /// <param name="action">The async action to execute.</param>
  /// <returns>The thrown exception for further inspection.</returns>
  public static async Task<TException> AssertThrowsAsync<TException, TResult>(
    Func<Task<TResult>> action)
    where TException : Exception
  {
    return await Assert.ThrowsAsync<TException>(async () => await action());
  }

  /// <summary>
  ///   Asserts that a method throws a specific exception type (void return).
  /// </summary>
  /// <typeparam name="TException">The expected exception type.</typeparam>
  /// <param name="action">The async action to execute.</param>
  /// <returns>The thrown exception for further inspection.</returns>
  public static async Task<TException> AssertThrowsAsync<TException>(
    Func<Task> action)
    where TException : Exception
  {
    return await Assert.ThrowsAsync<TException>(async () => await action());
  }

  #endregion
}
