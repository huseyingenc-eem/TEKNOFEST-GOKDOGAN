namespace GOKDOGANIHA.Core.Models;

/// <summary>
/// GPS fix tipi. MAVLink GPS_RAW_INT.fix_type ile uyumludur.
/// </summary>
public enum GpsFix
{
    None,
    NoFix,
    Fix2D,
    Fix3D,
    Dgps,
    Rtk
}
