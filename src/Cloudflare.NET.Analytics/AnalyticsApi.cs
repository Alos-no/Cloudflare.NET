namespace Cloudflare.NET.Analytics;

using GraphQL;
using GraphQL.Client.Abstractions;
using Microsoft.Extensions.Logging;

/// <summary>Implements a generic client for the Cloudflare Analytics GraphQL endpoint.</summary>
/// <remarks>Initializes a new instance of the <see cref="AnalyticsApi" /> class.</remarks>
/// <param name="graphQlClient">The GraphQL client for making requests.</param>
/// <param name="loggerFactory">Factory used to create a type-scoped logger.</param>
public class AnalyticsApi(IGraphQLClient graphQlClient, ILoggerFactory loggerFactory) : IAnalyticsApi
{
  #region Properties & Fields - Non-Public

  /// <summary>The GraphQL client instance.</summary>
  private readonly IGraphQLClient _graphQlClient = graphQlClient;

  /// <summary>Logger for GraphQL requests and errors.</summary>
  private readonly ILogger<AnalyticsApi> _logger = loggerFactory.CreateLogger<AnalyticsApi>();

  #endregion

  #region Methods Impl

  /// <inheritdoc />
  public async Task<TResponse> SendQueryAsync<TResponse>(GraphQLRequest request, CancellationToken cancellationToken = default)
  {
    _logger.LogTrace("Sending GraphQL request OperationName={OperationName}", request.OperationName);

    var response = await _graphQlClient.SendQueryAsync<TResponse>(request, cancellationToken);

    if (response.Errors?.Length > 0)
    {
      foreach (var err in response.Errors)
        _logger.LogError("GraphQL error: {Message}", err.Message);

      var errorMessages = string.Join("\n", response.Errors.Select(e => e.Message));
      throw new InvalidOperationException($"GraphQL query failed: {errorMessages}");
    }

    if (response.Data is null)
    {
      _logger.LogError("GraphQL query returned no data for OperationName={OperationName}", request.OperationName);
      throw new InvalidOperationException("GraphQL query returned no data.");
    }

    return response.Data;
  }

  #endregion
}
