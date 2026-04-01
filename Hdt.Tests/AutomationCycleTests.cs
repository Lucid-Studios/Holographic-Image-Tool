using FluentAssertions;
using Hdt.Tests.TestSupport;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Hdt.Tests;

public sealed partial class AutomationCycleTests
{
    [Theory]
    [InlineData("Initial", "candidate-ready")]
    [InlineData("Closing", "candidate-ready")]
    [InlineData("Approved", "hitl-required")]
    public void Automation_Cycle_Creates_Receipts_And_Live_State_For_Successful_Run(string developmentPosture, string expectedStatus)
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var auditRoot = Path.Combine(tempDir, "audit");
        var repoChecksHelper = WriteRepoChecksHelper(tempDir, "repo-checks-success.ps1", 0, "Repo checks succeeded.");

        var result = RunPowerShellScript(
            Path.Combine(TestPaths.RepositoryRoot, "scripts", "Invoke-HdtAutomationCycle.ps1"),
            "-DevelopmentPosture", developmentPosture,
            "-ForceDigest",
            "-AuditRoot", auditRoot,
            "-RepoChecksScriptPath", repoChecksHelper);

        result.ExitCode.Should().Be(0, result.ToString());

        var cycleStatePath = Path.Combine(auditRoot, "state", "local-automation-cycle.json");
        var taskingStatusPath = Path.Combine(auditRoot, "state", "local-automation-tasking-status.json");
        var orchestrationStatusPath = Path.Combine(auditRoot, "state", "master-thread-orchestration-status.json");

        File.Exists(cycleStatePath).Should().BeTrue();
        File.Exists(taskingStatusPath).Should().BeTrue();
        File.Exists(orchestrationStatusPath).Should().BeTrue();

        using var cycleState = JsonDocument.Parse(File.ReadAllText(cycleStatePath));
        cycleState.RootElement.GetProperty("status").GetString().Should().Be(expectedStatus);
        cycleState.RootElement.GetProperty("developmentPosture").GetString().Should().Be(developmentPosture);
        cycleState.RootElement.GetProperty("stewardStage").GetString().Should().Be("S1 WitnessSteward");
        cycleState.RootElement.GetProperty("workClassification").GetString().Should().NotBeNullOrWhiteSpace();
        cycleState.RootElement.GetProperty("governanceAction").GetString().Should().NotBeNullOrWhiteSpace();
        cycleState.RootElement.GetProperty("digestDisposition").GetString().Should().Be("emitted");
        cycleState.RootElement.GetProperty("workReportDisposition").GetString().Should().Be("emitted");

        var releaseBundles = Directory.GetDirectories(Path.Combine(auditRoot, "runs", "release-candidates"));
        var digestBundles = Directory.GetDirectories(Path.Combine(auditRoot, "runs", "release-digests"));
        var workReportBundles = Directory.GetDirectories(Path.Combine(auditRoot, "runs", "work-reports"));

        releaseBundles.Should().ContainSingle();
        digestBundles.Should().ContainSingle();
        workReportBundles.Should().ContainSingle();

        var manifestPath = Path.Combine(releaseBundles[0], "build-evidence-manifest.json");
        var summaryPath = Path.Combine(releaseBundles[0], "build-evidence-summary.md");
        var repoChecksReceiptPath = Path.Combine(releaseBundles[0], "repo-checks-receipt.json");
        var gitWorktreeReceiptPath = Path.Combine(releaseBundles[0], "git-worktree-receipt.json");
        var dopingHeaderJsonPath = Path.Combine(releaseBundles[0], "doping-header.json");
        var receiptJsonPath = Path.Combine(releaseBundles[0], "receipt.json");
        var noticeJsonPath = Path.Combine(releaseBundles[0], "notice.json");
        var digestJsonPath = Path.Combine(digestBundles[0], "release-candidate-digest.json");
        var digestMarkdownPath = Path.Combine(digestBundles[0], "release-candidate-digest.md");
        var workReportJsonPath = Path.Combine(workReportBundles[0], "work-report.json");
        var workReportMarkdownPath = Path.Combine(workReportBundles[0], "work-report.md");

