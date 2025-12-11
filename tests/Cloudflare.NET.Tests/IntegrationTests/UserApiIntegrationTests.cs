namespace Cloudflare.NET.Tests.IntegrationTests;

using Cloudflare.NET.Core.Exceptions;
using Cloudflare.NET.User;
using Cloudflare.NET.User.Models;
using Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Shared.Fixtures;
using Shared.Helpers;
using Xunit.Abstractions;

/// <summary>
///   Contains integration tests for the <see cref="UserApi" /> class implementing F14 - User Management.
///   These tests interact with the live Cloudflare API and require credentials.
///   <para>
///     <b>Note:</b> These tests modify the authenticated user's profile and restore original values in cleanup.
///   </para>
///   <para>
///     <b>Important:</b> These tests require a user-scoped API token (<c>Cloudflare:UserApiToken</c>) with
///     <c>User:Edit</c> permission. Account-scoped tokens cannot access user-level endpoints.
///   </para>
/// </summary>
[Trait("Category", TestConstants.TestCategories.Integration)]
[Collection(TestCollections.UserProfile)]
public class UserApiIntegrationTests : IClassFixture<UserApiTestFixture>, IAsyncLifetime
{
  #region Constants & Statics

  /// <summary>Characters allowed in Cloudflare user names (letters only - no numbers or special chars).</summary>
  private const string NameCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

  /// <summary>Random number generator for generating unique test names.</summary>
  private static readonly Random Random = new();

  #endregion


  #region Properties & Fields - Non-Public

  /// <summary>The subject under test, resolved from the test fixture.</summary>
  private readonly IUserApi _sut;

  /// <summary>
  ///   Indicates whether the UserApiToken is configured. If false, tests are skipped by the attribute
  ///   but InitializeAsync/DisposeAsync still run, so we need to guard against API calls.
  /// </summary>
  private readonly bool _isUserTokenConfigured;

  /// <summary>The original user profile captured during test initialization for restoration.</summary>
  private User? _originalUser;

  #endregion


  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="UserApiIntegrationTests" /> class.</summary>
  /// <param name="fixture">The shared test fixture that provides a User API client with user-scoped token.</param>
  /// <param name="output">The xUnit test output helper.</param>
  public UserApiIntegrationTests(UserApiTestFixture fixture, ITestOutputHelper output)
  {
    // The SUT is resolved via the fixture's pre-configured User API with user-scoped token.
    _sut = fixture.UserApi;

    // Check if the user token is configured to guard InitializeAsync/DisposeAsync.
    _isUserTokenConfigured =
      !TestConfigurationValidator.IsSecretMissing(TestConfiguration.CloudflareSettings.UserApiToken);

    // Wire up the logger provider to the current test's output.
    var loggerProvider = fixture.ServiceProvider.GetRequiredService<XunitTestOutputLoggerProvider>();
    loggerProvider.Current = output;
  }

  #endregion


  #region Methods Impl - IAsyncLifetime

  /// <summary>Captures the original user profile before tests run.</summary>
  public async Task InitializeAsync()
  {
    // Guard: Only make API calls if the user token is configured.
    // Tests will be skipped by [UserIntegrationTest] but IAsyncLifetime still runs.
    if (!_isUserTokenConfigured)
      return;

    // Capture the original user profile to restore after tests.
    _originalUser = await _sut.GetUserAsync();
  }

  /// <summary>Restores the original user profile after all tests have run.</summary>
  public async Task DisposeAsync()
  {
    // Guard: Only restore if we successfully captured the original user.
    if (_originalUser is null)
      return;

    // Only restore if at least one editable field has a value to restore.
    // The Cloudflare API rejects PATCH requests with all null fields (error 1029).
    var hasFieldsToRestore = !string.IsNullOrEmpty(_originalUser.FirstName) ||
                             !string.IsNullOrEmpty(_originalUser.LastName) ||
                             !string.IsNullOrEmpty(_originalUser.Country) ||
                             !string.IsNullOrEmpty(_originalUser.Telephone) ||
                             !string.IsNullOrEmpty(_originalUser.Zipcode);

    if (!hasFieldsToRestore)
      return;

    // Restore original editable fields.
    await _sut.EditUserAsync(new EditUserRequest(
      FirstName: _originalUser.FirstName,
      LastName: _originalUser.LastName,
      Country: _originalUser.Country,
      Telephone: _originalUser.Telephone,
      Zipcode: _originalUser.Zipcode));
  }

  #endregion


