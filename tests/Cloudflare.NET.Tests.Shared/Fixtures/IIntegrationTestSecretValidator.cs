namespace Cloudflare.NET.Tests.Shared.Fixtures;

/// <summary>
///   Defines the contract for a class that can validate the presence of required secrets for a specific set of
///   integration tests.
/// </summary>
public interface IIntegrationTestSecretValidator
{
  /// <summary>Gets a list of human-readable names of secrets that are missing.</summary>
  /// <returns>A list of missing secret names, or an empty list if all are present.</returns>
  List<string> GetMissingSecrets();
}
