namespace MacroMate.WebApi.Features.Users.Events;

public record FamilyCreated(Guid Id, FoundingFamilyMember FoundingMember);
public record FoundingFamilyMember(Guid FamilyMemberId, Guid UserId, string Name);
