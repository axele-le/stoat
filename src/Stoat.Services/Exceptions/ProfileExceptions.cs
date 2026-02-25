namespace Stoat.Services.Exceptions;

public class ProfileException : Exception
{
    public ProfileException(string message) : base(message) { }
    public ProfileException(string message, Exception inner) : base(message, inner) { }
}

public class ProfileNotFoundException : ProfileException
{
    public Guid ProfileId { get; }

    public ProfileNotFoundException(Guid profileId)
        : base($"Profile with ID {profileId} was not found.")
    {
        ProfileId = profileId;
    }
}

public class NoActiveProfileException : ProfileException
{
    public NoActiveProfileException()
        : base("No profile is currently active. Please select a profile first.") { }
}

public class ProfileStorageException : ProfileException
{
    public ProfileStorageException(string message, Exception inner)
        : base(message, inner) { }
}

public class ExportImportException : ProfileException
{
    public ExportImportException(string message) : base(message) { }
    public ExportImportException(string message, Exception inner) : base(message, inner) { }
}

public class InvalidExportPasswordException : ExportImportException
{
    public InvalidExportPasswordException()
        : base("Invalid password or corrupted export file.") { }
}
