namespace Hdt.Core.Validation;

public enum ValidationErrorCode
{
    MissingFile = 10,
    MissingSidecar = 11,
    SchemaMismatch = 12,
    DigestMismatch = 13,
    HashMismatch = 14,
    SignatureMismatch = 15,
    InvalidLayerMap = 16,
    InvalidVisibilityPolicy = 17,
    UnsupportedSchema = 18,
    InvalidJson = 19
}
