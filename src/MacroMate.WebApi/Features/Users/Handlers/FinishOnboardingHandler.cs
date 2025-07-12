using Carter;

using MacroMate.WebApi.Features.Users.Events;
using MacroMate.WebApi.Features.Users.Projections;

using Marten;

using Microsoft.AspNetCore.Http.HttpResults;

using Wolverine;

namespace MacroMate.WebApi.Features.Users.Handlers;

internal class FinishOnboarding : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app) => app
        .MapPost("/finish-onboarding",
            async Task<Results<Ok, BadRequest<string>>> (
                HttpContext context,
                IMessageBus messageBus) =>
            {
                Guid userId = context.GetUserId();
                await messageBus.InvokeAsync(new FinishOnboardingRequest(userId));
                return TypedResults.Ok();
            })
        .WithName("FinishOnboarding");
}

public record FinishOnboardingRequest(
    Guid UserId
);

public class FinishOnboardingHandler(
    ILogger<FinishOnboardingRequest> logger,
    IDocumentSession session)
{
    public async Task Handle(FinishOnboardingRequest request)
    {
        UserView? user = await session
            .Query<UserView>()
            .Where(x => x.Id == request.UserId)
            .SingleOrDefaultAsync();
        
        if (user is null)
        {
            logger.LogWarning("User {UserId} not found", request.UserId);
            return;
        }

        session.Events.Append(user.Id, new UserOnboarded(user.Id));
        logger.LogInformation("User {UserId} finished onboarding", user.Id);
        
        await session.SaveChangesAsync();
    }
}
