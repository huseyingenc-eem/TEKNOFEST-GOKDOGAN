namespace GOKDOGANIHA.Core.Models;

public enum GpsHealth
{
    Unavailable,
    Critical,
    Warning,
    Healthy
}

/// <summary>Operatör gösterimi için fix, uydu ve HDOP'u tek sağlık durumunda birleştirir.</summary>
public static class GpsHealthEvaluator
{
    public static GpsHealth Evaluate(bool telemetryValid, GpsFix fix, int satellites, double? hdop)
    {
        if (!telemetryValid) return GpsHealth.Unavailable;
        var hasThreeDimensionalFix = fix is GpsFix.Fix3D or GpsFix.Dgps or GpsFix.Rtk;
        if (!hasThreeDimensionalFix || satellites < 7 || hdop is > 2.0)
            return GpsHealth.Critical;
        if (satellites < 12 || hdop is null or <= 0 || hdop > 1.4)
            return GpsHealth.Warning;
        return GpsHealth.Healthy;
    }
}