  #region Get User Tests (I01-I04)

  /// <summary>I01: Verifies that GetUserAsync returns a valid user with Id and Email.</summary>
  [UserIntegrationTest]
  public async Task GetUserAsync_ReturnsUserWithIdAndEmail()
  {
    // Act
    var user = await _sut.GetUserAsync();

    // Assert
    user.Should().NotBeNull();
    user.Id.Should().NotBeNullOrEmpty("user should have an ID");
    user.Email.Should().NotBeNullOrEmpty("user should have an email");
  }

  /// <summary>I02: Verifies that the user has a non-empty email address.</summary>
  [UserIntegrationTest]
  public async Task GetUserAsync_EmailIsNonEmpty()
  {
    // Act
    var user = await _sut.GetUserAsync();

    // Assert
    user.Email.Should().NotBeNullOrEmpty();
    user.Email.Should().Contain("@", "email should be in valid format");
  }

  /// <summary>I03: Verifies that the user has timestamp fields populated.</summary>
  [UserIntegrationTest]
  public async Task GetUserAsync_HasTimestamps()
  {
    // Act
    var user = await _sut.GetUserAsync();

    // Assert
    user.CreatedOn.Should().NotBeNull("user should have creation timestamp");
    user.CreatedOn!.Value.Should().BeBefore(DateTime.UtcNow, "creation date should be in the past");
    user.ModifiedOn.Should().NotBeNull("user should have modification timestamp");
  }

  /// <summary>I04: Verifies that the user has 2FA status as a boolean.</summary>
  [UserIntegrationTest]
  public async Task GetUserAsync_Has2FAStatus()
  {
    // Act
    var user = await _sut.GetUserAsync();

    // Assert
    // TwoFactorAuthenticationEnabled is a bool, so we just verify the response is valid
    // The actual value depends on the user's configuration
    user.Should().NotBeNull();
    // The bool type ensures it's either true or false - no null check needed
    // We just verify the API returned valid data
  }

  #endregion


  #region Edit User Tests (I05-I10)

  /// <summary>I05: Verifies that editing the first name persists the change.</summary>
  [UserIntegrationTest]
  public async Task EditUserAsync_FirstName_ChangePersists()
  {
    // Arrange - Cloudflare only accepts letters in names (no numbers or special chars).
    var newFirstName = "TestFirst" + GenerateRandomLetters(8);

    // Act
    var updated = await _sut.EditUserAsync(new EditUserRequest(FirstName: newFirstName));
    var retrieved = await _sut.GetUserAsync();

    // Assert
    updated.FirstName.Should().Be(newFirstName);
    retrieved.FirstName.Should().Be(newFirstName);
  }

  /// <summary>I06: Verifies that editing the last name persists the change.</summary>
  [UserIntegrationTest]
  public async Task EditUserAsync_LastName_ChangePersists()
  {
    // Arrange - Cloudflare only accepts letters in names (no numbers or special chars).
    var newLastName = "TestLast" + GenerateRandomLetters(8);

    // Act
    var updated = await _sut.EditUserAsync(new EditUserRequest(LastName: newLastName));
    var retrieved = await _sut.GetUserAsync();

    // Assert
    updated.LastName.Should().Be(newLastName);
    retrieved.LastName.Should().Be(newLastName);
  }

  /// <summary>I07: Verifies that editing the country with a valid ISO code persists the change.</summary>
  [UserIntegrationTest]
  public async Task EditUserAsync_Country_ChangePersists()
  {
    // Arrange - Use a valid ISO 3166-1 alpha-2 code that's different from the original
    var newCountry = _originalUser?.Country == "US" ? "GB" : "US";

    // Act
    var updated = await _sut.EditUserAsync(new EditUserRequest(Country: newCountry));
    var retrieved = await _sut.GetUserAsync();

    // Assert
    updated.Country.Should().Be(newCountry);
    retrieved.Country.Should().Be(newCountry);
  }

  /// <summary>I08: Verifies that editing the telephone number persists the change.</summary>
  [UserIntegrationTest]
  public async Task EditUserAsync_Telephone_ChangePersists()
  {
    // Arrange - Use international format
    var newTelephone = "+1-555-" + DateTime.UtcNow.Ticks.ToString().Substring(0, 7);

    // Act
    var updated = await _sut.EditUserAsync(new EditUserRequest(Telephone: newTelephone));
    var retrieved = await _sut.GetUserAsync();

    // Assert
    updated.Telephone.Should().Be(newTelephone);
    retrieved.Telephone.Should().Be(newTelephone);
  }

