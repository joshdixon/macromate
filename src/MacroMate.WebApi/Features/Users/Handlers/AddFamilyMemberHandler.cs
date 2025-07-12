using Carter;

using FluentValidation;

using MacroMate.WebApi.Features.Users.Aggregates;
using MacroMate.WebApi.Features.Users.Events;
using MacroMate.WebApi.Features.Users.Projections;

using Marten;

using Microsoft.AspNetCore.Http.HttpResults;

using Wolverine;

namespace MacroMate.WebApi.Features.Users.Handlers;

internal record AddFamilyMemberEndpointRequest(
    string AddedUserDisplayName,
    string AddedUserEmail
);

internal class AddFamilyMemberEndpointRequestValidator : AbstractValidator<AddFamilyMemberEndpointRequest>
{
    public AddFamilyMemberEndpointRequestValidator()
    {
        RuleFor(x => x.AddedUserDisplayName).NotEmpty();
        RuleFor(x => x.AddedUserEmail).NotEmpty().EmailAddress();
    }
}

internal class AddFamilyMember : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app) => app
        .MapPost("/family/members",
            async Task<Results<Ok, BadRequest<string>>> (
                AddFamilyMemberEndpointRequest request,
                HttpContext context,
                IMessageBus messageBus) =>
            {
                await messageBus.InvokeAsync(new AddFamilyMemberRequest(
                    context.GetUserId(),
                    request.AddedUserDisplayName,
                    request.AddedUserEmail));
                
                return TypedResults.Ok();
            })
        .WithName("AddFamilyMember");
}

public record AddFamilyMemberRequest(
    Guid FamilyUserId, 
    string AddedUserDisplayName,
    string AddedUserEmail
);

public class AddFamilyMemberHandler(
    ILogger<AddFamilyMemberHandler> logger,
    IDocumentSession session)
{
    public async Task Handle(AddFamilyMemberRequest request)
    {
        UserView? user = await session
            .Query<UserView>()
            .Where(x => x.Id == request.FamilyUserId)
            .SingleOrDefaultAsync();
        
        if (user is null)
        {
            logger.LogWarning("User {UserId} not found", request.FamilyUserId);
            return;
        }

        Guid familyId = user.FamilyId ?? Guid.NewGuid();
        if (user.FamilyId is null)
        {
            session.Events.StartStream<Family>(familyId, new FamilyCreated(
                familyId,
                new FoundingFamilyMember(
                    Guid.NewGuid(),
                    user.Id,
                    user.Name
                )
            ));
        }

        session.Events.Append(familyId,
            new FamilyMemberAdded(
                familyId,
                Guid.NewGuid(),
                request.AddedUserDisplayName
            ));
        
        await session.SaveChangesAsync();
    }
}
