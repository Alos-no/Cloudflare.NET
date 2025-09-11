Cloudflare.NET.SampleR2Console

This sample shows how to:
- Bootstrap both the Cloudflare REST client and the R2 client with the Generic Host
- Create or reuse a bucket
- Upload and download small objects
- List and delete objects

Configuration
- appsettings.json is included. You can also use environment variables or User Secrets.

Required keys
- Cloudflare:ApiToken
- Cloudflare:AccountId
- R2:AccessKeyId
- R2:SecretAccessKey
- R2:EndpointUrl (defaults to https://{0}.r2.cloudflarestorage.com)
- R2:SampleBucketName (optional; if empty, the sample will create a temporary bucket and delete it)

Run
> dotnet run

Notes
- The sample uploads and deletes small test objects under the "samples/" prefix.
- If R2:SampleBucketName is blank, the sample creates a unique bucket and deletes it at the end.