using MacroMate.WebApi.Features.Users.Events;

using Marten.Events.Projections;

namespace MacroMate.WebApi.Features.Users.Projections;

public record UserView(
    Guid Id,
    string WorkOsUserId,
    string Name,
    Guid? FamilyId,
    Guid? FamilyMemberId
);

internal class UserViewProjection : MultiStreamProjection<UserView, Guid>
{
    public UserViewProjection()
    {
        Identity<UserCreated>(x => x.Id);
        Identity<FamilyCreated>(x => x.FoundingMember.UserId);
    }
    
    public UserView Create(UserCreated @event)
        => new(
            @event.Id,
            @event.WorkOsUserId,
            @event.Name,
            null,
            null
        );
    
    public UserView Apply(UserView view, FamilyCreated @event)
        => view with
        {
            FamilyId = @event.Id,
            FamilyMemberId = @event.FoundingMember.FamilyMemberId
        };
}
