using Xws.Exec.Cli;

namespace Xws.Exec.Tests;

[Collection("EnvironmentVariables")]
public sealed class SecurityTests
{
    [Fact]
    public void ExecutionCommandHelpers_RedactsSensitiveEnvValues()
    {
        const string secret = "super-secret-value";
        var original = Environment.GetEnvironmentVariable("XWS_OKX_SECRET");
        var originalError = Console.Error;
        try
        {
            Environment.SetEnvironmentVariable("XWS_OKX_SECRET", secret);

            using var writer = new StringWriter();
            Console.SetError(writer);

            ExecutionCommandHelpers.Fail($"boom {secret}", 1);

            var output = writer.ToString();
            Assert.DoesNotContain(secret, output, StringComparison.Ordinal);
            Assert.Contains("[REDACTED]", output, StringComparison.Ordinal);
        }
        finally
        {
            Console.SetError(originalError);
            Environment.SetEnvironmentVariable("XWS_OKX_SECRET", original);
        }
    }
}
