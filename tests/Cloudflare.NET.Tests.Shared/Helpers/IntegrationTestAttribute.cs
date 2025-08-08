namespace Cloudflare.NET.Tests.Shared.Helpers;

using System.Reflection;

/// <summary>
///   A custom attribute for integration tests. This attribute automatically skips the test
///   if any required secrets are not configured. It discovers all
///   <see cref="IIntegrationTestSecretValidator" /> implementations in the loaded assemblies to
///   determine the full set of required secrets.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class IntegrationTestAttribute : FactAttribute
{
  #region Constants & Statics

  /// <summary>
  ///   A lazily-initialized list of all missing secrets, discovered by scanning assemblies.
  ///   This ensures the expensive reflection scan only happens once per test run.
  /// </summary>
  private static readonly Lazy<List<string>> MissingSecrets = new(DiscoverMissingSecrets);

  #endregion

  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="IntegrationTestAttribute" /> class.</summary>
  public IntegrationTestAttribute()
  {
    if (MissingSecrets.Value.Any())
      // If any validator reported missing secrets, skip the test with an informative message.
      Skip =
        $"Skipping integration test: The following secrets are not configured: {string.Join(", ", MissingSecrets.Value)}. Please check user secrets or environment variables.";
  }

  #endregion

  #region Methods

  /// <summary>
  ///   Scans loaded assemblies for implementations of
  ///   <see cref="IIntegrationTestSecretValidator" />, runs them, and aggregates a list of all
  ///   missing secrets.
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
                            .Where(instance => instance != null)
                            .SelectMany(instance => instance!.GetMissingSecrets());

    return allMissingSecrets.Distinct().ToList();
  }

  #endregion
}
