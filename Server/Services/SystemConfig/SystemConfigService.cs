using Microsoft.Extensions.Logging;
using Server.AI.Configuration;
using Server.Common;
using Server.Common.Audit;
using Server.Common.Timesheets;
using Server.Exceptions;
using Server.Models.DTOs.SystemConfig;
using Server.Services.Shared;

namespace Server.Services.SystemConfig;

public class SystemConfigService(
    ISystemConfigRepository systemConfigRepository,
    IAuditService auditService,
    IConfigEncryptionHelper encryptionHelper,
    ILogger<SystemConfigService> logger) : ISystemConfigService
{
    private const string MaskedApiKey = "****************************";

    public async Task<SystemConfigResponseDto> GetConfigAsync(CancellationToken cancellationToken = default)
    {
        var configs = await systemConfigRepository.GetAllAsync(cancellationToken);
        var dict = configs.ToDictionary(c => c.ConfigKey, c => c.ConfigValue);

        var apiKey = dict.GetValueOrDefault(ConfigKeys.LlmApiKey, string.Empty);
        var hasKey = !string.IsNullOrWhiteSpace(apiKey);

        return new SystemConfigResponseDto
        {
            LlmProvider = dict.GetValueOrDefault(ConfigKeys.LlmProvider, LlmProviderKeys.Gemini),
            LlmApiKeyMasked = hasKey ? MaskedApiKey : string.Empty,
            SchedulerIntervalHours = ParseIntOrDefault(
                dict.GetValueOrDefault(ConfigKeys.SchedulerIntervalHours),
                SchedulerDefaults.IntervalHours),
            MaxWeeklyHours = ParseIntOrDefault(
                dict.GetValueOrDefault(ConfigKeys.MaxWeeklyHours),
                (int)TimesheetDefaults.DefaultMaxWeeklyHours)
        };
    }

    public async Task UpdateConfigAsync(
        long actorUserId,
        UpdateSystemConfigRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var updatedKeys = new List<string>();

        if (!string.IsNullOrWhiteSpace(request.LlmProvider))
            await UpdateKeyAsync(ConfigKeys.LlmProvider, request.LlmProvider.Trim(), actorUserId, now, updatedKeys, cancellationToken);

        if (request.LlmApiKey is not null)
        {
            var apiKeyValue = string.IsNullOrWhiteSpace(request.LlmApiKey)
                ? string.Empty
                : encryptionHelper.Encrypt(request.LlmApiKey.Trim());
            await UpdateKeyAsync(ConfigKeys.LlmApiKey, apiKeyValue, actorUserId, now, updatedKeys, cancellationToken);
        }

        if (request.SchedulerIntervalHours.HasValue)
            await UpdateKeyAsync(ConfigKeys.SchedulerIntervalHours, request.SchedulerIntervalHours.Value.ToString(), actorUserId, now, updatedKeys, cancellationToken);

        if (request.MaxWeeklyHours.HasValue)
            await UpdateKeyAsync(ConfigKeys.MaxWeeklyHours, request.MaxWeeklyHours.Value.ToString(), actorUserId, now, updatedKeys, cancellationToken);

        if (updatedKeys.Count == 0)
            throw new ValidationAppException("At least one setting must be provided.");

        await auditService.LogUpdateAsync(
            actorUserId,
            AuditEntityConstants.SystemConfigurations,
            0,
            null,
            new { updatedKeys, llm_api_key = AuditRedactionConstants.RedactedSecret },
            cancellationToken);

        await systemConfigRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "System config updated. {EntityName} by {ActorUserId} keys {UpdatedKeys}",
            AuditEntityConstants.SystemConfigurations, actorUserId, string.Join(",", updatedKeys));
    }

    private async Task UpdateKeyAsync(
        string key,
        string value,
        long actorUserId,
        DateTime now,
        List<string> updatedKeys,
        CancellationToken cancellationToken)
    {
        var config = await systemConfigRepository.GetByKeyAsync(key, cancellationToken)
            ?? throw new NotFoundAppException($"Configuration key '{key}' not found.");

        config.ConfigValue = value;
        config.UpdatedAt = now;
        config.UpdatedByUserId = actorUserId;

        await systemConfigRepository.UpdateAsync(config, cancellationToken);
        updatedKeys.Add(key);
    }

    private static int ParseIntOrDefault(string? value, int defaultValue) =>
        int.TryParse(value, out var parsed) ? parsed : defaultValue;
}
