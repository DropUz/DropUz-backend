using DropUz.Common.Domain;
using Xunit;

namespace DropUz.Common.Tests.Domain;

public sealed class ResultTests
{
    [Fact]
    public void SuccessResultCannotContainError()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Result.Success(Error.Failure("test.error", "Unexpected")));

        Assert.Equal("Successful result cannot contain an error.", exception.Message);
    }

    [Fact]
    public void FailureResultMustContainError()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Result.Failure(Error.None));

        Assert.Equal("Failed result must contain an error.", exception.Message);
    }

    [Fact]
    public void AccessingFailureValueThrows()
    {
        Result<string> result = Result.Failure<string>(Error.NotFound("test.missing", "Missing"));

        var exception = Assert.Throws<InvalidOperationException>(() => result.Value);

        Assert.Equal("Cannot access the value of a failed result.", exception.Message);
    }
}
