using System.Net;
using HappyCity.Web.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace HappyCity.FoundationTests;

public sealed class TrustedProxyTests
{
    [Fact]
    public void Disabled_by_default_returns_null()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        Assert.Null(TrustedProxyConfiguration.Create(config));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-an-ip")]
    [InlineData("0.0.0.0")]
    [InlineData("::")]
    [InlineData("255.255.255.255")]
    public void Enabled_without_valid_explicit_proxy_ip_fails_closed(string? proxy)
    {
        var values = new Dictionary<string, string?> { ["TrustedProxy:Enabled"] = "true" };
        if (proxy is not null) values["TrustedProxy:KnownProxies:0"] = proxy;
        var config = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        Assert.Throws<InvalidOperationException>(() => TrustedProxyConfiguration.Create(config));
    }

    [Fact]
    public async Task Enabled_proxy_honors_only_trusted_peer_and_never_trusts_forwarded_host()
    {
        await using var baseFactory = new FoundationFactory();
        await using var factory = baseFactory.WithWebHostBuilder(builder =>
            builder.UseSetting("TrustedProxy:Enabled", "true")
                   .UseSetting("TrustedProxy:KnownProxies:0", "127.0.0.1"));
        using var _ = factory.CreateClient(); // Initialize test server.

        async Task<HttpContext> Send(string peer, string? forwardedFor = null, string? forwardedProto = null, string? forwardedHost = null) =>
            await factory.Server.SendAsync(context =>
            {
                context.Connection.RemoteIpAddress = IPAddress.Parse(peer);
                context.Request.Scheme = forwardedProto is null ? "https" : "http";
                context.Request.Host = new HostString("localhost", 7143);
                context.Request.Method = "GET";
                context.Request.Path = "/";
                if (forwardedFor is not null) context.Request.Headers["X-Forwarded-For"] = forwardedFor;
                if (forwardedProto is not null) context.Request.Headers["X-Forwarded-Proto"] = forwardedProto;
                if (forwardedHost is not null) context.Request.Headers["X-Forwarded-Host"] = forwardedHost;
            });

        // Trusted loopback proxy resolves client IP & https scheme, but X-Forwarded-Host is still ignored.
        var proxied = await Send("127.0.0.1", "203.0.113.10", "https", "evil.example");
        Assert.Equal(200, proxied.Response.StatusCode);
        Assert.Equal("203.0.113.10", proxied.Connection.RemoteIpAddress?.ToString());
        Assert.Equal("https", proxied.Request.Scheme);
        Assert.Equal("localhost", proxied.Request.Host.Host);

        // Exhaust rate limit for client 203.0.113.10 through trusted proxy (119 more = 120 total).
        for (var i = 0; i < 119; i++)
            Assert.Equal(200, (await Send("127.0.0.1", "203.0.113.10", "https")).Response.StatusCode);
        Assert.Equal(429, (await Send("127.0.0.1", "203.0.113.10", "https")).Response.StatusCode);

        // Distinct client behind same trusted proxy still has its own budget.
        Assert.Equal(200, (await Send("127.0.0.1", "203.0.113.11", "https")).Response.StatusCode);

        // Untrusted direct peer spoofing X-Forwarded-For cannot hijack or evade rate limits.
        var untrusted = await Send("198.51.100.5", "203.0.113.10", null, "evil.example");
        Assert.Equal(200, untrusted.Response.StatusCode);
        Assert.Equal("198.51.100.5", untrusted.Connection.RemoteIpAddress?.ToString());
    }
}
