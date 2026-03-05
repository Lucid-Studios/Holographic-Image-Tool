using Hdt.Adapters.Contracts;

namespace Hdt.Adapters.TestDoubles;

public sealed class InMemoryOeBindingAdapter : IOeBindingAdapter
{
    private readonly List<OeBindingRequest> _bindings = [];

    public string Name => "oe-in-memory";

    public IReadOnlyCollection<AdapterCapability> Capabilities =>
    [
        new("oe-binding", "Captures OE binding requests for tests."),
        new("continuity-reference", "Returns deterministic continuity identifiers.")
    ];

    public string Bind(OeBindingRequest request)
    {
        _bindings.Add(request);
        return $"oe://binding/{request.ArtifactId}";
    }

    public IReadOnlyList<OeBindingRequest> Bindings => _bindings;
}
