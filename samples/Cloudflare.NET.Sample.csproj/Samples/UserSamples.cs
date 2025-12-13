namespace Cloudflare.NET.Sample.Samples;

using ApiTokens.Models;
using AuditLogs.Models;
using Members.Models;
using Microsoft.Extensions.Logging;
using User.Models;

/// <summary>
///   Demonstrates User-level API operations including:
///   <list type="bullet">
///     <item><description>F11: User Memberships (list, get, update, delete memberships)</description></item>
///     <item><description>F14: User Management (get, edit user profile)</description></item>
///     <item><description>F15: User Audit Logs (list user audit logs)</description></item>
///     <item><description>F16: User Invitations (list, get, respond to invitations)</description></item>
///     <item><description>F17: User API Tokens (CRUD, verify, roll, permission groups)</description></item>
///   </list>
/// </summary>
public class UserSamples(ICloudflareApiClient cf, ILogger<UserSamples> logger)
{
  #region Methods - User Management (F14)

  /// <summary>
  ///   Demonstrates User Management operations.
  ///   <para>
  ///     The User API operates on the currently authenticated user only (self-only access).
  ///     Only the following fields can be edited: FirstName, LastName, Country, Telephone, Zipcode.
  ///   </para>
  /// </summary>
  public async Task RunUserManagementSamplesAsync()
  {
    logger.LogInformation("=== F14: User Management Operations ===");

    // 1. Get authenticated user profile.
    logger.LogInformation("--- Getting User Profile ---");
    var user = await cf.User.GetUserAsync();
    logger.LogInformation("User Profile:");
    logger.LogInformation("  Id:        {Id}", user.Id);
    logger.LogInformation("  Email:     {Email}", user.Email);
    logger.LogInformation("  FirstName: {FirstName}", user.FirstName ?? "Not set");
    logger.LogInformation("  LastName:  {LastName}", user.LastName ?? "Not set");
    logger.LogInformation("  Country:   {Country}", user.Country ?? "Not set");
    logger.LogInformation("  Telephone: {Telephone}", user.Telephone ?? "Not set");
    logger.LogInformation("  Zipcode:   {Zipcode}", user.Zipcode ?? "Not set");

    // Security settings.
    logger.LogInformation("  Security:");
    logger.LogInformation("    2FA Enabled:    {TwoFactorEnabled}", user.TwoFactorAuthenticationEnabled);
    logger.LogInformation("    2FA Locked:     {TwoFactorLocked}", user.TwoFactorAuthenticationLocked);
    logger.LogInformation("    Suspended:      {Suspended}", user.Suspended);

    if (user.Betas is { Count: > 0 })
      logger.LogInformation("    Beta Features:  {Betas}", string.Join(", ", user.Betas));

    // Zone plan tiers.
    logger.LogInformation("  Zone Plans:");
    logger.LogInformation("    Has Pro:        {HasPro}", user.HasProZones);
    logger.LogInformation("    Has Business:   {HasBusiness}", user.HasBusinessZones);
    logger.LogInformation("    Has Enterprise: {HasEnterprise}", user.HasEnterpriseZones);

    // Creation date.
    if (user.CreatedOn.HasValue)
      logger.LogInformation("  Created On: {CreatedOn}", user.CreatedOn);

    if (user.ModifiedOn.HasValue)
      logger.LogInformation("  Modified On: {ModifiedOn}", user.ModifiedOn);

    // 2. Edit user profile (demonstration - update and restore).
    // Note: We'll only update fields that can be safely changed.
    logger.LogInformation("--- Editing User Profile ---");

    try
    {
      // Store original values to restore later.
      var originalFirstName = user.FirstName;
      var originalLastName  = user.LastName;

      // Update names.
      var editRequest = new EditUserRequest(
        FirstName: $"{originalFirstName ?? "Test"} [SDK Update]",
        LastName:  originalLastName
      );
      var updated = await cf.User.EditUserAsync(editRequest);
      logger.LogInformation("Updated FirstName to: {FirstName}", updated.FirstName);

      // Restore original values.
      var restoreRequest = new EditUserRequest(
        FirstName: originalFirstName,
        LastName:  originalLastName
      );
      var restored = await cf.User.EditUserAsync(restoreRequest);
      logger.LogInformation("Restored FirstName to: {FirstName}", restored.FirstName);
    }
    catch (Exception ex)
    {
      logger.LogWarning("User profile update failed: {Message}", ex.Message);
    }

    logger.LogInformation("");
    logger.LogInformation("Note: Only the following fields can be edited via API:");
    logger.LogInformation("  - FirstName, LastName, Country, Telephone, Zipcode");
    logger.LogInformation("Email and 2FA settings are managed through Cloudflare dashboard.");
  }

