using System.Reflection;

namespace Hdt.Schemas;

public static class SchemaCatalog
{
    private static readonly IReadOnlyDictionary<string, SchemaDefinition> Definitions =
        new Dictionary<string, SchemaDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            [BuildKey("oan.hopng_manifest", "0.1.0")] = new(
                "oan.hopng_manifest",
                "0.1.0",
                "Hdt.Schemas.Schemas.oan.hopng_manifest.v0.1.0.json",
                true,
                1,
                ["schema", "schemaVersion", "artifactId", "projectionFile", "sidecars", "fileDigests", "visibilityPolicy"]),
            [BuildKey("oan.hopng_layer_map", "0.1.0")] = new(
                "oan.hopng_layer_map",
                "0.1.0",
                "Hdt.Schemas.Schemas.oan.hopng_layer_map.v0.1.0.json",
                true,
                1,
                ["schema", "schemaVersion", "artifactId", "layers"]),
            [BuildKey("oan.hopng_trust_envelope", "0.1.0")] = new(
                "oan.hopng_trust_envelope",
                "0.1.0",
                "Hdt.Schemas.Schemas.oan.hopng_trust_envelope.v0.1.0.json",
                true,
                1,
                ["schema", "schemaVersion", "artifactId", "signer", "keyId", "publicKey", "signingScope"]),
            [BuildKey("oan.hopng_transform_history", "0.1.0")] = new(
                "oan.hopng_transform_history",
                "0.1.0",
                "Hdt.Schemas.Schemas.oan.hopng_transform_history.v0.1.0.json",
                true,
                1,
                ["schema", "schemaVersion", "artifactId", "transforms"]),
            [BuildKey("oan.hopng_depth_field", "0.1.0")] = new(
                "oan.hopng_depth_field",
                "0.1.0",
                "Hdt.Schemas.Schemas.oan.hopng_depth_field.v0.1.0.json",
                true,
                1,
                ["schema", "schemaVersion", "artifactId", "planes"]),
            [BuildKey("oan.hopng_universe_layer", "0.1.0")] = new(
                "oan.hopng_universe_layer",
                "0.1.0",
                "Hdt.Schemas.Schemas.oan.hopng_universe_layer.v0.1.0.json",
                true,
                2,
                ["schema", "schemaVersion", "artifactId", "universes"]),
            [BuildKey("oan.hopng_gluing_manifest", "0.1.0")] = new(
                "oan.hopng_gluing_manifest",
                "0.1.0",
                "Hdt.Schemas.Schemas.oan.hopng_gluing_manifest.v0.1.0.json",
                true,
                2,
                ["schema", "schemaVersion", "artifactId", "relations"]),
            [BuildKey("oan.hopng_projection_rules", "0.1.0")] = new(
                "oan.hopng_projection_rules",
                "0.1.0",
                "Hdt.Schemas.Schemas.oan.hopng_projection_rules.v0.1.0.json",
                true,
                2,
                ["schema", "schemaVersion", "artifactId", "rules"]),
            [BuildKey("oan.hopng_legibility_profile", "0.1.0")] = new(
                "oan.hopng_legibility_profile",
                "0.1.0",
                "Hdt.Schemas.Schemas.oan.hopng_legibility_profile.v0.1.0.json",
                true,
                2,
                ["schema", "schemaVersion", "artifactId", "requiredUniverses", "requiredRelations", "projectionIntegrityRequired"]),
            [BuildKey("oan.hopng_event_slice", "0.1.0")] = new("oan.hopng_event_slice", "0.1.0", string.Empty, false, 3, Array.Empty<string>()),
            [BuildKey("oan.hopng_phase_slice", "0.1.0")] = new("oan.hopng_phase_slice", "0.1.0", string.Empty, false, 3, Array.Empty<string>()),
            [BuildKey("oan.hopng_phase_policy", "0.1.0")] = new("oan.hopng_phase_policy", "0.1.0", string.Empty, false, 3, Array.Empty<string>()),
            [BuildKey("oan.hopng_optical_channels", "0.1.0")] = new("oan.hopng_optical_channels", "0.1.0", string.Empty, false, 3, Array.Empty<string>()),
            [BuildKey("oan.hopng_perspectival_engram", "0.1.0")] = new("oan.hopng_perspectival_engram", "0.1.0", string.Empty, false, 4, Array.Empty<string>()),
            [BuildKey("oan.hopng_participatory_engram", "0.1.0")] = new("oan.hopng_participatory_engram", "0.1.0", string.Empty, false, 4, Array.Empty<string>()),
            [BuildKey("oan.hopng_peral_universe_set", "0.1.0")] = new("oan.hopng_peral_universe_set", "0.1.0", string.Empty, false, 4, Array.Empty<string>()),
            [BuildKey("oan.hopng_modal_transform", "0.1.0")] = new("oan.hopng_modal_transform", "0.1.0", string.Empty, false, 4, Array.Empty<string>()),
            [BuildKey("oan.hopng_formation_contract", "0.1.0")] = new("oan.hopng_formation_contract", "0.1.0", string.Empty, false, 5, Array.Empty<string>()),
            [BuildKey("oan.identity_review_delta", "0.1.0")] = new("oan.identity_review_delta", "0.1.0", string.Empty, false, 5, Array.Empty<string>()),
            [BuildKey("oan.cert_acceptance_record", "0.1.0")] = new("oan.cert_acceptance_record", "0.1.0", string.Empty, false, 5, Array.Empty<string>()),
            [BuildKey("oan.module_self_base_set", "0.1.0")] = new("oan.module_self_base_set", "0.1.0", string.Empty, false, 5, Array.Empty<string>()),
            [BuildKey("oan.commitment_projection", "0.1.0")] = new("oan.commitment_projection", "0.1.0", string.Empty, false, 5, Array.Empty<string>()),
            [BuildKey("oan.role_glyphic_act", "0.1.0")] = new("oan.role_glyphic_act", "0.1.0", string.Empty, false, 6, Array.Empty<string>()),
            [BuildKey("oan.oe_chain_boundary", "0.1.0")] = new("oan.oe_chain_boundary", "0.1.0", string.Empty, false, 6, Array.Empty<string>()),
            [BuildKey("oan.hopng_role_header", "0.1.0")] = new("oan.hopng_role_header", "0.1.0", string.Empty, false, 6, Array.Empty<string>()),
            [BuildKey("oan.hopng_role_endcap", "0.1.0")] = new("oan.hopng_role_endcap", "0.1.0", string.Empty, false, 6, Array.Empty<string>())
        };

    public static IReadOnlyCollection<SchemaDefinition> All => Definitions.Values.ToArray();

    public static bool TryGet(string logicalName, string schemaVersion, out SchemaDefinition definition) =>
        Definitions.TryGetValue(BuildKey(logicalName, schemaVersion), out definition!);

    public static string GetSchemaText(SchemaDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(definition.ResourceName))
        {
            throw new InvalidOperationException(
                $"Schema '{definition.LogicalName}' version '{definition.SchemaVersion}' is reserved for a later phase.");
        }

        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(definition.ResourceName)
            ?? throw new InvalidOperationException($"Embedded schema resource '{definition.ResourceName}' was not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string BuildKey(string logicalName, string schemaVersion) => $"{logicalName}@{schemaVersion}";
}
