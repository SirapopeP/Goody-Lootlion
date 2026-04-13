using Lootlion.Application.Abstractions;

namespace Lootlion.Api;

/// <summary>ลบบัญชี guest เด็กที่หมดเวลา 7 วันโดยไม่ถูกผู้ปกครองกรอกข้อมูลครบ</summary>
public sealed class GuestAccountCleanupHostedService : BackgroundService
{
    private readonly IServiceProvider _services;
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    public GuestAccountCleanupHostedService(IServiceProvider services)
    {
        _services = services;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _services.CreateScope();
            var cleanup = scope.ServiceProvider.GetRequiredService<IGuestAccountCleanupService>();
            await cleanup.RunCleanupAsync(stoppingToken);
            await Task.Delay(Interval, stoppingToken);
        }
    }
}
