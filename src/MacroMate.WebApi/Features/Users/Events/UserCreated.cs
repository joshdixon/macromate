namespace MacroMate.WebApi.Features.Users.Events;

public record UserCreated(Guid Id, 
    string Name, 
    string FirstName,
    string LastName,
    string Email,
    string WorkOsUserId
);
