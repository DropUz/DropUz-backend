using DropUz.Common.Application.Clock;
using DropUz.Common.Application.Data;
using DropUz.Common.Application.Messaging;
using DropUz.Common.Domain;
using DropUz.Modules.Admin.Application.Audit;
using DropUz.Modules.Admin.Domain.Settings;
using Microsoft.EntityFrameworkCore;

namespace DropUz.Modules.Admin.Application.Settings;

public sealed class SetSupportTelegramUrlCommandHandler(
    IMainRepository repository,
    IDateTimeProvider dateTimeProvider,
    IAdminAuditService auditService)
    : ICommandHandler<SetSupportTelegramUrlCommand, SupportTelegramUrlResponse>
{
    public async Task<Result<SupportTelegramUrlResponse>> Handle(
        SetSupportTelegramUrlCommand command,
        CancellationToken cancellationToken)
    {
        AdminSetting? setting = await GetSettingAsync(repository, cancellationToken);
        if (setting is null)
        {
            setting = AdminSetting.Create(AdminSettingsKeys.SupportTelegramUrl, command.Url.Trim(), dateTimeProvider.UtcNow);
            await repository.AddAsync(setting);
        }
        else
        {
            setting.SetValue(command.Url.Trim(), dateTimeProvider.UtcNow);
        }

        await auditService.RecordAsync(
            AdminAuditActions.Settings.SupportTelegramUrlUpdated,
            entityType: "AdminSetting",
            entityId: setting.Id,
            cancellationToken: cancellationToken);

        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(Map(setting));
    }

    internal static async Task<AdminSetting?> GetSettingAsync(
        IMainRepository repository,
        CancellationToken cancellationToken)
    {
        return await repository
            .Query<AdminSetting>(setting => setting.Key == AdminSettingsKeys.SupportTelegramUrl)
            .FirstOrDefaultAsync(cancellationToken);
    }

    internal static SupportTelegramUrlResponse Map(AdminSetting? setting)
    {
        return new SupportTelegramUrlResponse(setting?.Value, setting?.UpdatedAtUtc);
    }
}

public sealed class GetSupportTelegramUrlQueryHandler(IMainRepository repository)
    : IQueryHandler<GetSupportTelegramUrlQuery, SupportTelegramUrlResponse>
{
    public async Task<Result<SupportTelegramUrlResponse>> Handle(
        GetSupportTelegramUrlQuery request,
        CancellationToken cancellationToken)
    {
        AdminSetting? setting = await SetSupportTelegramUrlCommandHandler.GetSettingAsync(
            repository,
            cancellationToken);

        return Result.Success(SetSupportTelegramUrlCommandHandler.Map(setting));
    }
}
