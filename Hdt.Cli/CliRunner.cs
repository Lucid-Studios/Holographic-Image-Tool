using System.Text.Json;
using Hdt.Core;
using Hdt.Core.Services;

namespace Hdt.Cli;

public sealed class CliRunner
{
    private readonly TextWriter _writer;
    private readonly HopngArtifactBuilder _builder = new();
    private readonly HopngArtifactValidator _validator = new();
    private readonly HopngArtifactLoader _loader = new();
    private readonly HopngArtifactInspector _inspector = new();

    public CliRunner(TextWriter writer)
    {
        _writer = writer;
    }

    public int Execute(string[] args)
    {
        if (args.Length == 0)
        {
            WriteUsage();
            return 1;
        }

        var command = args[0].ToLowerInvariant();
        var options = CliOptions.Parse(args.Skip(1).ToArray());

        try
        {
            return command switch
            {
                "new" => CreateArtifact(options),
                "validate" => ValidateArtifact(options),
                "show" => ShowArtifact(options),
                "merge-layers" => Reserved("Merge-HOPNGLayers", 2),
                "render-phase-stack" => Reserved("Render-HOPNGPhaseStack", 3),
                "compare-surfaces" => Reserved("Compare-HOPNGSurfaces", 3),
                "invoke-formation" => Reserved("Invoke-HOPNGFormation", 5),
                "bind-oe" => Reserved("Bind-HOPNGToOE", 6),
                _ => UnknownCommand(command)
            };
        }
        catch (Exception ex)
        {
            Write(new { error = ex.Message }, options.Json);
            return 1;
        }
    }

    private int CreateArtifact(CliOptions options)
    {
        var request = new NewHopngRequest(
            options.Require("output-dir"),
            options.Require("name"),
            options.Get("signer", Environment.UserName),
            options.Get("key-id", "local-dev-key"),
            options.Get("display-name"),
            options.Get("artifact-id"),
            options.Get("private-key"),
            options.Get("private-key-out"),
            options.Get("public-key-out"));

        var artifact = _builder.Create(request);
        Write(new
        {
            artifactId = artifact.Manifest.ArtifactId,
            manifest = artifact.Layout.ManifestPath,
            projection = artifact.Layout.ProjectionPath,
            signature = artifact.Layout.SignaturePath
        }, options.Json);

        return 0;
    }

    private int ValidateArtifact(CliOptions options)
    {
        var result = _validator.Validate(options.Require("path"));
        Write(new { isValid = result.IsValid, errors = result.Errors }, options.Json);
        return result.IsValid ? 0 : (int)result.Errors[0].Code;
    }

    private int ShowArtifact(CliOptions options)
    {
        var path = options.Require("path");
        var view = options.Get("view", "prime");
        var artifact = _loader.Load(path);
        var validation = _validator.Validate(path);
        var payload = string.Equals(view, "privileged", StringComparison.OrdinalIgnoreCase)
            ? _inspector.BuildPrivilegedView(artifact, validation)
            : _inspector.BuildPrimeSafeView(artifact, validation);

        Write(payload, options.Json);
        return validation.IsValid ? 0 : (int)validation.Errors[0].Code;
    }

    private int Reserved(string commandName, int phase)
    {
        _writer.WriteLine(ReservedPhaseCommand.BuildMessage(commandName, phase));
        return 21;
    }

    private int UnknownCommand(string command)
    {
        _writer.WriteLine($"Unknown command '{command}'.");
        WriteUsage();
        return 1;
    }

    private void WriteUsage()
    {
        _writer.WriteLine("Hdt.Cli commands:");
        _writer.WriteLine("  new --output-dir <dir> --name <artifact> [--display-name <text>] [--signer <name>] [--key-id <id>] [--json]");
        _writer.WriteLine("  validate --path <manifest-or-png> [--json]");
        _writer.WriteLine("  show --path <manifest-or-png> [--view prime|privileged] [--json]");
        _writer.WriteLine("  merge-layers | render-phase-stack | compare-surfaces | invoke-formation | bind-oe");
    }

    private void Write(object value, bool asJson)
    {
        if (asJson)
        {
            _writer.WriteLine(JsonSerializer.Serialize(value, JsonDefaults.SerializerOptions));
            return;
        }

        if (value is string text)
        {
            _writer.WriteLine(text);
            return;
        }

        _writer.WriteLine(JsonSerializer.Serialize(value, new JsonSerializerOptions(JsonDefaults.SerializerOptions)
        {
            WriteIndented = true
        }));
    }
}

public sealed class CliOptions
{
    private readonly Dictionary<string, string?> _values;

    private CliOptions(Dictionary<string, string?> values)
    {
        _values = values;
    }

    public bool Json => _values.ContainsKey("json");

    public string Require(string key) =>
        _values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidOperationException($"Missing required option '--{key}'.");

    public string? Get(string key) => _values.TryGetValue(key, out var value) ? value : null;

    public string Get(string key, string fallback) => Get(key) ?? fallback;

    public static CliOptions Parse(string[] args)
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < args.Length; index++)
        {
            var token = args[index];
            if (!token.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            var key = token[2..];
            if (index + 1 < args.Length && !args[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                values[key] = args[index + 1];
                index++;
            }
            else
            {
                values[key] = "true";
            }
        }

        return new CliOptions(values);
    }
}
