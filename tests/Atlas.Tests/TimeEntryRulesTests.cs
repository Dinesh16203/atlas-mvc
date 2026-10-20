using Atlas.Web.Data;

namespace Atlas.Tests;

public sealed class TimeEntryRulesTests
{
    private static readonly DateTime Today = new(2026, 10, 20);

    [Theory]
    [InlineData(0.25)]
    [InlineData(1.0)]
    [InlineData(8.0)]
    public void Validate_accepts_hours_within_daily_range(double hours)
    {
        Assert.Null(TimeEntryRules.Validate(hours, Today, Today));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.1)]
    [InlineData(8.25)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Validate_rejects_hours_outside_daily_range(double hours)
    {
        Assert.Equal("Actual hours must be between 0.25 and 8.00.", TimeEntryRules.Validate(hours, Today, Today));
    }

    [Fact]
    public void Validate_rejects_future_dates()
    {
        Assert.Equal("Future-dated entries are not allowed.", TimeEntryRules.Validate(1, Today.AddDays(1), Today));
    }

    [Theory]
    [InlineData(40, 40, 0)]
    [InlineData(36.5, 40, -3.5)]
    [InlineData(42, 40, 2)]
    public void Variance_is_entered_minus_capacity(double entered, double capacity, double expected)
    {
        Assert.Equal(expected, TimeEntryRules.Variance(entered, capacity));
    }
}
