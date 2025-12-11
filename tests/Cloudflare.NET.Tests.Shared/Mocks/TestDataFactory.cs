namespace Cloudflare.NET.Tests.Shared.Mocks;

/// <summary>
///   Factory for generating consistent test data (IDs, entities, requests).
///   Use this to avoid magic strings scattered across tests.
/// </summary>
public static class TestDataFactory
{
  #region Constants

  /// <summary>Well-known test zone ID for mock responses.</summary>
  public const string TestZoneId = "023e105f4ecef8ad9ca31a8372d0c353";

  /// <summary>Well-known test account ID for mock responses.</summary>
  public const string TestAccountId = "01a7362d577a6c3019a474fd6f485823";

  /// <summary>Well-known test user ID for mock responses.</summary>
  public const string TestUserId = "7c5dae5552338874e5053f2534d2767a";

  /// <summary>Well-known test domain for mock responses.</summary>
  public const string TestDomain = "example.com";

  /// <summary>Well-known test API token ID.</summary>
  public const string TestApiTokenId = "f267e341f3dd4697bd3b9f71dd96247f";

  /// <summary>Well-known test DNS record ID.</summary>
  public const string TestDnsRecordId = "372e67954025e0ba6aaa6d586b9e0b59";

  /// <summary>Well-known test member ID.</summary>
  public const string TestMemberId = "4536bcfad5faccb111b47003c79917fa";

  /// <summary>Well-known test role ID.</summary>
  public const string TestRoleId = "3536bcfad5faccb999b47003c79917fb";

  /// <summary>Well-known test subscription ID.</summary>
  public const string TestSubscriptionId = "506e3185e9c882d175a2d0cb0093d9f2";

  /// <summary>Well-known test widget site key.</summary>
  public const string TestWidgetSiteKey = "0x4AAAAAAACdAMz9fjHP0dVe";

  /// <summary>Well-known test worker route ID.</summary>
  public const string TestWorkerRouteId = "9a7806061c88ada191ed06f989cc3dac";

  /// <summary>Standard ISO 8601 date string for testing.</summary>
  public const string TestDateTimeString = "2024-01-15T10:30:00Z";

  /// <summary>Standard created_on date string for testing.</summary>
  public const string TestCreatedOnString = "2024-01-01T00:00:00Z";

  /// <summary>Standard modified_on date string for testing.</summary>
  public const string TestModifiedOnString = "2024-01-15T10:30:00Z";

  #endregion


  #region ID Generators

  /// <summary>Generates a random Cloudflare-style ID (32 hex characters).</summary>
  /// <returns>A 32-character hexadecimal ID.</returns>
  public static string GenerateId() => Guid.NewGuid().ToString("N");

  /// <summary>Generates a unique test domain name.</summary>
  /// <returns>A unique domain name in the format test-{guid}.example.com.</returns>
  public static string GenerateTestDomain() => $"test-{Guid.NewGuid():N}.example.com";

  /// <summary>Generates a unique DNS record name.</summary>
  /// <param name="baseDomain">Base domain to append to.</param>
  /// <returns>A unique DNS record name.</returns>
  public static string GenerateDnsRecordName(string baseDomain = TestDomain) =>
    $"test-{Guid.NewGuid().ToString("N")[..8]}.{baseDomain}";

  /// <summary>Generates a unique test email address.</summary>
  /// <returns>A unique email address.</returns>
  public static string GenerateTestEmail() => $"test-{Guid.NewGuid().ToString("N")[..8]}@example.com";

  /// <summary>Generates a unique test token name.</summary>
  /// <returns>A unique token name.</returns>
  public static string GenerateTokenName() => $"Test Token {Guid.NewGuid().ToString("N")[..8]}";

  #endregion


  #region Entity Generators

