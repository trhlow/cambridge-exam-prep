using ExamPrep.Infrastructure;

namespace ExamPrep.Tests.Infrastructure;

/// <summary>VE-41: the pool is large enough for a full exam room unless the connection string sets its own size.</summary>
public sealed class ConnectionPoolTests
{
    [Fact]
    public void WithPoolSize_AddsTheDefaultSize() =>
        Assert.Equal($"server=db;database=x;Maximum Pool Size={DependencyInjection.DefaultMaxPoolSize}", DependencyInjection.WithPoolSize("server=db;database=x;"));

    [Theory]
    [InlineData("server=db;Max Pool Size=40")]
    [InlineData("server=db;maximum pool size=40")]
    [InlineData("server=db;MaxPoolSize=40")]
    public void WithPoolSize_KeepsASizeTheStringSets(string connection) =>
        Assert.Equal(connection, DependencyInjection.WithPoolSize(connection));
}
