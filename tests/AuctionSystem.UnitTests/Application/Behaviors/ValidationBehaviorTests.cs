using AuctionSystem.Application.Behaviors;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace AuctionSystem.UnitTests.Application.Behaviors;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WhenNoValidators_InvokesNext()
    {
        var behavior = new ValidationBehavior<TestRequest, TestResponse>([]);
        var called = false;

        var result = await behavior.Handle(new TestRequest("ok"), () =>
        {
            called = true;
            return Task.FromResult(new TestResponse("done"));
        }, CancellationToken.None);

        Assert.True(called);
        Assert.Equal("done", result.Value);
    }

    [Fact]
    public async Task Handle_WhenValidationFails_ThrowsValidationExceptionAndDoesNotInvokeNext()
    {
        var validator = new InlineValidator<TestRequest>();
        validator.RuleFor(x => x.Value).NotEmpty().WithMessage("Value is required.");

        var behavior = new ValidationBehavior<TestRequest, TestResponse>([validator]);
        var called = false;

        var exception = await Assert.ThrowsAsync<ValidationException>(() => behavior.Handle(new TestRequest(string.Empty), () =>
        {
            called = true;
            return Task.FromResult(new TestResponse("done"));
        }, CancellationToken.None));

        Assert.False(called);
        Assert.Contains(exception.Errors, x => x.ErrorMessage == "Value is required.");
    }

    [Fact]
    public async Task Handle_WhenValidationPasses_InvokesNext()
    {
        var validator = new InlineValidator<TestRequest>();
        validator.RuleFor(x => x.Value).NotEmpty();

        var behavior = new ValidationBehavior<TestRequest, TestResponse>([validator]);

        var result = await behavior.Handle(new TestRequest("ok"), () => Task.FromResult(new TestResponse("done")), CancellationToken.None);

        Assert.Equal("done", result.Value);
    }

    private sealed record TestRequest(string Value) : IRequest<TestResponse>;

    private sealed record TestResponse(string Value);
}