using System.Security.Cryptography;
using Lootlion.Application.Abstractions;
using Lootlion.Application.Dtos;
using Lootlion.Domain.Entities;
using Lootlion.Domain.Enums;
using Lootlion.Infrastructure.Data;
using Lootlion.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lootlion.Infrastructure.Identity;

public sealed class IdentityAuthService : IAuthService
{
    public const int GuestAccountExpiryDays = 7;

    /// <summary>อีเมลภายในสำหรับ guest เท่านั้น — ผ่าน Identity (RequireUniqueEmail) แต่ไม่ส่งให้ client/JWT เมื่อยังเป็น guest</summary>
    internal const string GuestSyntheticEmailDomain = "@guest.lootlion.internal";

    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IJwtTokenService _jwt;
    private readonly LootlionDbContext _db;
    private readonly JwtOptions _jwtOptions;
    private readonly IHouseholdService _households;

    public IdentityAuthService(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IJwtTokenService jwt,
        LootlionDbContext db,
        IOptions<JwtOptions> jwtOptions,
        IHouseholdService households)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwt = jwt;
        _db = db;
        _jwtOptions = jwtOptions.Value;
        _households = households;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            EmailConfirmed = true,
            DisplayName = request.DisplayName.Trim()
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var msg = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException(msg);
        }

        return await IssueTokensAsync(user, cancellationToken, false);
    }

    public async Task<AuthResponse> RegisterWizardAsync(RegisterWizardRequest request, CancellationToken cancellationToken = default)
    {
        var nickname = request.Nickname.Trim();
        if (nickname.Length == 0)
            throw new InvalidOperationException("Nickname is required.");

        if (request.Role == RegistrationRoleDto.Child)
        {
            if (!request.JoinHouseholdIdAsChild.HasValue)
                throw new InvalidOperationException("Select a family to join.");
            if (!string.IsNullOrWhiteSpace(request.UserName) || !string.IsNullOrWhiteSpace(request.Password))
                throw new InvalidOperationException("Child accounts do not use a password at signup.");

            var householdId = request.JoinHouseholdIdAsChild.Value;
            var household = await _db.Households.AsNoTracking().FirstOrDefaultAsync(h => h.Id == householdId, cancellationToken);
            if (household is null)
                throw new InvalidOperationException("Family not found.");
            if (!household.AllowChildPickJoin)
                throw new InvalidOperationException("This family is not open for child join.");

            var guestUserName = await AllocateGuestUserNameAsync(cancellationToken);
            var password = GenerateInternalPassword();
            var userId = Guid.NewGuid();
            var syntheticEmail = $"{userId:N}{GuestSyntheticEmailDomain}";
            var user = new AppUser
            {
                Id = userId,
                UserName = guestUserName,
                NormalizedUserName = _userManager.NormalizeName(guestUserName),
                Email = syntheticEmail,
                NormalizedEmail = _userManager.NormalizeEmail(syntheticEmail),
                DisplayName = nickname,
                EmailConfirmed = true,
                GuestAccountExpiresUtc = DateTime.UtcNow.AddDays(GuestAccountExpiryDays)
            };

            var create = await _userManager.CreateAsync(user, password);
            if (!create.Succeeded)
            {
                var msg = string.Join("; ", create.Errors.Select(e => e.Description));
                throw new InvalidOperationException(msg);
            }

            _db.HouseholdMembers.Add(new HouseholdMember
            {
                Id = Guid.NewGuid(),
                HouseholdId = householdId,
                UserId = user.Id,
                Role = MemberRole.Child,
                JoinedUtc = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);

            return await IssueTokensAsync(user, cancellationToken, true);
        }

        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
            throw new InvalidOperationException("Username and password are required for a parent account.");

        if (request.CreateNewHousehold == request.JoinHouseholdIdAsParent.HasValue)
            throw new InvalidOperationException("Choose either create a new family or join an existing one.");

        if (request.CreateNewHousehold && string.IsNullOrWhiteSpace(request.NewHouseholdName))
            throw new InvalidOperationException("Family name is required when creating a new family.");

        var parent = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = request.UserName.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            EmailConfirmed = true,
            DisplayName = nickname
        };

        var parentResult = await _userManager.CreateAsync(parent, request.Password);
        if (!parentResult.Succeeded)
        {
            var msg = string.Join("; ", parentResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException(msg);
        }

        if (request.CreateNewHousehold)
        {
            await _households.CreateAsync(parent.Id, new CreateHouseholdRequest(request.NewHouseholdName!.Trim()), cancellationToken);
        }
        else
        {
            await _households.JoinHouseholdAsParentAsync(parent.Id, request.JoinHouseholdIdAsParent!.Value, cancellationToken);
        }

        return await IssueTokensAsync(parent, cancellationToken, false);
    }

    public async Task<AuthResponse> CompleteGuestChildAsync(Guid parentUserId, CompleteGuestChildRequest request, CancellationToken cancellationToken = default)
    {
        var child = await _userManager.FindByIdAsync(request.ChildUserId.ToString());
        if (child is null)
            throw new InvalidOperationException("User not found.");

        if (child.GuestAccountExpiresUtc is null)
            throw new InvalidOperationException("This account is not waiting for parent setup.");

        if (child.GuestAccountExpiresUtc <= DateTime.UtcNow)
            throw new InvalidOperationException("Guest account has expired.");

        var childHouseholdIds = await _db.HouseholdMembers
            .AsNoTracking()
            .Where(m => m.UserId == child.Id)
            .Select(m => m.HouseholdId)
            .ToListAsync(cancellationToken);

        var parentOk = await _db.HouseholdMembers.AnyAsync(
            m => childHouseholdIds.Contains(m.HouseholdId) && m.UserId == parentUserId && m.Role == MemberRole.Parent,
            cancellationToken);
        if (!parentOk)
            throw new InvalidOperationException("Only a parent in the same family can complete this account.");

        var newUserName = request.UserName.Trim();
        var taken = await _userManager.FindByNameAsync(newUserName);
        if (taken is not null && taken.Id != child.Id)
            throw new InvalidOperationException("Username is already taken.");

        child.UserName = newUserName;
        child.NormalizedUserName = _userManager.NormalizeName(newUserName);

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            child.Email = request.Email.Trim();
            child.NormalizedEmail = _userManager.NormalizeEmail(child.Email);
        }

        child.GuestAccountExpiresUtc = null;

        var update = await _userManager.UpdateAsync(child);
        if (!update.Succeeded)
        {
            var msg = string.Join("; ", update.Errors.Select(e => e.Description));
            throw new InvalidOperationException(msg);
        }

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(child);
        var pwd = await _userManager.ResetPasswordAsync(child, resetToken, request.Password);
        if (!pwd.Succeeded)
        {
            var msg = string.Join("; ", pwd.Errors.Select(e => e.Description));
            throw new InvalidOperationException(msg);
        }

        var reloaded = await _userManager.FindByIdAsync(child.Id.ToString());
        if (reloaded is null)
            throw new InvalidOperationException("User not found after update.");

        return await IssueTokensAsync(reloaded, cancellationToken, false);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var id = request.LoginIdentifier.Trim();
        AppUser? user = null;
        if (id.Contains('@', StringComparison.Ordinal))
            user = await _userManager.FindByEmailAsync(id);
        else
            user = await _userManager.FindByNameAsync(id);

        if (user is null)
            throw new InvalidOperationException("Invalid credentials.");

        var signIn = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!signIn.Succeeded)
            throw new InvalidOperationException("Invalid credentials.");

        var isGuest = user.GuestAccountExpiresUtc is not null;
        return await IssueTokensAsync(user, cancellationToken, isGuest);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default)
    {
        var raw = request.RefreshToken?.Trim();
        if (string.IsNullOrEmpty(raw))
            throw new InvalidOperationException("Invalid refresh token.");

        var hash = TokenHasher.Hash(raw);
        var existing = await _db.RefreshTokens
            .FirstOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);

        if (existing is null || existing.RevokedUtc is not null || existing.ExpiresUtc <= DateTime.UtcNow)
            throw new InvalidOperationException("Invalid refresh token.");

        var user = await _userManager.FindByIdAsync(existing.UserId.ToString());
        if (user is null)
            throw new InvalidOperationException("Invalid refresh token.");

        existing.RevokedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        var isGuest = user.GuestAccountExpiresUtc is not null;
        return await IssueTokensAsync(user, cancellationToken, isGuest);
    }

    private async Task<string> AllocateGuestUserNameAsync(CancellationToken cancellationToken)
    {
        for (var i = 0; i < 12; i++)
        {
            var candidate = $"guest_{Guid.NewGuid():N}";
            var exists = await _userManager.FindByNameAsync(candidate);
            if (exists is null)
                return candidate;
        }

        throw new InvalidOperationException("Could not allocate a guest username.");
    }

    private static string GenerateInternalPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%";
        var bytes = new byte[36];
        RandomNumberGenerator.Fill(bytes);
        var result = new char[36];
        for (var i = 0; i < 36; i++)
            result[i] = chars[bytes[i] % chars.Length];
        return new string(result);
    }

    private async Task<AuthResponse> IssueTokensAsync(AppUser user, CancellationToken cancellationToken, bool isGuestChild)
    {
        var publicEmail = isGuestChild || string.IsNullOrEmpty(user.Email)
            ? null
            : user.Email!.EndsWith(GuestSyntheticEmailDomain, StringComparison.Ordinal)
                ? null
                : user.Email;
        var access = _jwt.CreateToken(
            user.Id,
            string.IsNullOrEmpty(publicEmail) ? null : publicEmail,
            user.DisplayName,
            isGuestChild);
        var rawRefresh = GenerateOpaqueToken();
        var hash = TokenHasher.Hash(rawRefresh);
        var expires = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays);

        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = hash,
            ExpiresUtc = expires,
            CreatedUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            access,
            rawRefresh,
            user.Id,
            publicEmail ?? string.Empty,
            user.DisplayName,
            isGuestChild);
    }

    private static string GenerateOpaqueToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return WebEncoders.Base64UrlEncode(bytes);
    }
}
