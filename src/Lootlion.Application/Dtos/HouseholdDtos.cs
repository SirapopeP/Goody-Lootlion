namespace Lootlion.Application.Dtos;

public record HouseholdDto(Guid Id, string Name, DateTime CreatedUtc);

public record CreateHouseholdRequest(string Name);

public record HouseholdMemberDto(Guid UserId, string DisplayName, string Email, string Role);
