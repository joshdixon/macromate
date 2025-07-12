using System.Collections.Immutable;

using MacroMate.WebApi.Features.Users.Events;

namespace MacroMate.WebApi.Features.Users.Aggregates;

public record Family(Guid Id, ImmutableList<FamilyMember> Members)
{
    public static Family Create(FamilyCreated @event) => new(
        Guid.NewGuid(),
        [
            new FamilyMember(
                @event.FoundingMember.FamilyMemberId,
                @event.FoundingMember.Name,
                @event.FoundingMember.UserId)
        ]);
    
    public Family Apply(FamilyMemberAdded @event) => this with
    {
        Members = Members.Add(new FamilyMember(@event.FamilyMemberId, @event.FamilyMemberName, null))
    };
}

public record FamilyMember(Guid FamilyMemberId, string Name, Guid? UserId);
