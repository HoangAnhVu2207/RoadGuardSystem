namespace RoadGuardSystem.BusinessObjects.Retention;

// PR41A evaluator produces review eligibility only; it never authorizes deletion.
public sealed record RetentionEligibilityInput(bool InventoryComplete, bool BasisConfirmed,
    bool BasisCurrent, int ActiveHoldCount, IReadOnlyList<DateTimeOffset?> ObligationEligibleAfter);
public sealed record RetentionEligibilityResult(string Eligibility, IReadOnlyList<string> ReasonCodes,
    DateTimeOffset? EligibleAfter);

public static class RetentionEligibilityEvaluator
{
    public static DateTimeOffset WarrantyEligibleAfter(DateOnly warrantyEndDate)
    {
        // Vietnam uses UTC+07:00 without daylight saving. Retain the entire local calendar day.
        var nextDay = warrantyEndDate.AddYears(5).AddDays(1);
        return new DateTimeOffset(nextDay.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(7)).ToUniversalTime();
    }
    public static RetentionEligibilityResult Evaluate(RetentionEligibilityInput input, DateTimeOffset evaluatedAt)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.ObligationEligibleAfter);
        ArgumentOutOfRangeException.ThrowIfNegative(input.ActiveHoldCount);
        var reasons = new List<string>();
        if (input.ActiveHoldCount > 0) reasons.Add("ACTIVE_HOLD");
        if (!input.InventoryComplete) reasons.Add("INVENTORY_INCOMPLETE");
        if (!input.BasisConfirmed) reasons.Add("BASIS_UNCONFIRMED");
        if (!input.BasisCurrent) reasons.Add("BASIS_STALE");
        if (input.ObligationEligibleAfter.Count == 0 || input.ObligationEligibleAfter.Any(value => value is null))
            reasons.Add("OBLIGATION_UNRESOLVED");
        var proven = input.InventoryComplete && input.BasisConfirmed && input.BasisCurrent
            && input.ObligationEligibleAfter.Count > 0 && input.ObligationEligibleAfter.All(value => value is not null);
        DateTimeOffset? eligibleAfter = proven ? input.ObligationEligibleAfter.Max()!.Value.ToUniversalTime() : null;
        if (eligibleAfter > evaluatedAt) reasons.Add("RETENTION_PERIOD_ACTIVE");
        var eligibility = input.ActiveHoldCount > 0 ? "BLOCKED_HOLD"
            : !proven ? "WAITING_RETENTION_BASIS"
            : eligibleAfter > evaluatedAt ? "RETAIN_UNTIL" : "ELIGIBLE_FOR_REVIEW";
        return new(eligibility, reasons.ToArray(), eligibleAfter);
    }
}
