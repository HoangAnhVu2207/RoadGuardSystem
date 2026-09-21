namespace RoadGuardSystem.Repositories.Storage;

public sealed class FileStorageException : Exception
{
    public FileStorageException(string code, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
    }

    public string Code { get; }
}