  #endregion


  #region Methods - User Memberships (F11)

  /// <summary>
  ///   Demonstrates User Memberships operations.
  ///   <para>
  ///     Memberships represent the accounts the user has access to.
  ///     Users can accept, reject, or leave accounts through this API.
  ///   </para>
  /// </summary>
  public async Task RunUserMembershipsSamplesAsync()
  {
    logger.LogInformation("=== F11: User Memberships Operations ===");

    // 1. List all memberships with automatic pagination.
    logger.LogInformation("--- Listing All Memberships ---");
    var membershipCount = 0;
    var memberships = new List<(string Id, string AccountName)>();

    await foreach (var membership in cf.User.ListAllMembershipsAsync())
    {
      membershipCount++;
      memberships.Add((membership.Id, membership.Account?.Name ?? "Unknown"));

      if (membershipCount <= 5)
      {
        logger.LogInformation("  Membership: {AccountName} ({Id})", membership.Account?.Name, membership.Id);
        logger.LogInformation("    Status:      {Status}", membership.Status);
        logger.LogInformation("    API Access:  {ApiAccess}", membership.ApiAccessEnabled);

        if (membership.Roles is { Count: > 0 })
          logger.LogInformation("    Roles:       {Roles}", string.Join(", ", membership.Roles));
      }
    }

    logger.LogInformation("Total memberships: {Count}", membershipCount);

    // 2. List memberships with filters (e.g., pending invitations).
    logger.LogInformation("--- Listing Pending Memberships ---");

    try
    {
      var pendingFilters = new ListMembershipsFilters(Status: MemberStatus.Pending);
      var pendingPage = await cf.User.ListMembershipsAsync(pendingFilters);
      logger.LogInformation("Pending invitations: {Count}", pendingPage.Items.Count);

      foreach (var pending in pendingPage.Items)
        logger.LogInformation("  Invitation from: {AccountName}", pending.Account?.Name ?? "Unknown");
    }
    catch (Exception ex)
    {
      logger.LogInformation("No pending memberships or error: {Message}", ex.Message);
    }

    // 3. Get a specific membership's details.
    if (memberships.Count > 0)
    {
      logger.LogInformation("--- Getting Membership Details ---");
      var membershipId = memberships[0].Id;
      var details = await cf.User.GetMembershipAsync(membershipId);
      logger.LogInformation("Membership Details:");
      logger.LogInformation("  Id:          {Id}", details.Id);
      logger.LogInformation("  Account:     {AccountName} ({AccountId})", details.Account?.Name, details.Account?.Id);
      logger.LogInformation("  Status:      {Status}", details.Status);
      logger.LogInformation("  API Access:  {ApiAccess}", details.ApiAccessEnabled);

      if (details.Permissions is not null)
      {
        logger.LogInformation("  Permissions:");

        if (details.Permissions.Dns is not null)
          logger.LogInformation("    DNS:       Read={Read}, Write={Write}",
                                details.Permissions.Dns.Read, details.Permissions.Dns.Write);

        if (details.Permissions.Zones is not null)
          logger.LogInformation("    Zones:     Read={Read}, Write={Write}",
                                details.Permissions.Zones.Read, details.Permissions.Zones.Write);

        if (details.Permissions.Analytics is not null)
          logger.LogInformation("    Analytics: Read={Read}, Write={Write}",
                                details.Permissions.Analytics.Read, details.Permissions.Analytics.Write);

        if (details.Permissions.Billing is not null)
          logger.LogInformation("    Billing:   Read={Read}, Write={Write}",
                                details.Permissions.Billing.Read, details.Permissions.Billing.Write);
      }
    }

    // Note: UpdateMembershipAsync and DeleteMembershipAsync are Preview operations.
    // They are demonstrated conceptually to avoid affecting real memberships.
    logger.LogInformation("--- Membership Management (Conceptual) ---");
    logger.LogInformation("To accept an invitation, use UpdateMembershipAsync with:");
    logger.LogInformation("  Status: MemberStatus.Accepted");
    logger.LogInformation("");
    logger.LogInformation("To reject an invitation, use UpdateMembershipAsync with:");
    logger.LogInformation("  Status: MemberStatus.Rejected");
    logger.LogInformation("");
    logger.LogInformation("To leave an account, use DeleteMembershipAsync.");
    logger.LogInformation("Warning: Leaving an account requires a new invitation to rejoin.");
  }

