using xws.Core.Shared.Logging;

namespace xws.tests;

[Collection("EnvironmentVariables")]
public sealed class SecurityTests
{
    [Fact]
    public void Logger_RedactsSensitiveEnvValues()
    {
        const string secret = "super-secret-value";
        var original = Environment.GetEnvironmentVariable("XWS_HL_PRIVATE_KEY");
        try
        {
            Environment.SetEnvironmentVariable("XWS_HL_PRIVATE_KEY", secret);

            var errors = new List<string>();
            Logger.Configure(_ => { }, msg => errors.Add(msg));

            Logger.Error($"boom {secret}");

            Assert.Single(errors);
            Assert.DoesNotContain(secret, errors[0], StringComparison.Ordinal);
            Assert.Contains("[REDACTED]", errors[0], StringComparison.Ordinal);
        }
        finally
        {
            Logger.Configure(_ => { }, _ => { });
            Environment.SetEnvironmentVariable("XWS_HL_PRIVATE_KEY", original);
        }
    }

    [Fact]
    public void Logger_Info_RedactsSensitiveEnvValues()
    {
        const string secret = "super-secret-value";
        var original = Environment.GetEnvironmentVariable("XWS_HL_PRIVATE_KEY");
        try
        {
            Environment.SetEnvironmentVariable("XWS_HL_PRIVATE_KEY", secret);

            var infos = new List<string>();
            Logger.Configure(msg => infos.Add(msg), _ => { });

            Logger.Info($"boom {secret}");

            Assert.Single(infos);
            Assert.DoesNotContain(secret, infos[0], StringComparison.Ordinal);
            Assert.Contains("[REDACTED]", infos[0], StringComparison.Ordinal);
        }
        finally
        {
            Logger.Configure(_ => { }, _ => { });
            Environment.SetEnvironmentVariable("XWS_HL_PRIVATE_KEY", original);
        }
    }
}
