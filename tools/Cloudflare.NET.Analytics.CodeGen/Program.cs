using System.CommandLine;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

// *************************************************************************************************
// Cloudflare.Analytics.CodeGen
// ----------------------------
// Dev-time code generator that fetches the Cloudflare Analytics GraphQL schema via introspection,
// then emits a single C# file (AnalyticsModels.g.cs) containing concrete, non-partial records and
// enums for all relevant types. It preserves schema descriptions as XML doc comments and applies
// [JsonPropertyName] to match GraphQL names.
// *************************************************************************************************

#region System.CommandLine Setup

// Create command-line options for each configurable parameter.
// This provides strong typing, validation, and auto-generated help text.
var apiUrlOption = new Option<string>("--api-url")
{
  Description = "The Cloudflare GraphQL API endpoint."
};

var apiTokenOption = new Option<string>("--api-token")
{
  Description = "The Cloudflare API Token. Can also be set via the CF_API_TOKEN environment variable."
};

var outputPathOption = new Option<FileInfo>("--out")
{
  Description = "The file path for the generated C# models."
};

var schemaJsonOutOption = new Option<FileInfo>("--schema-out")
{
  Description = "The file path for the raw introspection schema JSON output."
};

var includeRegexOption = new Option<string>("--include")
{
  Description = "A regex to include types from the schema. e.g., \"^r2|^R2|.*AdaptiveGroups$\""
};

var excludeRegexOption = new Option<string>("--exclude")
{
  Description = "A regex to exclude types from the schema."
};

var namespaceOption = new Option<string>("--namespace")
{
  Description = "The namespace for the generated C# types."
};

var writeSchemaJsonOption = new Option<bool>("--emit-schema-json")
{
  Description = "If set, writes the raw schema to the path specified by --schema-out.",
  DefaultValueFactory = _ => true
};

var verboseOption = new Option<bool>("--verbose")
{
  Description = "Enable verbose output during execution."
};

// Configure the root command for the application.
var rootCommand = new RootCommand("Cloudflare.NET Analytics Code Generator")
{
  apiUrlOption,
  apiTokenOption,
  outputPathOption,
  schemaJsonOutOption,
  includeRegexOption,
  excludeRegexOption,
  namespaceOption,
  writeSchemaJsonOption,
  verboseOption
};

rootCommand.Description = "Fetches the Cloudflare GraphQL schema and generates C# model classes.";

// Set the action (RC API) for the root command. We read option values from ParseResult and
// apply robust runtime defaults when options are omitted.
rootCommand.SetAction(async parseResult =>
{
  // ---- Extract values via RC API ----
  var apiUrl   = parseResult.GetValue(apiUrlOption) ?? "https://api.cloudflare.com/client/v4/graphql";
  var apiToken = parseResult.GetValue(apiTokenOption) ?? Environment.GetEnvironmentVariable("CF_API_TOKEN");

  // Use absolute defaults anchored at the executable directory to avoid bin-relative paths.
  var defaultOutFile       = GetRepoRelativeFile("src", "Cloudflare.NET.Analytics", "Models", "AnalyticsModels.g.cs");
  var defaultSchemaOutFile = GetRepoRelativeFile("src", "Cloudflare.NET.Analytics", "Models", "schema.analytics.json");

  var outputPath = parseResult.GetValue(outputPathOption) ?? defaultOutFile;
  var schemaOut  = parseResult.GetValue(schemaJsonOutOption) ?? defaultSchemaOutFile;

  var includeRegex    = parseResult.GetValue(includeRegexOption) ?? string.Empty;
  var excludeRegex    = parseResult.GetValue(excludeRegexOption) ?? "^__|^Query$|^Mutation$|^Subscription$";
  var @namespace      = parseResult.GetValue(namespaceOption) ?? "Cloudflare.NET.Analytics.Models";
  var writeSchemaJson = parseResult.GetValue(writeSchemaJsonOption);
  var verbose         = parseResult.GetValue(verboseOption);

  // ---- Guardrails ----
  if (string.IsNullOrWhiteSpace(apiToken))

  {
    Console.WriteLine(
      "API Token not found. You can provide it via the --api-token argument, the CF_API_TOKEN environment variable, or enter it below.");
    Console.Write("Enter API Token: ");
    apiToken = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(apiToken))
    {
      Console.Error.WriteLine("\nERROR: Missing API token. An API token is required to proceed.");
      return 1;
    }
  }

  // ---- Fetch schema via introspection ----
  if (verbose) Console.WriteLine($"Introspecting schema from {apiUrl} ...");

  var schema = await FetchSchemaAsync(apiUrl, apiToken);

  if (schema is null)
  {
    Console.Error.WriteLine("Failed to fetch/parse introspection schema.");
    return 2;
  }

  if (writeSchemaJson)
  {
    schemaOut.Directory?.Create();
    await File.WriteAllTextAsync(schemaOut.FullName, JsonSerializer.Serialize(schema, new JsonSerializerOptions { WriteIndented = true }));
    if (verbose) Console.WriteLine($"Wrote raw schema JSON -> {schemaOut.FullName}");
  }

  // ---- Emit code ----
  var emitter = new CSharpEmitter(@namespace, includeRegex, excludeRegex, verbose);
  var code    = emitter.Emit(schema);

  outputPath.Directory?.Create();
  await File.WriteAllTextAsync(outputPath.FullName, code, Encoding.UTF8);
  Console.WriteLine($"Generated models -> {outputPath.FullName}");

  return 0;
});