  #endregion


  #region Methods - User Audit Logs (F15)

  /// <summary>
  ///   Demonstrates User Audit Logs operations.
  ///   <para>
  ///     User audit logs show actions taken by the authenticated user across all accounts.
  ///     Logs are retained for 30 days.
  ///   </para>
  /// </summary>
  public async Task RunUserAuditLogsSamplesAsync()
  {
    logger.LogInformation("=== F15: User Audit Logs Operations ===");

    // 1. Get recent audit logs (last 7 days).
    logger.LogInformation("--- Getting Recent User Audit Logs (last 7 days) ---");
    var filters = new ListAuditLogsFilters(Since: DateTime.UtcNow.AddDays(-7), Limit: 25);
    var logsPage = await cf.AuditLogs.ListUserAuditLogsAsync(filters);
    logger.LogInformation("Retrieved {Count} user audit logs", logsPage.Items.Count);

    foreach (var log in logsPage.Items.Take(5))
    {
      logger.LogInformation("  {Time}: {ActionType}", log.Action?.Time, log.Action?.Type);

      if (log.Resource is not null)
        logger.LogInformation("    Resource: {ResourceType} ({ResourceId})", log.Resource.Type, log.Resource.Id);

      if (log.Action?.Description is not null)
        logger.LogInformation("    Description: {Description}", log.Action.Description);
    }

    if (logsPage.Items.Count > 5)
      logger.LogInformation("  ... and {Count} more", logsPage.Items.Count - 5);

    // 2. Get all user audit logs with automatic pagination.
    logger.LogInformation("--- Listing All User Audit Logs (last 7 days, paginated) ---");
    var allLogsFilters = new ListAuditLogsFilters(Since: DateTime.UtcNow.AddDays(-7));
    var logCount = 0;

    await foreach (var log in cf.AuditLogs.ListAllUserAuditLogsAsync(allLogsFilters))
    {
      logCount++;

      if (logCount <= 3)
        logger.LogInformation("  {Time}: {ActionType}",
                              log.Action?.Time,
                              log.Action?.Type);

      // Limit iteration for the sample.
      if (logCount >= 50)
      {
        logger.LogInformation("  ... stopping at 50 logs for sample");

        break;
      }
    }

    logger.LogInformation("Processed {Count} user audit logs", logCount);

    // 3. Filtering tips.
    logger.LogInformation("--- Filtering User Audit Logs ---");
    logger.LogInformation("Tip: Use ListAuditLogsFilters to filter by:");
    logger.LogInformation("  - Since/Before: Date range");
    logger.LogInformation("  - Direction: Sort order (asc/desc)");
    logger.LogInformation("  - Limit: Number of results per page");
  }

  #endregion


  #region Methods - User Invitations (F16)

