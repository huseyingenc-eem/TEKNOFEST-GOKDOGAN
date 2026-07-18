using System.Collections.Generic;
using GOKDOGANIHA.Core.Models.Connection;

namespace GOKDOGANIHA.Tests.Models;

public class ConnectionStatusTests
{
    [Fact]
    public void New_status_starts_offline()
    {
        var s = new ConnectionStatus("SUNUCU");

        Assert.Equal(ConnectionPhase.Offline, s.Phase);
        Assert.True(s.IsOffline);
        Assert.False(s.IsOnline);
        Assert.Equal("SUNUCU", s.Name);
    }

    [Fact]
    public void MarkOnline_sets_online_and_clears_retry_counters()
    {
        var s = new ConnectionStatus("SUNUCU");
        s.MarkRetrying(3, 8);

        s.MarkOnline();

        Assert.Equal(ConnectionPhase.Online, s.Phase);
        Assert.True(s.IsOnline);
        Assert.False(s.IsBusy);
        Assert.Equal(0, s.RetryAttempt);
        Assert.Equal(0.0, s.NextRetryInSeconds);
    }

    [Fact]
    public void MarkRetrying_tracks_attempt_and_is_busy()
    {
        var s = new ConnectionStatus("TELEMETRİ");

        s.MarkRetrying(2, 4, "Yeniden bağlanıyor");

        Assert.Equal(ConnectionPhase.Retrying, s.Phase);
        Assert.True(s.IsBusy);
        Assert.Equal(2, s.RetryAttempt);
        Assert.Equal(4.0, s.NextRetryInSeconds);
    }

    [Fact]
    public void MarkRetrying_clamps_attempt_and_delay_to_valid_range()
    {
        var s = new ConnectionStatus("VİDEO");

        s.MarkRetrying(0, -5);

        Assert.Equal(1, s.RetryAttempt);
        Assert.Equal(0.0, s.NextRetryInSeconds);
    }

    [Fact]
    public void MarkFaulted_sets_faulted_with_message()
    {
        var s = new ConnectionStatus("SUNUCU");

        s.MarkFaulted("Zaman aşımı");

        Assert.Equal(ConnectionPhase.Faulted, s.Phase);
        Assert.True(s.IsFaulted);
        Assert.Equal("Zaman aşımı", s.Message);
    }

    [Fact]
    public void MarkFaulted_falls_back_to_default_message_when_blank()
    {
        var s = new ConnectionStatus("SUNUCU");

        s.MarkFaulted("   ");

        Assert.Equal("Bağlantı hatası", s.Message);
    }

    [Fact]
    public void Phase_change_raises_derived_property_notifications()
    {
        var s = new ConnectionStatus("SUNUCU");
        var changed = new List<string>();
        s.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        s.MarkOnline();

        Assert.Contains(nameof(ConnectionStatus.Phase), changed);
        Assert.Contains(nameof(ConnectionStatus.IsOnline), changed);
        Assert.Contains(nameof(ConnectionStatus.IsBusy), changed);
    }

    [Fact]
    public void Empty_name_falls_back_to_default()
    {
        var s = new ConnectionStatus("   ");

        Assert.Equal("BAĞLANTI", s.Name);
    }
}
