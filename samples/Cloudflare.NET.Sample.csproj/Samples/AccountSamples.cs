namespace Cloudflare.NET.Sample.Samples;

using Accounts.Buckets;
using Accounts.Models;
using ApiTokens.Models;
using AuditLogs.Models;
using Members.Models;
using Microsoft.Extensions.Logging;
using Roles.Models;

/// <summary>
///   Demonstrates Account-level API operations including:
///   <list type="bullet">
///     <item><description>F06: Account Management (list, get, update accounts)</description></item>
///     <item><description>F07: Account Audit Logs (list, filter audit logs)</description></item>
///     <item><description>F08: Account API Tokens (CRUD, verify, roll, permission groups)</description></item>
///     <item><description>F09: Account Members (list, get, create, update, delete members)</description></item>
///     <item><description>F10: Account Roles (list, get roles)</description></item>
///     <item><description>R2 Bucket operations (create, list, custom domains, CORS, lifecycle)</description></item>
///   </list>
/// </summary>
public class AccountSamples(ICloudflareApiClient cf, ILogger<AccountSamples> logger)
{
  #region Methods - Account Management (F06)

  /// <summary>
  ///   Demonstrates Account Management operations.
  ///   <para>
  ///     Note: Account creation and deletion are tenant-admin operations with limited test coverage.
  ///     Most users will only be able to list and update accounts they have access to.
  ///   </para>
  /// </summary>
  public async Task RunAccountManagementSamplesAsync(string accountId)
  {
    logger.LogInformation("=== F06: Account Management Operations ===");

    // 1. List all accounts with automatic pagination.
    logger.LogInformation("--- Listing All Accounts ---");
    var accountCount = 0;

    await foreach (var account in cf.Accounts.ListAllAccountsAsync(new ListAccountsFilters(PerPage: 10)))
    {
      accountCount++;

      if (accountCount <= 5)
      {
        logger.LogInformation("  Account: {Name} ({Id})", account.Name, account.Id);
        logger.LogInformation("    Type: {Type}", account.Type);
      }
    }

    logger.LogInformation("Total accounts accessible: {Count}", accountCount);

    // 2. List accounts with filters (first page only).
    logger.LogInformation("--- Listing Accounts (first page) ---");
    var accountsPage = await cf.Accounts.ListAccountsAsync(new ListAccountsFilters(PerPage: 5));
    logger.LogInformation("Accounts on page 1: {Count}", accountsPage.Items.Count);

    // 3. Get account details.
    logger.LogInformation("--- Getting Account Details ---");
    var accountDetails = await cf.Accounts.GetAccountAsync(accountId);
    logger.LogInformation("Account Details:");
    logger.LogInformation("  Id:          {Id}", accountDetails.Id);
    logger.LogInformation("  Name:        {Name}", accountDetails.Name);
    logger.LogInformation("  Type:        {Type}", accountDetails.Type);

    if (accountDetails.Settings is not null)
    {
      logger.LogInformation("  Settings:");
      logger.LogInformation("    Enforce 2FA: {EnforceTwoFactor}", accountDetails.Settings.EnforceTwofactor);
    }

    logger.LogInformation("  Created On:  {CreatedOn}", accountDetails.CreatedOn);

    // 4. Update account (demonstration - toggle 2FA enforcement if possible).
    // Note: This may fail depending on account type and permissions.
    logger.LogInformation("--- Updating Account (Name) ---");

    try
    {
      // Just update the name to demonstrate the API (we'll restore it immediately).
      var originalName = accountDetails.Name;
      var tempName     = $"{originalName} [Updated by SDK]";

      var updateRequest = new UpdateAccountRequest(Name: tempName);
      var updated       = await cf.Accounts.UpdateAccountAsync(accountId, updateRequest);
      logger.LogInformation("Updated account name to: {Name}", updated.Name);

      // Restore original name.
      var restoreRequest = new UpdateAccountRequest(Name: originalName);
      var restored       = await cf.Accounts.UpdateAccountAsync(accountId, restoreRequest);
      logger.LogInformation("Restored account name to: {Name}", restored.Name);
    }
    catch (Exception ex)
    {
      logger.LogWarning("Account update failed (may lack permissions): {Message}", ex.Message);
    }

    // Note: CreateAccountAsync and DeleteAccountAsync are tenant-admin operations.
    // They are not demonstrated here as they require special privileges.
    logger.LogInformation("Note: Account creation/deletion are tenant-admin operations - not demonstrated.");
  }

  #endregion


  #region Methods - Account Audit Logs (F07)

