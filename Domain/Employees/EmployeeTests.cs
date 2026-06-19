using in_ctech_management_backend.Domain.Employees;
using in_ctech_management_backend.Domain.Employees.Enums;
using in_ctech_management_backend.Domain.Jobs;
using in_ctech_management_backend.Domain.PurchaseOrders.Enums;

namespace in_ctech_management_backend.Tests.Domain.Employees;

public class EmployeeTests
{
	private static readonly JobId _jobId = new JobId(Guid.NewGuid());

	// ═══════════════════════════════════════════════════
	// Helpers
	// ═══════════════════════════════════════════════════

	private static Employee CreatePermanentEmployee(
		string firstName = "Kaoutar",
		string lastName = "Test",
		decimal monthlyLeaveAllowance = 1.5m) =>
		Employee.Create(
			firstName, lastName,
			trigram: "KAT",
			cnss: "123456",
			nationalId: "AB123456",
			phoneNumber: "0600000000",
			email: "kaoutar@winity.com",
			bankAccountNumber: null,
			registrationNumber: "WP-001",
			paymentMethod: PaymentMethod.BANK_TRANSFER,
			contractType: ContractType.PERMANENT,
			freelancerType: null,
			startDate: DateTime.UtcNow.AddYears(-1),
			contractEndDate: null,
			jobId: _jobId,
			managerId: null,
			monthlyLeaveAllowance: monthlyLeaveAllowance);

	private static Employee CreateFreelancerEmployee() =>
		Employee.Create(
			"John", "Doe",
			trigram: "JDO",
			cnss: "654321",
			nationalId: "CD654321",
			phoneNumber: "0611111111",
			email: null,
			bankAccountNumber: null,
			registrationNumber: "WP-002",
			paymentMethod: PaymentMethod.BANK_TRANSFER,
			contractType: ContractType.FREELANCE,
			freelancerType: FreelancerType.SELF_EMPLOYED,
			startDate: null,
			contractEndDate: null,
			jobId: _jobId,
			managerId: null,
			monthlyLeaveAllowance: null);

	// ═══════════════════════════════════════════════════
	// Create
	// ═══════════════════════════════════════════════════

	[Fact]
	public void Create_ValidPermanentEmployee_ShouldSetPropertiesCorrectly()
	{
		var emp = CreatePermanentEmployee();

		Assert.Equal("Kaoutar", emp.FirstName);
		Assert.Equal("Test", emp.LastName);
		Assert.Equal("Kaoutar Test", emp.FullName);
		Assert.True(emp.IsActive);
		Assert.Equal(ContractType.PERMANENT, emp.ContractType);
		Assert.Equal(1.5m, emp.MonthlyLeaveAllowance);
		Assert.Equal(0m, emp.LeaveBalance);
		Assert.Equal(0m, emp.AnnualConsumedLeaves);
	}

	[Fact]
	public void Create_FreelancerEmployee_ShouldHaveNullLeaveBalance()
	{
		var emp = CreateFreelancerEmployee();

		Assert.Null(emp.LeaveBalance);
		Assert.Null(emp.MonthlyLeaveAllowance);
	}

	[Fact]
	public void Create_EmptyFirstName_ShouldThrowArgumentException()
	{
		Assert.Throws<ArgumentException>(() =>
			Employee.Create(
				"", "Test", "KAT", "123", "AB1", "0600",
				null, null, "WP-001",
				PaymentMethod.BANK_TRANSFER,
				ContractType.PERMANENT,
				null, null, null, _jobId, null, 1.5m));
	}

	[Fact]
	public void Create_PermanentWithoutMonthlyAllowance_ShouldThrowArgumentException()
	{
		Assert.Throws<ArgumentException>(() =>
			Employee.Create(
				"Kaoutar", "Test", "KAT", "123", "AB1", "0600",
				null, null, "WP-001",
				PaymentMethod.BANK_TRANSFER,
				ContractType.PERMANENT,
				null, null, null, _jobId, null,
				monthlyLeaveAllowance: null));
	}

