namespace GOKDOGANIHA.Core.Models;

/// <summary>
/// Tek bir kamera frame'inde kilitlenme kurallarının doğrulama sonucu.
/// 5 kurala karşılık 5 boolean + diagnostik metrikler.
/// </summary>
public sealed record LockFrameResult(
    bool Valid,
    bool CenterInside,
    bool SizeWithinTolerance,
    bool IouAboveThreshold,
    bool IsNotRepeatLock,
    double IoU,
    double SizeErrorPercent,
    string? FailReason);

/// <summary>İki dikdörtgen — kamera frame koordinatlarında.</summary>
public readonly record struct BoundingBox(double X, double Y, double Width, double Height)
{
    public double Left => X;
    public double Top => Y;
    public double Right => X + Width;
    public double Bottom => Y + Height;
    public double CenterX => X + Width / 2;
    public double CenterY => Y + Height / 2;
}
