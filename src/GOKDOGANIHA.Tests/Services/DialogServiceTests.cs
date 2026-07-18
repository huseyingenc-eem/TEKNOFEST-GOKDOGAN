using System.Threading.Tasks;
using GOKDOGANIHA.Core.Abstractions;

namespace GOKDOGANIHA.Tests.Services;

public class DialogServiceTests
{
    // Fake impl just to prove the interface contract — WPF MessageBox'ı unit
    // test'inden çağırmak istemiyoruz. Asıl değeri: VM'ler IDialogService
    // üzerinden konuşuyor olmalı, DialogService gerçek impl bunu karşılıyor.
    private sealed class FakeDialog : IDialogService
    {
        public int InfoCount; public int WarnCount; public int ErrorCount;
        public string? LastTitle; public string? LastMessage;
        public Task ShowInfoAsync(string t, string m)  { InfoCount++;  LastTitle = t; LastMessage = m; return Task.CompletedTask; }
        public Task ShowWarnAsync(string t, string m)  { WarnCount++;  LastTitle = t; LastMessage = m; return Task.CompletedTask; }
        public Task ShowErrorAsync(string t, string m) { ErrorCount++; LastTitle = t; LastMessage = m; return Task.CompletedTask; }
        public Task<bool> ConfirmAsync(string t, string m, string yesText = "Evet", string noText = "İptal")
        {
            LastTitle = t; LastMessage = m; return Task.FromResult(false);
        }
    }

    [Fact]
    public async Task FakeDialog_records_invocations()
    {
        var d = new FakeDialog();
        await d.ShowInfoAsync("TITLE", "HELLO");
        await d.ShowErrorAsync("X", "Y");

        Assert.Equal(1, d.InfoCount);
        Assert.Equal(1, d.ErrorCount);
        Assert.Equal("X", d.LastTitle);
        Assert.Equal("Y", d.LastMessage);
    }
}
