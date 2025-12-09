namespace Cloudflare.NET.Tests.UnitTests.Json;

using System.Text.Json;
using System.Text.Json.Serialization;
using Accounts.Models;
using Core.Json;
using Shared.Fixtures;
using Xunit.Abstractions;

/// <summary>
///   Contains unit tests for verifying the JSON serialization of R2 lifecycle models.
///   These tests ensure the SDK produces JSON that matches the Cloudflare R2 lifecycle API expectations.
/// </summary>
/// <remarks>
///   The Cloudflare R2 lifecycle API uses camelCase property names, unlike most Cloudflare APIs which use snake_case.
///   This test class validates that the serialization options used in <see cref="Accounts.AccountsApi.SetBucketLifecycleAsync"/>
///   produce correctly formatted JSON.
/// </remarks>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class LifecycleModelSerializationTests
{
  #region Properties & Fields - Non-Public

  /// <summary>
  ///   JSON serializer options that match the configuration used in SetBucketLifecycleAsync.
  ///   This must be kept in sync with the actual implementation.
  /// </summary>
  private readonly JsonSerializerOptions _lifecycleSerializerOptions = new()
  {
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
  };

  /// <summary>Test output helper for diagnostic logging during tests.</summary>
  private readonly ITestOutputHelper _testOutputHelper;

  #endregion


  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="LifecycleModelSerializationTests"/> class.</summary>
  /// <param name="testOutputHelper">The xUnit test output helper for logging.</param>
  public LifecycleModelSerializationTests(ITestOutputHelper testOutputHelper)
  {
    _testOutputHelper = testOutputHelper;
  }

  #endregion


  #region LifecycleCondition Serialization Tests

  /// <summary>Verifies that an age-based lifecycle condition serializes with correct property names and values.</summary>
  [Fact]
  public void Serialize_LifecycleCondition_AgeBased_ProducesCorrectJson()
  {
    // Arrange - Create an age-based condition (7 days = 604800 seconds)
    var condition = LifecycleCondition.AfterDays(7);

    // Act
    var json = JsonSerializer.Serialize(condition, _lifecycleSerializerOptions);

    // Assert - Verify exact JSON structure
    using var doc = JsonDocument.Parse(json);
    var root = doc.RootElement;

    // Verify property names are camelCase as expected by Cloudflare
    root.GetProperty("type").GetString().Should().Be("Age");
    root.GetProperty("maxAge").GetInt32().Should().Be(604800); // 7 * 86400

    // Verify 'date' property is not present (null values should be omitted)
    root.TryGetProperty("date", out _).Should().BeFalse();
  }

  /// <summary>Verifies that a date-based lifecycle condition serializes correctly.</summary>
  [Fact]
  public void Serialize_LifecycleCondition_DateBased_ProducesCorrectJson()
  {
    // Arrange
    var targetDate = new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc);
    var condition = LifecycleCondition.OnDate(targetDate);

    // Act
    var json = JsonSerializer.Serialize(condition, _lifecycleSerializerOptions);

    // Assert
    using var doc = JsonDocument.Parse(json);
    var root = doc.RootElement;

    root.GetProperty("type").GetString().Should().Be("Date");
    root.TryGetProperty("maxAge", out _).Should().BeFalse(); // maxAge should be omitted for date-based
    root.GetProperty("date").GetDateTime().Should().Be(targetDate);
  }

  /// <summary>Verifies the exact raw JSON output for an age-based condition matches Cloudflare's expected format.</summary>
  [Fact]
  public void Serialize_LifecycleCondition_AgeBased_MatchesCloudflareFormat()
  {
    // Arrange
    var condition = LifecycleCondition.AfterDays(1); // 1 day = 86400 seconds

    // Act
    var json = JsonSerializer.Serialize(condition, _lifecycleSerializerOptions);

    // Assert - The exact JSON format expected by Cloudflare
    var expectedJson = """{"type":"Age","maxAge":86400}""";
    json.Should().Be(expectedJson);
  }

  #endregion


  #region AbortMultipartUploadsTransition Serialization Tests

  /// <summary>Verifies that AbortMultipartUploadsTransition serializes with correct nested structure.</summary>
  [Fact]
  public void Serialize_AbortMultipartUploadsTransition_ProducesCorrectJson()
  {
    // Arrange
    var transition = new AbortMultipartUploadsTransition(LifecycleCondition.AfterDays(7));

    // Act
    var json = JsonSerializer.Serialize(transition, _lifecycleSerializerOptions);

    // Assert
    using var doc = JsonDocument.Parse(json);
    var root = doc.RootElement;

    // Verify nested 'condition' object
    var conditionElement = root.GetProperty("condition");
    conditionElement.GetProperty("type").GetString().Should().Be("Age");
    conditionElement.GetProperty("maxAge").GetInt32().Should().Be(604800);
  }

  /// <summary>Verifies the exact raw JSON for AbortMultipartUploadsTransition.</summary>
  [Fact]
  public void Serialize_AbortMultipartUploadsTransition_MatchesCloudflareFormat()
  {
    // Arrange
    var transition = new AbortMultipartUploadsTransition(LifecycleCondition.AfterDays(7));

    // Act
    var json = JsonSerializer.Serialize(transition, _lifecycleSerializerOptions);

    // Assert
    var expectedJson = """{"condition":{"type":"Age","maxAge":604800}}""";
    json.Should().Be(expectedJson);
  }

  #endregion


  #region DeleteObjectsTransition Serialization Tests

  /// <summary>Verifies that DeleteObjectsTransition serializes correctly.</summary>
  [Fact]
  public void Serialize_DeleteObjectsTransition_ProducesCorrectJson()
  {
    // Arrange - Delete objects after 90 days
    var transition = new DeleteObjectsTransition(LifecycleCondition.AfterDays(90));

    // Act
    var json = JsonSerializer.Serialize(transition, _lifecycleSerializerOptions);

    // Assert
    using var doc = JsonDocument.Parse(json);
    var root = doc.RootElement;

    var conditionElement = root.GetProperty("condition");
    conditionElement.GetProperty("type").GetString().Should().Be("Age");
    conditionElement.GetProperty("maxAge").GetInt32().Should().Be(90 * 86400);
  }

  #endregion


  #region StorageClassTransition Serialization Tests

  /// <summary>Verifies that StorageClassTransition serializes with storage class value.</summary>
  [Fact]
  public void Serialize_StorageClassTransition_ProducesCorrectJson()
  {
    // Arrange
    var transition = new StorageClassTransition(
      LifecycleCondition.AfterDays(30),
      R2StorageClass.InfrequentAccess
    );

    // Act
    var json = JsonSerializer.Serialize(transition, _lifecycleSerializerOptions);

    // Assert
    using var doc = JsonDocument.Parse(json);
    var root = doc.RootElement;

    var conditionElement = root.GetProperty("condition");
    conditionElement.GetProperty("type").GetString().Should().Be("Age");
    conditionElement.GetProperty("maxAge").GetInt32().Should().Be(30 * 86400);

    root.GetProperty("storageClass").GetString().Should().Be("InfrequentAccess");
  }

  #endregion


  #region LifecycleRule Serialization Tests

  /// <summary>Verifies that a minimal lifecycle rule (abort multipart only) serializes correctly.</summary>
  [Fact]
  public void Serialize_LifecycleRule_AbortMultipartOnly_ProducesCorrectJson()
  {
    // Arrange
    var rule = new LifecycleRule(
      Id: "abort-incomplete-multipart-uploads",
      Enabled: true,
      AbortMultipartUploadsTransition: new AbortMultipartUploadsTransition(
        LifecycleCondition.AfterDays(7)
      )
    );

    // Act
    var json = JsonSerializer.Serialize(rule, _lifecycleSerializerOptions);

    // Assert
    using var doc = JsonDocument.Parse(json);
    var root = doc.RootElement;

    root.GetProperty("id").GetString().Should().Be("abort-incomplete-multipart-uploads");
    root.GetProperty("enabled").GetBoolean().Should().BeTrue();

    // Conditions should be omitted when null
    root.TryGetProperty("conditions", out _).Should().BeFalse();

    // DeleteObjectsTransition should be omitted when null
    root.TryGetProperty("deleteObjectsTransition", out _).Should().BeFalse();

    // StorageClassTransitions should be omitted when null
    root.TryGetProperty("storageClassTransitions", out _).Should().BeFalse();

    // AbortMultipartUploadsTransition should be present
    var abortTransition = root.GetProperty("abortMultipartUploadsTransition");
    abortTransition.GetProperty("condition").GetProperty("type").GetString().Should().Be("Age");
    abortTransition.GetProperty("condition").GetProperty("maxAge").GetInt32().Should().Be(604800);
  }

  /// <summary>Verifies that a rule with delete objects transition serializes correctly.</summary>
  [Fact]
  public void Serialize_LifecycleRule_DeleteObjectsWithPrefix_ProducesCorrectJson()
  {
    // Arrange
    var rule = new LifecycleRule(
      Id: "delete-old-logs",
      Enabled: true,
      Conditions: new LifecycleRuleConditions("logs/"),
      DeleteObjectsTransition: new DeleteObjectsTransition(LifecycleCondition.AfterDays(90))
    );

    // Act
    var json = JsonSerializer.Serialize(rule, _lifecycleSerializerOptions);

    // Assert
    using var doc = JsonDocument.Parse(json);
    var root = doc.RootElement;

    root.GetProperty("id").GetString().Should().Be("delete-old-logs");
    root.GetProperty("enabled").GetBoolean().Should().BeTrue();
    root.GetProperty("conditions").GetProperty("prefix").GetString().Should().Be("logs/");

    var deleteTransition = root.GetProperty("deleteObjectsTransition");
    deleteTransition.GetProperty("condition").GetProperty("type").GetString().Should().Be("Age");
    deleteTransition.GetProperty("condition").GetProperty("maxAge").GetInt32().Should().Be(90 * 86400);
  }

  /// <summary>Verifies that a rule with storage class transition serializes correctly.</summary>
  [Fact]
  public void Serialize_LifecycleRule_StorageClassTransition_ProducesCorrectJson()
  {
    // Arrange
    var rule = new LifecycleRule(
      Id: "archive-old-data",
      Enabled: true,
      Conditions: new LifecycleRuleConditions("archive/"),
      StorageClassTransitions: new[]
      {
        new StorageClassTransition(LifecycleCondition.AfterDays(30), R2StorageClass.InfrequentAccess)
      }
    );

    // Act
    var json = JsonSerializer.Serialize(rule, _lifecycleSerializerOptions);

    // Assert
    using var doc = JsonDocument.Parse(json);
    var root = doc.RootElement;

    root.GetProperty("id").GetString().Should().Be("archive-old-data");
    root.GetProperty("storageClassTransitions").GetArrayLength().Should().Be(1);

    var transition = root.GetProperty("storageClassTransitions")[0];
    transition.GetProperty("condition").GetProperty("maxAge").GetInt32().Should().Be(30 * 86400);
    transition.GetProperty("storageClass").GetString().Should().Be("InfrequentAccess");
  }

  #endregion


  #region BucketLifecyclePolicy Serialization Tests

  /// <summary>Verifies that a complete lifecycle policy with multiple rules serializes correctly.</summary>
  [Fact]
  public void Serialize_BucketLifecyclePolicy_MultipleRules_ProducesCorrectJson()
  {
    // Arrange
    var policy = new BucketLifecyclePolicy(
    [
      new LifecycleRule(
        Id: "Delete old logs",
        Enabled: true,
        Conditions: new LifecycleRuleConditions("logs/"),
        DeleteObjectsTransition: new DeleteObjectsTransition(LifecycleCondition.AfterDays(90))
      ),
      new LifecycleRule(
        Id: "Cleanup multipart uploads",
        Enabled: true,
        AbortMultipartUploadsTransition: new AbortMultipartUploadsTransition(LifecycleCondition.AfterDays(7))
      ),
      new LifecycleRule(
        Id: "Archive old data",
        Enabled: true,
        Conditions: new LifecycleRuleConditions("archive/"),
        StorageClassTransitions:
        [
          new StorageClassTransition(LifecycleCondition.AfterDays(30), R2StorageClass.InfrequentAccess)
        ]
      )
    ]);

    // Act
    var json = JsonSerializer.Serialize(policy, _lifecycleSerializerOptions);

    // Assert
    using var doc = JsonDocument.Parse(json);
    var root = doc.RootElement;

    root.GetProperty("rules").GetArrayLength().Should().Be(3);

    // Verify first rule (delete logs)
    var deleteRule = root.GetProperty("rules")[0];
    deleteRule.GetProperty("id").GetString().Should().Be("Delete old logs");
    deleteRule.GetProperty("conditions").GetProperty("prefix").GetString().Should().Be("logs/");
    deleteRule.GetProperty("deleteObjectsTransition").GetProperty("condition").GetProperty("maxAge").GetInt32()
              .Should().Be(90 * 86400);

    // Verify second rule (abort multipart)
    var abortRule = root.GetProperty("rules")[1];
    abortRule.GetProperty("id").GetString().Should().Be("Cleanup multipart uploads");
    abortRule.GetProperty("abortMultipartUploadsTransition").GetProperty("condition").GetProperty("maxAge").GetInt32()
             .Should().Be(7 * 86400);

    // Verify third rule (archive)
    var archiveRule = root.GetProperty("rules")[2];
    archiveRule.GetProperty("id").GetString().Should().Be("Archive old data");
    archiveRule.GetProperty("storageClassTransitions")[0].GetProperty("storageClass").GetString()
               .Should().Be("InfrequentAccess");
  }

  /// <summary>Verifies that an empty policy serializes to a valid JSON with empty rules array.</summary>
  [Fact]
  public void Serialize_BucketLifecyclePolicy_EmptyRules_ProducesValidJson()
  {
    // Arrange
    var policy = new BucketLifecyclePolicy([]);

    // Act
    var json = JsonSerializer.Serialize(policy, _lifecycleSerializerOptions);

    // Assert
    json.Should().Be("""{"rules":[]}""");
  }

  /// <summary>
  ///   Verifies the exact JSON format that matches the user's failing request scenario.
  ///   This test replicates the exact structure from the user's code.
  /// </summary>
  [Fact]
  public void Serialize_BucketLifecyclePolicy_AbortMultipartOnlyRule_MatchesExpectedFormat()
  {
    // Arrange - This matches the user's code structure
    var lifecyclePolicy = new BucketLifecyclePolicy(
    [
      new LifecycleRule(
        Id: "abort-incomplete-multipart-uploads",
        Enabled: true,
        AbortMultipartUploadsTransition: new AbortMultipartUploadsTransition(
          LifecycleCondition.AfterDays(7)
        )
      )
    ]);

    // Act
    var json = JsonSerializer.Serialize(lifecyclePolicy, _lifecycleSerializerOptions);

    // Assert - Verify the JSON is well-formed and matches Cloudflare's expected structure
    var expectedJson = """{"rules":[{"id":"abort-incomplete-multipart-uploads","enabled":true,"abortMultipartUploadsTransition":{"condition":{"type":"Age","maxAge":604800}}}]}""";
    json.Should().Be(expectedJson);

    // Also verify it can be parsed back
    using var doc = JsonDocument.Parse(json);
    doc.RootElement.GetProperty("rules")[0]
       .GetProperty("abortMultipartUploadsTransition")
       .GetProperty("condition")
       .GetProperty("type")
       .GetString()
       .Should().Be("Age");
  }

  #endregion


  #region Deserialization / Round-Trip Tests

  /// <summary>Verifies that a lifecycle policy can be serialized and deserialized correctly.</summary>
  [Fact]
  public void RoundTrip_BucketLifecyclePolicy_PreservesAllData()
  {
    // Arrange
    var originalPolicy = new BucketLifecyclePolicy(
    [
      new LifecycleRule(
        Id: "test-rule",
        Enabled: true,
        Conditions: new LifecycleRuleConditions("test/"),
        AbortMultipartUploadsTransition: new AbortMultipartUploadsTransition(LifecycleCondition.AfterDays(7)),
        DeleteObjectsTransition: new DeleteObjectsTransition(LifecycleCondition.AfterDays(30)),
        StorageClassTransitions:
        [
          new StorageClassTransition(LifecycleCondition.AfterDays(14), R2StorageClass.InfrequentAccess)
        ]
      )
    ]);

    // Act
    var json = JsonSerializer.Serialize(originalPolicy, _lifecycleSerializerOptions);
    var deserializedPolicy = JsonSerializer.Deserialize<BucketLifecyclePolicy>(json, _lifecycleSerializerOptions);

    // Assert
    deserializedPolicy.Should().NotBeNull();
    deserializedPolicy!.Rules.Should().HaveCount(1);

    var rule = deserializedPolicy.Rules[0];
    rule.Id.Should().Be("test-rule");
    rule.Enabled.Should().BeTrue();
    rule.Conditions!.Prefix.Should().Be("test/");
    rule.AbortMultipartUploadsTransition!.Condition.Type.Should().Be(LifecycleConditionType.Age);
    rule.AbortMultipartUploadsTransition.Condition.MaxAge.Should().Be(7 * 86400);
    rule.DeleteObjectsTransition!.Condition.MaxAge.Should().Be(30 * 86400);
    rule.StorageClassTransitions!.Should().HaveCount(1);
    rule.StorageClassTransitions[0].StorageClass.Should().Be(R2StorageClass.InfrequentAccess);
  }

  /// <summary>Verifies deserialization of JSON returned by Cloudflare API (as documented).</summary>
  [Fact]
  public void Deserialize_CloudflareApiResponse_ParsesCorrectly()
  {
    // Arrange - JSON format as returned by Cloudflare API
    const string cloudflareJson = """
      {
        "rules": [
          {
            "id": "Expire all objects older than 24 hours",
            "conditions": { "prefix": "prefix" },
            "enabled": true,
            "abortMultipartUploadsTransition": {
              "condition": { "maxAge": 86400, "type": "Age" }
            },
            "deleteObjectsTransition": {
              "condition": { "maxAge": 86400, "type": "Age" }
            },
            "storageClassTransitions": [
              {
                "condition": { "maxAge": 86400, "type": "Age" },
                "storageClass": "InfrequentAccess"
              }
            ]
          }
        ]
      }
      """;

    // Act
    var policy = JsonSerializer.Deserialize<BucketLifecyclePolicy>(cloudflareJson, _lifecycleSerializerOptions);

    // Assert
    policy.Should().NotBeNull();
    policy!.Rules.Should().HaveCount(1);

    var rule = policy.Rules[0];
    rule.Id.Should().Be("Expire all objects older than 24 hours");
    rule.Enabled.Should().BeTrue();
    rule.Conditions!.Prefix.Should().Be("prefix");

    rule.AbortMultipartUploadsTransition!.Condition.Type.Should().Be(LifecycleConditionType.Age);
    rule.AbortMultipartUploadsTransition.Condition.MaxAge.Should().Be(86400);

    rule.DeleteObjectsTransition!.Condition.Type.Should().Be(LifecycleConditionType.Age);
    rule.DeleteObjectsTransition.Condition.MaxAge.Should().Be(86400);

    rule.StorageClassTransitions.Should().HaveCount(1);
    rule.StorageClassTransitions![0].Condition.Type.Should().Be(LifecycleConditionType.Age);
    rule.StorageClassTransitions[0].StorageClass.Should().Be(R2StorageClass.InfrequentAccess);
  }

  #endregion


  #region API Request Body Verification Tests

  /// <summary>
  ///   Verifies that the JSON produced by the SDK matches the exact format expected by the Cloudflare R2 lifecycle API.
  ///   This test simulates the exact serialization path used by SetBucketLifecycleAsync.
  /// </summary>
  [Fact]
  public void Serialize_SetBucketLifecycleAsync_ExactFormatVerification()
  {
    // Arrange - Policy matching the user's failing request
    var lifecyclePolicy = new BucketLifecyclePolicy(
    [
      new LifecycleRule(
        Id: "abort-incomplete-multipart-uploads",
        Enabled: true,
        AbortMultipartUploadsTransition: new AbortMultipartUploadsTransition(
          LifecycleCondition.AfterDays(7) // 7 days = 604800 seconds
        )
      )
    ]);

    // Act - Use the exact same serialization options as SetBucketLifecycleAsync
    var json = JsonSerializer.Serialize(lifecyclePolicy, _lifecycleSerializerOptions);

    // Assert - Parse and verify the JSON structure
    using var doc = JsonDocument.Parse(json);
    var root = doc.RootElement;

    // Verify "rules" array exists and has exactly one element
    root.TryGetProperty("rules", out var rulesElement).Should().BeTrue("'rules' property must exist");
    rulesElement.ValueKind.Should().Be(JsonValueKind.Array);
    rulesElement.GetArrayLength().Should().Be(1);

    // Verify the rule structure
    var rule = rulesElement[0];
    rule.GetProperty("id").GetString().Should().Be("abort-incomplete-multipart-uploads");
    rule.GetProperty("enabled").GetBoolean().Should().BeTrue();

    // Verify abortMultipartUploadsTransition exists (NOT abortIncompleteMultipartUploadsTransition)
    rule.TryGetProperty("abortMultipartUploadsTransition", out var abortTransition).Should().BeTrue(
      "'abortMultipartUploadsTransition' property must exist");

    // Verify the nested condition structure
    var condition = abortTransition.GetProperty("condition");
    condition.GetProperty("type").GetString().Should().Be("Age", "Cloudflare expects 'Age' with capital A");
    condition.GetProperty("maxAge").GetInt32().Should().Be(604800, "7 days = 604800 seconds");

    // Verify no unexpected properties exist in the rule
    rule.TryGetProperty("conditions", out _).Should().BeFalse("null conditions should be omitted");
    rule.TryGetProperty("deleteObjectsTransition", out _).Should().BeFalse("null transitions should be omitted");
    rule.TryGetProperty("storageClassTransitions", out _).Should().BeFalse("null transitions should be omitted");
  }

  /// <summary>
  ///   Verifies the JSON output is syntactically valid and well-formed for Cloudflare.
  ///   This test logs the actual JSON for debugging purposes.
  /// </summary>
  [Fact]
  public void Serialize_BucketLifecyclePolicy_OutputsWellFormedJson()
  {
    // Arrange - Match user's failing scenario
    var lifecyclePolicy = new BucketLifecyclePolicy(
    [
      new LifecycleRule(
        Id: "abort-incomplete-multipart-uploads",
        Enabled: true,
        AbortMultipartUploadsTransition: new AbortMultipartUploadsTransition(
          LifecycleCondition.AfterDays(7)
        )
      )
    ]);

    // Act
    var json = JsonSerializer.Serialize(lifecyclePolicy, _lifecycleSerializerOptions);

    // Assert - JSON should be valid and match expected format
    var expectedPattern = """{"rules":[{"id":"abort-incomplete-multipart-uploads","enabled":true,"abortMultipartUploadsTransition":{"condition":{"type":"Age","maxAge":604800}}}]}""";
    json.Should().Be(expectedPattern, "JSON must match Cloudflare's expected format exactly");

    // Additional validation - can be parsed by standard JSON parser
    var parsed = JsonDocument.Parse(json);
    parsed.Should().NotBeNull();
  }

  #endregion


  #region Edge Case Tests

  /// <summary>Verifies that a disabled rule serializes correctly.</summary>
  [Fact]
  public void Serialize_LifecycleRule_Disabled_ProducesCorrectJson()
  {
    // Arrange
    var rule = new LifecycleRule(
      Id: "disabled-rule",
      Enabled: false,
      DeleteObjectsTransition: new DeleteObjectsTransition(LifecycleCondition.AfterDays(30))
    );

    // Act
    var json = JsonSerializer.Serialize(rule, _lifecycleSerializerOptions);

    // Assert
    using var doc = JsonDocument.Parse(json);
    doc.RootElement.GetProperty("enabled").GetBoolean().Should().BeFalse();
  }

  /// <summary>Verifies that a rule without an ID serializes correctly (ID is optional).</summary>
  [Fact]
  public void Serialize_LifecycleRule_NoId_OmitsIdProperty()
  {
    // Arrange
    var rule = new LifecycleRule(
      Enabled: true,
      DeleteObjectsTransition: new DeleteObjectsTransition(LifecycleCondition.AfterDays(30))
    );

    // Act
    var json = JsonSerializer.Serialize(rule, _lifecycleSerializerOptions);

    // Assert
    using var doc = JsonDocument.Parse(json);
    doc.RootElement.TryGetProperty("id", out _).Should().BeFalse();
    doc.RootElement.GetProperty("enabled").GetBoolean().Should().BeTrue();
  }

  /// <summary>Verifies that a condition with zero maxAge serializes correctly.</summary>
  [Fact]
  public void Serialize_LifecycleCondition_ZeroMaxAge_ProducesCorrectJson()
  {
    // Arrange - Zero days means immediate
    var condition = LifecycleCondition.AfterSeconds(0);

    // Act
    var json = JsonSerializer.Serialize(condition, _lifecycleSerializerOptions);

    // Assert
    using var doc = JsonDocument.Parse(json);
    doc.RootElement.GetProperty("maxAge").GetInt32().Should().Be(0);
  }

  /// <summary>Verifies that conditions with empty prefix serialize correctly.</summary>
  [Fact]
  public void Serialize_LifecycleRuleConditions_EmptyPrefix_ProducesCorrectJson()
  {
    // Arrange - Empty prefix means apply to all objects
    var conditions = new LifecycleRuleConditions("");

    // Act
    var json = JsonSerializer.Serialize(conditions, _lifecycleSerializerOptions);

    // Assert
    using var doc = JsonDocument.Parse(json);
    doc.RootElement.GetProperty("prefix").GetString().Should().BeEmpty();
  }

  /// <summary>Verifies that null conditions (no prefix filter) omits the conditions property.</summary>
  [Fact]
  public void Serialize_LifecycleRuleConditions_NullPrefix_OmitsPrefixProperty()
  {
    // Arrange
    var conditions = new LifecycleRuleConditions();

    // Act
    var json = JsonSerializer.Serialize(conditions, _lifecycleSerializerOptions);

    // Assert
    using var doc = JsonDocument.Parse(json);
    doc.RootElement.TryGetProperty("prefix", out _).Should().BeFalse();
  }

  /// <summary>
  ///   Verifies the exact JSON format for a 1-day abort multipart uploads rule.
  ///   This test replicates the exact scenario reported in issue where SetBucketLifecycleAsync
  ///   fails with Cloudflare error 10040 "The JSON you provided was not well formed."
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     This test validates:
  ///     <list type="bullet">
  ///       <item><description>1 day = 86400 seconds (correct conversion)</description></item>
  ///       <item><description>Enum serializes as "Age" (string, not integer 0)</description></item>
  ///       <item><description>Property names are camelCase (type, maxAge)</description></item>
  ///       <item><description>Null properties are omitted (conditions, deleteObjectsTransition, etc.)</description></item>
  ///     </list>
  ///   </para>
  /// </remarks>
  [Fact]
  public void Serialize_BucketLifecyclePolicy_OneDayAbortMultipart_ProducesCorrectJson()
  {
    // Arrange - Exact scenario from user's OrganizationS3ProvisioningService
    // MultipartUploadAbortDays = 1
    var lifecyclePolicy = new BucketLifecyclePolicy(
    [
      new LifecycleRule(
        Id: "abort-incomplete-multipart-uploads",
        Enabled: true,
        AbortMultipartUploadsTransition: new AbortMultipartUploadsTransition(
          LifecycleCondition.AfterDays(1) // 1 day = 86400 seconds
        )
      )
    ]);

    // Act - Use the exact same serialization options as SetBucketLifecycleAsync
    var json = JsonSerializer.Serialize(lifecyclePolicy, _lifecycleSerializerOptions);

    // Assert - Verify exact JSON format expected by Cloudflare R2 API
    var expectedJson = """{"rules":[{"id":"abort-incomplete-multipart-uploads","enabled":true,"abortMultipartUploadsTransition":{"condition":{"type":"Age","maxAge":86400}}}]}""";
    json.Should().Be(expectedJson, "JSON must match Cloudflare's expected format for 1-day abort rule");

    // Additional structural validation
    using var doc = JsonDocument.Parse(json);
    var root = doc.RootElement;

    // Verify rules array
    root.GetProperty("rules").GetArrayLength().Should().Be(1);

    var rule = root.GetProperty("rules")[0];

    // Verify rule properties
    rule.GetProperty("id").GetString().Should().Be("abort-incomplete-multipart-uploads");
    rule.GetProperty("enabled").GetBoolean().Should().BeTrue();

    // Verify abortMultipartUploadsTransition structure
    var abortTransition = rule.GetProperty("abortMultipartUploadsTransition");
    var condition = abortTransition.GetProperty("condition");

    // Critical: Verify enum serializes as string "Age", not integer 0
    condition.GetProperty("type").GetString().Should().Be("Age",
      "LifecycleConditionType.Age must serialize as string 'Age', not integer 0");

    // Verify maxAge is 86400 seconds (1 day)
    condition.GetProperty("maxAge").GetInt32().Should().Be(86400,
      "1 day must equal 86400 seconds");

    // Verify null properties are omitted (not serialized as null)
    rule.TryGetProperty("conditions", out _).Should().BeFalse("null conditions should be omitted");
    rule.TryGetProperty("deleteObjectsTransition", out _).Should().BeFalse("null deleteObjectsTransition should be omitted");
    rule.TryGetProperty("storageClassTransitions", out _).Should().BeFalse("null storageClassTransitions should be omitted");
  }

  /// <summary>
  ///   Diagnostic test that outputs the raw JSON for debugging purposes.
  ///   This test always passes but logs the exact JSON that would be sent to Cloudflare.
  /// </summary>
  [Fact]
  public void Serialize_BucketLifecyclePolicy_OneDayAbortMultipart_OutputsJsonForDebugging()
  {
    // Arrange - Same as user's scenario
    var lifecyclePolicy = new BucketLifecyclePolicy(
    [
      new LifecycleRule(
        Id: "abort-incomplete-multipart-uploads",
        Enabled: true,
        AbortMultipartUploadsTransition: new AbortMultipartUploadsTransition(
          LifecycleCondition.AfterDays(1)
        )
      )
    ]);

    // Act
    var json = JsonSerializer.Serialize(lifecyclePolicy, _lifecycleSerializerOptions);

    // Output for debugging (visible in test output)
    _testOutputHelper.WriteLine("=== JSON that would be sent to Cloudflare R2 API ===");
    _testOutputHelper.WriteLine(json);
    _testOutputHelper.WriteLine("=== End JSON ===");

    // Pretty-print for readability
    using var doc = JsonDocument.Parse(json);
    var prettyJson = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
    _testOutputHelper.WriteLine("\n=== Pretty-printed JSON ===");
    _testOutputHelper.WriteLine(prettyJson);
    _testOutputHelper.WriteLine("=== End Pretty JSON ===");

    // Verify it's valid JSON (test always passes if we get here)
    doc.Should().NotBeNull();
  }

  #endregion
}
