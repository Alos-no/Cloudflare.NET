# User API

The User API provides access to operations for the currently authenticated user. All operations in this API affect only your own profile and resources.

## Quick Links

| API | Property | Description |
|-----|----------|-------------|
| [Profile](profile.md) | `cf.User` | View and edit your user profile |
| [Memberships](memberships.md) | `cf.User` | Manage your account memberships |
| [Invitations](invitations.md) | `cf.User` | Accept or reject account invitations |
| [API Tokens](api-tokens.md) | `cf.ApiTokens` | Manage your personal API tokens |
| [Audit Logs](audit-logs.md) | `cf.AuditLogs` | View your activity logs |

## Authentication

All User API operations use the currently authenticated API token. The operations affect only the user who owns the token.

```csharp
public class UserService(ICloudflareApiClient cf)
{
    public async Task<User> GetProfileAsync()
    {
        return await cf.User.GetUserAsync();
    }
}
```

## User vs Account Operations

| Feature | User API | Account API |
|---------|----------|-------------|
| **Scope** | Self only | All account resources |
| **Profile** | Edit own profile | N/A |
| **Memberships** | View/leave accounts | Invite/manage members |
| **Invitations** | Accept/reject | Send invitations |
| **API Tokens** | Personal tokens | Account-scoped tokens |
| **Audit Logs** | Own activity | All account activity |

## Related

- [Account Management](../accounts/account-management.md) - Manage accounts
- [Getting Started](../getting-started.md) - SDK setup
