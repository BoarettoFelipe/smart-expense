using SmartExpense.Application.Abstractions.Dashboard;

namespace SmartExpense.Tests.Application.Dashboard;

internal sealed class FakeDashboardReadService : IDashboardReadService
{
    public DashboardReadData Data { get; set; } = new(
        0m,
        0m,
        0,
        null,
        [],
        []);

    public int CallCount { get; private set; }

    public Guid? LastUserId { get; private set; }

    public int? LastMonth { get; private set; }

    public int? LastYear { get; private set; }

    public DateOnly? LastStartDate { get; private set; }

    public DateOnly? LastEndDateExclusive { get; private set; }

    public Task<DashboardReadData> GetMonthlyAsync(
        Guid userId,
        int month,
        int year,
        DateOnly startDate,
        DateOnly? endDateExclusive,
        CancellationToken cancellationToken = default)
    {
        CallCount++;
        LastUserId = userId;
        LastMonth = month;
        LastYear = year;
        LastStartDate = startDate;
        LastEndDateExclusive = endDateExclusive;

        return Task.FromResult(Data);
    }
}
