namespace Cloudflare.NET.Analytics.Tests.UnitTests;

using GraphQL;
using GraphQL.Client.Abstractions;
using Microsoft.Extensions.Options;

/// <summary>Contains unit tests for the <see cref="AnalyticsApi" /> class.</summary>
public class AnalyticsApiUnitTests
{
  #region Methods

  /// <summary>
  ///   Verifies that SendQueryAsync returns the data correctly when the API call is
  ///   successful.
  /// </summary>
  [Fact]
  public async Task SendQueryAsync_OnSuccess_ReturnsData()
  {
    // Arrange
    var request         = new GraphQLRequest { Query         = "query { accounts { id } }" };
    var expectedData    = new { viewer                       = new { accounts = new[] { new { id = "123" } } } };
    var graphQlResponse = new GraphQLResponse<object> { Data = expectedData };

    var mockGraphQlClient = new Mock<IGraphQLClient>();
    mockGraphQlClient
      .Setup(c => c.SendQueryAsync<object>(request, It.IsAny<CancellationToken>()))
      .ReturnsAsync(graphQlResponse);

    // The options are not used directly by the AnalyticsApi but are required by the constructor.
    _ = Options.Create(new CloudflareApiOptions());
    var sut = new AnalyticsApi(mockGraphQlClient.Object);

    // Act
    var result = await sut.SendQueryAsync<object>(request);

    // Assert
    result.Should().BeSameAs(expectedData);
    mockGraphQlClient.Verify(c => c.SendQueryAsync<object>(request, It.IsAny<CancellationToken>()), Times.Once);
  }

  /// <summary>
  ///   Verifies that SendQueryAsync throws an InvalidOperationException when the GraphQL
  ///   response contains errors.
  /// </summary>
  [Fact]
  public async Task SendQueryAsync_OnApiErrors_ThrowsInvalidOperationException()
  {
    // Arrange
    var request         = new GraphQLRequest { Query = "query { accounts { id } }" };
    var errors          = new[] { new GraphQLError { Message = "Invalid field 'id'" } };
    var graphQlResponse = new GraphQLResponse<object> { Errors = errors };

    var mockGraphQlClient = new Mock<IGraphQLClient>();
    mockGraphQlClient
      .Setup(c => c.SendQueryAsync<object>(request, It.IsAny<CancellationToken>()))
      .ReturnsAsync(graphQlResponse);

    _ = Options.Create(new CloudflareApiOptions());
    var sut = new AnalyticsApi(mockGraphQlClient.Object);

    // Act
    var action = async () => await sut.SendQueryAsync<object>(request);

    // Assert
    await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("GraphQL query failed: Invalid field 'id'");
  }

  /// <summary>
  ///   Verifies that SendQueryAsync throws an InvalidOperationException when the GraphQL
  ///   response contains no data.
  /// </summary>
  [Fact]
  public async Task SendQueryAsync_OnNullData_ThrowsInvalidOperationException()
  {
    // Arrange
    var request = new GraphQLRequest { Query = "query { accounts { id } }" };
    // The response object has a null Data property.
    var graphQlResponse = new GraphQLResponse<object> { Data = null! };

    var mockGraphQlClient = new Mock<IGraphQLClient>();
    mockGraphQlClient
      .Setup(c => c.SendQueryAsync<object>(request, It.IsAny<CancellationToken>()))
      .ReturnsAsync(graphQlResponse);

    _ = Options.Create(new CloudflareApiOptions());
    var sut = new AnalyticsApi(mockGraphQlClient.Object);

    // Act
    var action = async () => await sut.SendQueryAsync<object>(request);

    // Assert
    await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("GraphQL query returned no data.");
  }

  #endregion
}
