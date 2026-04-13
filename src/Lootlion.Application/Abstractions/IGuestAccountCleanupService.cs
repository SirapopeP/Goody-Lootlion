namespace Lootlion.Application.Abstractions;

public interface IGuestAccountCleanupService
{
    Task RunCleanupAsync(CancellationToken cancellationToken = default);
}
