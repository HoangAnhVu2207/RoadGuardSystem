using RoadGuardSystem.DTOs.Reporting;

namespace RoadGuardSystem.Services.Reporting;

public static class CurrentRepairCaptureConsumer
{
    public static ReportingCaptureDto Apply(ReportingCaptureDto capture, CurrentRepairFacts facts)
    {
        ArgumentNullException.ThrowIfNull(capture); ArgumentNullException.ThrowIfNull(facts);
        if (facts.ProjectId!=capture.Summary.ProjectId || facts.Items is null || facts.MissingReasons is null ||
            facts.Items.Any(item=>item is null || item.ProjectId!=facts.ProjectId || item.Id==Guid.Empty || item.ObligationId==Guid.Empty ||
                string.IsNullOrWhiteSpace(item.Version) || string.IsNullOrWhiteSpace(item.ObligationVersion) ||
                item.Presentation is not ("UNREPAIRED" or "REPORTED_AWAITING_REVIEW" or "CONFIRMED") ||
                item.DecisionId==Guid.Empty || item.DecisionId.HasValue!=!string.IsNullOrWhiteSpace(item.DecisionVersion) ||
                item.Presentation=="CONFIRMED" && item.DecisionId is null) ||
            facts.Items.Select(item=>item.Id).Distinct().Count()!=facts.Items.Length || facts.MissingReasons.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("Current repair producer facts are invalid or outside scope.");
        var metrics=capture.Summary.Metrics.Where(metric=>metric.Code!="repairItemsByStatus").ToList();
        var items=capture.Items.Where(item=>item.Metric!="repairItemsByStatus").ToList();
        // Unknown inventory stays unknown; even known rows cannot assert a complete denominator.
        if (facts.MissingReasons.Length!=0)
            metrics.Add(new("repairItemsByStatus",new(),null,"count",null,null,false,"UNAVAILABLE",facts.MissingReasons.Distinct().ToArray(),[]));
        else
        {
            foreach (var group in facts.Items.GroupBy(item=>item.Presentation).OrderBy(group=>group.Key,StringComparer.Ordinal))
            {
                var refs=group.SelectMany(item=>new[] { new ReportingSourceRefDto("RepairItem",item.Id,item.Version),
                    new ReportingSourceRefDto("RepairObligation",item.ObligationId,item.ObligationVersion) }
                    .Concat(item.DecisionId is Guid decision ? [new ReportingSourceRefDto("RepairDecision",decision,item.DecisionVersion!)] : []))
                    .Distinct().ToArray();
                metrics.Add(new("repairItemsByStatus",new(Status:group.Key),group.LongCount(),"count",null,null,false,"AVAILABLE",[],refs));
                items.AddRange(group.Select(item=>new ReportingItemDto(item.Id,"repairItemsByStatus","RepairItem",item.Version,item.Presentation)));
            }
            if(facts.Items.Length==0)metrics.Add(new("repairItemsByStatus",new(),0,"count",null,null,false,"AVAILABLE",[],[]));
        }
        // Keep period allocation, rate and its denominator under their existing source gates.
        return capture with{Summary=capture.Summary with{Metrics=metrics.ToArray()},Items=items.ToArray()};
    }
}
