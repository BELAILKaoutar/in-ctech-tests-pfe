using FluentAssertions;
using in_ctech_management_backend.Domain.Employees;
using in_ctech_management_backend.Domain.LeaveRequests;
using in_ctech_management_backend.Domain.LeaveRequests.Enums;

namespace in_ctech_management_backend.Tests.Domain.LeaveRequests;

public class LeaveRequestTests
{
    private static readonly EmployeeId _employeeId = new EmployeeId(Guid.NewGuid());
    private const string _createdBy = "test.user@winity.com";

    // ═══════════════════════════════════════════════════
    // Create
    // ═══════════════════════════════════════════════════

    [Fact]
    public void Create_ValidRequest_ShouldReturnPendingLeaveRequest()
    {
        // Arrange
        var startDate = new DateOnly(2026, 6, 16); // lundi
        var endDate = new DateOnly(2026, 6, 18); // mercredi

        // Act
        var leaveRequest = LeaveRequest.Create(
            _employeeId,
            LeaveType.PaidLeave,
            startDate,
            endDate,
            DayPeriod.Morning,
            DayPeriod.Afternoon,
            "Vacances",
            _createdBy);

        // Assert
        leaveRequest.Status.Should().Be(LeaveRequestStatus.Pending);
        leaveRequest.NumberOfDays.Should().Be(3m);
        leaveRequest.EmployeeId.Value.Should().Be(_employeeId.Value);
        leaveRequest.LeaveType.Should().Be(LeaveType.PaidLeave);
    }

    [Fact]
    public void Create_StartDateAfterEndDate_ShouldThrowArgumentException()
    {
        // Arrange
        var startDate = new DateOnly(2026, 6, 20);
        var endDate = new DateOnly(2026, 6, 18);

        // Act
        var act = () => LeaveRequest.Create(
            _employeeId, LeaveType.PaidLeave,
            startDate, endDate,
            DayPeriod.Morning, DayPeriod.Afternoon,
            "Test", _createdBy);

        // Assert
        act.Should().Throw<ArgumentException>()
           .WithMessage("*date de début*");
    }

    [Fact]
    public void Create_SameDayAfternoonToMorning_ShouldThrowArgumentException()
    {
        // Arrange
        var date = new DateOnly(2026, 6, 16);

        // Act
        var act = () => LeaveRequest.Create(
            _employeeId, LeaveType.PaidLeave,
            date, date,
            DayPeriod.Afternoon, DayPeriod.Morning,
            "Test", _createdBy);

        // Assert
        act.Should().Throw<ArgumentException>()
           .WithMessage("*Période invalide*");
    }

    [Fact]
    public void Create_OnlyWeekendDays_ShouldThrowArgumentException()
    {
        // Arrange — samedi + dimanche
        var startDate = new DateOnly(2026, 6, 20); // samedi
        var endDate = new DateOnly(2026, 6, 21); // dimanche

        // Act
        var act = () => LeaveRequest.Create(
            _employeeId, LeaveType.PaidLeave,
            startDate, endDate,
            DayPeriod.Morning, DayPeriod.Afternoon,
            "Test", _createdBy);

        // Assert
        act.Should().Throw<ArgumentException>()
           .WithMessage("*nombre de jours*");
    }

    [Fact]
    public void Create_WithExcludedHoliday_ShouldDeductHolidayFromCount()
    {
        // Arrange — lundi à mercredi, mardi est férié
        var startDate = new DateOnly(2026, 6, 22); // lundi
        var endDate = new DateOnly(2026, 6, 24); // mercredi
        var holidays = new HashSet<DateOnly> { new DateOnly(2026, 6, 23) };

        // Act
        var leaveRequest = LeaveRequest.Create(
            _employeeId, LeaveType.PaidLeave,
            startDate, endDate,
            DayPeriod.Morning, DayPeriod.Afternoon,
            "Test", _createdBy,
            holidays);

        // Assert
        leaveRequest.NumberOfDays.Should().Be(2m);
    }

    // ═══════════════════════════════════════════════════
    // Approve
    // ═══════════════════════════════════════════════════

    [Fact]
    public void Approve_PendingRequest_ShouldSetStatusApproved()
    {
        // Arrange
        var lr = CreateValidLeaveRequest();

        // Act
        lr.Approve("manager@winity.com");

        // Assert
        lr.Status.Should().Be(LeaveRequestStatus.Approved);
        lr.UpdatedBy.Should().Be("manager@winity.com");
        lr.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Approve_AlreadyApproved_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var lr = CreateValidLeaveRequest();
        lr.Approve("manager@winity.com");

        // Act
        var act = () => lr.Approve("manager@winity.com");

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*en attente*");
    }

    // ═══════════════════════════════════════════════════
    // Reject
    // ═══════════════════════════════════════════════════

    [Fact]
    public void Reject_PendingRequest_ShouldSetStatusRejected()
    {
        // Arrange
        var lr = CreateValidLeaveRequest();

        // Act
        lr.Reject("manager@winity.com", "Période chargée");

        // Assert
        lr.Status.Should().Be(LeaveRequestStatus.Rejected);
        lr.RejectionReason.Should().Be("Période chargée");
    }

