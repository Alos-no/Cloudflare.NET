// Suppress obsolete warnings for tests that validate backward-compatible deprecated methods.
// These tests ensure the old API surface continues to work correctly through the delegation layer.
#pragma warning disable CS0618

namespace Cloudflare.NET.Tests.IntegrationTests;

using System.Net;
using Accounts;
using Accounts.Models;
using Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Shared.Fixtures;
using Shared.Helpers;
using Xunit.Abstractions;

/// <summary>
///   Contains integration tests for the R2 bucket operations of <see cref="AccountsApi" />. These tests interact with the
///   live Cloudflare API and require credentials.
/// </summary>
/// <remarks>
///   This test class focuses on R2 bucket operations including:
///   <list type="bullet">
///     <item><description>R2 bucket create, list, delete operations</description></item>
///     <item><description>Custom domain management</description></item>
///     <item><description>CORS policy management</description></item>
///     <item><description>Lifecycle policy management</description></item>
///   </list>
///   For Account Management integration tests (list accounts, get account, update account),
///   see <see cref="AccountManagementApiIntegrationTests" />.
/// </remarks>
[Trait("Category", TestConstants.TestCategories.Integration)]
public class R2BucketApiIntegrationTests : IClassFixture<CloudflareApiTestFixture>, IAsyncLifetime
{
  #region Properties & Fields - Non-Public

  /// <summary>The subject under test, resolved from the test fixture.</summary>
  private readonly IAccountsApi _sut;

  /// <summary>A unique name for the R2 bucket used in this test run, to avoid collisions.</summary>
  private readonly string _bucketName = $"cfnet-test-bucket-{Guid.NewGuid():N}";

  /// <summary>The settings loaded from the test configuration.</summary>
  private readonly TestCloudflareSettings _settings;

  /// <summary>The xUnit test output helper for writing warnings.</summary>
  private readonly ITestOutputHelper _output;

  #endregion

  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="R2BucketApiIntegrationTests" /> class.</summary>
  /// <param name="fixture">The shared test fixture that provides configured API clients.</param>
  /// <param name="output">The xUnit test output helper.</param>
  public R2BucketApiIntegrationTests(CloudflareApiTestFixture fixture, ITestOutputHelper output)
  {
    // The SUT is resolved via the fixture's pre-configured DI container.
    _sut      = fixture.AccountsApi;
    _settings = TestConfiguration.CloudflareSettings;
    _output   = output;

    // Wire up the logger provider to the current test's output.
    var loggerProvider = fixture.ServiceProvider.GetRequiredService<XunitTestOutputLoggerProvider>();
    loggerProvider.Current = output;
  }

  #endregion

  #region Methods Impl

  /// <summary>Asynchronously creates the R2 bucket required for the tests. This runs once before any tests in this class.</summary>
  public async Task InitializeAsync()
  {
    // Create a new R2 bucket for the test run.
    await _sut.CreateR2BucketAsync(_bucketName);
  }

  /// <summary>Asynchronously deletes the R2 bucket after all tests in this class have run.</summary>
  public async Task DisposeAsync()
  {
    // Clean up the R2 bucket.
    // A retry loop is helpful here as some resources (like custom domains)
    // may take time to fully detach, causing a temporary conflict when deleting the bucket.
    var       attempts    = 0;
    const int maxAttempts = 3;

    while (true)
      try
      {
        await _sut.DeleteR2BucketAsync(_bucketName);
        return; // Success
      }
      catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
      {
        attempts++;
        if (attempts >= maxAttempts)
          // Log or handle the final failure if necessary
          throw; // Rethrow after final attempt

        // Wait before retrying
        await Task.Delay(TimeSpan.FromSeconds(5));
      }
  }

  #endregion

  #region Methods

  /// <summary>Verifies that the r2.dev URL for a bucket can be disabled.</summary>
  [IntegrationTest]
  public async Task CanDisableDevUrl()
  {
    // Arrange & Act
    var action = async () => await _sut.DisableDevUrlAsync(_bucketName);

    // Assert
    await action.Should().NotThrowAsync();
  }

  /// <summary>Tests the full lifecycle of a custom domain: attach, get status, and detach.</summary>
  [IntegrationTest]
  public async Task CanManageCustomDomainLifecycle()
  {
    // Arrange
    // A unique hostname for this test run. The BaseDomain is now inferred by the fixture.
    var hostname = $"cfnet-test-{Guid.NewGuid():N}.{_settings.BaseDomain}";
    var zoneId   = _settings.ZoneId;

    try
    {
      // Act & Assert

      // 1. Attach Custom Domain
      var attachResult = await _sut.AttachCustomDomainAsync(_bucketName, hostname, zoneId);
      attachResult.Should().NotBeNull();
      attachResult.Domain.Should().Be(hostname);

      // 2. Get Custom Domain Status
      var statusResult = await _sut.GetCustomDomainStatusAsync(_bucketName, hostname);
      statusResult.Should().NotBeNull();
      statusResult.Status.Should()
                  .BeOneOf("pending", "active"); // Status depends on timing

      // 3. Update Custom Domain (set minimum TLS version)
      var updateRequest = new UpdateCustomDomainRequest(Enabled: true, MinTls: "1.2");
      var updateResult = await _sut.Buckets.UpdateCustomDomainAsync(_bucketName, hostname, updateRequest);
      updateResult.Should().NotBeNull();
      updateResult.Domain.Should().Be(hostname);
    }
    finally
    {
      // 4. Detach Custom Domain (Cleanup)
      var detachAction = async () => await _sut.DetachCustomDomainAsync(_bucketName, hostname);
      await detachAction.Should().NotThrowAsync("the custom domain should be cleaned up successfully");
    }
  }

