using RoadGuardSystem.aBusinessObjects.Commons;
namespace RoadGuardSystem.BusinessObjects.Surveys;

public static class SurveyTypeWire
{
    public static string Code(SurveyType type) => type switch { SurveyType.Original => "BASELINE", SurveyType.Periodic => "PERIODIC", SurveyType.Supplementary => "AD_HOC", _ => "UNKNOWN" };
}
