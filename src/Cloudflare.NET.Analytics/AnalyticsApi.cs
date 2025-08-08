namespace Cloudflare.NET.Analytics;

using GraphQL;
using GraphQL.Client.Abstractions;

/// <summary>Implements a generic client for the Cloudflare Analytics GraphQL endpoint.</summary>
/// <remarks>Initializes a new instance of the <see cref="AnalyticsApi" /> class.</remarks>
/// <param name="graphQlClient">The GraphQL client for making requests.</param>
public class AnalyticsApi(IGraphQLClient graphQlClient) : IAnalyticsApi
{
  #region Properties & Fields - Non-Public

  /// <summary>The GraphQL client instance.</summary>
  private readonly IGraphQLClient _graphQlClient = graphQlClient;

  #endregion

  #region Methods Impl

  /// <inheritdoc />
  public async Task<TResponse> SendQueryAsync<TResponse>(GraphQLRequest request, CancellationToken cancellationToken = default)
  {
    var response = await _graphQlClient.SendQueryAsync<TResponse>(request, cancellationToken);

    if (response.Errors?.Length > 0)
    {
      var errorMessages = string.Join("\n", response.Errors.Select(e => e.Message));
      // TODO: Log the error messages.
      throw new InvalidOperationException($"GraphQL query failed: {errorMessages}");
    }

    if (response.Data is null)
      throw new InvalidOperationException("GraphQL query returned no data.");

    return response.Data;
  }

  #endregion
}
