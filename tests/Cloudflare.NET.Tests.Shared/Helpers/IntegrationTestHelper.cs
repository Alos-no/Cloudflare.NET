namespace Cloudflare.NET.Tests.Shared.Helpers;

using System.Reflection;

/// <summary>
///   Provides shared helper logic for integration test attributes.
///   <para>
///     This class extracts the common secret validation logic so it can be reused
///     by both <see cref="IntegrationTestAttribute" /> and <see cref="IntegrationTestTheoryAttribute" />.
///   </para>
/// </summary>
internal static class IntegrationTestHelper
{
  #region Constants & Statics

  /// <summary>
  ///   A lazily-initialized list of all missing secrets, discovered by scanning assemblies.
  ///   This ensures the expensive reflection scan only happens once per test run.
  /// </summary>
  private static readonly Lazy<List<string>> MissingSecrets = new(DiscoverMissingSecrets);

  #endregion


  #region Methods

  /// <summary>
  ///   Gets the skip message if any secrets are missing, or <c>null</c> if all secrets are configured.
  /// </summary>
  /// <returns>
  ///   A descriptive skip message listing missing secrets, or <c>null</c> if no secrets are missing.
  /// </returns>
  public static string? GetSkipMessage()
  {
    if (MissingSecrets.Value.Count == 0)
      return null;

    return
      $"Skipping integration test: The following secrets are not configured: {string.Join(", ", MissingSecrets.Value)}. Please check user secrets or environment variables.";
  }

  /// <summary>
  ///   Scans loaded assemblies for implementations of <see cref="IIntegrationTestSecretValidator" />,
  ///   runs them, and aggregates a list of all missing secrets.
  /// </summary>
  /// <returns>A distinct list of missing secret names.</returns>
  private static List<string> DiscoverMissingSecrets()
  {
    var validatorType = typeof(IIntegrationTestSecretValidator);

    var allValidators = AppDomain.CurrentDomain.GetAssemblies()
                                 .SelectMany(assembly =>
                                 {
                                   try
                                   {
                                     return assembly.GetTypes();
                                   }
                                   catch (ReflectionTypeLoadException)
                                   {
                                     // Can happen with dynamic assemblies, safe to ignore.
                                     return Type.EmptyTypes;
                                   }
                                 })
                                 .Where(type => validatorType.IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract);

    var allMissingSecrets = allValidators
                            .Select(validator => Activator.CreateInstance(validator) as IIntegrationTestSecretValidator)
                            .Where(instance => instance is not null)
                            .SelectMany(instance => instance!.GetMissingSecrets());

    return allMissingSecrets.Distinct().ToList();
  }

  #endregion
}