        File.Exists(manifestPath).Should().BeTrue();
        File.Exists(summaryPath).Should().BeTrue();
        File.Exists(repoChecksReceiptPath).Should().BeTrue();
        File.Exists(gitWorktreeReceiptPath).Should().BeTrue();
        File.Exists(dopingHeaderJsonPath).Should().BeTrue();
        File.Exists(receiptJsonPath).Should().BeTrue();
        File.Exists(noticeJsonPath).Should().BeTrue();
        File.Exists(digestJsonPath).Should().BeTrue();
        File.Exists(digestMarkdownPath).Should().BeTrue();
        File.Exists(workReportJsonPath).Should().BeTrue();
        File.Exists(workReportMarkdownPath).Should().BeTrue();

        using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
        manifest.RootElement.GetProperty("status").GetString().Should().Be(expectedStatus);
        manifest.RootElement.GetProperty("developmentPosture").GetString().Should().Be(developmentPosture);
        manifest.RootElement.GetProperty("stewardStage").GetString().Should().Be("S1 WitnessSteward");
        manifest.RootElement.GetProperty("repoChecks").GetProperty("exitCode").GetInt32().Should().Be(0);
        manifest.RootElement.GetProperty("workReport").GetProperty("emitted").GetBoolean().Should().BeTrue();

        using var repoChecksReceipt = JsonDocument.Parse(File.ReadAllText(repoChecksReceiptPath));
        repoChecksReceipt.RootElement.GetProperty("succeeded").GetBoolean().Should().BeTrue();
        repoChecksReceipt.RootElement.GetProperty("exitCode").GetInt32().Should().Be(0);

        using var digest = JsonDocument.Parse(File.ReadAllText(digestJsonPath));
        digest.RootElement.GetProperty("status").GetString().Should().Be(expectedStatus);
        digest.RootElement.GetProperty("hitlStillRequired").GetBoolean().Should().Be(expectedStatus == "hitl-required");
        if (expectedStatus == "hitl-required")
        {
            digest.RootElement.GetProperty("note").GetString().Should().Contain("explicit HITL approval is still required");
        }

        using var notice = JsonDocument.Parse(File.ReadAllText(noticeJsonPath));
        notice.RootElement.GetProperty("activeHolds").ValueKind.Should().Be(JsonValueKind.Array);
        notice.RootElement.GetProperty("activeHolds").EnumerateArray()
            .Select(item => item.ValueKind)
            .Should()
            .OnlyContain(kind => kind == JsonValueKind.String);

