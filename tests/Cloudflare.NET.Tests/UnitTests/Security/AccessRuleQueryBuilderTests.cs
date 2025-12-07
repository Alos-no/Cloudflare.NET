namespace Cloudflare.NET.Tests.UnitTests.Security;

using NET.Security.Firewall;
using NET.Security.Firewall.Models;
using Shared.Fixtures;

/// <summary>Contains unit tests for the <see cref="AccessRuleQueryBuilder" /> class.</summary>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class AccessRuleQueryBuilderTests
{
  #region Methods

  [Fact]
  public void Build_WithNoFilters_ReturnsEmptyString()
  {
    // Arrange
    ListAccessRulesFilters? filters = null;

    // Act
    var result = AccessRuleQueryBuilder.Build(filters);

    // Assert
    result.Should().BeEmpty();
  }

  [Fact]
  public void Build_WithAllFilters_ConstructsCorrectQueryString()
  {
    // Arrange
    var filters = new ListAccessRulesFilters(
      "test note",
      AccessRuleMode.Block,
      FilterMatch.Any,
      AccessRuleTarget.Ip,
      "1.2.3.4",
      2,
      50,
      AccessRuleOrderField.Mode,
      ListOrderDirection.Descending
    );

    // Act
    var result = AccessRuleQueryBuilder.Build(filters);

    // Assert
    var expected = "?notes=test%20note" +
      "&mode=block" +
      "&match=any" +
      "&configuration.target=ip" +
      "&configuration.value=1.2.3.4" +
      "&page=2" +
      "&per_page=50" +
      "&order=mode" +
      "&direction=desc";
    result.Should().Be(expected);
  }

  [Fact]
  public void Build_WithPartialFilters_ConstructsCorrectQueryString()
  {
    // Arrange
    var filters = new ListAccessRulesFilters(
      Mode: AccessRuleMode.Whitelist,
      Page: 1
    );

    // Act
    var result = AccessRuleQueryBuilder.Build(filters);

    // Assert
    var expected = "?mode=whitelist&page=1";
    result.Should().Be(expected);
  }

  #endregion
}