  /// <summary>
  ///   Demonstrates Account Audit Logs operations.
  ///   <para>
  ///     Audit logs provide a record of actions taken on account resources.
  ///     Logs are retained for 30 days.
  ///   </para>
  /// </summary>
  public async Task RunAccountAuditLogsSamplesAsync(string accountId)
  {
    logger.LogInformation("=== F07: Account Audit Logs Operations ===");

    // 1. Get recent audit logs (last 7 days).
    logger.LogInformation("--- Getting Recent Audit Logs (last 7 days) ---");
    var filters = new ListAuditLogsFilters(Since: DateTime.UtcNow.AddDays(-7), Limit: 25);
    var logsPage = await cf.AuditLogs.GetAccountAuditLogsAsync(accountId, filters);
    logger.LogInformation("Retrieved {Count} audit logs", logsPage.Items.Count);

    foreach (var log in logsPage.Items.Take(5))
    {
      logger.LogInformation("  {Time}: {ActionType}", log.Action?.Time, log.Action?.Type);
      logger.LogInformation("    Actor: {ActorEmail}", log.Actor?.Email ?? "Unknown");

      if (log.Resource is not null)
        logger.LogInformation("    Resource: {ResourceType} ({ResourceId})", log.Resource.Type, log.Resource.Id);
    }

    if (logsPage.Items.Count > 5)
      logger.LogInformation("  ... and {Count} more", logsPage.Items.Count - 5);

    // 2. Get all audit logs with automatic pagination (limited to recent week).
    logger.LogInformation("--- Listing All Audit Logs (last 7 days, paginated) ---");
    var allLogsFilters = new ListAuditLogsFilters(Since: DateTime.UtcNow.AddDays(-7));
    var logCount = 0;

    await foreach (var log in cf.AuditLogs.GetAllAccountAuditLogsAsync(accountId, allLogsFilters))
    {
      logCount++;

      if (logCount <= 3)
        logger.LogInformation("  {Time}: {ActionType} by {Actor}",
                              log.Action?.Time,
                              log.Action?.Type,
                              log.Actor?.Email);

      // Limit iteration for the sample.
      if (logCount >= 50)
      {
        logger.LogInformation("  ... stopping at 50 logs for sample");

        break;
      }
    }

    logger.LogInformation("Processed {Count} audit logs", logCount);

    // 3. Filter audit logs by action type (if supported).
    logger.LogInformation("--- Filtering Audit Logs ---");
    logger.LogInformation("Tip: Use ListAuditLogsFilters to filter by:");
    logger.LogInformation("  - Since/Before: Date range");
    logger.LogInformation("  - ActorEmail: Specific user actions");
    logger.LogInformation("  - ZoneId: Zone-specific actions");
    logger.LogInformation("  - Direction: Sort order (asc/desc)");
  }

  #endregion


  #region Methods - Account API Tokens (F08)

  /// <summary>
  ///   Demonstrates Account API Tokens operations.
  ///   <para>
  ///     API tokens provide fine-grained access control for Cloudflare API operations.
  ///     The token secret is only returned on creation and roll - store it securely!
  ///   </para>
  /// </summary>
  public async Task<List<Func<Task>>> RunAccountApiTokensSamplesAsync(string accountId)
  {
    var cleanupActions = new List<Func<Task>>();
    logger.LogInformation("=== F08: Account API Tokens Operations ===");

    // 1. List permission groups (needed for token creation).
    // Note: Permission groups endpoint does NOT support pagination - all groups are returned at once.
    logger.LogInformation("--- Listing Permission Groups ---");
    var permissionGroups = await cf.ApiTokens.GetAccountPermissionGroupsAsync(accountId);
    logger.LogInformation("Available permission groups: {Count}", permissionGroups.Items.Count);

    string? readOnlyGroupId = null;

    foreach (var group in permissionGroups.Items.Take(10))
    {
      logger.LogInformation("  {Name} ({Id})", group.Name, group.Id);

      // Look for a read-only group to use in token creation.
      if (group.Name?.Contains("Read", StringComparison.OrdinalIgnoreCase) == true && readOnlyGroupId is null)
        readOnlyGroupId = group.Id;
    }

    // 2. List all permission groups with automatic pagination.
    logger.LogInformation("--- Listing All Permission Groups (paginated) ---");
    var groupCount = 0;

    await foreach (var group in cf.ApiTokens.GetAllAccountPermissionGroupsAsync(accountId))
    {
      groupCount++;

      if (groupCount > 50)
        break;
    }

    logger.LogInformation("Total permission groups: {Count}+", groupCount);

    // 3. List existing account tokens.
    logger.LogInformation("--- Listing Account Tokens ---");
    var tokensPage = await cf.ApiTokens.ListAccountTokensAsync(accountId, new ListApiTokensFilters(PerPage: 10));
    logger.LogInformation("Existing tokens: {Count}", tokensPage.Items.Count);

    foreach (var token in tokensPage.Items.Take(5))
    {
      logger.LogInformation("  {Name}: {Status}", token.Name, token.Status);

      if (token.ExpiresOn.HasValue)
        logger.LogInformation("    Expires: {Expires}", token.ExpiresOn);
    }

    // 4. Verify current token.
    logger.LogInformation("--- Verifying Current Token ---");

    try
    {
      var verification = await cf.ApiTokens.VerifyAccountTokenAsync(accountId);
      logger.LogInformation("Current token verification:");
      logger.LogInformation("  Id:     {Id}", verification.Id);
      logger.LogInformation("  Status: {Status}", verification.Status);

      if (verification.ExpiresOn.HasValue)
        logger.LogInformation("  Expires: {Expires}", verification.ExpiresOn);
    }
    catch (Exception ex)
    {
      logger.LogWarning("Token verification failed: {Message}", ex.Message);
    }

    // Note: Token creation, update, roll, and deletion are demonstrated conceptually.
    // Creating actual tokens requires proper permission groups and may affect production access.
    logger.LogInformation("--- Token Creation (Conceptual) ---");
    logger.LogInformation("To create a token, use CreateAccountTokenAsync with:");
    logger.LogInformation("  - Name: A descriptive name");
    logger.LogInformation("  - Policies: Array of permission policies");
    logger.LogInformation("  - Condition (optional): IP restrictions");
    logger.LogInformation("  - ExpiresOn (optional): Expiration date");
    logger.LogInformation("");
    logger.LogInformation("IMPORTANT: The token value is only returned on creation!");
    logger.LogInformation("Store it securely - it cannot be retrieved again.");
    logger.LogInformation("");
    logger.LogInformation("To rotate a token's secret, use RollAccountTokenAsync.");

    return cleanupActions;
  }