  /// <summary>
  ///   Demonstrates User Invitations operations.
  ///   <para>
  ///     User invitations are sent by account admins to invite users to join their accounts.
  ///     Users can accept or reject invitations through this API.
  ///   </para>
  /// </summary>
  public async Task RunUserInvitationsSamplesAsync()
  {
    logger.LogInformation("=== F16: User Invitations Operations ===");

    // 1. List all pending invitations.
    logger.LogInformation("--- Listing User Invitations ---");

    try
    {
      var invitations = await cf.User.ListInvitationsAsync();
      logger.LogInformation("Pending invitations: {Count}", invitations.Count);

      foreach (var invite in invitations)
      {
        logger.LogInformation("  Invitation: {Id}", invite.Id);
        logger.LogInformation("    Organization: {OrgName}", invite.OrganizationName ?? "Unknown");
        logger.LogInformation("    Email:        {Email}", invite.InvitedMemberEmail);
        logger.LogInformation("    Status:       {Status}", invite.Status);
        logger.LogInformation("    Invited On:   {InvitedOn}", invite.InvitedOn);
        logger.LogInformation("    Expires:      {ExpiresOn}", invite.ExpiresOn);

        if (invite.Roles is { Count: > 0 })
          logger.LogInformation("    Roles:        {Roles}", string.Join(", ", invite.Roles));

        // 2. Get invitation details (if we have an invitation).
        logger.LogInformation("--- Getting Invitation Details ---");
        var details = await cf.User.GetInvitationAsync(invite.Id);
        logger.LogInformation("Invitation Details:");
        logger.LogInformation("  Id:              {Id}", details.Id);
        logger.LogInformation("  Organization:    {OrgName}", details.OrganizationName ?? "Unknown");
        logger.LogInformation("  Email:           {Email}", details.InvitedMemberEmail);
        logger.LogInformation("  Invited On:      {InvitedOn}", details.InvitedOn);
        logger.LogInformation("  Expires On:      {ExpiresOn}", details.ExpiresOn);
        logger.LogInformation("  Status:          {Status}", details.Status);

        // Don't process more than one invitation in the sample.
        break;
      }

      if (invitations.Count == 0)
      {
        logger.LogInformation("No pending invitations found.");
        logger.LogInformation("Tip: Invitations appear here when account admins invite you to their accounts.");
      }
    }
    catch (Exception ex)
    {
      logger.LogWarning("Failed to list invitations: {Message}", ex.Message);
    }

    // Note: Responding to invitations is a Preview operation.
    // Demonstrated conceptually to avoid accidentally accepting/rejecting invitations.
    logger.LogInformation("--- Responding to Invitations (Conceptual) ---");
    logger.LogInformation("To accept an invitation, use RespondToInvitationAsync with:");
    logger.LogInformation("  Status: MemberStatus.Accepted");
    logger.LogInformation("");
    logger.LogInformation("To reject an invitation, use RespondToInvitationAsync with:");
    logger.LogInformation("  Status: MemberStatus.Rejected");
    logger.LogInformation("");
    logger.LogInformation("Warning: Responding to invitations cannot be undone!");
  }

  #endregion


  #region Methods - User API Tokens (F17)

