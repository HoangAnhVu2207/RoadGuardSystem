namespace RoadGuardSystem.Repositories.Storage;

public static class FileStorageErrorCodes
{
    public const string PathInvalid = "file_path_invalid";
    public const string TooLarge = "file_too_large";
    public const string MimeMismatch = "file_mime_mismatch";
    public const string ChecksumMismatch = "file_checksum_mismatch";
    public const string OwnerNotFound = "file_owner_not_found";
    public const string IdempotencyConflict = "idempotency_key_conflict";
    public const string StorageUnavailable = "file_storage_unavailable";
    public const string ContentInvalid = "file_content_invalid";
    public const string Empty = "file_empty";
    public const string SizeMismatch = "file_size_mismatch";
}
