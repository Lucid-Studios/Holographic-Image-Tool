namespace Hdt.Adapters.Contracts;

public sealed record OeBindingRequest(
    string ArtifactId,
    ArtifactPointer Pointer,
    string Header,
    string Endcap);
