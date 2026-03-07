using Hdt.Core.Models;
using Hdt.Core.Security;

namespace Hdt.Core.Services;

public static class TemporalSliceDigestService
{
    public static string ComputeEventSliceDigest(EventSlice slice)
    {
        var payload = new
        {
            slice.EventSliceId,
            slice.ArtifactId,
            slice.N,
            slice.TimestampStartUtc,
            slice.TimestampEndUtc,
            slice.RawStartN,
            slice.RawEndN,
            slice.RawSliceSpan,
            slice.ObservedEventCount,
            slice.UniverseStates,
            slice.ProtectedEvidenceRefs
        };

        return ArtifactHashing.ComputeSha256(CanonicalJson.SerializeToUtf8Bytes(payload));
    }

    public static string ComputePhaseSliceDigest(PhaseSlice slice)
    {
        var payload = new
        {
            slice.PhaseSliceId,
            slice.ArtifactId,
            slice.N,
            slice.TimestampStartUtc,
            slice.TimestampEndUtc,
            slice.SourceEventSliceIds,
            slice.SourceRawStartN,
            slice.SourceRawEndN,
            slice.DeltaHorizon,
            slice.DeltaHorizonMs,
            slice.UniverseStates
        };

        return ArtifactHashing.ComputeSha256(CanonicalJson.SerializeToUtf8Bytes(payload));
    }
}
