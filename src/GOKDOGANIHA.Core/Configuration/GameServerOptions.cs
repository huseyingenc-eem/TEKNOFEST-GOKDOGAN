namespace GOKDOGANIHA.Core.Configuration;

public sealed class GameServerOptions
{
    public string BaseUrl { get; set; } = "http://127.0.0.25:5000";
    public string KullaniciAdi { get; set; } = "";
    public string Sifre { get; set; } = "";
    public int TakimNumarasi { get; set; }
}
