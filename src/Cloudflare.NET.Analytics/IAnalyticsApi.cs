namespace Cloudflare.NET.Analytics;

using GraphQL;

/// <summary>Defines the contract for a generic client to the Cloudflare Analytics GraphQL API.</summary>
public interface IAnalyticsApi
{
  /// <summary>Sends a GraphQL query to the Cloudflare API.</summary>
  /// <typeparam name="TResponse">The type to deserialize the GraphQL response data into.</typeparam>
  /// <param name="request">The GraphQL request object, containing the query and variables.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>
  ///   A task that represents the asynchronous operation. The task result contains the
  ///   deserialized data from the API response's "data" field.
  /// </returns>
  /// <exception cref="InvalidOperationException">
  ///   Thrown if the GraphQL API returns errors or if the response data is null.
  /// </exception>
  Task<TResponse> SendQueryAsync<TResponse>(GraphQLRequest request, CancellationToken cancellationToken = default);
}
