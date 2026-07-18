using System.Text.Json;
using GOKDOGANIHA.Core.Models.Server;

namespace GOKDOGANIHA.Tests.Services;

public class ProtocolSerializationTests
{
    [Fact]
    public void Outgoing_gps_time_does_not_include_day_field()
    {
        var packet = new TelemetryPacket(
            7, 41.5, 36.1, 100, 0, 180, 0, 25, 80, 1, 0,
            0, 0, 0, 0, new CompetitionTime(12, 30, 45, 123));

        var json = JsonSerializer.Serialize(packet);

        Assert.Contains("\"gps_saati\"", json);
        Assert.DoesNotContain("\"gun\"", json);
        Assert.Contains("\"saat\":12", json);
    }

    [Fact]
    public void Lock_and_kamikaze_times_follow_documented_shape()
    {
        var time = new CompetitionTime(12, 30, 45, 123);
        var lockJson = JsonSerializer.Serialize(new KilitlenmeBilgisi(time, 1));
        var kamikazeJson = JsonSerializer.Serialize(new KamikazeBilgisi(time, time, "teknofest"));

        Assert.DoesNotContain("\"gun\"", lockJson);
        Assert.DoesNotContain("\"gun\"", kamikazeJson);
        Assert.Contains("\"kilitlenmeBitisZamani\"", lockJson);
        Assert.Contains("\"kamikazeBaslangicZamani\"", kamikazeJson);
    }

    [Fact]
    public void Opponent_speed_uses_iha_hizi_response_name()
    {
        var position = new KonumBilgisi(2, 41.5, 36.1, 100, 0, 180, 0, 41, 100);
        var json = JsonSerializer.Serialize(position);

        Assert.Contains("\"iha_hizi\":41", json);
        Assert.DoesNotContain("\"iha_hiz\":", json);
    }
}
