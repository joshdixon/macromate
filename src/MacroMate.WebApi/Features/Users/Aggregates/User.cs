using MacroMate.WebApi.Features.Users.Events;

namespace MacroMate.WebApi.Features.Users.Aggregates;

public record User(
    Guid Id,
    string Name,
    string FirstName,
    string LastName,
    string Email,
    string WorkOsUserId,
    bool Onboarded
)
{
    public static User Create(UserCreated @event) => new(
        @event.Id,
        @event.Name,
        @event.FirstName,
        @event.LastName,
        @event.Email,
        @event.WorkOsUserId,
        false);
    
    public User Apply(UserOnboarded @event) => this with
    {
        Onboarded = true
    };
}