	[Fact]
	public void Create_InvalidEmail_ShouldThrowArgumentException()
	{
		Assert.Throws<ArgumentException>(() =>
			Employee.Create(
				"Kaoutar", "Test", "KAT", "123", "AB1", "0600",
				email: "not-an-email",
				bankAccountNumber: null,
				registrationNumber: "WP-001",
				PaymentMethod.BANK_TRANSFER,
				ContractType.PERMANENT,
				null, null, null, _jobId, null, 1.5m));
	}

	// ═══════════════════════════════════════════════════
	// Activate / Deactivate
	// ═══════════════════════════════════════════════════

	[Fact]
	public void Deactivate_ActiveEmployee_ShouldSetIsActiveFalse()
	{
		var emp = CreatePermanentEmployee();
		emp.Deactivate();
		Assert.False(emp.IsActive);
	}

	[Fact]
	public void Deactivate_AlreadyInactive_ShouldThrowInvalidOperationException()
	{
		var emp = CreatePermanentEmployee();
		emp.Deactivate();
		Assert.Throws<InvalidOperationException>(() => emp.Deactivate());
	}

	[Fact]
	public void Activate_InactiveEmployee_ShouldSetIsActiveTrue()
	{
		var emp = CreatePermanentEmployee();
		emp.Deactivate();
		emp.Activate();
		Assert.True(emp.IsActive);
	}

	[Fact]
	public void Activate_AlreadyActive_ShouldThrowInvalidOperationException()
	{
		var emp = CreatePermanentEmployee();
		Assert.Throws<InvalidOperationException>(() => emp.Activate());
	}

	// ═══════════════════════════════════════════════════
	// AccrueMonthlyLeave
	// ═══════════════════════════════════════════════════

	[Fact]
	public void AccrueMonthlyLeave_EligibleEmployee_ShouldIncreaseLeaveBalance()
	{
		var emp = CreatePermanentEmployee(monthlyLeaveAllowance: 1.5m);
		var today = DateTime.UtcNow;

		var result = emp.AccrueMonthlyLeave(today);

		Assert.True(result);
		Assert.Equal(1.5m, emp.LeaveBalance);
	}

	[Fact]
	public void AccrueMonthlyLeave_CalledTwiceSameMonth_ShouldCreditOnlyOnce()
	{
		var emp = CreatePermanentEmployee(monthlyLeaveAllowance: 1.5m);
		var today = DateTime.UtcNow;

		emp.AccrueMonthlyLeave(today);
		var result = emp.AccrueMonthlyLeave(today);

		Assert.False(result);
		Assert.Equal(1.5m, emp.LeaveBalance);
	}

	[Fact]
	public void AccrueMonthlyLeave_FreelancerEmployee_ShouldReturnFalse()
	{
		var emp = CreateFreelancerEmployee();
		var result = emp.AccrueMonthlyLeave(DateTime.UtcNow);
		Assert.False(result);
	}

	// ═══════════════════════════════════════════════════
	// AddConsumedLeaves / AdjustConsumedLeaves
	// ═══════════════════════════════════════════════════

	[Fact]
	public void AddConsumedLeaves_ShouldDeductFromBalanceAndIncreaseConsumed()
	{
		var emp = CreatePermanentEmployee();
		emp.AccrueMonthlyLeave(DateTime.UtcNow);
		emp.AddConsumedLeaves(1m);

		Assert.Equal(0.5m, emp.LeaveBalance);
		Assert.Equal(1m, emp.AnnualConsumedLeaves);
	}

	[Fact]
	public void AddConsumedLeaves_ZeroDays_ShouldThrowArgumentException()
	{
		var emp = CreatePermanentEmployee();
		Assert.Throws<ArgumentException>(() => emp.AddConsumedLeaves(0m));
	}

	[Fact]
	public void AdjustConsumedLeaves_PositiveDelta_ShouldCreditBalance()
	{
		var emp = CreatePermanentEmployee();
		emp.AccrueMonthlyLeave(DateTime.UtcNow);
		emp.AddConsumedLeaves(1m);

		emp.AdjustConsumedLeaves(0.5m);

		Assert.Equal(1m, emp.LeaveBalance);
		Assert.Equal(0.5m, emp.AnnualConsumedLeaves);
	}

