using NSec.Cryptography;

namespace Hdt.Core.Security;

public sealed class Ed25519SignatureService
{
    private readonly SignatureAlgorithm _algorithm = SignatureAlgorithm.Ed25519;

    public KeyMaterial CreateOrLoad(string? privateKeyPath, string generatedPrivateKeyPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(generatedPrivateKeyPath)!);

        if (!string.IsNullOrWhiteSpace(privateKeyPath))
        {
            var rawPrivateKey = Convert.FromBase64String(File.ReadAllText(privateKeyPath).Trim());
            return Import(rawPrivateKey);
        }

        var creationParameters = new KeyCreationParameters
        {
            ExportPolicy = KeyExportPolicies.AllowPlaintextArchiving
        };

        using var key = new Key(_algorithm, creationParameters);
        var privateBytes = key.Export(KeyBlobFormat.RawPrivateKey);
        var publicBytes = key.PublicKey.Export(KeyBlobFormat.RawPublicKey);

        File.WriteAllText(generatedPrivateKeyPath, Convert.ToBase64String(privateBytes));

        return new KeyMaterial(Convert.ToBase64String(privateBytes), Convert.ToBase64String(publicBytes));
    }

    public byte[] Sign(string privateKeyBase64, byte[] data)
    {
        using var key = Key.Import(_algorithm, Convert.FromBase64String(privateKeyBase64), KeyBlobFormat.RawPrivateKey);
        return _algorithm.Sign(key, data);
    }

    public bool Verify(string publicKeyBase64, byte[] data, byte[] signature)
    {
        var publicKey = PublicKey.Import(_algorithm, Convert.FromBase64String(publicKeyBase64), KeyBlobFormat.RawPublicKey);
        return _algorithm.Verify(publicKey, data, signature);
    }

    private KeyMaterial Import(byte[] rawPrivateKey)
    {
        using var key = Key.Import(_algorithm, rawPrivateKey, KeyBlobFormat.RawPrivateKey);
        var publicBytes = key.PublicKey.Export(KeyBlobFormat.RawPublicKey);
        return new KeyMaterial(Convert.ToBase64String(rawPrivateKey), Convert.ToBase64String(publicBytes));
    }
}

public sealed record KeyMaterial(string PrivateKeyBase64, string PublicKeyBase64);