// Parse then invoke (no CommandLineBuilder / no CommandExtensions required in RC).
var parseResult = rootCommand.Parse(args);
return await parseResult.InvokeAsync();

#endregion


// ========== Local helpers ==========

static FileInfo GetRepoRelativeFile(params string[] segments)
{
  // Probe upward from likely starting points to find the Git repository root:
  // 1) the host's base directory (where the tool actually runs from),
  // 2) the current working directory (useful if invoked via `dotnet run` from repo root).
  var candidates = new[]
  {
    AppContext.BaseDirectory,
    Directory.GetCurrentDirectory()
  };

  foreach (var start in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
  {
    if (TryFindGitRepoRoot(start, out var repoRoot))
    {
      var full = Path.GetFullPath(Path.Combine(repoRoot, Path.Combine(segments)));
      return new FileInfo(full);
    }
  }

  // Fallback: if no .git was found (uncommon in CI tarballs, etc.), anchor to BaseDirectory.
  var fallback = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, Path.Combine(segments)));
  return new FileInfo(fallback);
}

static bool TryFindGitRepoRoot(string startDirectory, out string repoRoot)
{
  // Walk parents until drive/root; treat either ".git" directory OR file as a repo marker.
  var dir = Path.GetFullPath(startDirectory);

  while (!string.IsNullOrEmpty(dir))
  {
    if (IsGitRepoRoot(dir))
    {
      repoRoot = dir;
      return true;
    }

    var parent = Directory.GetParent(dir);
    if (parent is null) break;

    dir = parent.FullName;
  }

  repoRoot = string.Empty;
  return false;
}

static bool IsGitRepoRoot(string directory)
{
  var gitDirPath  = Path.Combine(directory, ".git");

  // Support both classic repos (.git is a directory) and linked worktrees (.git is a file pointing to gitdir).
  return Directory.Exists(gitDirPath) || File.Exists(gitDirPath);
}

static async Task<IntrospectionSchema?> FetchSchemaAsync(string graphqlEndpoint, string bearerToken)
{
  using var http = new HttpClient();
  http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

  var payload = new
  {
    query     = IntrospectionQuery.Text,
    variables = new { }
  };

  var json = JsonSerializer.Serialize(payload);
  var resp = await http.PostAsync(graphqlEndpoint, new StringContent(json, Encoding.UTF8, "application/json"));

  if (!resp.IsSuccessStatusCode)
  {
    Console.Error.WriteLine($"GraphQL Introspection failed: {(int)resp.StatusCode} {resp.ReasonPhrase}");
    var err = await resp.Content.ReadAsStringAsync();
    Console.Error.WriteLine(err);
    return null;
  }

  var body = await resp.Content.ReadAsStringAsync();

  var doc = JsonSerializer.Deserialize<GraphQLIntrospectionResponse>(body, new JsonSerializerOptions
  {
    PropertyNameCaseInsensitive = true
  });

  if (doc?.Data?.Schema is null)
  {
    Console.Error.WriteLine("Introspection returned no schema data.");
    return null;
  }

  return doc.Data.Schema;
}


