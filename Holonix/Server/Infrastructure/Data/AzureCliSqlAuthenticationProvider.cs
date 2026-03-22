using Azure.Core;
using Azure.Identity;
using Microsoft.Data.SqlClient;

namespace Holonix.Server.Infrastructure.Data;

public sealed class AzureCliSqlAuthenticationProvider : SqlAuthenticationProvider
{
    private static readonly string[] Scopes = ["https://database.windows.net/.default"];
    private readonly AzureCliCredential _credential;

    public AzureCliSqlAuthenticationProvider(string? tenantId = null)
    {
        _credential = string.IsNullOrWhiteSpace(tenantId)
            ? new AzureCliCredential()
            : new AzureCliCredential(new AzureCliCredentialOptions { TenantId = tenantId });
    }

    public override async Task<SqlAuthenticationToken> AcquireTokenAsync(SqlAuthenticationParameters parameters)
    {
        var token = await _credential.GetTokenAsync(
            new TokenRequestContext(Scopes),
            CancellationToken.None);

        return new SqlAuthenticationToken(token.Token, token.ExpiresOn);
    }

    public override bool IsSupported(SqlAuthenticationMethod authenticationMethod)
    {
        return authenticationMethod == SqlAuthenticationMethod.ActiveDirectoryDefault
            || authenticationMethod == SqlAuthenticationMethod.ActiveDirectoryInteractive;
    }
}