        using var workReport = JsonDocument.Parse(File.ReadAllText(workReportJsonPath));
        workReport.RootElement.GetProperty("activeHolds").ValueKind.Should().Be(JsonValueKind.Array);
        workReport.RootElement.GetProperty("activeHolds").EnumerateArray()
            .Select(item => item.ValueKind)
            .Should()
            .OnlyContain(kind => kind == JsonValueKind.String);
    }

    [Fact]
    public void Automation_Cycle_Preserves_Latest_Digest_And_Work_Report_References_When_Not_Due()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var auditRoot = Path.Combine(tempDir, "audit");
        var repoChecksHelper = WriteRepoChecksHelper(tempDir, "repo-checks-success.ps1", 0, "Repo checks succeeded.");

        var firstResult = RunPowerShellScript(
            Path.Combine(TestPaths.RepositoryRoot, "scripts", "Invoke-HdtAutomationCycle.ps1"),
            "-DevelopmentPosture", "Closing",
            "-ForceDigest",
            "-AuditRoot", auditRoot,
            "-RepoChecksScriptPath", repoChecksHelper);

        firstResult.ExitCode.Should().Be(0, firstResult.ToString());

        var secondResult = RunPowerShellScript(
            Path.Combine(TestPaths.RepositoryRoot, "scripts", "Invoke-HdtAutomationCycle.ps1"),
            "-DevelopmentPosture", "Closing",
            "-AuditRoot", auditRoot,
            "-RepoChecksScriptPath", repoChecksHelper);

        secondResult.ExitCode.Should().Be(0, secondResult.ToString());

        var cycleStatePath = Path.Combine(auditRoot, "state", "local-automation-cycle.json");
        var taskingStatusPath = Path.Combine(auditRoot, "state", "local-automation-tasking-status.json");

        using var cycleState = JsonDocument.Parse(File.ReadAllText(cycleStatePath));
        using var taskingState = JsonDocument.Parse(File.ReadAllText(taskingStatusPath));

        cycleState.RootElement.GetProperty("digestDisposition").GetString().Should().Be("skipped-not-due");
        cycleState.RootElement.GetProperty("workReportDisposition").GetString().Should().Be("skipped-not-due");

        var lastDigestBundlePath = cycleState.RootElement.GetProperty("lastDigestBundlePath").GetString();
        var lastWorkReportBundlePath = cycleState.RootElement.GetProperty("lastWorkReportBundlePath").GetString();

        lastDigestBundlePath.Should().NotBeNullOrWhiteSpace();
        lastWorkReportBundlePath.Should().NotBeNullOrWhiteSpace();

        var tasks = taskingState.RootElement.GetProperty("tasks").EnumerateArray().ToArray();
        var workReportingTask = tasks.Single(task => task.GetProperty("id").GetString() == "work-reporting");
        var digestSurfaceTask = tasks.Single(task => task.GetProperty("id").GetString() == "digest-surface");

        workReportingTask.GetProperty("status").GetString().Should().Be("not-due");
        workReportingTask.GetProperty("latestBundlePath").GetString().Should().Be(lastWorkReportBundlePath);
        workReportingTask.GetProperty("latestReceiptPath").GetString().Should().Be(Path.Combine(lastWorkReportBundlePath!, "work-report.json"));

        digestSurfaceTask.GetProperty("status").GetString().Should().Be("not-due");
        digestSurfaceTask.GetProperty("latestBundlePath").GetString().Should().Be(lastDigestBundlePath);
        digestSurfaceTask.GetProperty("latestReceiptPath").GetString().Should().Be(Path.Combine(lastDigestBundlePath!, "release-candidate-digest.json"));
    }

    [Fact]
    public void Automation_Cycle_Produces_Blocked_State_And_Failure_Receipts_When_Repo_Checks_Fail()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var auditRoot = Path.Combine(tempDir, "audit");
        var repoChecksHelper = WriteRepoChecksHelper(tempDir, "repo-checks-failure.ps1", 42, "Repo checks failed intentionally.");

        var result = RunPowerShellScript(
            Path.Combine(TestPaths.RepositoryRoot, "scripts", "Invoke-HdtAutomationCycle.ps1"),
            "-DevelopmentPosture", "Closing",
            "-ForceDigest",
            "-AuditRoot", auditRoot,
            "-RepoChecksScriptPath", repoChecksHelper);

        result.ExitCode.Should().Be(1, result.ToString());

        var cycleStatePath = Path.Combine(auditRoot, "state", "local-automation-cycle.json");
        var manifestPath = Path.Combine(Directory.GetDirectories(Path.Combine(auditRoot, "runs", "release-candidates")).Single(), "build-evidence-manifest.json");
        var repoChecksReceiptPath = Path.Combine(Directory.GetDirectories(Path.Combine(auditRoot, "runs", "release-candidates")).Single(), "repo-checks-receipt.json");

        using var cycleState = JsonDocument.Parse(File.ReadAllText(cycleStatePath));
        cycleState.RootElement.GetProperty("status").GetString().Should().Be("blocked");
        cycleState.RootElement.GetProperty("repoChecksExitCode").GetInt32().Should().Be(42);

        using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
        manifest.RootElement.GetProperty("status").GetString().Should().Be("blocked");
        manifest.RootElement.GetProperty("failureReasons").EnumerateArray().Select(item => item.GetString()).Should().Contain(reason => reason!.Contains("Repo checks did not complete successfully."));

        using var repoChecksReceipt = JsonDocument.Parse(File.ReadAllText(repoChecksReceiptPath));
        repoChecksReceipt.RootElement.GetProperty("exitCode").GetInt32().Should().Be(42);
        repoChecksReceipt.RootElement.GetProperty("succeeded").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public void Automation_Cycle_Blocks_Invalid_Audit_Root_Without_Emitting_A_Green_Manifest()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var invalidAuditRoot = Path.Combine(tempDir, "invalid-audit-root.txt");
        File.WriteAllText(invalidAuditRoot, "not a directory");
        var repoChecksHelper = WriteRepoChecksHelper(tempDir, "repo-checks-success.ps1", 0, "Repo checks succeeded.");

        var result = RunPowerShellScript(
            Path.Combine(TestPaths.RepositoryRoot, "scripts", "Invoke-HdtAutomationCycle.ps1"),
            "-DevelopmentPosture", "Initial",
            "-ForceDigest",
            "-AuditRoot", invalidAuditRoot,
            "-RepoChecksScriptPath", repoChecksHelper);

        result.ExitCode.Should().Be(1, result.ToString());
        result.StandardOutput.Should().Contain("Status: blocked");
        result.StandardOutput.Should().Contain("Audit root fallback:");

        Directory.Exists(Path.Combine(invalidAuditRoot, "runs", "release-candidates")).Should().BeFalse();

        var fallbackRootMatch = AuditRootFallbackRegex().Match(result.StandardOutput);
        fallbackRootMatch.Success.Should().BeTrue(result.StandardOutput);
        var fallbackRoot = fallbackRootMatch.Groups["path"].Value.Trim();
        var cycleStatePath = Path.Combine(fallbackRoot, "state", "local-automation-cycle.json");
        File.Exists(cycleStatePath).Should().BeTrue();

        using var cycleState = JsonDocument.Parse(File.ReadAllText(cycleStatePath));
        cycleState.RootElement.GetProperty("status").GetString().Should().Be("blocked");
        cycleState.RootElement.GetProperty("usedAuditRootFallback").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public void Automation_Status_Wrapper_Renders_Text_And_Json_For_Live_Audit_State()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var auditRoot = Path.Combine(tempDir, "audit");
        var repoChecksHelper = WriteRepoChecksHelper(tempDir, "repo-checks-success.ps1", 0, "Repo checks succeeded.");

        var cycleResult = RunPowerShellScript(
            Path.Combine(TestPaths.RepositoryRoot, "scripts", "Invoke-HdtAutomationCycle.ps1"),
            "-DevelopmentPosture", "Closing",
            "-ForceDigest",
            "-AuditRoot", auditRoot,
            "-RepoChecksScriptPath", repoChecksHelper);

        cycleResult.ExitCode.Should().Be(0, cycleResult.ToString());

        var textResult = RunPowerShellScript(
            Path.Combine(TestPaths.RepositoryRoot, "Show-HDTAutomationStatus.ps1"),
            "-View", "all",
            "-AuditRoot", auditRoot);

        textResult.ExitCode.Should().Be(0, textResult.ToString());
        textResult.StandardOutput.Should().Contain("HDT Automation Summary");
        textResult.StandardOutput.Should().Contain("HDT Automation Tasking");
        textResult.StandardOutput.Should().Contain("HDT Automation Orchestration");
        textResult.StandardOutput.Should().Contain("Status: candidate-ready");
        textResult.StandardOutput.Should().Contain("Current observed git worktree state:");

        var jsonResult = RunPowerShellScript(
            Path.Combine(TestPaths.RepositoryRoot, "Show-HDTAutomationStatus.ps1"),
            "-View", "all",
            "-Json",
            "-AuditRoot", auditRoot);

        jsonResult.ExitCode.Should().Be(0, jsonResult.ToString());

        using var payload = JsonDocument.Parse(jsonResult.StandardOutput);
        payload.RootElement.GetProperty("summary").GetProperty("status").GetString().Should().Be("candidate-ready");
        payload.RootElement.GetProperty("tasking").GetProperty("tasks").GetArrayLength().Should().BeGreaterThan(0);
        payload.RootElement.GetProperty("orchestration").GetProperty("publishReady").ValueKind.Should().BeOneOf(JsonValueKind.True, JsonValueKind.False);
        payload.RootElement.GetProperty("currentObservation").GetProperty("worktreeState").GetString().Should().NotBeNullOrWhiteSpace();
        payload.RootElement.GetProperty("currentObservation").GetProperty("emittedWorktreeState").GetString()
            .Should().Be(payload.RootElement.GetProperty("orchestration").GetProperty("worktreeState").GetString());
        payload.RootElement.GetProperty("currentObservation").GetProperty("divergesFromEmittedState").ValueKind
            .Should().BeOneOf(JsonValueKind.True, JsonValueKind.False);
    }

    [Fact]
    public void Automation_Status_Wrapper_Fails_Cleanly_When_State_Is_Missing()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var auditRoot = Path.Combine(tempDir, "missing-audit");

        var result = RunPowerShellScript(
            Path.Combine(TestPaths.RepositoryRoot, "Show-HDTAutomationStatus.ps1"),
            "-View", "all",
            "-AuditRoot", auditRoot);

        result.ExitCode.Should().Be(1, result.ToString());
        result.StandardError.Should().Contain("Required automation state file was not found");
    }

    [Fact]
    public void Automation_Receipt_Wrapper_Renders_Text_And_Json_For_Latest_Receipts()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var auditRoot = Path.Combine(tempDir, "audit");
        var repoChecksHelper = WriteRepoChecksHelper(tempDir, "repo-checks-success.ps1", 0, "Repo checks succeeded.");

        var cycleResult = RunPowerShellScript(
            Path.Combine(TestPaths.RepositoryRoot, "scripts", "Invoke-HdtAutomationCycle.ps1"),
            "-DevelopmentPosture", "Closing",
            "-ForceDigest",
            "-AuditRoot", auditRoot,
            "-RepoChecksScriptPath", repoChecksHelper);

        cycleResult.ExitCode.Should().Be(0, cycleResult.ToString());

        var textResult = RunPowerShellScript(
            Path.Combine(TestPaths.RepositoryRoot, "Show-HDTAutomationReceipt.ps1"),
            "-View", "all",
            "-AuditRoot", auditRoot);

        textResult.ExitCode.Should().Be(0, textResult.ToString());
        textResult.StandardOutput.Should().Contain("HDT Automation Bundle");
        textResult.StandardOutput.Should().Contain("HDT Automation Digest");
        textResult.StandardOutput.Should().Contain("Status: candidate-ready");

        var jsonResult = RunPowerShellScript(
            Path.Combine(TestPaths.RepositoryRoot, "Show-HDTAutomationReceipt.ps1"),
            "-View", "all",
            "-Json",
            "-AuditRoot", auditRoot);

        jsonResult.ExitCode.Should().Be(0, jsonResult.ToString());

        using var payload = JsonDocument.Parse(jsonResult.StandardOutput);
        payload.RootElement.GetProperty("bundle").GetProperty("manifest").GetProperty("status").GetString().Should().Be("candidate-ready");
        payload.RootElement.GetProperty("digest").GetProperty("receipt").GetProperty("status").GetString().Should().Be("candidate-ready");
    }

    [Fact]
    public void Automation_Receipt_Wrapper_Can_Target_Explicit_Bundle_Id_And_Fails_When_Missing()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var auditRoot = Path.Combine(tempDir, "audit");
        var repoChecksHelper = WriteRepoChecksHelper(tempDir, "repo-checks-success.ps1", 0, "Repo checks succeeded.");

        var cycleResult = RunPowerShellScript(
            Path.Combine(TestPaths.RepositoryRoot, "scripts", "Invoke-HdtAutomationCycle.ps1"),
            "-DevelopmentPosture", "Closing",
            "-ForceDigest",
            "-AuditRoot", auditRoot,
            "-RepoChecksScriptPath", repoChecksHelper);

        cycleResult.ExitCode.Should().Be(0, cycleResult.ToString());

        var cycleStatePath = Path.Combine(auditRoot, "state", "local-automation-cycle.json");
        using var cycleState = JsonDocument.Parse(File.ReadAllText(cycleStatePath));
        var bundleId = cycleState.RootElement.GetProperty("lastBundleId").GetString();

        var byIdResult = RunPowerShellScript(
            Path.Combine(TestPaths.RepositoryRoot, "Show-HDTAutomationReceipt.ps1"),
            "-View", "bundle",
            "-Json",
            "-AuditRoot", auditRoot,
            "-BundleId", bundleId!);

        byIdResult.ExitCode.Should().Be(0, byIdResult.ToString());

        using var byIdPayload = JsonDocument.Parse(byIdResult.StandardOutput);
        byIdPayload.RootElement.GetProperty("bundle").GetProperty("manifest").GetProperty("bundleId").GetString().Should().Be(bundleId);

        var missingResult = RunPowerShellScript(
            Path.Combine(TestPaths.RepositoryRoot, "Show-HDTAutomationReceipt.ps1"),
            "-View", "bundle",
            "-AuditRoot", auditRoot,
            "-BundleId", "missing-bundle-id");

        missingResult.ExitCode.Should().Be(1, missingResult.ToString());
        missingResult.StandardError.Should().Contain("Required automation receipt file was not found");
    }

    [Fact]
    public void Automation_Status_Wrapper_Reports_Divergence_When_Current_Git_View_Differs_From_Emitted_State()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var auditRoot = Path.Combine(tempDir, "audit");
        var repoChecksHelper = WriteRepoChecksHelper(tempDir, "repo-checks-success.ps1", 0, "Repo checks succeeded.");

        var cycleResult = RunPowerShellScript(
            Path.Combine(TestPaths.RepositoryRoot, "scripts", "Invoke-HdtAutomationCycle.ps1"),
            "-DevelopmentPosture", "Closing",
            "-ForceDigest",
            "-AuditRoot", auditRoot,
            "-RepoChecksScriptPath", repoChecksHelper);

        cycleResult.ExitCode.Should().Be(0, cycleResult.ToString());

        var orchestrationStatusPath = Path.Combine(auditRoot, "state", "master-thread-orchestration-status.json");
        var orchestrationNode = JsonNode.Parse(File.ReadAllText(orchestrationStatusPath))!.AsObject();
        var emittedWorktreeState = orchestrationNode["worktreeState"]!.GetValue<string>();
        var requiredWorktreeState = orchestrationNode["requiredWorktreeState"]!.GetValue<string>();
        var flippedWorktreeState = emittedWorktreeState == "clean" ? "dirty" : "clean";
        var branchAligned = orchestrationNode["branchAligned"]!.GetValue<bool>();
        var worktreeAligned = flippedWorktreeState == requiredWorktreeState;
        var publishReady = branchAligned && worktreeAligned;

        orchestrationNode["worktreeState"] = flippedWorktreeState;
        orchestrationNode["worktreeAligned"] = worktreeAligned;
        orchestrationNode["publishReady"] = publishReady;

        var reasons = new JsonArray();
        if (!worktreeAligned)
        {
            reasons.Add($"Current worktree state '{flippedWorktreeState}' does not match required worktree state '{requiredWorktreeState}'.");
        }

        orchestrationNode["reasons"] = reasons;
        File.WriteAllText(
            orchestrationStatusPath,
            orchestrationNode.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

        var jsonResult = RunPowerShellScript(
            Path.Combine(TestPaths.RepositoryRoot, "Show-HDTAutomationStatus.ps1"),
            "-View", "all",
            "-Json",
            "-AuditRoot", auditRoot);

        jsonResult.ExitCode.Should().Be(0, jsonResult.ToString());

        using var payload = JsonDocument.Parse(jsonResult.StandardOutput);
        var currentObservation = payload.RootElement.GetProperty("currentObservation");
        currentObservation.GetProperty("divergesFromEmittedState").GetBoolean().Should().BeTrue();
        currentObservation.GetProperty("emittedWorktreeState").GetString().Should().Be(flippedWorktreeState);
        currentObservation.GetProperty("note").GetString().Should().Contain("differs from the last emitted .audit orchestration surface");
    }

    [Fact]
    public void Automation_Receipt_Wrapper_Reports_Work_Report_Emission_Truthfully_When_Not_Due()
    {
        var tempDir = TestPaths.CreateTempDirectory();
        var auditRoot = Path.Combine(tempDir, "audit");
        var repoChecksHelper = WriteRepoChecksHelper(tempDir, "repo-checks-success.ps1", 0, "Repo checks succeeded.");

        var firstCycleResult = RunPowerShellScript(
            Path.Combine(TestPaths.RepositoryRoot, "scripts", "Invoke-HdtAutomationCycle.ps1"),
            "-DevelopmentPosture", "Closing",
            "-ForceDigest",
            "-AuditRoot", auditRoot,
            "-RepoChecksScriptPath", repoChecksHelper);

        firstCycleResult.ExitCode.Should().Be(0, firstCycleResult.ToString());

        var secondCycleResult = RunPowerShellScript(
            Path.Combine(TestPaths.RepositoryRoot, "scripts", "Invoke-HdtAutomationCycle.ps1"),
            "-DevelopmentPosture", "Closing",
            "-AuditRoot", auditRoot,
            "-RepoChecksScriptPath", repoChecksHelper);

        secondCycleResult.ExitCode.Should().Be(0, secondCycleResult.ToString());

        var bundleTextResult = RunPowerShellScript(
            Path.Combine(TestPaths.RepositoryRoot, "Show-HDTAutomationReceipt.ps1"),
            "-View", "bundle",
            "-AuditRoot", auditRoot);

        bundleTextResult.ExitCode.Should().Be(0, bundleTextResult.ToString());
        bundleTextResult.StandardOutput.Should().Contain("Work report emitted: False");

        var bundleJsonResult = RunPowerShellScript(
            Path.Combine(TestPaths.RepositoryRoot, "Show-HDTAutomationReceipt.ps1"),
            "-View", "bundle",
            "-Json",
            "-AuditRoot", auditRoot);

        bundleJsonResult.ExitCode.Should().Be(0, bundleJsonResult.ToString());

        using var payload = JsonDocument.Parse(bundleJsonResult.StandardOutput);
        payload.RootElement.GetProperty("bundle").GetProperty("manifest").GetProperty("workReport").GetProperty("emitted").GetBoolean().Should().BeFalse();
    }

    private static string WriteRepoChecksHelper(string tempDir, string fileName, int exitCode, string message)
    {
        var helperPath = Path.Combine(tempDir, fileName);
        var script = $$"""
        param(
            [string]$DevelopmentPosture
        )

        Write-Host "{{message}}"
        Write-Host "Development posture: $DevelopmentPosture"
        exit {{exitCode}}
        """;

        File.WriteAllText(helperPath, script);
        return helperPath;
    }

    private static PowerShellRunResult RunPowerShellScript(string scriptPath, params string[] args)
    {
        var startInfo = new ProcessStartInfo("powershell.exe")
        {
            WorkingDirectory = TestPaths.RepositoryRoot,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(scriptPath);

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = Process.Start(startInfo)!;
        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return new PowerShellRunResult(process.ExitCode, standardOutput, standardError);
    }

    [GeneratedRegex(@"Audit root fallback:\s*(?<path>.+)$", RegexOptions.Multiline)]
    private static partial Regex AuditRootFallbackRegex();

    private sealed record PowerShellRunResult(int ExitCode, string StandardOutput, string StandardError)
    {
        public override string ToString() => $"ExitCode: {ExitCode}{Environment.NewLine}STDOUT:{Environment.NewLine}{StandardOutput}{Environment.NewLine}STDERR:{Environment.NewLine}{StandardError}";
    }
}
