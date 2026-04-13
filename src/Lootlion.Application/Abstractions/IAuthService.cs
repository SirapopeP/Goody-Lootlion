using Lootlion.Application.Dtos;

namespace Lootlion.Application.Abstractions;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> RegisterWizardAsync(RegisterWizardRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> CompleteGuestChildAsync(Guid parentUserId, CompleteGuestChildRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default);
}
