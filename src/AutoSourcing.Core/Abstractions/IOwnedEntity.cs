namespace AutoSourcing.Core.Abstractions;

// An entity that belongs to a single user. Queries are filtered to the current user, and new rows
// are stamped with the current user automatically on save.
public interface IOwnedEntity
{
    int UserId { get; set; }
}