  #endregion


  #region Methods - Account Members (F09)

  /// <summary>
  ///   Demonstrates Account Members operations.
  ///   <para>
  ///     Account members are users who have been granted access to an account's resources.
  ///     Members can be invited, managed, and removed.
  ///   </para>
  /// </summary>
  public async Task RunAccountMembersSamplesAsync(string accountId)
  {
    logger.LogInformation("=== F09: Account Members Operations ===");

    // 1. List all account members with automatic pagination.
    logger.LogInformation("--- Listing All Account Members ---");
    var memberCount = 0;

    await foreach (var member in cf.Members.ListAllAccountMembersAsync(accountId))
    {
      memberCount++;

      if (memberCount <= 5)
      {
        logger.LogInformation("  Member: {Email} ({Id})", member.User?.Email ?? "Unknown", member.Id);
        logger.LogInformation("    Status: {Status}", member.Status);
        logger.LogInformation("    Roles:  {Roles}", string.Join(", ", member.Roles?.Select(r => r.Name) ?? []));
      }
    }

    logger.LogInformation("Total members: {Count}", memberCount);

    // 2. List members with filters (e.g., accepted members only).
    logger.LogInformation("--- Listing Members with Filters ---");
    var acceptedFilters = new ListAccountMembersFilters(Status: MemberStatus.Accepted, PerPage: 10);
    var acceptedPage = await cf.Members.ListAccountMembersAsync(accountId, acceptedFilters);
    logger.LogInformation("Accepted members: {Count}", acceptedPage.Items.Count);

    // 3. Get a specific member's details.
    if (acceptedPage.Items.Count > 0)
    {
      logger.LogInformation("--- Getting Member Details ---");
      var memberId = acceptedPage.Items[0].Id;
      var memberDetails = await cf.Members.GetAccountMemberAsync(accountId, memberId);
      logger.LogInformation("Member Details:");
      logger.LogInformation("  Id:       {Id}", memberDetails.Id);
      logger.LogInformation("  Email:    {Email}", memberDetails.User?.Email);
      logger.LogInformation("  Status:   {Status}", memberDetails.Status);

      if (memberDetails.Roles is not null)
      {
        logger.LogInformation("  Roles:");

        foreach (var role in memberDetails.Roles)
          logger.LogInformation("    - {Name}: {Description}", role.Name, role.Description ?? "No description");
      }
    }

    // Note: Member creation (invitation), update, and deletion are Preview operations.
    // They are demonstrated conceptually to avoid affecting production accounts.
    logger.LogInformation("--- Member Management (Conceptual) ---");
    logger.LogInformation("To invite a new member, use CreateAccountMemberAsync with:");
    logger.LogInformation("  - Email: The user's email address");
    logger.LogInformation("  - Roles: Array of role IDs to assign");
    logger.LogInformation("");
    logger.LogInformation("To update a member's roles, use UpdateAccountMemberAsync.");
    logger.LogInformation("To remove a member, use DeleteAccountMemberAsync.");
  }

  #endregion


  #region Methods - Account Roles (F10)