  /// <summary>
  ///   Demonstrates User API Tokens operations.
  ///   <para>
  ///     User API tokens are created and managed by the authenticated user.
  ///     The token secret is only returned on creation and roll - store it securely!
  ///   </para>
  /// </summary>
  public async Task<List<Func<Task>>> RunUserApiTokensSamplesAsync()
  {
    var cleanupActions = new List<Func<Task>>();
    logger.LogInformation("=== F17: User API Tokens Operations ===");

    // 1. List permission groups (needed for token creation).
    // Note: Permission groups endpoint does NOT support pagination - all groups are returned at once.
    logger.LogInformation("--- Listing User Permission Groups ---");
    var permissionGroups = await cf.ApiTokens.GetUserPermissionGroupsAsync();
    logger.LogInformation("Available permission groups: {Count}", permissionGroups.Items.Count);

    foreach (var group in permissionGroups.Items.Take(10))
      logger.LogInformation("  {Name} ({Id})", group.Name, group.Id);

    // 2. List all permission groups with automatic pagination.
    logger.LogInformation("--- Listing All User Permission Groups (paginated) ---");
    var groupCount = 0;

    await foreach (var group in cf.ApiTokens.GetAllUserPermissionGroupsAsync())
    {
      groupCount++;

      if (groupCount > 50)
        break;
    }

    logger.LogInformation("Total permission groups: {Count}+", groupCount);

    // 3. List existing user tokens.
    logger.LogInformation("--- Listing User Tokens ---");
    var tokensPage = await cf.ApiTokens.ListUserTokensAsync(new ListApiTokensFilters(PerPage: 10));
    logger.LogInformation("Existing tokens: {Count}", tokensPage.Items.Count);

    foreach (var token in tokensPage.Items.Take(5))
    {
      logger.LogInformation("  {Name}: {Status}", token.Name, token.Status);

      if (token.ExpiresOn.HasValue)
        logger.LogInformation("    Expires: {Expires}", token.ExpiresOn);

      if (token.LastUsedOn.HasValue)
        logger.LogInformation("    Last Used: {LastUsed}", token.LastUsedOn);
    }

    // 4. List all user tokens with automatic pagination.
    logger.LogInformation("--- Listing All User Tokens (paginated) ---");
    var tokenCount = 0;

    await foreach (var token in cf.ApiTokens.ListAllUserTokensAsync())
    {
      tokenCount++;

      if (tokenCount > 20)
        break;
    }

    logger.LogInformation("Total user tokens: {Count}+", tokenCount);

    // 5. Verify current token.
    logger.LogInformation("--- Verifying Current Token ---");

    try
    {
      var verification = await cf.ApiTokens.VerifyUserTokenAsync();
      logger.LogInformation("Current token verification:");
      logger.LogInformation("  Id:     {Id}", verification.Id);
      logger.LogInformation("  Status: {Status}", verification.Status);

      if (verification.ExpiresOn.HasValue)
        logger.LogInformation("  Expires: {Expires}", verification.ExpiresOn);

      if (verification.NotBefore.HasValue)
        logger.LogInformation("  Not Before: {NotBefore}", verification.NotBefore);
    }
    catch (Exception ex)
    {
      logger.LogWarning("Token verification failed: {Message}", ex.Message);
    }

    // 6. Get details for a specific token (if any exist).
    if (tokensPage.Items.Count > 0)
    {
      logger.LogInformation("--- Getting Token Details ---");
      var tokenId = tokensPage.Items[0].Id;
      var tokenDetails = await cf.ApiTokens.GetUserTokenAsync(tokenId);
      logger.LogInformation("Token Details:");
      logger.LogInformation("  Id:     {Id}", tokenDetails.Id);
      logger.LogInformation("  Name:   {Name}", tokenDetails.Name);
      logger.LogInformation("  Status: {Status}", tokenDetails.Status);

      if (tokenDetails.Policies is { Count: > 0 })
      {
        logger.LogInformation("  Policies:");

        foreach (var policy in tokenDetails.Policies.Take(3))
          logger.LogInformation("    - Effect: {Effect}, Groups: {Groups}",
                                policy.Effect,
                                policy.PermissionGroups?.Count ?? 0);
      }
    }

    // Note: Token creation, update, roll, and deletion are demonstrated conceptually.
    // Creating actual tokens requires proper permission groups and may affect production access.
    logger.LogInformation("--- Token Management (Conceptual) ---");
    logger.LogInformation("To create a user token, use CreateUserTokenAsync with:");
    logger.LogInformation("  - Name: A descriptive name");
    logger.LogInformation("  - Policies: Array of permission policies");
    logger.LogInformation("  - Condition (optional): IP restrictions");
    logger.LogInformation("  - ExpiresOn (optional): Expiration date");
    logger.LogInformation("");
    logger.LogInformation("IMPORTANT: The token value is only returned on creation!");
    logger.LogInformation("Store it securely - it cannot be retrieved again.");
    logger.LogInformation("");
    logger.LogInformation("To rotate a token's secret, use RollUserTokenAsync.");
    logger.LogInformation("To disable a token, use UpdateUserTokenAsync with Status: Disabled.");
    logger.LogInformation("To delete a token, use DeleteUserTokenAsync.");

    return cleanupActions;
  }

  #endregion
}
