using System;
using Xunit;
using DevopsCommander.Shared;

namespace DevopsCommander.Tests
{
    public class ResultTests
    {
        [Fact]
        public void Success_ShouldHoldValue_And_HaveSuccessFlags()
        {
            var result = Result<string>.Success("hello");

            Assert.True(result.IsSuccess);
            Assert.False(result.IsFailure);
            Assert.Equal("hello", result.Value);
            Assert.Throws<InvalidOperationException>(() => _ = result.Error);
        }

        [Fact]
        public void Failure_ShouldHoldError_And_ThrowOnValueAccess()
        {
            var result = Result<int>.Failure("something went wrong");

            Assert.False(result.IsSuccess);
            Assert.True(result.IsFailure);
            Assert.Equal("something went wrong", result.Error);
            Assert.Throws<InvalidOperationException>(() => _ = result.Value);
        }

        [Fact]
        public void ImplicitConversion_ShouldCreateSuccessResult()
        {
            Result<int> result = 42;

            Assert.True(result.IsSuccess);
            Assert.Equal(42, result.Value);
        }

        [Fact]
        public void Map_OnSuccess_ShouldTransformValue()
        {
            var result = Result<int>.Success(10);
            var mapped = result.Map(x => x * 2);

            Assert.True(mapped.IsSuccess);
            Assert.Equal(20, mapped.Value);
        }

        [Fact]
        public void Map_OnFailure_ShouldPropagateError()
        {
            var result = Result<int>.Failure("calculation error");
            var mapped = result.Map(x => x * 2);

            Assert.True(mapped.IsFailure);
            Assert.Equal("calculation error", mapped.Error);
        }

        [Fact]
        public void Bind_OnSuccess_ShouldChainResult()
        {
            var result = Result<string>.Success("123");
            var bound = result.Bind(s => int.TryParse(s, out var n)
                ? Result<int>.Success(n)
                : Result<int>.Failure("invalid number"));

            Assert.True(bound.IsSuccess);
            Assert.Equal(123, bound.Value);
        }

        [Fact]
        public void Bind_OnFailure_ShouldShortCircuit()
        {
            var result = Result<string>.Failure("initial error");
            var bound = result.Bind(s => Result<int>.Success(42));

            Assert.True(bound.IsFailure);
            Assert.Equal("initial error", bound.Error);
        }

        [Fact]
        public void Match_ShouldExecuteAppropriateBranch()
        {
            var success = Result<int>.Success(100);
            var successOutcome = success.Match(val => $"Value: {val}", err => $"Error: {err}");
            Assert.Equal("Value: 100", successOutcome);

            var failure = Result<int>.Failure("not found");
            var failureOutcome = failure.Match(val => $"Value: {val}", err => $"Error: {err}");
            Assert.Equal("Error: not found", failureOutcome);
        }
    }
}