  /// <summary>
  ///   Demonstrates Account Roles operations.
  ///   <para>
  ///     Roles are predefined by Cloudflare and define sets of permissions
  ///     that can be assigned to account members. Roles are read-only.
  ///   </para>
  /// </summary>
  public async Task RunAccountRolesSamplesAsync(string accountId)
  {
    logger.LogInformation("=== F10: Account Roles Operations ===");

    // 1. List all account roles with automatic pagination.
    logger.LogInformation("--- Listing All Account Roles ---");
    var roleCount = 0;
    var roles = new List<(string Id, string Name)>();

    await foreach (var role in cf.Roles.ListAllAccountRolesAsync(accountId))
    {
      roleCount++;
      roles.Add((role.Id, role.Name ?? "Unknown"));

      if (roleCount <= 10)
      {
        logger.LogInformation("  Role: {Name}", role.Name);
        logger.LogInformation("    Id:          {Id}", role.Id);

        if (role.Description is not null)
          logger.LogInformation("    Description: {Description}", role.Description);
      }
    }

    logger.LogInformation("Total roles: {Count}", roleCount);

    // 2. List roles with pagination control.
    logger.LogInformation("--- Listing Roles (first page) ---");
    var rolesPage = await cf.Roles.ListAccountRolesAsync(accountId, new ListAccountRolesFilters(PerPage: 5));
    logger.LogInformation("Roles on page 1: {Count}", rolesPage.Items.Count);

    // 3. Get a specific role's details.
    if (roles.Count > 0)
    {
      logger.LogInformation("--- Getting Role Details ---");
      var roleId = roles[0].Id;
      var roleDetails = await cf.Roles.GetAccountRoleAsync(accountId, roleId);
      logger.LogInformation("Role Details:");
      logger.LogInformation("  Id:          {Id}", roleDetails.Id);
      logger.LogInformation("  Name:        {Name}", roleDetails.Name);
      logger.LogInformation("  Description: {Description}", roleDetails.Description ?? "No description");

      // Display permissions if available.
      if (roleDetails.Permissions is not null)
      {
        logger.LogInformation("  Permissions:");

        if (roleDetails.Permissions.Dns is not null)
          logger.LogInformation("    DNS:          Read={Read}, Write={Write}",
                                roleDetails.Permissions.Dns.Read,
                                roleDetails.Permissions.Dns.Write);

        if (roleDetails.Permissions.Zones is not null)
          logger.LogInformation("    Zones:        Read={Read}, Write={Write}",
                                roleDetails.Permissions.Zones.Read,
                                roleDetails.Permissions.Zones.Write);

        if (roleDetails.Permissions.Analytics is not null)
          logger.LogInformation("    Analytics:    Read={Read}, Write={Write}",
                                roleDetails.Permissions.Analytics.Read,
                                roleDetails.Permissions.Analytics.Write);
      }
    }

    logger.LogInformation("");
    logger.LogInformation("Note: Roles are predefined by Cloudflare and cannot be created, modified, or deleted.");
    logger.LogInformation("Use role IDs when inviting new account members.");
  }

  #endregion


  #region Methods - R2 Samples

  /// <summary>
  ///   Demonstrates R2 bucket operations including:
  ///   <list type="bullet">
  ///     <item><description>Bucket creation, retrieval, listing, and deletion</description></item>
  ///     <item><description>Custom domain lifecycle (list, attach, update, status, detach)</description></item>
  ///     <item><description>Managed domain (r2.dev) configuration</description></item>
  ///     <item><description>CORS configuration</description></item>
  ///     <item><description>Lifecycle policy management</description></item>
  ///     <item><description>Bucket lock (retention) configuration</description></item>
  ///     <item><description>Sippy migration (conceptual)</description></item>
  ///     <item><description>Temporary credentials (conceptual)</description></item>
  ///   </list>
  /// </summary>
  public async Task<List<Func<Task>>> RunR2SamplesAsync(string zoneId, string baseDomain)
  {
    var cleanupActions = new List<Func<Task>>();

    // Create a unique name for the R2 bucket used in this test run, to avoid collisions.
    var bucketName = $"cfnet-sample-bucket-{Guid.NewGuid():N}";

    // 1. Create R2 Bucket using the new Buckets API.
    logger.LogInformation("Creating R2 Bucket: {BucketName}", bucketName);
    var bucket = await cf.Accounts.Buckets.CreateAsync(bucketName);
    logger.LogInformation("Created R2 Bucket: {Name} on {Date}", bucket.Name, bucket.CreationDate);

    cleanupActions.Add(async () =>
    {
      logger.LogInformation("Deleting R2 Bucket: {BucketName}", bucketName);
      await cf.Accounts.Buckets.DeleteAsync(bucketName);
      logger.LogInformation("Deleted R2 Bucket: {BucketName}", bucketName);
    });

    // 2. Get R2 Bucket details.
    logger.LogInformation("Getting R2 Bucket details: {BucketName}", bucketName);
    var bucketDetails = await cf.Accounts.Buckets.GetAsync(bucketName);
    logger.LogInformation("Bucket Details:");
    logger.LogInformation("  Name:         {Name}", bucketDetails.Name);
    logger.LogInformation("  Created:      {CreationDate}", bucketDetails.CreationDate);
    logger.LogInformation("  Location:     {Location}", bucketDetails.Location ?? "default");
    logger.LogInformation("  StorageClass: {StorageClass}", bucketDetails.StorageClass ?? "Standard");

    // 3. List R2 Buckets (paginated).
    logger.LogInformation("Listing all R2 buckets...");
    var count = 0;

    await foreach (var b in cf.Accounts.Buckets.ListAllAsync(new ListR2BucketsFilters { PerPage = 5 }))
    {
      count++;

      if (count <= 5)
        logger.LogInformation("  Found bucket: {Name}", b.Name);
    }

    logger.LogInformation("Total buckets found: {Count} (showing first 5)", count);

    // 4. R2 Custom Domain Lifecycle.
    await RunCustomDomainLifecycleAsync(bucketName, zoneId, baseDomain, cleanupActions);

    // 5. R2 Managed Domain (r2.dev) Configuration.
    await RunManagedDomainConfigurationAsync(bucketName);

    // 6. R2 CORS Configuration.
    await RunCorsConfigurationAsync(bucketName);

    // 7. R2 Lifecycle Configuration.
    await RunLifecycleConfigurationAsync(bucketName);

    // 8. R2 Bucket Lock Configuration.
    await RunBucketLockConfigurationAsync(bucketName);

    // 9. Sippy Migration (Conceptual).
    RunSippyConceptualSample();

    // 10. Temporary Credentials (Conceptual).
    RunTempCredentialsConceptualSample();

    return cleanupActions;
  }

