using AutoSourcing.Core.Entities;
using AutoSourcing.Core.Enums;

namespace AutoSourcing.Services.Outreach;

public interface ISequenceService
{
    Task<List<Sequence>> GetSequencesAsync(CancellationToken cancellationToken = default);
    Task<Sequence?> GetSequenceAsync(int id, CancellationToken cancellationToken = default);
    Task<Sequence> CreateAsync(SaveSequenceRequest request, CancellationToken cancellationToken = default);
    Task<Sequence?> UpdateAsync(int id, SaveSequenceRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    IReadOnlyList<PersonalisationOption> GetPersonalisationOptions();
    Task<SequencePreview> PreviewAsync(int leadId, string? subjectTemplate, string bodyTemplate, OutreachChannel channel, bool includeUnsubscribe, int? jobId = null, CancellationToken cancellationToken = default);
}
