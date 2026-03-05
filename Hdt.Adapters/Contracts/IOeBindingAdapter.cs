namespace Hdt.Adapters.Contracts;

public interface IOeBindingAdapter
{
    string Name { get; }
    IReadOnlyCollection<AdapterCapability> Capabilities { get; }
    string Bind(OeBindingRequest request);
}
