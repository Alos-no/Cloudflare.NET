namespace Cloudflare.NET.Tests.UnitTests.Security;

using Cloudflare.NET.Tests.Shared.Fixtures;
using NET.Security.Firewall;
using NET.Security.Firewall.Models;

/// <summary>Contains unit tests for the <see cref="AccessRuleQueryBuilder" /> class.</summary>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class AccessRuleQueryBuilderTests
{
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
      Notes: "test note",
      Mode: AccessRuleMode.Block,
      Match: FilterMatch.Any,
      ConfigurationTarget: AccessRuleTarget.Ip,
      ConfigurationValue: "1.2.3.4",
      Page: 2,
      PerPage: 50,
      Order: AccessRuleOrderField.Mode,
      Direction: ListOrderDirection.Descending
    );

    // Act
    var result = AccessRuleQueryBuilder.Build(filters);

    // Assert
    var expected = "?notes=test%20note"               +
                   "&mode=block"                       +
                   "&match=any"                        +
                   "&configuration.target=ip"          +
                   "&configuration.value=1.2.3.4"      +
                   "&page=2"                           +
                   "&per_page=50"                      +
                   "&order=mode"                       +
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
}