  /// <summary>Demonstrates custom domain lifecycle including list, attach, update, status, and detach.</summary>
  private async Task RunCustomDomainLifecycleAsync(string bucketName, string zoneId, string baseDomain, List<Func<Task>> cleanupActions)
  {
    var hostname = $"r2-sample-{Guid.NewGuid():N}.{baseDomain}";

    logger.LogInformation("--- Running Custom Domain Lifecycle for {Hostname} ---", hostname);

    // 1. List existing custom domains (should be empty for new bucket).
    logger.LogInformation("Listing custom domains for bucket {BucketName}", bucketName);
    var existingDomains = await cf.Accounts.Buckets.ListCustomDomainsAsync(bucketName);
    logger.LogInformation("Found {Count} existing custom domains", existingDomains.Count);

    // 2. Attach Custom Domain.
    logger.LogInformation("Attaching custom domain {Hostname} to bucket {BucketName}", hostname, bucketName);
    var attachResult = await cf.Accounts.Buckets.AttachCustomDomainAsync(bucketName, hostname, zoneId);
    logger.LogInformation("Attach initiated. Domain: {Domain}, Status: {Status}", attachResult.Domain, attachResult.Status);

    cleanupActions.Insert(0, async () =>
    {
      logger.LogInformation("Detaching custom domain: {Hostname}", hostname);
      await cf.Accounts.Buckets.DetachCustomDomainAsync(bucketName, hostname);
      logger.LogInformation("Detached custom domain: {Hostname}", hostname);
    });

    // 3. List custom domains again to see the attached domain.
    logger.LogInformation("Listing custom domains after attach...");
    var domainsAfterAttach = await cf.Accounts.Buckets.ListCustomDomainsAsync(bucketName);
    logger.LogInformation("Found {Count} custom domains:", domainsAfterAttach.Count);

    foreach (var domain in domainsAfterAttach)
    {
      logger.LogInformation("  Domain: {Domain}", domain.Domain);
      logger.LogInformation("    Enabled: {Enabled}", domain.Enabled);
      logger.LogInformation("    Status:  {Status}", domain.Status?.Ownership ?? "pending");
      logger.LogInformation("    MinTLS:  {MinTls}", domain.MinTls ?? "default");
    }

    // 4. Get Custom Domain Status.
    logger.LogInformation("Getting status for custom domain: {Hostname}", hostname);
    var statusResult = await cf.Accounts.Buckets.GetCustomDomainStatusAsync(bucketName, hostname);
    logger.LogInformation("Got status. Domain: {Domain}, Status: {Status}", statusResult.Domain, statusResult.Status);

    // 5. Update Custom Domain (set minimum TLS version).
    logger.LogInformation("Updating custom domain {Hostname} to require TLS 1.2", hostname);
    var updateResult = await cf.Accounts.Buckets.UpdateCustomDomainAsync(
      bucketName,
      hostname,
      new UpdateCustomDomainRequest(MinTls: "1.2")
    );
    logger.LogInformation("Updated domain. Status: {Status}", updateResult.Status);
  }

  /// <summary>Demonstrates managed domain (r2.dev) configuration.</summary>
  private async Task RunManagedDomainConfigurationAsync(string bucketName)
  {
    logger.LogInformation("--- Running Managed Domain (r2.dev) Configuration for bucket {BucketName} ---", bucketName);

    // 1. Get initial managed domain status.
    logger.LogInformation("Getting managed domain status for bucket {BucketName}", bucketName);
    var initialStatus = await cf.Accounts.Buckets.GetManagedDomainAsync(bucketName);
    logger.LogInformation("Initial status:");
    logger.LogInformation("  BucketId: {BucketId}", initialStatus.BucketId);
    logger.LogInformation("  Domain:   {Domain}", initialStatus.Domain ?? "not available");
    logger.LogInformation("  Enabled:  {Enabled}", initialStatus.Enabled);

    // 2. Enable managed domain (public r2.dev access).
    logger.LogInformation("Enabling managed domain for bucket {BucketName}", bucketName);
    var enableResult = await cf.Accounts.Buckets.EnableManagedDomainAsync(bucketName);
    logger.LogInformation("Managed domain enabled:");
    logger.LogInformation("  Domain:  {Domain}", enableResult.Domain);
    logger.LogInformation("  Enabled: {Enabled}", enableResult.Enabled);

    // 3. Disable managed domain (restore private access).
    logger.LogInformation("Disabling managed domain for bucket {BucketName}", bucketName);
    await cf.Accounts.Buckets.DisableManagedDomainAsync(bucketName);
    logger.LogInformation("Managed domain disabled successfully");

    // 4. Verify disabled status.
    var finalStatus = await cf.Accounts.Buckets.GetManagedDomainAsync(bucketName);
    logger.LogInformation("Final status: Enabled={Enabled}", finalStatus.Enabled);
  }

