using FluentAssertions;
using SportShop.Application.Common;
using Xunit;

namespace SportShop.Application.Tests;

public sealed class CommonApplicationTests
{
    private static readonly string[] _errorMessages = ["msg"];

    [Fact]
    public void Result_Success_ShouldHaveIsSuccessTrueAndNoError()
    {
        var result = Result.Success();
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Result_Failure_ShouldHaveIsFailureTrueAndError()
    {
        var error = Error.NotFound("item.not_found", "Item tidak ditemukan");
        var result = Result.Failure(error);
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Result_Value_Success_ShouldReturnValue()
    {
        var result = Result.Success("test-value");
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("test-value");

        Result<string> implicitResult = "implicit-value";
        implicitResult.IsSuccess.Should().BeTrue();
        implicitResult.Value.Should().Be("implicit-value");
    }

    [Fact]
    public void Result_Value_Failure_AccessingValueShouldThrow()
    {
        var error = Error.Validation("input.invalid", "Input tidak valid");
        var result = Result.Failure<string>(error);
        result.IsFailure.Should().BeTrue();
        var act = () => result.Value;
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Error_FactoryMethods_ShouldSetCorrectErrorType()
    {
        var validation = Error.Validation("code", "desc", new Dictionary<string, string[]> { { "field", _errorMessages } });
        validation.Type.Should().Be(ErrorType.Validation);
        validation.ValidationErrors.Should().ContainKey("field");

        var notFound = Error.NotFound("code", "desc");
        notFound.Type.Should().Be(ErrorType.NotFound);

        var conflict = Error.Conflict("code", "desc");
        conflict.Type.Should().Be(ErrorType.Conflict);

        var unauthorized = Error.Unauthorized("code", "desc");
        unauthorized.Type.Should().Be(ErrorType.Unauthorized);

        var forbidden = Error.Forbidden("code", "desc");
        forbidden.Type.Should().Be(ErrorType.Forbidden);

        var business = Error.BusinessRule("code", "desc");
        business.Type.Should().Be(ErrorType.BusinessRule);

        var rateLimit = Error.RateLimit("code", "desc");
        rateLimit.Type.Should().Be(ErrorType.RateLimit);

        var unexpected = Error.Unexpected("code", "desc");
        unexpected.Type.Should().Be(ErrorType.Unexpected);
    }

    [Fact]
    public void PagedResult_Create_ShouldCalculateTotalPages()
    {
        var items = new List<string> { "a", "b", "c" };
        var paged = PagedResult.Create(items, 1, 2, 5);

        paged.Items.Should().HaveCount(3);
        paged.Page.Should().Be(1);
        paged.PageSize.Should().Be(2);
        paged.TotalItems.Should().Be(5);
        paged.TotalPages.Should().Be(3);

        var emptyPaged = PagedResult.Create(new List<string>(), 1, 0, 0);
        emptyPaged.TotalPages.Should().Be(0);
    }
}