  /// <summary>Creates a minimal Zone entity for testing.</summary>
  /// <param name="id">Optional zone ID.</param>
  /// <param name="name">Optional zone name.</param>
  /// <param name="status">Zone status (default: active).</param>
  /// <param name="type">Zone type (default: full).</param>
  /// <returns>An anonymous object representing a zone.</returns>
  public static object CreateZone(
    string? id = null,
    string? name = null,
    string status = "active",
    string type = "full")
  {
    return new
    {
      id = id ?? GenerateId(),
      name = name ?? TestDomain,
      status,
      account = new { id = TestAccountId, name = "Test Account" },
      owner = new { id = TestUserId, type = "user" },
      plan = new
      {
        id = "0feeeeeeeeeeeeeeeeeeeeeeeeeeeeee",
        name = "Free Website",
        price = 0,
        currency = "USD",
        is_subscribed = true,
        can_subscribe = true,
        legacy_id = "free",
        legacy_discount = false,
        externally_managed = false
      },
      name_servers = new[] { "ns1.example.com", "ns2.example.com" },
      original_name_servers = new[] { "ns1.original.com" },
      type,
      development_mode = 0,
      paused = false,
      permissions = new[] { "#zone:read", "#zone:edit" },
      activated_on = TestDateTimeString,
      created_on = TestCreatedOnString,
      modified_on = TestModifiedOnString
    };
  }

  /// <summary>Creates a minimal DnsRecord entity for testing.</summary>
  /// <param name="id">Optional record ID.</param>
  /// <param name="type">Record type (default: A).</param>
  /// <param name="name">Optional record name.</param>
  /// <param name="content">Optional record content.</param>
  /// <param name="proxied">Whether the record is proxied (default: false).</param>
  /// <returns>An anonymous object representing a DNS record.</returns>
  public static object CreateDnsRecord(
    string? id = null,
    string type = "A",
    string? name = null,
    string? content = null,
    bool proxied = false)
  {
    return new
    {
      id = id ?? GenerateId(),
      type,
      name = name ?? $"test.{TestDomain}",
      content = content ?? GetDefaultDnsContent(type),
      zone_id = TestZoneId,
      zone_name = TestDomain,
      proxiable = true,
      proxied,
      ttl = 3600,
      locked = false,
      created_on = TestCreatedOnString,
      modified_on = TestModifiedOnString
    };
  }

  /// <summary>Creates a minimal Account entity for testing.</summary>
  /// <param name="id">Optional account ID.</param>
  /// <param name="name">Optional account name.</param>
  /// <param name="type">Account type (default: standard).</param>
  /// <returns>An anonymous object representing an account.</returns>
  public static object CreateAccount(
    string? id = null,
    string? name = null,
    string type = "standard")
  {
    return new
    {
      id = id ?? TestAccountId,
      name = name ?? "Test Account",
      type,
      settings = new
      {
        enforce_twofactor = false,
        use_account_custom_ns_by_default = false
      },
      created_on = TestCreatedOnString
    };
  }

  /// <summary>Creates a minimal AuditLog entity for testing.</summary>
  /// <param name="id">Optional audit log ID.</param>
  /// <param name="actionType">Action type (default: token_create).</param>
  /// <returns>An anonymous object representing an audit log entry.</returns>
  public static object CreateAuditLog(
    string? id = null,
    string actionType = "token_create")
  {
    return new
    {
      id = id ?? GenerateId(),
      action = new { type = actionType, result = true },
      actor = new
      {
        id = TestUserId,
        email = "test@example.com",
        type = "user",
        ip = "192.0.2.1"
      },
      when = TestDateTimeString,
      resource = new
      {
        type = "token",
        id = TestApiTokenId
      },
      @interface = "API",
      metadata = new { }
    };
  }

  /// <summary>Creates a minimal ApiToken entity for testing.</summary>
  /// <param name="id">Optional token ID.</param>
  /// <param name="status">Token status (default: active).</param>
  /// <param name="name">Optional token name.</param>
  /// <returns>An anonymous object representing an API token.</returns>
  public static object CreateApiToken(
    string? id = null,
    string status = "active",
    string? name = null)
  {
    return new
    {
      id = id ?? TestApiTokenId,
      name = name ?? "Test Token",
      status,
      issued_on = TestCreatedOnString,
      modified_on = TestModifiedOnString,
      policies = new[]
      {
        new
        {
          id = GenerateId(),
          effect = "allow",
          permission_groups = new[]
          {
            new { id = GenerateId(), name = "Zone Read" }
          },
          resources = new Dictionary<string, string>
          {
            ["com.cloudflare.api.account.zone.*"] = "*"
          }
        }
      }
    };
  }

