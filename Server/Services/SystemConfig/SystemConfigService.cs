using System.Text.Json;
using Server.Common;
using Server.Exceptions;
using Server.Models.DTOs.SystemConfig;
using Server.Models.Entities;

namespace Server.Services.SystemConfig;

public class SystemConfigService(
    ISystemConfigRepository systemConfigRepository,
    IAuditLogRepository auditLogRepository,
    ConfigEncryptionHelper encryptionHelper) : ISystemConfigService
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
            LlmProvider = dict.GetValueOrDefault(ConfigKeys.LlmProvider, "Gemini"),
            LlmApiKeyMasked = hasKey ? MaskedApiKey : string.Empty,
            SchedulerIntervalHours = int.TryParse(dict.GetValueOrDefault(ConfigKeys.SchedulerIntervalHours, "4"), out var interval) ? interval : 4,
            MaxWeeklyHours = int.TryParse(dict.GetValueOrDefault(ConfigKeys.MaxWeeklyHours, "40"), out var hours) ? hours : 40
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

        if (!string.IsNullOrWhiteSpace(request.LlmApiKey))
            await UpdateKeyAsync(ConfigKeys.LlmApiKey, encryptionHelper.Encrypt(request.LlmApiKey.Trim()), actorUserId, now, updatedKeys, cancellationToken);

        if (request.SchedulerIntervalHours.HasValue)
            await UpdateKeyAsync(ConfigKeys.SchedulerIntervalHours, request.SchedulerIntervalHours.Value.ToString(), actorUserId, now, updatedKeys, cancellationToken);

        if (request.MaxWeeklyHours.HasValue)
            await UpdateKeyAsync(ConfigKeys.MaxWeeklyHours, request.MaxWeeklyHours.Value.ToString(), actorUserId, now, updatedKeys, cancellationToken);

        if (updatedKeys.Count == 0)
            throw new ValidationAppException("At least one setting must be provided.");

        await auditLogRepository.AddAsync(new AuditLog
        {
            ActorUserId = actorUserId,
            EntityName = "SYSTEM_CONFIGURATIONS",
            EntityId = 0,
            ActionType = "UPDATE",
            NewValues = JsonSerializer.Serialize(new { updatedKeys, llm_api_key = "***REDACTED***" }),
            CreatedAt = now
        }, cancellationToken);

        await systemConfigRepository.SaveChangesAsync(cancellationToken);
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
}