  /// <summary>Demonstrates CORS policy configuration.</summary>
  private async Task RunCorsConfigurationAsync(string bucketName)
  {
    logger.LogInformation("--- Running CORS Configuration for bucket {BucketName} ---", bucketName);

    // 1. Set CORS Policy with multiple rules.
    logger.LogInformation("Setting CORS policy for bucket {BucketName}", bucketName);
    var corsPolicy = new BucketCorsPolicy(
      new[]
      {
        // Rule for local development.
        new CorsRule(
          new CorsAllowed(
            new[] { "GET", "PUT", "POST", "DELETE" },
            new[] { "http://localhost:3000", "http://localhost:5173" },
            new[] { "Content-Type", "Authorization" }
          ),
          "Local Development",
          new[] { "ETag", "Content-Length" },
          3600
        ),
        // Rule for production.
        new CorsRule(
          new CorsAllowed(
            new[] { "GET", "HEAD" },
            new[] { "https://example.com" },
            new[] { "Content-Type" }
          ),
          "Production",
          new[] { "ETag" },
          7200
        )
      }
    );

    await cf.Accounts.Buckets.SetCorsAsync(bucketName, corsPolicy);
    logger.LogInformation("CORS policy set successfully with {RuleCount} rules", corsPolicy.Rules.Count);

    // 2. Get CORS Policy.
    logger.LogInformation("Retrieving CORS policy for bucket {BucketName}", bucketName);
    var retrievedPolicy = await cf.Accounts.Buckets.GetCorsAsync(bucketName);
    logger.LogInformation("Retrieved CORS policy with {RuleCount} rules:", retrievedPolicy.Rules.Count);

    foreach (var rule in retrievedPolicy.Rules)
    {
      logger.LogInformation("  Rule: {Id}", rule.Id ?? "Unnamed");
      logger.LogInformation("    Methods: {Methods}", string.Join(", ", rule.Allowed.Methods));
      logger.LogInformation("    Origins: {Origins}", string.Join(", ", rule.Allowed.Origins));
      logger.LogInformation("    Max Age: {MaxAge}s", rule.MaxAgeSeconds);
    }

    // 3. Update CORS Policy (simpler policy).
    logger.LogInformation("Updating CORS policy to a simpler configuration");
    var simpleCorsPolicy = new BucketCorsPolicy(
      new[]
      {
        new CorsRule(
          new CorsAllowed(
            new[] { "GET" },
            new[] { "*" },
            new[] { "Content-Type" }
          ),
          "Public Read",
          MaxAgeSeconds: 86400
        )
      }
    );

    await cf.Accounts.Buckets.SetCorsAsync(bucketName, simpleCorsPolicy);
    logger.LogInformation("CORS policy updated to public read-only access");

    // 4. Delete CORS Policy.
    logger.LogInformation("Deleting CORS policy from bucket {BucketName}", bucketName);
    await cf.Accounts.Buckets.DeleteCorsAsync(bucketName);
    logger.LogInformation("CORS policy deleted successfully");
  }

  /// <summary>Demonstrates lifecycle policy configuration.</summary>
  private async Task RunLifecycleConfigurationAsync(string bucketName)
  {
    logger.LogInformation("--- Running Lifecycle Configuration for bucket {BucketName} ---", bucketName);

    // 1. Set Lifecycle Policy with multiple rules demonstrating all capabilities.
    logger.LogInformation("Setting lifecycle policy for bucket {BucketName}", bucketName);
    var lifecyclePolicy = new BucketLifecyclePolicy(
      new[]
      {
        // Rule to delete old log files after 90 days.
        new LifecycleRule(
          "Delete old logs",
          true,
          new LifecycleRuleConditions("logs/"),
          DeleteObjectsTransition: new DeleteObjectsTransition(LifecycleCondition.AfterDays(90))
        ),
        // Rule to abort incomplete multipart uploads after 7 days.
        new LifecycleRule(
          "Cleanup incomplete uploads",
          true,
          AbortMultipartUploadsTransition: new AbortMultipartUploadsTransition(LifecycleCondition.AfterDays(7))
        ),
        // Rule to transition archived data to Infrequent Access storage class after 30 days.
        new LifecycleRule(
          "Archive to Infrequent Access",
          true,
          new LifecycleRuleConditions("archive/"),
          StorageClassTransitions: new[]
          {
            new StorageClassTransition(LifecycleCondition.AfterDays(30), R2StorageClass.InfrequentAccess)
          }
        ),
        // Combined rule: transition to IA then delete (for temp files).
        new LifecycleRule(
          "Temp file lifecycle",
          true,
          new LifecycleRuleConditions("temp/"),
          StorageClassTransitions: new[]
          {
            new StorageClassTransition(LifecycleCondition.AfterDays(14), R2StorageClass.InfrequentAccess)
          },
          DeleteObjectsTransition: new DeleteObjectsTransition(LifecycleCondition.AfterDays(60))
        )
      }
    );

    await cf.Accounts.Buckets.SetLifecycleAsync(bucketName, lifecyclePolicy);
    logger.LogInformation("Lifecycle policy set successfully with {RuleCount} rules", lifecyclePolicy.Rules.Count);

    // 2. Get Lifecycle Policy.
    logger.LogInformation("Retrieving lifecycle policy for bucket {BucketName}", bucketName);
    var retrievedPolicy = await cf.Accounts.Buckets.GetLifecycleAsync(bucketName);
    logger.LogInformation("Retrieved lifecycle policy with {RuleCount} rules:", retrievedPolicy.Rules.Count);

    foreach (var rule in retrievedPolicy.Rules)
    {
      logger.LogInformation("  Rule: {Id} (Enabled: {Enabled})", rule.Id ?? "Unnamed", rule.Enabled);

      if (rule.Conditions?.Prefix != null)
        logger.LogInformation("    Prefix filter: {Prefix}", rule.Conditions.Prefix);

      if (rule.DeleteObjectsTransition != null)
        logger.LogInformation("    Delete after: {Days} days", rule.DeleteObjectsTransition.Condition.MaxAge);

      if (rule.AbortMultipartUploadsTransition != null)
        logger.LogInformation("    Abort multipart after: {Days} days", rule.AbortMultipartUploadsTransition.Condition.MaxAge);

      if (rule.StorageClassTransitions != null)
        foreach (var transition in rule.StorageClassTransitions)
          logger.LogInformation("    Transition to {StorageClass} after: {Days} days", transition.StorageClass,
                                transition.Condition.MaxAge);
    }

    // 3. Update Lifecycle Policy (simpler configuration).
    logger.LogInformation("Updating lifecycle policy to a simpler configuration");
    var simpleLifecyclePolicy = new BucketLifecyclePolicy(
      new[]
      {
        // Single rule: delete all objects after 365 days.
        new LifecycleRule(
          "Annual cleanup",
          true,
          DeleteObjectsTransition: new DeleteObjectsTransition(LifecycleCondition.AfterDays(365))
        )
      }
    );

    await cf.Accounts.Buckets.SetLifecycleAsync(bucketName, simpleLifecyclePolicy);
    logger.LogInformation("Lifecycle policy updated to annual cleanup rule");

    // 4. Delete Lifecycle Policy.
    logger.LogInformation("Deleting lifecycle policy from bucket {BucketName}", bucketName);
    await cf.Accounts.Buckets.DeleteLifecycleAsync(bucketName);
    logger.LogInformation("Lifecycle policy deleted successfully");
  }

