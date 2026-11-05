namespace IncidentReporting.Domain.Exceptions;

public class DuplicateIncidentSourceMessageIdException : DomainException
{
    public string SourceMessageId { get; }

    public DuplicateIncidentSourceMessageIdException(string sourceMessageId)
        : base($"An incident with SourceMessageId '{sourceMessageId}' already exists.")
    {
        SourceMessageId = sourceMessageId;
    }
}