	[Fact]
	public void AdjustConsumedLeaves_ZeroDelta_ShouldDoNothing()
	{
		var emp = CreatePermanentEmployee();
		emp.AccrueMonthlyLeave(DateTime.UtcNow);
		var balanceBefore = emp.LeaveBalance;

		emp.AdjustConsumedLeaves(0m);

		Assert.Equal(balanceBefore, emp.LeaveBalance);
	}

	// ═══════════════════════════════════════════════════
	// UpdatePurchasePrice
	// ═══════════════════════════════════════════════════

	[Fact]
	public void UpdatePurchasePrice_ValidPrice_ShouldSetPurchasePrice()
	{
		var emp = CreatePermanentEmployee();
		emp.UpdatePurchasePrice(500m, Currency.MAD);

		Assert.Equal(500m, emp.PurchasePrice);
		Assert.Equal(Currency.MAD, emp.PurchasePriceCurrency);
	}

	[Fact]
	public void UpdatePurchasePrice_NegativePrice_ShouldThrowArgumentException()
	{
		var emp = CreatePermanentEmployee();
		Assert.Throws<ArgumentException>(() => emp.UpdatePurchasePrice(-100m, Currency.MAD));
	}

	[Fact]
	public void UpdatePurchasePrice_PriceWithoutCurrency_ShouldThrowArgumentException()
	{
		var emp = CreatePermanentEmployee();
		Assert.Throws<ArgumentException>(() => emp.UpdatePurchasePrice(500m, null));
	}

	[Fact]
	public void UpdatePurchasePrice_Null_ShouldClearPurchasePrice()
	{
		var emp = CreatePermanentEmployee();
		emp.UpdatePurchasePrice(500m, Currency.MAD);
		emp.UpdatePurchasePrice(null, null);

		Assert.Null(emp.PurchasePrice);
		Assert.Null(emp.PurchasePriceCurrency);
	}

	// ═══════════════════════════════════════════════════
	// ResetAnnualConsumedLeaves
	// ═══════════════════════════════════════════════════

	[Fact]
	public void ResetAnnualConsumedLeaves_ShouldSetConsumedToZero()
	{
		var emp = CreatePermanentEmployee();
		emp.AccrueMonthlyLeave(DateTime.UtcNow);
		emp.AddConsumedLeaves(1m);

		emp.ResetAnnualConsumedLeaves();

		Assert.Equal(0m, emp.AnnualConsumedLeaves);
	}

	// ═══════════════════════════════════════════════════
	// IsLeaveEligible
	// ═══════════════════════════════════════════════════

	[Theory]
	[InlineData(ContractType.PERMANENT, true)]
	[InlineData(ContractType.FIXED_TERM, true)]
	[InlineData(ContractType.ANAPEC, true)]
	[InlineData(ContractType.FREELANCE, false)]
	public void IsLeaveEligible_ShouldReturnCorrectResult(ContractType contractType, bool expected)
	{
		Assert.Equal(expected, Employee.IsLeaveEligible(contractType));
	}

	// ═══════════════════════════════════════════════════
	// EmployeeStatusHistory
	// ═══════════════════════════════════════════════════

	[Fact]
	public void EmployeeStatusHistory_Create_ShouldSetPropertiesCorrectly()
	{
		var empId = new EmployeeId(Guid.NewGuid());
		var history = EmployeeStatusHistory.Create(empId, true, false, null);

		Assert.Equal(empId, history.EmployeeId);
		Assert.True(history.OldStatus);
		Assert.False(history.NewStatus);
		Assert.Null(history.ChangedByEmployeeId);
	}

	[Fact]
	public void EmployeeStatusHistory_Create_NullEmployeeId_ShouldThrowArgumentNullException()
	{
		Assert.Throws<ArgumentNullException>(() =>
			EmployeeStatusHistory.Create(null!, true, false, null));
	}
}
