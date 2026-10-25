using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using PawConnect.Api.Domain.Exceptions;
using PawConnect.Api.Middleware;

namespace PawConnect.Tests;

public class ExceptionHandlerTests
{
    private static Exception Make(string kind) => kind switch
    {
        "NotFound" => new NotFoundException("Pet", 5),
        "Conflict" => new ConflictException("You already have an active application for this pet."),
        "Unauthenticated" => new AuthenticationFailedException("Invalid email or password."),
        "Forbidden" => new ForbiddenException("You do not have permission."),
        "Validation" => new AppValidationException(new Dictionary<string, string[]> { ["Email"] = new[] { "Taken." } }),
        "TooLarge" => new BadHttpRequestException("internal parser detail", StatusCodes.Status413PayloadTooLarge),
        "BadRequest" => new BadHttpRequestException("internal parser detail", StatusCodes.Status400BadRequest),
        _ => new InvalidOperationException("Server=db;User=root;Password=hunter2"),
    };

    private static async Task<(DefaultHttpContext Context, string Body)> Handle(Exception exception)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Response.Body = new MemoryStream();

        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var handled = await handler.TryHandleAsync(context, exception, TestContext.Current.CancellationToken);

        Assert.True(handled);

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync(TestContext.Current.CancellationToken);

        return (context, body);
    }

    [Theory]
    [InlineData("NotFound", 404)]
    [InlineData("Conflict", 409)]
    [InlineData("Unauthenticated", 401)]
    [InlineData("Forbidden", 403)]
    [InlineData("Validation", 400)]
    [InlineData("TooLarge", 413)]
    [InlineData("BadRequest", 400)]
    [InlineData("Unexpected", 500)]
    public async Task EachExceptionBecomesTheRightProblemResponse(string kind, int expectedStatus)
    {
        var (context, body) = await Handle(Make(kind));

        Assert.Equal(expectedStatus, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
        Assert.Contains($"\"status\":{expectedStatus}", body);
        Assert.Contains("/api/test", body);
    }

    [Fact]
    public async Task UnexpectedErrors_NeverLeakInternalDetails()
    {
        var (_, body) = await Handle(Make("Unexpected"));

        Assert.DoesNotContain("hunter2", body);
        Assert.DoesNotContain("Password", body);
        Assert.DoesNotContain("InvalidOperationException", body);
        Assert.Contains("Please try again later.", body);
    }

    [Fact]
    public async Task BadHttpRequests_DoNotEchoTheParserMessage()
    {
        var (_, body) = await Handle(Make("BadRequest"));

        Assert.DoesNotContain("internal parser detail", body);
    }

    [Fact]
    public async Task NotFound_NamesTheResourceAndTheId()
    {
        var (_, body) = await Handle(Make("NotFound"));

        Assert.Contains("Pet with id '5' was not found.", body);
    }

    [Fact]
    public async Task ValidationErrors_ListTheFieldsInTheProblemBody()
    {
        var (_, body) = await Handle(Make("Validation"));

        Assert.Contains("\"errors\"", body);
        Assert.Contains("\"Email\"", body);
        Assert.Contains("Taken.", body);
    }

    [Fact]
    public async Task Unauthenticated_AddsTheBearerChallengeHeader()
    {
        var (context, _) = await Handle(Make("Unauthenticated"));

        Assert.Equal("Bearer", context.Response.Headers.WWWAuthenticate.ToString());
    }

    [Fact]
    public async Task WhenTheClientHasLeft_NothingIsWritten()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        var context = new DefaultHttpContext { RequestAborted = cancelled.Token };
        context.Response.Body = new MemoryStream();

        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var handled = await handler.TryHandleAsync(context, new OperationCanceledException(), TestContext.Current.CancellationToken);

        Assert.True(handled);
        Assert.Equal(0, context.Response.Body.Length);
    }
}