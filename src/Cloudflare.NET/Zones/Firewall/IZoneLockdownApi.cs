namespace Cloudflare.NET.Zones.Firewall;

using Security.Firewall.Models;

/// <summary>Defines the contract for managing Zone Lockdown rules.</summary>
public interface IZoneLockdownApi
{
  /// <summary>Lists all Lockdown rules for a zone.</summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A read-only list of Lockdown rules.</returns>
  Task<IReadOnlyList<Lockdown>> ListAsync(string zoneId, CancellationToken cancellationToken = default);

  /// <summary>Gets a single Lockdown rule by its ID.</summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="lockdownId">The ID of the Lockdown rule.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The requested Lockdown rule.</returns>
  Task<Lockdown> GetAsync(string zoneId, string lockdownId, CancellationToken cancellationToken = default);

  /// <summary>Creates a new Lockdown rule for a zone.</summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="request">The request containing the details of the rule to create.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The created Lockdown rule.</returns>
  Task<Lockdown> CreateAsync(string zoneId, CreateLockdownRequest request, CancellationToken cancellationToken = default);

  /// <summary>Updates an existing Lockdown rule.</summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="lockdownId">The ID of the rule to update.</param>
  /// <param name="request">The request containing the fields to update.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The updated Lockdown rule.</returns>
  Task<Lockdown> UpdateAsync(string                zoneId,
                             string                lockdownId,
                             UpdateLockdownRequest request,
                             CancellationToken     cancellationToken = default);

  /// <summary>Deletes a Lockdown rule from a zone.</summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="lockdownId">The ID of the rule to delete.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The ID of the deleted rule.</returns>
  Task<string> DeleteAsync(string zoneId, string lockdownId, CancellationToken cancellationToken = default);
}