  /// <summary>Demonstrates bucket lock (retention) policy configuration.</summary>
  private async Task RunBucketLockConfigurationAsync(string bucketName)
  {
    logger.LogInformation("--- Running Bucket Lock Configuration for bucket {BucketName} ---", bucketName);

    // 1. Get initial lock policy (should be empty for new bucket).
    logger.LogInformation("Getting bucket lock policy for {BucketName}", bucketName);
    var initialPolicy = await cf.Accounts.Buckets.GetLockAsync(bucketName);
    logger.LogInformation("Initial lock policy has {RuleCount} rules", initialPolicy.Rules.Count);

    // 2. Set a bucket lock policy with multiple rules.
    logger.LogInformation("Setting bucket lock policy for {BucketName}", bucketName);
    var lockPolicy = new BucketLockPolicy(
      new[]
      {
        // Rule for compliance data: 7 year retention.
        new BucketLockRule(
          Id: "compliance-retention",
          Enabled: true,
          Prefix: "compliance/",
          Condition: BucketLockCondition.ForDays(2555) // ~7 years
        ),
        // Rule for logs: 90 day retention.
        new BucketLockRule(
          Id: "log-retention",
          Enabled: true,
          Prefix: "logs/",
          Condition: BucketLockCondition.ForDays(90)
        ),
        // General rule: 30 day retention for all objects.
        new BucketLockRule(
          Id: "general-retention",
          Enabled: true,
          Condition: BucketLockCondition.ForDays(30)
        )
      }
    );

    var setResult = await cf.Accounts.Buckets.SetLockAsync(bucketName, lockPolicy);
    logger.LogInformation("Bucket lock policy set with {RuleCount} rules:", setResult.Rules.Count);

    foreach (var rule in setResult.Rules)
    {
      logger.LogInformation("  Rule: {Id} (Enabled: {Enabled})", rule.Id ?? "Unnamed", rule.Enabled);
      logger.LogInformation("    Prefix:    {Prefix}", rule.Prefix ?? "(all objects)");
      logger.LogInformation("    Type:      {Type}", rule.Condition?.Type);

      if (rule.Condition?.MaxAgeSeconds.HasValue == true)
        logger.LogInformation("    Duration:  {Days} days", rule.Condition.MaxAgeSeconds.Value / 86400);
    }

    // 3. Get updated lock policy.
    logger.LogInformation("Retrieving bucket lock policy for {BucketName}", bucketName);
    var retrievedPolicy = await cf.Accounts.Buckets.GetLockAsync(bucketName);
    logger.LogInformation("Retrieved lock policy has {RuleCount} rules", retrievedPolicy.Rules.Count);

    // 4. Delete lock policy.
    logger.LogInformation("Deleting bucket lock policy from {BucketName}", bucketName);
    await cf.Accounts.Buckets.DeleteLockAsync(bucketName);
    logger.LogInformation("Bucket lock policy deleted successfully");
  }