  /// <summary>Creates a minimal AccountMember entity for testing.</summary>
  /// <param name="id">Optional member ID.</param>
  /// <param name="email">Optional user email.</param>
  /// <param name="status">Member status (default: accepted).</param>
  /// <returns>An anonymous object representing an account member.</returns>
  public static object CreateAccountMember(
    string? id = null,
    string? email = null,
    string status = "accepted")
  {
    return new
    {
      id = id ?? TestMemberId,
      status,
      user = new
      {
        id = TestUserId,
        email = email ?? "test@example.com",
        first_name = "Test",
        last_name = "User",
        two_factor_authentication_enabled = false
      },
      roles = new[]
      {
        new { id = TestRoleId, name = "Administrator", description = "Full access", permissions = new { } }
      }
    };
  }

  /// <summary>Creates a minimal AccountRole entity for testing.</summary>
  /// <param name="id">Optional role ID.</param>
  /// <param name="name">Optional role name.</param>
  /// <returns>An anonymous object representing an account role.</returns>
  public static object CreateAccountRole(
    string? id = null,
    string? name = null)
  {
    return new
    {
      id = id ?? TestRoleId,
      name = name ?? "Administrator",
      description = "Administrative access to the entire Account",
      permissions = new
      {
        analytics = new { read = true, write = true },
        billing = new { read = true, write = true },
        zone = new { read = true, write = true }
      }
    };
  }

  /// <summary>Creates a minimal User entity for testing.</summary>
  /// <param name="id">Optional user ID.</param>
  /// <param name="email">Optional user email.</param>
  /// <returns>An anonymous object representing a user.</returns>
  public static object CreateUser(
    string? id = null,
    string? email = null)
  {
    return new
    {
      id = id ?? TestUserId,
      email = email ?? "test@example.com",
      first_name = "Test",
      last_name = "User",
      username = "testuser",
      telephone = "+1234567890",
      country = "US",
      zipcode = "12345",
      created_on = TestCreatedOnString,
      modified_on = TestModifiedOnString,
      two_factor_authentication_enabled = false,
      suspended = false
    };
  }

  /// <summary>Creates a minimal Subscription entity for testing.</summary>
  /// <param name="id">Optional subscription ID.</param>
  /// <param name="state">Subscription state (default: Paid).</param>
  /// <returns>An anonymous object representing a subscription.</returns>
  public static object CreateSubscription(
    string? id = null,
    string state = "Paid")
  {
    return new
    {
      id = id ?? TestSubscriptionId,
      state,
      price = 20,
      currency = "USD",
      frequency = "monthly",
      rate_plan = new
      {
        id = "free",
        public_name = "Free Plan",
        currency = "USD",
        scope = "zone"
      },
      current_period_start = TestCreatedOnString,
      current_period_end = "2024-02-01T00:00:00Z"
    };
  }

  /// <summary>Creates a minimal TurnstileWidget entity for testing.</summary>
  /// <param name="siteKey">Optional site key.</param>
  /// <param name="name">Optional widget name.</param>
  /// <param name="mode">Widget mode (default: managed).</param>
  /// <returns>An anonymous object representing a Turnstile widget.</returns>
  public static object CreateTurnstileWidget(
    string? siteKey = null,
    string? name = null,
    string mode = "managed")
  {
    return new
    {
      sitekey = siteKey ?? TestWidgetSiteKey,
      name = name ?? "Test Widget",
      mode,
      secret = "0x4AAAAAAACdAMz9fjHP0dVeSecret",
      domains = new[] { "example.com" },
      bot_fight_mode = false,
      created_on = TestCreatedOnString,
      modified_on = TestModifiedOnString
    };
  }

  /// <summary>Creates a minimal WorkerRoute entity for testing.</summary>
  /// <param name="id">Optional route ID.</param>
  /// <param name="pattern">Optional route pattern.</param>
  /// <param name="script">Optional script name.</param>
  /// <returns>An anonymous object representing a Worker route.</returns>
  public static object CreateWorkerRoute(
    string? id = null,
    string? pattern = null,
    string? script = null)
  {
    return new
    {
      id = id ?? TestWorkerRouteId,
      pattern = pattern ?? "example.com/*",
      script = script ?? "my-worker"
    };
  }

