using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GOKDOGANIHA.Core.Configuration;

public sealed class GameServerOptions : INotifyPropertyChanged
{
    private string _baseUrl = "http://127.0.0.25:5000";
    private string _kullaniciAdi = string.Empty;
    private string _sifre = string.Empty;
    private int _takimNumarasi;
    private CompetitionServerEnvironment _environment = CompetitionServerEnvironment.Official;

    public string BaseUrl { get => _baseUrl; set => Set(ref _baseUrl, value?.Trim() ?? string.Empty); }
    public string KullaniciAdi { get => _kullaniciAdi; set => Set(ref _kullaniciAdi, value ?? string.Empty); }
    public string Sifre { get => _sifre; set => Set(ref _sifre, value ?? string.Empty); }
    public int TakimNumarasi { get => _takimNumarasi; set => Set(ref _takimNumarasi, Math.Max(0, value)); }
    public CompetitionServerEnvironment Environment { get => _environment; set => Set(ref _environment, value); }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
