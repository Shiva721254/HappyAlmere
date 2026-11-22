using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace HappyCity.Web.Security;

public static class TrustedProxyConfiguration
{
    public static ForwardedHeadersOptions? Create(IConfiguration configuration)
    {
        if (!configuration.GetValue<bool>("TrustedProxy:Enabled"))
            return null;

        var rawProxies = configuration.GetSection("TrustedProxy:KnownProxies").Get<string[]>();
        if (rawProxies is null || rawProxies.Length == 0)
            throw new InvalidOperationException("TrustedProxy:Enabled requires at least one explicit IP address in TrustedProxy:KnownProxies.");

        var options = new ForwardedHeadersOptions
        {
            // Never trust X-Forwarded-Host; reverse proxies must forward the canonical Host header directly.
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            ForwardLimit = 1
        };
        options.KnownProxies.Clear();
        options.KnownIPNetworks.Clear();

        foreach (var entry in rawProxies)
        {
            if (string.IsNullOrWhiteSpace(entry) || !IPAddress.TryParse(entry.Trim(), out var address))
                throw new InvalidOperationException("TrustedProxy:KnownProxies contains an invalid IP address.");
            if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any) || address.Equals(IPAddress.Broadcast))
                throw new InvalidOperationException("TrustedProxy:KnownProxies must not contain wildcard or broadcast addresses.");
            options.KnownProxies.Add(address);
        }

        return options;
    }
}
