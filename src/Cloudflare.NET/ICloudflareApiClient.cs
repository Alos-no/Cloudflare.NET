namespace Cloudflare.NET;

using Accounts;
using Zones;

/// <summary>Defines the contract for the primary client for interacting with the Cloudflare API.</summary>
public interface ICloudflareApiClient
{
  #region Properties & Fields - Public

  /// <summary>Gets the API resource for managing Account-level resources.</summary>
  IAccountsApi Accounts { get; }

  /// <summary>Gets the API resource for managing Zone-level resources.</summary>
  IZonesApi Zones { get; }

  #endregion
}
