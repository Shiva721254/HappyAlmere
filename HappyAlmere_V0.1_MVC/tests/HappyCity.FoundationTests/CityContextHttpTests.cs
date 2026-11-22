using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using HappyCity.Application;
using HappyCity.Infrastructure;
using HappyCity.Web.CityContext;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace HappyCity.FoundationTests;

public sealed class CityContextHttpTests
{
    [Fact, Trait("Category", "CityRecovery")]
    public async Task Actual_isolated_city_recovery_and_restored_application_are_verified()
    {
        try
        {
            await VerifyIsolatedCityRecoveryAsync();
        }
        catch (Exception)
        {
            // Cover initialization, assertions, disposal and cleanup without exposing
            // private exception messages, assertion values or an inner exception.
            throw new InvalidOperationException("City recovery verification failed; inspect sanitized City evidence. Private diagnostics withheld.");
        }
    }

    private async Task VerifyIsolatedCityRecoveryAsync()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "scripts", "verify-city-foundation.py"))) root = root.Parent;
        Assert.NotNull(root);
        var mode = string.Equals(Environment.GetEnvironmentVariable("IdentityFoundation__Enabled"), "true", StringComparison.OrdinalIgnoreCase) ? "enabled" : "disabled";
        var start = new ProcessStartInfo(OperatingSystem.IsWindows() ? "python" : "python3")
        {
            WorkingDirectory = root.FullName, UseShellExecute = false,
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
            CreateNoWindow = true
        };
        start.ArgumentList.Add("scripts/verify-city-foundation.py");
        start.ArgumentList.Add("--mode"); start.ArgumentList.Add(mode);
        // Parent generates the authority for both paths; READY cannot select another run.
        var runId = DateTime.UtcNow.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture)
            + "-" + Guid.NewGuid().ToString("N")[..10];
        start.Environment["HAPPYCITY_CITY_RUN_ID"] = runId;
        var expectedConfiguration = Path.Combine(root.FullName, "artifacts", "city-private", mode + "-" + runId, "city-runtime.private.env");
        var expectedEvidence = Path.Combine(root.FullName, "artifacts", "forward", "ha006a-" + mode + "-" + runId, "evidence.json");
        using var process = Process.Start(start)!;
        var errors = process.StandardError.ReadToEndAsync();
        var submitted = false;
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(180));
            var line = await process.StandardOutput.ReadLineAsync(timeout.Token);
            Assert.True(line is not null, "City helper did not reach READY; inspect sanitized city evidence. No gate passed by inference.");
            Assert.NotNull(line);
            using var ready = JsonDocument.Parse(line);
            var readyFields = ready.RootElement.EnumerateObject().Select(property => property.Name).Order().ToArray();
            Assert.True(readyFields.SequenceEqual(new[] { "configuration", "evidence", "mode", "protocolVersion", "runId", "status" }),
                "Unexpected READY schema.");
            Assert.Equal("READY", ready.RootElement.GetProperty("status").GetString());
            Assert.Equal(3, ready.RootElement.GetProperty("protocolVersion").GetInt32());
            Assert.True(ready.RootElement.GetProperty("mode").GetString() == mode &&
                ready.RootElement.GetProperty("runId").GetString() == runId, "READY identity mismatch.");
            var configuration = OwnedFile(ready.RootElement.GetProperty("configuration").GetString(), expectedConfiguration);
            var evidencePath = OwnedFile(ready.RootElement.GetProperty("evidence").GetString(), expectedEvidence);
            using var config = await ReadOwnedJsonAsync(configuration, expectedConfiguration, timeout.Token);
            var runtime = config.RootElement.GetProperty("catalog").GetString()!;
            var migration = config.RootElement.GetProperty("migration").GetString()!;
            var identity = config.RootElement.GetProperty("identity").GetString();
            var recorder = new CityCommandRecorder();
            await CityResolutionTests.VerifyAsync(runtime, migration, recorder);
            var observer = new CityRequestObserver();
            await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
                builder.UseSetting("ConnectionStrings:HappyCity", runtime);
                if (identity is not null) builder.UseSetting("ConnectionStrings:HappyCityIdentity", identity);
                builder.UseSetting("IdentityFoundation:Enabled", (mode == "enabled").ToString());
                builder.ConfigureAppConfiguration((_, configurationBuilder) => configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:HappyCity"] = runtime,
                    ["ConnectionStrings:HappyCityIdentity"] = identity,
                    ["IdentityFoundation:Enabled"] = (mode == "enabled").ToString(),
                    ["AllowedHosts"] = "localhost;127.0.0.1;almere.test;utrecht.test;alias.almere.test;inactive.test;city-inactive.test;no-primary.test;unknown.test"
                }));
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<DbContextOptions<HappyCityDbContext>>();
                    services.RemoveAll<HappyCityDbContext>();
                    services.AddDbContext<HappyCityDbContext>(options => options.UseNpgsql(DatabaseConfiguration.RequireVerifiedTls(runtime)).AddInterceptors(recorder));
                    services.AddSingleton(observer);
                    services.AddScoped<CityObservationFilter>();
                    services.AddControllersWithViews(options => options.Filters.Add<CityObservationFilter>());
                });
                builder.ConfigureLogging(logging => logging.ClearProviders());
            });
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
            async Task<HttpResponseMessage> Send(string host, string path = "/", bool overlap = false)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, path);
                request.Headers.Host = host;
                request.Headers.Add("X-Forwarded-Host", "evil.test");
                request.Headers.Add("Forwarded", "host=evil.test;proto=http");
                request.Headers.Add("Cookie", "city=foreign");
                if (overlap) request.Headers.Add("X-City-Test-Overlap", "true");
                return await client.SendAsync(request);
            }
            var parallel = await Task.WhenAll(Send("almere.test", "/?q=CITY&city=foreign", true), Send("utrecht.test", "/?q=CITY&city=foreign", true));
            for (var i = 0; i < parallel.Length; i++)
            {
                using var response = parallel[i];
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var html = await response.Content.ReadAsStringAsync();
                Assert.Contains(i == 0 ? "CITY-ALMERE-ONLY" : "CITY-UTRECHT-ONLY", html);
                Assert.DoesNotContain(i == 0 ? "CITY-UTRECHT-ONLY" : "CITY-ALMERE-ONLY", html);
                Assert.DoesNotContain("CITY never public", html);
            }
            var overlapScopes = observer.Overlapping.ToArray();
            Assert.Equal(2, overlapScopes.Length);
            Assert.NotEqual(overlapScopes[0].ContextId, overlapScopes[1].ContextId);
            Assert.NotSame(overlapScopes[0].Connection, overlapScopes[1].Connection);
            Assert.NotSame(overlapScopes[0].City, overlapScopes[1].City);
            Assert.NotEqual(overlapScopes[0].City.CityId, overlapScopes[1].City.CityId);
            foreach (var host in new[] { "utrecht.test", "almere.test", "utrecht.test" })
            {
                using var response = await Send(host, "/?q=CITY");
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Contains(host == "almere.test" ? "CITY-ALMERE-ONLY" : "CITY-UTRECHT-ONLY", await response.Content.ReadAsStringAsync());
            }
            using (var alias = await Send("alias.almere.test:9999", "/?q=CITY&returnUrl=https%3A%2F%2Fevil.test&token=synthetic"))
            {
                Assert.Equal(HttpStatusCode.MovedPermanently, alias.StatusCode);
                Assert.Equal("https://utrecht.test/?q=CITY", alias.Headers.Location!.AbsoluteUri);
            }
            foreach (var path in new[] { "/?q=one&q=two", "/?category=a&category=b", "/?cursor=a&cursor=b", "/?q=%00", "/?cursor=bad", "/?q=" + new string('x', 101) })
            {
                using var invalid = await Send("alias.almere.test", path);
                Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
                Assert.Null(invalid.Headers.Location);
            }
            var foreignCursor = DiscoveryCursor.Encode(new DiscoveryFilter("almere", null, "CITY"), 100, null, Guid.Parse("62000000-0000-4000-8000-000000000001"));
            using (var foreign = await Send("utrecht.test", "/?q=CITY&cursor=" + Uri.EscapeDataString(foreignCursor)))
                Assert.Equal(HttpStatusCode.BadRequest, foreign.StatusCode);
            foreach (var host in new[] { "unknown.test", "inactive.test", "city-inactive.test", "no-primary.test" })
            {
                using var rejected = await Send(host);
                Assert.Equal(HttpStatusCode.NotFound, rejected.StatusCode);
            }
            using (var hostile = await Send("evil.test")) Assert.Equal(HttpStatusCode.BadRequest, hostile.StatusCode);
            var count = recorder.Count;
            foreach (var path in new[] { "/Home/Privacy", "/Home/Business", "/Home/Error", "/css/site.css", "/not-found" })
            {
                using var response = await Send("unknown.test", path);
                Assert.Equal(path == "/Home/Error" ? HttpStatusCode.InternalServerError : path == "/not-found" ? HttpStatusCode.NotFound : HttpStatusCode.OK, response.StatusCode);
            }
            Assert.Equal(count, recorder.Count);
            using (var unsafeRequest = new HttpRequestMessage(HttpMethod.Post, "/") { Content = new StringContent("") })
            {
                unsafeRequest.Headers.Host = "alias.almere.test";
                using var response = await client.SendAsync(unsafeRequest);
                Assert.Null(response.Headers.Location);
                Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
            }
            // Reach the actual unchanged limiter; the first rejected request must have no DB command.
            var limited = false;
            for (var attempt = 0; attempt <= 120; attempt++)
            {
                var before = recorder.Count;
                using var response = await Send("almere.test", "/?q=no-synthetic-result");
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    Assert.Equal(before, recorder.Count);
                    limited = true; break;
                }
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(1, recorder.Snapshot().Skip(before).Count(command => command.Kind == "registeredResolver"));
            }
            Assert.True(limited);
            var application = new
            {
                version = 1,
                status = "PASS",
                checks = new[] { "restoredResolverMetadata", "oneResolverCommand", "physicalPositiveAndSqlstateControls", "contextOverlap", "sequentialIsolation", "publicationIsolation", "safeAlias", "invalidAliasInputs", "foreignCursor", "hostRejection", "platformZeroCommands", "unsafeMethodNoRedirect", "rateRejectionZeroCommands" },
                commands = recorder.Snapshot(),
                overlap = new { participants = overlapScopes.Length, distinctScopes = true, distinctConnectionObjects = true, barrierCompleted = true }
            };
            Assert.InRange(application.commands.Length, 1, 2048);
            Assert.All(application.commands, command =>
            {
                Assert.Contains(command.Kind, new[] { "registeredResolver", "localResolver", "discovery" });
                Assert.InRange(command.ParameterCount, 1, 32);
                Assert.Equal(command.ParameterCount, command.ParameterTypes.Length);
                Assert.True(command.ParametersReferenced);
            });
            var applicationJson = JsonSerializer.Serialize(application);
            Assert.InRange(applicationJson.Length, 1, 1048576);
            await process.StandardInput.WriteLineAsync(applicationJson);
            await process.StandardInput.FlushAsync();
            submitted = true;
            await process.WaitForExitAsync(timeout.Token);
            Assert.True(process.ExitCode == 0, "City helper final verification failed; inspect sanitized evidence.");
            using var evidence = await ReadOwnedJsonAsync(evidencePath, expectedEvidence, timeout.Token);
            Assert.Equal(1, evidence.RootElement.GetProperty("schemaVersion").GetInt32());
            Assert.Equal(3, evidence.RootElement.GetProperty("cityEvidenceVersion").GetInt32());
            Assert.Equal(2, evidence.RootElement.GetProperty("diagnosticVersion").GetInt32());
            Assert.Equal(1, evidence.RootElement.GetProperty("applicationEvidenceVersion").GetInt32());
            Assert.Equal("PASS", evidence.RootElement.GetProperty("status").GetString());
            Assert.Equal("PASS", evidence.RootElement.GetProperty("cleanup").GetString());
            Assert.False(evidence.RootElement.GetProperty("cleanupPending").GetBoolean());
        }
        finally
        {
            if (!process.HasExited)
            {
                try
                {
                    if (!submitted) { await process.StandardInput.WriteLineAsync("{\"status\":\"FAIL\"}"); await process.StandardInput.FlushAsync(); }
                    using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(35));
                    await process.WaitForExitAsync(cleanup.Token);
                }
                catch (Exception error) when (error is OperationCanceledException or IOException or InvalidOperationException)
                {
                    if (!process.HasExited) process.Kill(entireProcessTree: true);
                }
            }
            await errors; // Do not publish potentially private process diagnostics.
        }
    }
    private static async Task<JsonDocument> ReadOwnedJsonAsync(string supplied, string expected, CancellationToken cancellationToken)
    {
        var canonical = OwnedFile(supplied, expected);
        await using var stream = OpenReadyStream(canonical);
        // Opening may race a path replacement. Validate the opened object before reading
        // any content, then keep this exact handle through JSON parsing; never reopen by path.
        string resolved;
        if (OperatingSystem.IsWindows())
        {
            var buffer = new System.Text.StringBuilder(32768);
            var length = GetFinalPathNameByHandleW(stream.SafeFileHandle, buffer, (uint)buffer.Capacity, 0);
            if (length == 0 || length >= buffer.Capacity)
                throw new InvalidOperationException("READY opened-file identity unavailable.");
            resolved = buffer.ToString();
            if (resolved.StartsWith(@"\\?\UNC\", StringComparison.Ordinal)) resolved = @"\\" + resolved[8..];
            else if (resolved.StartsWith(@"\\?\", StringComparison.Ordinal)) resolved = resolved[4..];
            else throw new InvalidOperationException("READY opened-file identity format rejected.");
        }
        else if (OperatingSystem.IsLinux())
        {
            // procfs reports the object referenced by this live descriptor, not a
            // second lookup of the READY path. Missing procfs is a closed failure.
            var descriptor = stream.SafeFileHandle.DangerousGetHandle().ToInt64();
            resolved = new FileInfo("/proc/self/fd/" + descriptor.ToString(System.Globalization.CultureInfo.InvariantCulture)).LinkTarget
                ?? throw new InvalidOperationException("READY opened-file identity unavailable.");
        }
        else
        {
            throw new PlatformNotSupportedException("READY opened-file validation requires Windows or Linux.");
        }
        if (!Path.IsPathFullyQualified(resolved) || !ReadyPathsMatch(Path.GetFullPath(resolved), canonical))
            throw new InvalidOperationException("READY opened-file identity rejected.");
        // Recheck links/ancestors as well; subsequent path replacement cannot redirect
        // the already validated stream. This is not protection from same-user content writes.
        OwnedFile(supplied, expected);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    private static FileStream OpenReadyStream(string canonical)
    {
        if (!OperatingSystem.IsLinux())
            return new FileStream(canonical, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        // These Linux open flag values are supported on the x64/arm64 ABI only.
        if (System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture is not
            (System.Runtime.InteropServices.Architecture.X64 or System.Runtime.InteropServices.Architecture.Arm64))
            throw new PlatformNotSupportedException("READY Linux open ABI unavailable.");
        var noFollow = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture ==
            System.Runtime.InteropServices.Architecture.Arm64 ? 0x8000 : 0x20000;
        var readOnlyNonblockingNoFollowCloseOnExec = 0x800 | noFollow | 0x80000;
        var descriptor = LinuxOpen(canonical, readOnlyNonblockingNoFollowCloseOnExec);
        if (descriptor < 0) throw new InvalidOperationException("READY nonblocking open failed.");
        var handle = new Microsoft.Win32.SafeHandles.SafeFileHandle(new IntPtr(descriptor), ownsHandle: true);
        try
        {
            // AT_EMPTY_PATH queries this descriptor, not a second pathname lookup.
            const int emptyPath = 0x1000;
            const uint typeMask = 1;
            if (LinuxStatx(handle, "", emptyPath, typeMask, out var info) != 0 ||
                (info.Mask & typeMask) == 0 || (info.Mode & 0xf000) != 0x8000)
                throw new InvalidOperationException("READY descriptor is not a verified regular file.");
            // FileStream owns the same descriptor. No FIFO read or pathname reopen.
            return new FileStream(handle, FileAccess.Read, 4096, isAsync: false);
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit, Size = 256)]
    private struct LinuxStatxInfo
    {
        [System.Runtime.InteropServices.FieldOffset(0)] public uint Mask;
        [System.Runtime.InteropServices.FieldOffset(28)] public ushort Mode;
    }

    [System.Runtime.Versioning.SupportedOSPlatform("linux")]
    [System.Runtime.InteropServices.DllImport("libc", EntryPoint = "open", SetLastError = true)]
    private static extern int LinuxOpen(
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPUTF8Str)] string path, int flags);

    [System.Runtime.Versioning.SupportedOSPlatform("linux")]
    [System.Runtime.InteropServices.DllImport("libc", EntryPoint = "statx", SetLastError = true)]
    private static extern int LinuxStatx(Microsoft.Win32.SafeHandles.SafeFileHandle descriptor,
        [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPUTF8Str)] string path,
        int flags, uint mask, out LinuxStatxInfo info);

    private static bool ReadyPathsMatch(string actual, string expected)
    {
        if (string.Equals(actual, expected, StringComparison.Ordinal)) return true;
        if (!OperatingSystem.IsWindows()) return false;
        // Do not infer directory case semantics from the OS. Drive/share roots are
        // compared separately; every differing child name uses its actual parent's flag.
        var actualRoot = Path.GetPathRoot(actual)!;
        var expectedRoot = Path.GetPathRoot(expected)!;
        if (!string.Equals(actualRoot, expectedRoot, StringComparison.OrdinalIgnoreCase)) return false;
        var actualParts = actual[actualRoot.Length..].Split(Path.DirectorySeparatorChar);
        var expectedParts = expected[expectedRoot.Length..].Split(Path.DirectorySeparatorChar);
        if (actualParts.Length != expectedParts.Length) return false;
        var parent = expectedRoot;
        for (var index = 0; index < actualParts.Length; index++)
        {
            if (!string.Equals(actualParts[index], expectedParts[index], StringComparison.Ordinal))
            {
                if (!string.Equals(actualParts[index], expectedParts[index], StringComparison.OrdinalIgnoreCase)) return false;
                // FILE_READ_ATTRIBUTES, full sharing, OPEN_EXISTING, BACKUP_SEMANTICS
                // and OPEN_REPARSE_POINT: metadata only; never follow a final junction.
                using var directory = OpenReadyDirectory(parent, 0x80, 7, IntPtr.Zero, 3, 0x02200000, IntPtr.Zero);
                if (directory.IsInvalid || GetReadyDirectoryCase(directory, 23, out var flags, 4) == 0 || (flags & ~1u) != 0)
                    throw new InvalidOperationException("READY directory case semantics unavailable.");
                if ((flags & 1) != 0) return false;
            }
            parent = Path.Combine(parent, expectedParts[index]);
        }
        return true;
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    [System.Runtime.InteropServices.DefaultDllImportSearchPaths(System.Runtime.InteropServices.DllImportSearchPath.System32)]
    [System.Runtime.InteropServices.DllImport("kernel32.dll", EntryPoint = "CreateFileW", CharSet = System.Runtime.InteropServices.CharSet.Unicode,
        ExactSpelling = true, SetLastError = true)]
    private static extern Microsoft.Win32.SafeHandles.SafeFileHandle OpenReadyDirectory(string path,
        uint access, uint sharing, IntPtr security, uint disposition, uint attributes, IntPtr template);

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    [System.Runtime.InteropServices.DefaultDllImportSearchPaths(System.Runtime.InteropServices.DllImportSearchPath.System32)]
    [System.Runtime.InteropServices.DllImport("kernel32.dll", EntryPoint = "GetFileInformationByHandleEx", ExactSpelling = true, SetLastError = true)]
    private static extern int GetReadyDirectoryCase(Microsoft.Win32.SafeHandles.SafeFileHandle directory,
        int informationClass, out uint flags, uint size);

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    [System.Runtime.InteropServices.DefaultDllImportSearchPaths(System.Runtime.InteropServices.DllImportSearchPath.System32)]
    [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode,
        ExactSpelling = true, SetLastError = true)]
    private static extern uint GetFinalPathNameByHandleW(Microsoft.Win32.SafeHandles.SafeFileHandle file,
        System.Text.StringBuilder path, uint capacity, uint flags);

    private static string OwnedFile(string? supplied, string expected)
    {
        // Reject links/junctions in every existing component instead of following them.
        // No caller-selected path is read before equality with the parent-owned path.
        if (supplied is null || !Path.IsPathFullyQualified(supplied))
            throw new InvalidOperationException("READY file path rejected.");
        var canonical = Path.GetFullPath(expected);
        if (!string.Equals(supplied, Path.GetFullPath(supplied), StringComparison.Ordinal) ||
            !ReadyPathsMatch(Path.GetFullPath(supplied), canonical))
            throw new InvalidOperationException("READY file identity rejected.");
        FileSystemInfo component = new FileInfo(canonical);
        if (!component.Exists) throw new InvalidOperationException("READY file missing.");
        while (true)
        {
            if ((component.Attributes & FileAttributes.ReparsePoint) != 0 || component.LinkTarget is not null)
                throw new InvalidOperationException("READY linked path rejected.");
            var parent = component is FileInfo file ? file.Directory : ((DirectoryInfo)component).Parent;
            if (parent is null) break;
            component = parent;
        }
        return canonical;
    }
}

internal sealed class CityRequestObserver
{
    internal readonly ConcurrentQueue<(Guid ContextId, System.Data.Common.DbConnection Connection, ICityContext City)> Overlapping = new();
    internal readonly TaskCompletionSource Barrier = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal int Arrivals;
}

internal sealed class CityObservationFilter(CityRequestObserver observer) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<CityScopedAttribute>() is null ||
            context.HttpContext.Request.Headers["X-City-Test-Overlap"] != "true")
        {
            await next(); return;
        }
        var services = context.HttpContext.RequestServices;
        var city = services.GetRequiredService<ICityContext>();
        Assert.Same(city, services.GetRequiredService<RequestCityContext>());
        var database = services.GetRequiredService<HappyCityDbContext>();
        await database.Database.OpenConnectionAsync(context.HttpContext.RequestAborted);
        try
        {
            observer.Overlapping.Enqueue((database.ContextId.InstanceId, database.Database.GetDbConnection(), city));
            if (Interlocked.Increment(ref observer.Arrivals) == 2) observer.Barrier.TrySetResult();
            await observer.Barrier.Task.WaitAsync(TimeSpan.FromSeconds(10), context.HttpContext.RequestAborted);
            await next();
        }
        finally { await database.Database.CloseConnectionAsync(); }
    }
}
