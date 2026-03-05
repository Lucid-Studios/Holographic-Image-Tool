using System.Security.Cryptography;
using System.Text;
using Hdt.Core.Models;

namespace Hdt.Core.Security;

public static class ArtifactHashing
{
    public static string ComputeSha256(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    public static string ComputeSha256(string path) => ComputeSha256(File.ReadAllBytes(path));

    public static string ComputeArtifactSetSha256(IEnumerable<ArtifactFileDigest> fileDigests, string manifestCanonicalSha256)
    {
        var builder = new StringBuilder();
        builder.Append(manifestCanonicalSha256);
        builder.Append('\n');

        foreach (var digest in fileDigests.OrderBy(item => item.Role, StringComparer.Ordinal).ThenBy(item => item.Path, StringComparer.Ordinal))
        {
            builder.Append(digest.Role)
                .Append('|')
                .Append(digest.Path)
                .Append('|')
                .Append(digest.Sha256)
                .Append('\n');
        }

        return ComputeSha256(Encoding.UTF8.GetBytes(builder.ToString()));
    }
}