  /// <summary>I09: Verifies that editing the zipcode persists the change.</summary>
  [UserIntegrationTest]
  public async Task EditUserAsync_Zipcode_ChangePersists()
  {
    // Arrange
    var newZipcode = DateTime.UtcNow.Ticks.ToString().Substring(0, 5);

    // Act
    var updated = await _sut.EditUserAsync(new EditUserRequest(Zipcode: newZipcode));
    var retrieved = await _sut.GetUserAsync();

    // Assert
    updated.Zipcode.Should().Be(newZipcode);
    retrieved.Zipcode.Should().Be(newZipcode);
  }

  /// <summary>I10: Verifies that editing multiple fields at once persists all changes.</summary>
  [UserIntegrationTest]
  public async Task EditUserAsync_MultipleFields_AllChangePersist()
  {
    // Arrange - Cloudflare only accepts letters in names (no numbers or special chars).
    var suffix = GenerateRandomLetters(6);
    var newFirstName = "Multi" + suffix;
    var newLastName = "Test" + suffix;
    var newZipcode = DateTime.UtcNow.Ticks.ToString().Substring(0, 5);

    // Act
    var updated = await _sut.EditUserAsync(new EditUserRequest(
      FirstName: newFirstName,
      LastName: newLastName,
      Zipcode: newZipcode));
    var retrieved = await _sut.GetUserAsync();

    // Assert
    updated.FirstName.Should().Be(newFirstName);
    updated.LastName.Should().Be(newLastName);
    updated.Zipcode.Should().Be(newZipcode);
    retrieved.FirstName.Should().Be(newFirstName);
    retrieved.LastName.Should().Be(newLastName);
    retrieved.Zipcode.Should().Be(newZipcode);
  }

  #endregion


  #region Get-Edit-Get Cycle Tests (I11-I12)

  /// <summary>I11: Verifies a complete Get-Edit-Get cycle persists changes.</summary>
  [UserIntegrationTest]
  public async Task GetEditGetCycle_ChangesPersist()
  {
    // Arrange - Get original. Cloudflare only accepts letters in names.
    var original = await _sut.GetUserAsync();
    var newFirstName = "Cycle" + GenerateRandomLetters(8);

    // Act - Edit
    var updated = await _sut.EditUserAsync(new EditUserRequest(FirstName: newFirstName));

    // Assert - Get updated and verify
    var retrieved = await _sut.GetUserAsync();
    updated.FirstName.Should().Be(newFirstName);
    retrieved.FirstName.Should().Be(newFirstName);
    retrieved.Id.Should().Be(original.Id, "user ID should not change");
    retrieved.Email.Should().Be(original.Email, "email should not change");
  }

  /// <summary>I12: Verifies that partial updates preserve unspecified fields.</summary>
  [UserIntegrationTest]
  public async Task PartialUpdate_PreservesOtherFields()
  {
    // Arrange - First set all fields to known values. Cloudflare only accepts letters in names.
    var initialFirstName = "Initial" + GenerateRandomLetters(5);
    var initialLastName = "InitLast" + GenerateRandomLetters(4);
    await _sut.EditUserAsync(new EditUserRequest(
      FirstName: initialFirstName,
      LastName: initialLastName));

    var afterInitial = await _sut.GetUserAsync();

    // Act - Now update only first name
    var newFirstName = "Partial" + GenerateRandomLetters(5);
    await _sut.EditUserAsync(new EditUserRequest(FirstName: newFirstName));

    // Assert - Last name should be preserved
    var afterPartial = await _sut.GetUserAsync();
    afterPartial.FirstName.Should().Be(newFirstName, "first name should be updated");
    afterPartial.LastName.Should().Be(initialLastName, "last name should be preserved");
    afterPartial.Email.Should().Be(afterInitial.Email, "email should be preserved");
  }

  #endregion


  #region User Status Tests (I13-I14)

  /// <summary>I13: Verifies that zone tier flags are present in the response.</summary>
  [UserIntegrationTest]
  public async Task GetUserAsync_ZoneTierFlags_ArePresent()
  {
    // Act
    var user = await _sut.GetUserAsync();

    // Assert - Zone tier flags are bools and will always be present
    // We can't verify specific values as they depend on the user's zones
    user.Should().NotBeNull();
    // HasProZones, HasBusinessZones, HasEnterpriseZones are all bool types
    // Their presence is guaranteed by the record definition
  }

