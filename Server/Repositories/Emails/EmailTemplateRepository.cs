using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Entities;

namespace Server.Repositories.Emails;

public class EmailTemplateRepository(PrmDbContext context) : IEmailTemplateRepository
{
    public Task<EmailTemplate?> GetByKeyAsync(string templateKey, CancellationToken cancellationToken = default) =>
        context.EmailTemplates.FirstOrDefaultAsync(t => t.TemplateKey == templateKey, cancellationToken);
}