  /// <summary>Creates a minimal ZoneHold entity for testing.</summary>
  /// <param name="hold">Whether the zone is held (default: false).</param>
  /// <param name="holdAfter">Optional hold after date.</param>
  /// <param name="includeSubdomains">Whether the hold includes subdomains (default: null).</param>
  /// <returns>An anonymous object representing a zone hold.</returns>
  public static object CreateZoneHold(
    bool hold = false,
    string? holdAfter = null,
    bool? includeSubdomains = null)
  {
    return new
    {
      hold,
      hold_after = holdAfter,
      include_subdomains = includeSubdomains
    };
  }

  /// <summary>Creates a minimal ZoneSetting entity for testing.</summary>
  /// <param name="id">Setting ID.</param>
  /// <param name="value">Setting value.</param>
  /// <returns>An anonymous object representing a zone setting.</returns>
  public static object CreateZoneSetting(
    string id,
    object value)
  {
    return new
    {
      id,
      value,
      editable = true,
      modified_on = TestModifiedOnString
    };
  }

  #endregion


  #region Collection Generators

  /// <summary>Creates a list of Zone entities for pagination testing.</summary>
  /// <param name="count">Number of zones to create.</param>
  /// <returns>An enumerable of zone objects.</returns>
  public static IEnumerable<object> CreateZones(int count) =>
    Enumerable.Range(0, count).Select(i => CreateZone(
      id: GenerateId(),
      name: $"zone{i + 1}.example.com"));

  /// <summary>Creates a list of DnsRecord entities for pagination testing.</summary>
  /// <param name="count">Number of records to create.</param>
  /// <returns>An enumerable of DNS record objects.</returns>
  public static IEnumerable<object> CreateDnsRecords(int count) =>
    Enumerable.Range(0, count).Select(i => CreateDnsRecord(
      id: GenerateId(),
      name: $"record{i + 1}.{TestDomain}"));

  /// <summary>Creates a list of Account entities for pagination testing.</summary>
  /// <param name="count">Number of accounts to create.</param>
  /// <returns>An enumerable of account objects.</returns>
  public static IEnumerable<object> CreateAccounts(int count) =>
    Enumerable.Range(0, count).Select(i => CreateAccount(
      id: GenerateId(),
      name: $"Account {i + 1}"));

  /// <summary>Creates a list of AuditLog entities for pagination testing.</summary>
  /// <param name="count">Number of audit logs to create.</param>
  /// <returns>An enumerable of audit log objects.</returns>
  public static IEnumerable<object> CreateAuditLogs(int count) =>
    Enumerable.Range(0, count).Select(_ => CreateAuditLog(id: GenerateId()));

  /// <summary>Creates a list of AccountMember entities for pagination testing.</summary>
  /// <param name="count">Number of members to create.</param>
  /// <returns>An enumerable of member objects.</returns>
  public static IEnumerable<object> CreateAccountMembers(int count) =>
    Enumerable.Range(0, count).Select(i => CreateAccountMember(
      id: GenerateId(),
      email: $"member{i + 1}@example.com"));

  /// <summary>Creates a list of AccountRole entities for pagination testing.</summary>
  /// <param name="count">Number of roles to create.</param>
  /// <returns>An enumerable of role objects.</returns>
  public static IEnumerable<object> CreateAccountRoles(int count) =>
    Enumerable.Range(0, count).Select(i => CreateAccountRole(
      id: GenerateId(),
      name: $"Role {i + 1}"));

  #endregion


  #region Helper Methods

  /// <summary>Gets the default content for a DNS record type.</summary>
  /// <param name="type">The DNS record type.</param>
  /// <returns>Default content appropriate for the record type.</returns>
  private static string GetDefaultDnsContent(string type) =>
    type switch
    {
      "A" => "192.0.2.1",
      "AAAA" => "2001:db8::1",
      "CNAME" => "target.example.com",
      "MX" => "mail.example.com",
      "TXT" => "v=spf1 include:example.com ~all",
      "NS" => "ns1.example.com",
      "SRV" => "10 5 5060 sipserver.example.com",
      "CAA" => "0 issue \"letsencrypt.org\"",
      "PTR" => "host.example.com",
      _ => "192.0.2.1"
    };

  #endregion
}