  /// <summary>Verifies that R2 buckets can be listed successfully.</summary>
  [IntegrationTest]
  public async Task ListR2BucketsAsync_CanListSuccessfully()
  {
    // Arrange (bucket is created in InitializeAsync)

    // Act: Find the specific bucket we created by iterating through all buckets.
    R2Bucket? testBucket = null;

    await foreach (var bucket in _sut.ListAllR2BucketsAsync())
      if (bucket.Name == _bucketName)
      {
        testBucket = bucket;

        break;
      }

    // Assert
    testBucket.Should().NotBeNull("the bucket created in InitializeAsync should be found in the list");
    testBucket!.Name.Should().Be(_bucketName);
    // The List operation returns a summary object which does not include location details.
    // These properties may be null or have empty values depending on bucket configuration.
    testBucket.Location?.Value.Should().BeNullOrWhiteSpace();
    testBucket.Jurisdiction?.Value.Should().BeNullOrWhiteSpace();
    testBucket.StorageClass?.Value.Should().BeNullOrWhiteSpace();
    // Assert that the creation date is recent, within a tolerance, to account for test execution time.
    testBucket.CreationDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));
  }

  /// <summary>Verifies that the async stream can iterate through all created buckets.</summary>
  [IntegrationTest]
  public async Task ListAllR2BucketsAsync_CanIterateThroughAllBuckets()
  {
    // Arrange
    var bucketName2 = $"cfnet-test-bucket-{Guid.NewGuid():N}";
    await _sut.CreateR2BucketAsync(bucketName2);
    var createdBucketNames = new[] { _bucketName, bucketName2 };

    try
    {
      // Act
      var allBuckets = new List<R2Bucket>();
      await foreach (var bucket in _sut.ListAllR2BucketsAsync())
        allBuckets.Add(bucket);

      // Assert
      allBuckets.Should().NotBeEmpty();
      var allBucketNames = allBuckets.Select(b => b.Name).ToList();
      allBucketNames.Should().Contain(createdBucketNames);
    }
    finally
    {
      // Cleanup
      await _sut.DeleteR2BucketAsync(bucketName2);
    }
  }

  /// <summary>Verifies that an R2 bucket can be created and then deleted successfully.</summary>
  [IntegrationTest]
  public async Task CanCreateAndDeleteR2Bucket()
  {
    // Arrange
    var bucketName = $"cfnet-standalone-bucket-{Guid.NewGuid():N}";

    try
    {
      // Act
      var createResult = await _sut.CreateR2BucketAsync(bucketName);

      // Assert
      createResult.Should().NotBeNull();
      createResult.Name.Should().Be(bucketName);
    }
    finally
    {
      // Cleanup
      var deleteAction = async () => await _sut.DeleteR2BucketAsync(bucketName);
      await deleteAction.Should().NotThrowAsync("the bucket should be cleaned up successfully");
    }
  }

  /// <summary>Verifies that an R2 bucket can be created with a location hint and the hint is reflected in the response.</summary>
  [IntegrationTest]
  public async Task CanCreateR2BucketWithLocationHint()
  {
    // Arrange
    var bucketName   = $"cfnet-location-bucket-{Guid.NewGuid():N}";
    var locationHint = R2LocationHint.EastNorthAmerica;

    try
    {
      // Act
      var createResult = await _sut.CreateR2BucketAsync(
        bucketName,
        locationHint: locationHint
      );

      // Assert - Verify the response contains the bucket details
      createResult.Should().NotBeNull();
      createResult.Name.Should().Be(bucketName);
      createResult.CreationDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));

      // The location hint should be reflected in the response.
      // Note: Cloudflare may honor the hint or place data in a nearby region.
      // The response should show the actual location assigned.
      createResult.Location.Should().NotBeNull(
        "the API response should include the location when a location hint was provided");
      createResult.Location!.Value.Value.Should().NotBeNullOrEmpty(
        "the location value should not be empty");
    }
    finally
    {
      // Cleanup
      var deleteAction = async () => await _sut.DeleteR2BucketAsync(bucketName);
      await deleteAction.Should().NotThrowAsync("the bucket should be cleaned up successfully");
    }
  }

  /// <summary>Verifies that an R2 bucket can be created with a storage class and it is reflected in the response.</summary>
  [IntegrationTest]
  public async Task CanCreateR2BucketWithStorageClass()
  {
    // Arrange
    var bucketName   = $"cfnet-storage-bucket-{Guid.NewGuid():N}";
    var storageClass = R2StorageClass.Standard;

    try
    {
      // Act
      var createResult = await _sut.CreateR2BucketAsync(
        bucketName,
        locationHint: R2LocationHint.WestEurope,
        storageClass: storageClass
      );

      // Assert - Verify the storage class is reflected in the response
      createResult.Should().NotBeNull();
      createResult.Name.Should().Be(bucketName);
      createResult.CreationDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));

      // Verify the storage class was applied
      createResult.StorageClass.Should().NotBeNull(
        "the API response should include the storage class when specified");
      createResult.StorageClass!.Value.Should().Be(storageClass,
        "the storage class should match what was requested");

      // Verify location was also applied
      createResult.Location.Should().NotBeNull(
        "the API response should include the location when a location hint was provided");
    }
    finally
    {
      // Cleanup
      var deleteAction = async () => await _sut.DeleteR2BucketAsync(bucketName);
      await deleteAction.Should().NotThrowAsync("the bucket should be cleaned up successfully");
    }
  }

  /// <summary>Verifies that an R2 bucket can be created with Infrequent Access storage class.</summary>
  [IntegrationTest]
  public async Task CanCreateR2BucketWithInfrequentAccessStorageClass()
  {
    // Arrange
    var bucketName   = $"cfnet-ia-bucket-{Guid.NewGuid():N}";
    var storageClass = R2StorageClass.InfrequentAccess;

    try
    {
      // Act
      var createResult = await _sut.CreateR2BucketAsync(
        bucketName,
        locationHint: R2LocationHint.AsiaPacific,
        storageClass: storageClass
      );

      // Assert - Verify the Infrequent Access storage class is applied
      createResult.Should().NotBeNull();
      createResult.Name.Should().Be(bucketName);
      createResult.StorageClass.Should().NotBeNull(
        "the API response should include the storage class");
      createResult.StorageClass!.Value.Should().Be(R2StorageClass.InfrequentAccess,
        "the storage class should be InfrequentAccess as requested");
    }
    finally
    {
      // Cleanup
      var deleteAction = async () => await _sut.DeleteR2BucketAsync(bucketName);
      await deleteAction.Should().NotThrowAsync("the bucket should be cleaned up successfully");
    }
  }

  /// <summary>Verifies that an R2 bucket can be created with all optional parameters and they are properly applied.</summary>
  /// <remarks>
  ///   Note: Jurisdiction (e.g., <see cref="R2Jurisdiction.EuropeanUnion" />) requires specific account configuration
  ///   and may not be available on all accounts. This test uses a location hint and storage class only.
  ///   To test jurisdiction, ensure your Cloudflare account has the appropriate jurisdiction enabled.
  /// </remarks>
  [IntegrationTest]
  public async Task CanCreateR2BucketWithAllOptions()
  {
    // Arrange
    var bucketName   = $"cfnet-allopts-bucket-{Guid.NewGuid():N}";
    var locationHint = R2LocationHint.WestEurope;
    var storageClass = R2StorageClass.InfrequentAccess;

    try
    {
      // Act - Create bucket with location hint and storage class
      // Jurisdiction is not tested here as it requires specific account configuration
      var createResult = await _sut.CreateR2BucketAsync(
        bucketName,
        locationHint: locationHint,
        jurisdiction: null, // Set to R2Jurisdiction.EuropeanUnion if your account supports it
        storageClass: storageClass
      );

      // Assert - Verify all parameters were applied
      createResult.Should().NotBeNull();
      createResult.Name.Should().Be(bucketName);
      createResult.CreationDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));

      // Verify location was applied (may be the hint or a nearby region)
      createResult.Location.Should().NotBeNull(
        "the API response should include the location when a location hint was provided");
      createResult.Location!.Value.Value.Should().NotBeNullOrEmpty(
        "the location value should not be empty");

      // Verify storage class was applied
      createResult.StorageClass.Should().NotBeNull(
        "the API response should include the storage class when specified");
      createResult.StorageClass!.Value.Should().Be(storageClass,
        "the storage class should match what was requested");

      // Jurisdiction should be null since we didn't specify one
      // (or would need EU-enabled account to test)
    }
    finally
    {
      // Cleanup
      var deleteAction = async () => await _sut.DeleteR2BucketAsync(bucketName);
      await deleteAction.Should().NotThrowAsync("the bucket should be cleaned up successfully");
    }
  }

  /// <summary>Tests the full lifecycle of CORS configuration: set, get, update, and delete.</summary>
  [IntegrationTest]
  public async Task CanManageCorsLifecycle()
  {
    // Arrange
    var corsPolicy = new BucketCorsPolicy(
      new[]
      {
        new CorsRule(
          new CorsAllowed(
            new[] { "GET", "PUT", "POST" },
            new[] { "https://example.com", "https://app.example.com" },
            new[] { "Content-Type", "Authorization" }
          ),
          "Integration Test Rule",
          new[] { "ETag", "Content-Length" },
          3600
        )
      }
    );

    try
    {
      // Act & Assert

      // 1. Set CORS Policy
      var setAction = async () => await _sut.SetBucketCorsAsync(_bucketName, corsPolicy);
      await setAction.Should().NotThrowAsync("setting CORS policy should succeed");

      // 2. Get CORS Policy and verify it matches
      var retrievedPolicy = await _sut.GetBucketCorsAsync(_bucketName);
      retrievedPolicy.Should().NotBeNull();
      retrievedPolicy.Rules.Should().HaveCount(1);

      var retrievedRule = retrievedPolicy.Rules[0];
      retrievedRule.Id.Should().Be("Integration Test Rule");
      retrievedRule.Allowed.Methods.Should().BeEquivalentTo(new[] { "GET", "PUT", "POST" });
      retrievedRule.Allowed.Origins.Should().BeEquivalentTo(new[] { "https://example.com", "https://app.example.com" });
      retrievedRule.Allowed.Headers.Should().BeEquivalentTo(new[] { "Content-Type", "Authorization" });
      retrievedRule.ExposeHeaders.Should().BeEquivalentTo(new[] { "ETag", "Content-Length" });
      retrievedRule.MaxAgeSeconds.Should().Be(3600);

      // 3. Update CORS Policy with a different configuration
      var updatedPolicy = new BucketCorsPolicy(
        new[]
        {
          new CorsRule(
            new CorsAllowed(
              new[] { "GET" },
              new[] { "*" },
              null
            ),
            "Updated Rule",
            MaxAgeSeconds: 86400
          )
        }
      );

      await _sut.SetBucketCorsAsync(_bucketName, updatedPolicy);

      // 4. Verify the update
      var verifyUpdatedPolicy = await _sut.GetBucketCorsAsync(_bucketName);
      verifyUpdatedPolicy.Rules.Should().HaveCount(1);
      verifyUpdatedPolicy.Rules[0].Id.Should().Be("Updated Rule");
      verifyUpdatedPolicy.Rules[0].Allowed.Methods.Should().BeEquivalentTo(new[] { "GET" });
      verifyUpdatedPolicy.Rules[0].Allowed.Origins.Should().BeEquivalentTo(new[] { "*" });
      verifyUpdatedPolicy.Rules[0].MaxAgeSeconds.Should().Be(86400);
    }
    finally
    {
      // 5. Delete CORS Policy (Cleanup)
      var deleteAction = async () => await _sut.DeleteBucketCorsAsync(_bucketName);
      await deleteAction.Should().NotThrowAsync("the CORS policy should be cleaned up successfully");
    }
  }

  /// <summary>Verifies that getting CORS from a bucket with no CORS policy throws a 404 HttpRequestException.</summary>
  [IntegrationTest]
  public async Task GetBucketCorsAsync_ThrowsWhenNoPolicyExists()
  {
    // Arrange - Create a fresh bucket with no CORS policy
    var bucketName = $"cfnet-nocors-bucket-{Guid.NewGuid():N}";
    await _sut.CreateR2BucketAsync(bucketName);

    try
    {
      // Act
      var action = async () => await _sut.GetBucketCorsAsync(bucketName);

      // Assert - When no CORS is configured, the API returns 404 with error code 10059
      var exception = await action.Should().ThrowAsync<HttpRequestException>();
      exception.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    finally
    {
      // Cleanup
      await _sut.DeleteR2BucketAsync(bucketName);
    }
  }

  /// <summary>Tests the full lifecycle of object lifecycle rules: set, get, update, and delete.</summary>
  /// <remarks>
  ///   This test verifies all three lifecycle rule types: - DeleteObjectsTransition: Delete objects after a certain
  ///   age - AbortMultipartUploadsTransition: Abort incomplete multipart uploads after a certain age -
  ///   StorageClassTransitions: Transition objects to Infrequent Access storage class
  /// </remarks>
  [IntegrationTest]
  public async Task CanManageLifecycleRulesLifecycle()
  {
    // Arrange - Create a comprehensive lifecycle policy with all three rule types
    var lifecyclePolicy = new BucketLifecyclePolicy(
      new[]
      {
        // Rule to delete old logs after 90 days
        new LifecycleRule(
          "Delete old logs",
          true,
          new LifecycleRuleConditions("logs/"),
          DeleteObjectsTransition: new DeleteObjectsTransition(LifecycleCondition.AfterDays(90))
        ),
        // Rule to abort incomplete multipart uploads after 7 days
        new LifecycleRule(
          "Cleanup multipart uploads",
          true,
          new LifecycleRuleConditions(),
          new AbortMultipartUploadsTransition(LifecycleCondition.AfterDays(7))
        ),
        // Rule to transition to Infrequent Access after 30 days
        new LifecycleRule(
          "Archive old data",
          true,
          new LifecycleRuleConditions("archive/"),
          StorageClassTransitions: new[]
          {
            new StorageClassTransition(LifecycleCondition.AfterDays(30), R2StorageClass.InfrequentAccess)
          }
        )
      }
    );

    try
    {
      // Act & Assert

      // 1. Set Lifecycle Policy
      var setAction = async () => await _sut.SetBucketLifecycleAsync(_bucketName, lifecyclePolicy);
      await setAction.Should().NotThrowAsync("setting lifecycle policy should succeed");

      // 2. Get Lifecycle Policy and verify it matches
      var retrievedPolicy = await _sut.GetBucketLifecycleAsync(_bucketName);
      retrievedPolicy.Should().NotBeNull();
      retrievedPolicy.Rules.Should().HaveCount(3);

      // Verify the delete objects rule
      var deleteRule = retrievedPolicy.Rules.FirstOrDefault(r => r.Id == "Delete old logs");
      deleteRule.Should().NotBeNull("the delete logs rule should exist");
      deleteRule!.Enabled.Should().BeTrue();
      deleteRule.Conditions?.Prefix.Should().Be("logs/");
      deleteRule.DeleteObjectsTransition.Should().NotBeNull();
      deleteRule.DeleteObjectsTransition!.Condition.Type.Should().Be(LifecycleConditionType.Age);
      deleteRule.DeleteObjectsTransition.Condition.MaxAge.Should().Be(90 * 86400); // 90 days in seconds

      // Verify the abort multipart uploads rule
      var abortRule = retrievedPolicy.Rules.FirstOrDefault(r => r.Id == "Cleanup multipart uploads");
      abortRule.Should().NotBeNull("the abort multipart rule should exist");
      abortRule!.Enabled.Should().BeTrue();
      abortRule.AbortMultipartUploadsTransition.Should().NotBeNull();
      abortRule.AbortMultipartUploadsTransition!.Condition.Type.Should().Be(LifecycleConditionType.Age);
      abortRule.AbortMultipartUploadsTransition.Condition.MaxAge.Should().Be(7 * 86400); // 7 days in seconds

      // Verify the storage class transition rule
      var archiveRule = retrievedPolicy.Rules.FirstOrDefault(r => r.Id == "Archive old data");
      archiveRule.Should().NotBeNull("the archive rule should exist");
      archiveRule!.Enabled.Should().BeTrue();
      archiveRule.Conditions?.Prefix.Should().Be("archive/");
      archiveRule.StorageClassTransitions.Should().HaveCount(1);
      archiveRule.StorageClassTransitions![0].StorageClass.Should().Be(R2StorageClass.InfrequentAccess);
      archiveRule.StorageClassTransitions[0].Condition.Type.Should().Be(LifecycleConditionType.Age);
      archiveRule.StorageClassTransitions[0].Condition.MaxAge.Should().Be(30 * 86400); // 30 days in seconds

      // 3. Update Lifecycle Policy with a different configuration
      var updatedPolicy = new BucketLifecyclePolicy(
        new[]
        {
          new LifecycleRule(
            "Delete old logs - updated",
            true,
            new LifecycleRuleConditions("logs/v2/"),
            DeleteObjectsTransition: new DeleteObjectsTransition(LifecycleCondition.AfterDays(365))
          )
        }
      );

      await _sut.SetBucketLifecycleAsync(_bucketName, updatedPolicy);

      // 4. Verify the update replaced all rules
      var verifyUpdatedPolicy = await _sut.GetBucketLifecycleAsync(_bucketName);
      verifyUpdatedPolicy.Rules.Should().HaveCount(1);
      verifyUpdatedPolicy.Rules[0].Id.Should().Be("Delete old logs - updated");
      verifyUpdatedPolicy.Rules[0].Conditions?.Prefix.Should().Be("logs/v2/");
      verifyUpdatedPolicy.Rules[0].DeleteObjectsTransition!.Condition.MaxAge.Should().Be(365 * 86400); // 365 days in seconds
    }
    finally
    {
      // 5. Delete Lifecycle Policy (Cleanup)
      var deleteAction = async () => await _sut.DeleteBucketLifecycleAsync(_bucketName);
      await deleteAction.Should().NotThrowAsync("the lifecycle policy should be cleaned up successfully");
    }
  }

  /// <summary>
  ///   Tests the exact scenario reported in GitHub issue: 1-day abort multipart uploads with hyphenated ID.
  ///   This replicates the OrganizationS3ProvisioningService.ConfigureBucketLifecycleAsync pattern.
  /// </summary>
  /// <remarks>
  ///   This test validates that the Cloudflare API accepts:
  ///   <list type="bullet">
  ///     <item><description>Hyphenated rule IDs (e.g., "abort-incomplete-multipart-uploads")</description></item>
  ///     <item><description>1-day (86400 seconds) abort multipart uploads transition</description></item>
  ///     <item><description>Minimal rule with only abortMultipartUploadsTransition (no conditions, no other transitions)</description></item>
  ///   </list>
  /// </remarks>
  [IntegrationTest]
  public async Task SetBucketLifecycleAsync_OneDayAbortMultipartWithHyphenatedId_Succeeds()
  {
    // Arrange - Test with NULL conditions (the SDK should normalize this to empty conditions)
    // This replicates the exact user scenario where Conditions is not specified
    var lifecyclePolicy = new BucketLifecyclePolicy(
    [
      new LifecycleRule(
        Id: "abort-incomplete-multipart-uploads", // Hyphenated ID as used in production
        Enabled: true,
        // Conditions intentionally omitted (null) - SDK should normalize to empty object
        AbortMultipartUploadsTransition: new AbortMultipartUploadsTransition(
          LifecycleCondition.AfterDays(1) // 1 day = 86400 seconds
        )
      )
    ]);

    try
    {
      // Act - This should succeed
      var setAction = async () => await _sut.SetBucketLifecycleAsync(_bucketName, lifecyclePolicy);
      await setAction.Should().NotThrowAsync("setting 1-day abort multipart lifecycle with hyphenated ID should succeed");

      // Verify the policy was set correctly
      var retrievedPolicy = await _sut.GetBucketLifecycleAsync(_bucketName);
      retrievedPolicy.Should().NotBeNull();
      retrievedPolicy.Rules.Should().HaveCount(1);

      var rule = retrievedPolicy.Rules[0];
      rule.Id.Should().Be("abort-incomplete-multipart-uploads");
      rule.Enabled.Should().BeTrue();
      rule.AbortMultipartUploadsTransition.Should().NotBeNull();
      rule.AbortMultipartUploadsTransition!.Condition.Type.Should().Be(LifecycleConditionType.Age);
      rule.AbortMultipartUploadsTransition.Condition.MaxAge.Should().Be(86400); // 1 day in seconds
    }
    finally
    {
      // Cleanup - Reset to empty lifecycle
      await _sut.DeleteBucketLifecycleAsync(_bucketName);
    }
  }

  /// <summary>
  ///   Verifies that CORS can be set with minimal CorsRule (null Headers, null ExposeHeaders, null MaxAgeSeconds).
  ///   This tests that nullable properties in CorsRule are handled correctly by the API.
  /// </summary>
  [IntegrationTest]
  public async Task SetBucketCorsAsync_MinimalCorsRule_Succeeds()
  {
    // Arrange - CORS rule with only required properties (Allowed), all optional properties null
    var corsPolicy = new BucketCorsPolicy([
      new CorsRule(
        Allowed: new CorsAllowed(
          Methods: ["GET"],
          Origins: ["*"]
          // Headers is null - testing nullable property omission
        )
        // Id, ExposeHeaders, MaxAgeSeconds are all null
      )
    ]);

    try
    {
      // Act - This should succeed even with minimal properties
      var setAction = async () => await _sut.SetBucketCorsAsync(_bucketName, corsPolicy);
      await setAction.Should().NotThrowAsync("setting CORS with minimal properties should succeed");

      // Verify the policy was set
      var retrievedPolicy = await _sut.GetBucketCorsAsync(_bucketName);
      retrievedPolicy.Should().NotBeNull();
      retrievedPolicy.Rules.Should().HaveCount(1);
      retrievedPolicy.Rules[0].Allowed.Methods.Should().BeEquivalentTo(["GET"]);
      retrievedPolicy.Rules[0].Allowed.Origins.Should().BeEquivalentTo(["*"]);
    }
    finally
    {
      // Cleanup
      await _sut.DeleteBucketCorsAsync(_bucketName);
    }
  }

  /// <summary>
  ///   Verifies that an R2 bucket can be created with no optional parameters (null LocationHint, null StorageClass).
  ///   This tests that nullable properties in CreateBucketRequest are handled correctly.
  /// </summary>
  [IntegrationTest]
  public async Task CreateR2BucketAsync_MinimalParameters_Succeeds()
  {
    // Arrange - Create bucket with only the required name
    var bucketName = $"cfnet-minimal-bucket-{Guid.NewGuid():N}";

    try
    {
      // Act - This should succeed with all optional parameters null
      var result = await _sut.CreateR2BucketAsync(
        bucketName
        // locationHint: null (default)
        // jurisdiction: null (default)
        // storageClass: null (default)
      );

      // Assert
      result.Should().NotBeNull();
      result.Name.Should().Be(bucketName);
      result.CreationDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));
    }
    finally
    {
      // Cleanup
      await _sut.DeleteR2BucketAsync(bucketName);
    }
  }

  /// <summary>Verifies that getting lifecycle from a new bucket returns the default lifecycle policy.</summary>
  /// <remarks>
  ///   <para>
  ///     R2 automatically creates a "Default Multipart Abort Rule" for new buckets that aborts incomplete multipart
  ///     uploads after 7 days (604800 seconds).
  ///   </para>
  ///   <para>
  ///     <b>Note:</b> If this test fails with 5xx errors, it indicates a transient Cloudflare API issue.
  ///     Per testing guidelines, 5xx errors should cause test failure (not be silently ignored) to reveal real issues.
  ///   </para>
  /// </remarks>
  [IntegrationTest]
  public async Task GetBucketLifecycleAsync_ReturnsDefaultPolicyForNewBucket()
  {
    // Arrange - Create a fresh bucket
    var bucketName = $"cfnet-nolifecycle-bucket-{Guid.NewGuid():N}";
    await _sut.CreateR2BucketAsync(bucketName);

    try
    {
      // Act
      var result = await _sut.GetBucketLifecycleAsync(bucketName);

      // Assert - R2 automatically creates a default multipart abort rule for new buckets
      result.Should().NotBeNull();
      result.Rules.Should().HaveCount(1);

      var defaultRule = result.Rules[0];
      defaultRule.Id.Should().Be("Default Multipart Abort Rule");
      defaultRule.Enabled.Should().BeTrue();
      defaultRule.AbortMultipartUploadsTransition.Should().NotBeNull();
      defaultRule.AbortMultipartUploadsTransition!.Condition.Type.Should().Be(LifecycleConditionType.Age);
      defaultRule.AbortMultipartUploadsTransition.Condition.MaxAge.Should().Be(7 * 86400); // 7 days in seconds
    }
    finally
    {
      // Cleanup (best effort - ignore errors during cleanup only)
      try
      {
        await _sut.DeleteR2BucketAsync(bucketName);
      }
      catch (HttpRequestException)
      {
        // Cleanup may fail - that's OK for cleanup only
      }
    }
  }

  /// <summary>Verifies that GetAsync retrieves bucket properties for an existing bucket.</summary>
  [IntegrationTest]
  public async Task GetAsync_ReturnsBucketProperties()
  {
    // Arrange - The bucket is created in InitializeAsync

    // Act
    var result = await _sut.Buckets.GetAsync(_bucketName);

    // Assert
    result.Should().NotBeNull();
    result.Name.Should().Be(_bucketName);
    result.CreationDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(10));
  }

  /// <summary>Verifies that GetAsync throws 404 for a non-existent bucket.</summary>
  [IntegrationTest]
  public async Task GetAsync_NonExistentBucket_ThrowsNotFound()
  {
    // Arrange
    var nonExistentBucket = $"cfnet-nonexistent-{Guid.NewGuid():N}";

    // Act
    var action = async () => await _sut.Buckets.GetAsync(nonExistentBucket);

    // Assert
    var exception = await action.Should().ThrowAsync<HttpRequestException>();
    exception.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
  }

  /// <summary>Verifies that ListCustomDomainsAsync returns the list of custom domains for a bucket.</summary>
  [IntegrationTest]
  public async Task ListCustomDomainsAsync_ReturnsCustomDomains()
  {
    // Arrange - Attach a custom domain first
    var hostname = $"cfnet-list-{Guid.NewGuid():N}.{_settings.BaseDomain}";
    var zoneId   = _settings.ZoneId;

    try
    {
      await _sut.AttachCustomDomainAsync(_bucketName, hostname, zoneId);

      // Act - List custom domains
      var domains = await _sut.Buckets.ListCustomDomainsAsync(_bucketName);

      // Assert
      domains.Should().NotBeNull();
      domains.Should().ContainSingle(d => d.Domain == hostname);
    }
    finally
    {
      // Cleanup
      await _sut.DetachCustomDomainAsync(_bucketName, hostname);
    }
  }

  /// <summary>Verifies that ListCustomDomainsAsync returns an empty list when no custom domains are attached.</summary>
  [IntegrationTest]
  public async Task ListCustomDomainsAsync_NoCustomDomains_ReturnsEmptyList()
  {
    // Arrange - Create a fresh bucket with no custom domains
    var bucketName = $"cfnet-nodomain-bucket-{Guid.NewGuid():N}";
    await _sut.CreateR2BucketAsync(bucketName);

    try
    {
      // Act
      var domains = await _sut.Buckets.ListCustomDomainsAsync(bucketName);

      // Assert
      domains.Should().NotBeNull();
      domains.Should().BeEmpty();
    }
    finally
    {
      // Cleanup
      await _sut.DeleteR2BucketAsync(bucketName);
    }
  }

  /// <summary>Tests the full lifecycle of managed domain (r2.dev): get status, enable, and disable.</summary>
  [IntegrationTest]
  public async Task CanManageManagedDomainLifecycle()
  {
    // Arrange - Bucket is created in InitializeAsync

    try
    {
      // Act & Assert

      // 1. Get initial managed domain status
      var initialStatus = await _sut.Buckets.GetManagedDomainAsync(_bucketName);
      initialStatus.Should().NotBeNull();
      initialStatus.BucketId.Should().NotBeNullOrEmpty();

      // 2. Enable the managed domain
      var enableResult = await _sut.Buckets.EnableManagedDomainAsync(_bucketName);
      enableResult.Should().NotBeNull();
      enableResult.Enabled.Should().BeTrue();
      enableResult.Domain.Should().NotBeNullOrEmpty();
      enableResult.Domain.Should().EndWith(".r2.dev");
    }
    finally
    {
      // 3. Disable managed domain (Cleanup)
      var disableAction = async () => await _sut.Buckets.DisableManagedDomainAsync(_bucketName);
      await disableAction.Should().NotThrowAsync("the managed domain should be disabled successfully");
    }
  }

  /// <summary>Tests the full lifecycle of bucket lock: get, set, and delete lock rules.</summary>
  [IntegrationTest]
  public async Task CanManageBucketLockLifecycle()
  {
    // Arrange - Create a lock policy with age-based retention
    var lockPolicy = new BucketLockPolicy(
      new[]
      {
        new BucketLockRule(
          "integration-test-lock-rule",
          true,
          "protected/",
          BucketLockCondition.ForDays(1) // Minimal lock for testing
        )
      }
    );

    try
    {
      // Act & Assert

      // 1. Get initial lock status (should be empty or no rules)
      var initialLock = await _sut.Buckets.GetLockAsync(_bucketName);
      initialLock.Should().NotBeNull();

      // 2. Set lock rules
      var setResult = await _sut.Buckets.SetLockAsync(_bucketName, lockPolicy);
      setResult.Should().NotBeNull();
      setResult.Rules.Should().HaveCount(1);
      setResult.Rules[0].Id.Should().Be("integration-test-lock-rule");
      setResult.Rules[0].Enabled.Should().BeTrue();
      // Note: Cloudflare API does not echo back the Prefix field in responses

      // 3. Get lock status to verify it was applied
      var retrievedLock = await _sut.Buckets.GetLockAsync(_bucketName);
      retrievedLock.Should().NotBeNull();
      retrievedLock.Rules.Should().HaveCount(1);
      retrievedLock.Rules[0].Id.Should().Be("integration-test-lock-rule");
    }
    finally
    {
      // 4. Delete lock rules (Cleanup)
      var deleteAction = async () => await _sut.Buckets.DeleteLockAsync(_bucketName);
      await deleteAction.Should().NotThrowAsync("the lock policy should be removed successfully");

      // Verify it was deleted
      var verifyDeleted = await _sut.Buckets.GetLockAsync(_bucketName);
      verifyDeleted.Rules.Should().BeEmpty("the lock rules should have been removed");
    }
  }

  /// <summary>Verifies that GetSippyAsync returns the Sippy configuration status for a bucket.</summary>
  /// <remarks>
  ///   <para>
  ///     Sippy is an incremental migration service. When not configured, the enabled property is false.
  ///     This test does NOT configure Sippy as that would require external AWS/GCS credentials.
  ///   </para>
  /// </remarks>
  [IntegrationTest]
  public async Task GetSippyAsync_ReturnsDisabledStatusForNewBucket()
  {
    // Arrange - Create a fresh bucket with no Sippy configuration
    var bucketName = $"cfnet-nosippy-bucket-{Guid.NewGuid():N}";
    await _sut.CreateR2BucketAsync(bucketName);

    try
    {
      // Act
      var result = await _sut.Buckets.GetSippyAsync(bucketName);

      // Assert
      result.Should().NotBeNull();
      result.Enabled.Should().BeFalse("Sippy is not configured on new buckets");
      result.Source.Should().BeNull("no source is configured");
      result.Destination.Should().BeNull("no destination is configured");
    }
    finally
    {
      // Cleanup
      await _sut.DeleteR2BucketAsync(bucketName);
    }
  }

  /// <summary>Tests enabling and disabling Sippy incremental migration from AWS S3.</summary>
  /// <remarks>
  ///   <para><b>Documentation Evidence:</b></para>
  ///   <list type="bullet">
  ///     <item>Sippy requires valid AWS S3 credentials to configure: https://developers.cloudflare.com/r2/data-migration/sippy/</item>
  ///     <item>Without valid source credentials, the API will return an error when enabling Sippy</item>
  ///   </list>
  /// </remarks>
  [Fact(Skip = "Requires valid AWS S3 credentials (access key and secret) for source bucket - not available in test environment")]
  public async Task EnableSippyAsync_WithAwsSource_ConfiguresMigration()
  {
    // This test would enable Sippy migration from an AWS S3 bucket if credentials were available.
    // The implementation is complete but skipped due to the external dependency requirement.

    // Arrange
    var awsSource = SippyAwsSource.Create(
      bucket: "source-bucket-name",
      region: "us-east-1",
      accessKeyId: "AWS_ACCESS_KEY_ID",
      secretAccessKey: "AWS_SECRET_ACCESS_KEY"
    );
    var request = new EnableSippyFromAwsRequest(awsSource);

    // Act
    var result = await _sut.Buckets.EnableSippyAsync(_bucketName, request);

    // Assert
    result.Should().NotBeNull();
    result.Enabled.Should().BeTrue();
    result.Source.Should().NotBeNull();
    result.Source!.Provider.Should().Be(SippyProvider.Aws);

    // Cleanup
    await _sut.Buckets.DisableSippyAsync(_bucketName);
  }

  /// <summary>Tests creating temporary R2 access credentials.</summary>
  /// <remarks>
  ///   <para><b>Documentation Evidence:</b></para>
  ///   <list type="bullet">
  ///     <item>Creating temp credentials requires a parent R2 Access Key ID: https://developers.cloudflare.com/api/resources/r2/subresources/temporary_credentials/</item>
  ///     <item>R2 Access Keys are created in the Cloudflare dashboard under R2 > Manage R2 API Tokens</item>
  ///     <item>The test environment does not have an R2 access key configured</item>
  ///   </list>
  /// </remarks>
  [Fact(Skip = "Requires R2 Access Key ID (parentAccessKeyId) which is not available in test environment - R2 API keys are separate from API tokens")]
  public async Task CreateTempCredentialsAsync_ReturnsTemporaryCredentials()
  {
    // This test would create temporary R2 credentials if an R2 Access Key was available.
    // The implementation is complete but skipped due to the R2 API key requirement.

    // Arrange
    var request = new CreateTempCredentialsRequest(
      Bucket: _bucketName,
      ParentAccessKeyId: "R2_ACCESS_KEY_ID", // Requires actual R2 access key
      Permission: TempCredentialPermission.ObjectReadWrite,
      TtlSeconds: 3600
    );

    // Act
    var result = await _sut.Buckets.CreateTempCredentialsAsync(request);

    // Assert
    result.Should().NotBeNull();
    result.AccessKeyId.Should().NotBeNullOrEmpty();
    result.SecretAccessKey.Should().NotBeNullOrEmpty();
    result.SessionToken.Should().NotBeNullOrEmpty();
  }

  #endregion
}
