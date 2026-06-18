using Server.Models.Entities;

namespace Server.Repositories.Emails;

public interface IEmailTemplateRepository
{
    Task<EmailTemplate?> GetByKeyAsync(string templateKey, CancellationToken cancellationToken = default);
}
