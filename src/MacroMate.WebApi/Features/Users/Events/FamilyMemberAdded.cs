namespace MacroMate.WebApi.Features.Users.Events;

public record FamilyMemberAdded(Guid FamilyId, Guid FamilyMemberId, string FamilyMemberName);