// ===== Introspection DTOs (only what we need) =====

file sealed class GraphQLIntrospectionResponse
{
  #region Properties & Fields - Public

  [JsonPropertyName("data")]
  public IntrospectionData? Data { get; set; }

  #endregion
}

file sealed class IntrospectionData
{
  #region Properties & Fields - Public

  [JsonPropertyName("__schema")]
  public IntrospectionSchema? Schema { get; set; }

  #endregion
}

public sealed class IntrospectionSchema
{
  #region Properties & Fields - Public

  [JsonPropertyName("types")]
  public List<IntrospectionType> Types { get; set; } = new();
  [JsonPropertyName("queryType")]
  public IntrospectionTypeRef? QueryType { get; set; }

  #endregion
}

public sealed class IntrospectionType
{
  #region Properties & Fields - Public

  [JsonPropertyName("kind")]
  public string Kind { get; set; } = "";
  [JsonPropertyName("name")]
  public string? Name { get; set; }
  [JsonPropertyName("description")]
  public string? Description { get; set; }

  [JsonPropertyName("fields")]
  public List<IntrospectionField>? Fields { get; set; }
  [JsonPropertyName("inputFields")]
  public List<IntrospectionInputValue>? InputFields { get; set; }
  [JsonPropertyName("enumValues")]
  public List<IntrospectionEnumValue>? EnumValues { get; set; }
  [JsonPropertyName("ofType")]
  public IntrospectionTypeRef? OfType { get; set; }

  #endregion
}

public sealed class IntrospectionField
{
  #region Properties & Fields - Public

  [JsonPropertyName("name")]
  public string Name { get; set; } = "";
  [JsonPropertyName("description")]
  public string? Description { get; set; }
  [JsonPropertyName("type")]
  public IntrospectionTypeRef Type { get; set; } = new();

  #endregion
}

public sealed class IntrospectionInputValue
{
  #region Properties & Fields - Public

  [JsonPropertyName("name")]
  public string Name { get; set; } = "";
  [JsonPropertyName("description")]
  public string? Description { get; set; }
  [JsonPropertyName("type")]
  public IntrospectionTypeRef Type { get; set; } = new();

  #endregion
}

public sealed class IntrospectionEnumValue
{
  #region Properties & Fields - Public

  [JsonPropertyName("name")]
  public string Name { get; set; } = "";
  [JsonPropertyName("description")]
  public string? Description { get; set; }

  #endregion
}

public sealed class IntrospectionTypeRef
{
  #region Properties & Fields - Public

  [JsonPropertyName("kind")]
  public string Kind { get; set; } = "";
  [JsonPropertyName("name")]
  public string? Name { get; set; }
  [JsonPropertyName("ofType")]
  public IntrospectionTypeRef? OfType { get; set; }

  #endregion
}


// ===== Introspection query (canonical form) =====
// Source/background on GraphQL introspection: https://graphql.org/learn/introspection/
// and Cloudflare’s docs describing schema discovery via introspection. 
// We embed the canonical query verbatim for simplicity.
file static class IntrospectionQuery
{
  #region Constants & Statics

  public const string Text =
    """
    query IntrospectionQuery {
      __schema {
        queryType { name }
        mutationType { name }
        subscriptionType { name }
        types { ...FullType }
        directives {
          name
          description
          locations
          args { ...InputValue }
        }
      }
    }

    fragment FullType on __Type {
      kind
      name
      description
      fields(includeDeprecated: true) {
        name
        description
        args { ...InputValue }
        type { ...TypeRef }
        isDeprecated
        deprecationReason
      }
      inputFields { ...InputValue }
      interfaces { ...TypeRef }
      enumValues(includeDeprecated: true) { name description isDeprecated deprecationReason }
      possibleTypes { ...TypeRef }
    }

    fragment InputValue on __InputValue {
      name
      description
      type { ...TypeRef }
      defaultValue
    }

    fragment TypeRef on __Type {
      kind
      name
      ofType {
        kind
        name
        ofType {
          kind
          name
          ofType {
            kind
            name
            ofType {
              kind
              name
              ofType {
                kind
                name
                ofType {
                  kind
                  name
                  ofType {
                    kind
                    name
                  }
                }
              }
            }
          }
        }
      }
    }
    """;

  #endregion
}