    [Fact]
    public void Reject_EmptyReason_ShouldThrowArgumentException()
    {
        // Arrange
        var lr = CreateValidLeaveRequest();

        // Act
        var act = () => lr.Reject("manager@winity.com", "   ");

        // Assert
        act.Should().Throw<ArgumentException>()
           .WithMessage("*motif de refus*");
    }

    [Fact]
    public void Reject_AlreadyRejected_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var lr = CreateValidLeaveRequest();
        lr.Reject("manager@winity.com", "Motif");

        // Act
        var act = () => lr.Reject("manager@winity.com", "Autre motif");

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    // ═══════════════════════════════════════════════════
    // Cancel
    // ═══════════════════════════════════════════════════

    [Fact]
    public void Cancel_PendingRequest_ShouldSetStatusCancelled()
    {
        // Arrange
        var lr = CreateValidLeaveRequest();

        // Act
        lr.Cancel("collab@winity.com");

        // Assert
        lr.Status.Should().Be(LeaveRequestStatus.Cancelled);
        lr.UpdatedBy.Should().Be("collab@winity.com");
    }

    [Fact]
    public void Cancel_ApprovedRequest_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var lr = CreateValidLeaveRequest();
        lr.Approve("manager@winity.com");

        // Act
        var act = () => lr.Cancel("collab@winity.com");

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*en attente*");
    }

    // ═══════════════════════════════════════════════════
    // CalculateNumberOfDays (via PreviewNumberOfDays)
    // ═══════════════════════════════════════════════════

    [Fact]
    public void Preview_FullDay_ShouldReturnOne()
    {
        var date = new DateOnly(2026, 6, 16); // lundi
        var result = LeaveRequest.PreviewNumberOfDays(
            date, date, DayPeriod.Morning, DayPeriod.Afternoon);
        result.Should().Be(1m);
    }

    [Fact]
    public void Preview_HalfDayMorning_ShouldReturnHalf()
    {
        var date = new DateOnly(2026, 6, 16);
        var result = LeaveRequest.PreviewNumberOfDays(
            date, date, DayPeriod.Morning, DayPeriod.Morning);
        result.Should().Be(0.5m);
    }

    [Fact]
    public void Preview_HalfDayAfternoon_ShouldReturnHalf()
    {
        var date = new DateOnly(2026, 6, 16);
        var result = LeaveRequest.PreviewNumberOfDays(
            date, date, DayPeriod.Afternoon, DayPeriod.Afternoon);
        result.Should().Be(0.5m);
    }

    [Fact]
    public void Preview_StartAfternoon_ShouldDeductHalfDay()
    {
        // lundi après-midi → mercredi matin = 2 jours
        var result = LeaveRequest.PreviewNumberOfDays(
            new DateOnly(2026, 6, 22),
            new DateOnly(2026, 6, 24),
            DayPeriod.Afternoon,
            DayPeriod.Afternoon);
        result.Should().Be(2.5m);
    }

    [Fact]
    public void Preview_EndMorning_ShouldDeductHalfDay()
    {
        var result = LeaveRequest.PreviewNumberOfDays(
            new DateOnly(2026, 6, 22),
            new DateOnly(2026, 6, 24),
            DayPeriod.Morning,
            DayPeriod.Morning);
        result.Should().Be(2.5m);
    }

    // ═══════════════════════════════════════════════════
    // Recalculate
    // ═══════════════════════════════════════════════════

    [Fact]
    public void Recalculate_NewHolidayAdded_ShouldReduceNumberOfDays()
    {
        // Arrange — lundi à mercredi = 3 jours initialement
        var lr = LeaveRequest.Create(
            _employeeId, LeaveType.PaidLeave,
            new DateOnly(2026, 6, 22),
            new DateOnly(2026, 6, 24),
            DayPeriod.Morning, DayPeriod.Afternoon,
            "Test", _createdBy);

        lr.NumberOfDays.Should().Be(3m);

        // Act — on ajoute mardi comme férié
        var newHolidays = new HashSet<DateOnly> { new DateOnly(2026, 6, 23) };
        var delta = lr.Recalculate(newHolidays, _createdBy);

        // Assert
        lr.NumberOfDays.Should().Be(2m);
        delta.Should().Be(1m); // ancien - nouveau = 3 - 2
    }

    [Fact]
    public void Recalculate_NoChange_ShouldReturnZeroDelta()
    {
        // Arrange
        var lr = CreateValidLeaveRequest();
        var oldDays = lr.NumberOfDays;

        // Act — aucun jour férié
        var delta = lr.Recalculate(new HashSet<DateOnly>(), _createdBy);

        // Assert
        delta.Should().Be(0m);
        lr.NumberOfDays.Should().Be(oldDays);
    }

    // ═══════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════

    private static LeaveRequest CreateValidLeaveRequest() =>
        LeaveRequest.Create(
            _employeeId,
            LeaveType.PaidLeave,
            new DateOnly(2026, 6, 16), // lundi
            new DateOnly(2026, 6, 18), // mercredi
            DayPeriod.Morning,
            DayPeriod.Afternoon,
            "Vacances",
            _createdBy);
}