  /// <summary>
  ///   Demonstrates Sippy migration configuration conceptually.
  ///   Note: Actual Sippy configuration requires valid AWS or GCS credentials.
  /// </summary>
  private void RunSippyConceptualSample()
  {
    logger.LogInformation("--- Sippy Migration (Conceptual Sample) ---");
    logger.LogInformation("");
    logger.LogInformation("Sippy enables incremental migration from AWS S3 or GCS to R2.");
    logger.LogInformation("When enabled, objects not found in R2 are fetched from the source,");
    logger.LogInformation("returned to the client, and copied to R2 for future requests.");
    logger.LogInformation("");
    logger.LogInformation("To use Sippy, you need:");
    logger.LogInformation("  1. Source bucket credentials (AWS S3 or GCS)");
    logger.LogInformation("  2. The source bucket name and region");
    logger.LogInformation("");
    logger.LogInformation("Example code for AWS S3 migration:");
    logger.LogInformation(@"
    // Get current Sippy status
    var config = await cf.Accounts.Buckets.GetSippyAsync(""my-bucket"");
    Console.WriteLine($""Sippy enabled: {config.Enabled}"");

    // Enable Sippy from AWS S3
    var awsSource = SippyAwsSource.Create(
        bucket: ""source-bucket"",
        region: ""us-east-1"",
        accessKeyId: ""AKIAIOSFODNN7EXAMPLE"",
        secretAccessKey: ""wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY""
    );

    var result = await cf.Accounts.Buckets.EnableSippyAsync(
        ""my-bucket"",
        new EnableSippyFromAwsRequest(awsSource)
    );

    // When migration is complete, disable Sippy
    await cf.Accounts.Buckets.DisableSippyAsync(""my-bucket"");
");
    logger.LogInformation("");
    logger.LogInformation("Example code for GCS migration:");
    logger.LogInformation(@"
    var gcsSource = SippyGcsSource.Create(
        bucket: ""source-bucket"",
        clientEmail: ""sippy@project.iam.gserviceaccount.com"",
        privateKey: ""-----BEGIN PRIVATE KEY-----...""
    );

    var result = await cf.Accounts.Buckets.EnableSippyAsync(
        ""my-bucket"",
        new EnableSippyFromGcsRequest(gcsSource)
    );
");
  }

  /// <summary>
  ///   Demonstrates temporary credentials creation conceptually.
  ///   Note: Actual temporary credential creation requires a valid R2 Access Key ID.
  /// </summary>
  private void RunTempCredentialsConceptualSample()
  {
    logger.LogInformation("--- Temporary Credentials (Conceptual Sample) ---");
    logger.LogInformation("");
    logger.LogInformation("Temporary credentials provide scoped, time-limited access to R2.");
    logger.LogInformation("They're useful for granting limited access without exposing main credentials.");
    logger.LogInformation("");
    logger.LogInformation("To create temporary credentials, you need:");
    logger.LogInformation("  1. An R2 Access Key ID (from Cloudflare dashboard)");
    logger.LogInformation("  2. The target bucket name");
    logger.LogInformation("  3. Desired permission level and TTL");
    logger.LogInformation("");
    logger.LogInformation("Example code:");
    logger.LogInformation(@"
    // Create read-only credentials for a specific bucket
    var credentials = await cf.Accounts.Buckets.CreateTempCredentialsAsync(
        new CreateTempCredentialsRequest(
            Bucket: ""my-bucket"",
            ParentAccessKeyId: ""your-r2-access-key-id"",
            Permission: TempCredentialPermission.ObjectReadOnly,
            TtlSeconds: 3600 // 1 hour
        )
    );

    Console.WriteLine($""Access Key: {credentials.AccessKeyId}"");
    Console.WriteLine($""Secret Key: {credentials.SecretAccessKey}"");
    Console.WriteLine($""Session Token: {credentials.SessionToken}"");

    // Create write-only credentials scoped to a prefix
    var uploadCreds = await cf.Accounts.Buckets.CreateTempCredentialsAsync(
        new CreateTempCredentialsRequest(
            Bucket: ""my-bucket"",
            ParentAccessKeyId: ""your-r2-access-key-id"",
            Permission: TempCredentialPermission.ObjectWriteOnly,
            TtlSeconds: 900, // 15 minutes
            Prefixes: [""uploads/user123/""]
        )
    );

    // Create credentials for specific objects
    var downloadCreds = await cf.Accounts.Buckets.CreateTempCredentialsAsync(
        new CreateTempCredentialsRequest(
            Bucket: ""my-bucket"",
            ParentAccessKeyId: ""your-r2-access-key-id"",
            Permission: TempCredentialPermission.ObjectReadOnly,
            TtlSeconds: 3600,
            Objects: [""documents/report.pdf"", ""documents/summary.pdf""]
        )
    );
");
    logger.LogInformation("");
    logger.LogInformation("Permission levels:");
    logger.LogInformation("  - ObjectReadOnly:   Read objects only");
    logger.LogInformation("  - ObjectWriteOnly:  Write objects only");
    logger.LogInformation("  - ObjectReadWrite:  Read and write objects");
    logger.LogInformation("  - AdminReadOnly:    Admin read (includes bucket metadata)");
    logger.LogInformation("  - AdminReadWrite:   Full admin access");
  }

  #endregion
}
