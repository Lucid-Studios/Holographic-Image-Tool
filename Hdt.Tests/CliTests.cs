using FluentAssertions;
using Hdt.Cli;
using Hdt.Tests.TestSupport;
using System.Text.Json;

namespace Hdt.Tests;

public sealed class CliTests
{
    [Fact]
    public void Cli_New_Validate_And_Show_Work_End_To_End()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var output = new StringWriter();
        var runner = new CliRunner(output);

        var createExitCode = runner.Execute(["new", "--output-dir", tempDir, "--name", "cli-artifact", "--signer", "tester", "--key-id", "cli-key", "--json"]);
        output.GetStringBuilder().Clear();
        var validateExitCode = runner.Execute(["validate", "--path", Path.Combine(tempDir, "cli-artifact.hopng.json"), "--json"]);
        var validateJson = output.ToString();

        output.GetStringBuilder().Clear();
        var showPrimeExitCode = runner.Execute(["show", "--path", Path.Combine(tempDir, "cli-artifact.hopng.json"), "--view", "prime", "--json"]);
        var showPrime = output.ToString();

        output.GetStringBuilder().Clear();
        var showPrivilegedExitCode = runner.Execute(["show", "--path", Path.Combine(tempDir, "cli-artifact.hopng.json"), "--view", "privileged", "--json"]);
        var showPrivileged = output.ToString();

        createExitCode.Should().Be(0);
        validateExitCode.Should().Be(0, validateJson);
        showPrimeExitCode.Should().Be(0);
        showPrivilegedExitCode.Should().Be(0);
        showPrime.Should().Contain("\"view\": \"prime\"");
        showPrivileged.Should().Contain("\"view\": \"privileged\"");
        showPrime.Should().NotContain("\"trustEnvelope\"");
        showPrivileged.Should().Contain("\"trustEnvelope\"");

        var validateDocument = JsonDocument.Parse(validateJson);
        validateDocument.RootElement.GetProperty("isValid").GetBoolean().Should().BeTrue();
    }
}
