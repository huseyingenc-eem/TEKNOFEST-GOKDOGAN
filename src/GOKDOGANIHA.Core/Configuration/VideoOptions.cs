namespace GOKDOGANIHA.Core.Configuration;

/// <summary>
/// Görev video akışı ve kayıt ayarları.
/// </summary>
public sealed class VideoOptions
{
    /// <summary>RTSP (ya da diğer) akış URL'i.</summary>
    public string RtspUrl { get; set; } = "rtsp://192.168.1.100:554/stream";

    /// <summary>"720p/30", "1080p/30", "1080p/60" vb.</summary>
    public string Preset { get; set; } = "1080p/30";
}