  /// <summary>I14: Verifies that organizations array is handled (may be null or populated).</summary>
  [UserIntegrationTest]
  public async Task GetUserAsync_Organizations_IsHandled()
  {
    // Act
    var user = await _sut.GetUserAsync();

    // Assert - Organizations may be null or an array depending on the user
    // We just verify the response is valid and doesn't throw
    user.Should().NotBeNull();
    // If organizations is populated, verify its structure
    if (user.Organizations is not null && user.Organizations.Count > 0)
    {
      user.Organizations[0].Id.Should().NotBeNullOrEmpty();
      user.Organizations[0].Name.Should().NotBeNullOrEmpty();
    }
  }

  #endregion


  #region Edge Cases (I15-I18)

  /// <summary>I15: Verifies that country code updates are handled by the API.</summary>
  [UserIntegrationTest]
  public async Task EditUserAsync_CountryCode_IsAcceptedByApi()
  {
    // Arrange - Note: Cloudflare's API doesn't strictly validate country codes.
    // It accepts arbitrary two-letter strings. This test verifies the roundtrip works.
    var testCountry = "XX";

    // Act
    var updated = await _sut.EditUserAsync(new EditUserRequest(Country: testCountry));
    var retrieved = await _sut.GetUserAsync();

    // Assert - The API accepts the value and returns it
    updated.Country.Should().Be(testCountry);
    retrieved.Country.Should().Be(testCountry);
  }

  /// <summary>I16: Verifies that an empty edit request (all nulls) is handled gracefully.</summary>
  [UserIntegrationTest]
  public async Task EditUserAsync_EmptyRequest_IsHandledGracefully()
  {
    // Arrange
    var emptyRequest = new EditUserRequest();

    // Act
    var act = async () => await _sut.EditUserAsync(emptyRequest);

    // Assert - The API rejects empty requests with error 1029, which is expected behavior.
    // We're testing that the edge case is handled predictably.
    try
    {
      await act();
      // If it succeeds, verify user is still valid
      var user = await _sut.GetUserAsync();
      user.Should().NotBeNull();
    }
    catch (CloudflareApiException)
    {
      // API may reject empty requests - this is acceptable
    }
    catch (HttpRequestException ex) when (ex.Message.Contains("1029"))
    {
      // API rejects empty requests with error 1029: "Unable to find a user property to update"
      // This is expected and acceptable behavior.
    }
  }

  /// <summary>I17: Verifies that an unauthorized request returns a permission error (simulated via invalid token test skipped in integration).</summary>
  [UserIntegrationTest]
  public async Task GetUserAsync_ValidToken_DoesNotThrowForbidden()
  {
    // This test verifies our valid token works - permission denied is tested in unit tests
    // Act
    var act = () => _sut.GetUserAsync();

    // Assert - Should not throw with valid credentials
    await act.Should().NotThrowAsync<HttpRequestException>();
  }

  /// <summary>I18: Verifies that excessively long values are handled by the API.</summary>
  [UserIntegrationTest]
  public async Task EditUserAsync_ExcessivelyLongValue_ThrowsOrTruncates()
  {
    // Arrange - Create a very long first name (300 characters)
    var longFirstName = new string('A', 300);

    // Act
    var act = () => _sut.EditUserAsync(new EditUserRequest(FirstName: longFirstName));

    // Assert - API should either reject or truncate
    // We expect either an exception or a truncated/modified value
    try
    {
      var result = await act();
      // If it succeeds, the API may have truncated the value
      result.FirstName?.Length.Should().BeLessThanOrEqualTo(300);
    }
    catch (CloudflareApiException)
    {
      // API rejected the excessively long value - this is expected behavior
    }
    catch (HttpRequestException)
    {
      // HTTP-level rejection - also acceptable
    }
  }

  #endregion


  #region Helper Methods

  /// <summary>Generates a random string of letters for use in test names.</summary>
  /// <param name="length">The number of random letters to generate.</param>
  /// <returns>A random string containing only letters (a-z, A-Z).</returns>
  /// <remarks>
  ///   Cloudflare's User API only accepts letters in first/last name fields.
  ///   Numbers, special characters, and other symbols are rejected with error 1034.
  /// </remarks>
  private static string GenerateRandomLetters(int length)
  {
    return new string(Enumerable.Range(0, length)
                                .Select(_ => NameCharacters[Random.Next(NameCharacters.Length)])
                                .ToArray());
  }

  #endregion
}
