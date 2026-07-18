using GOKDOGANIHA.Core.Models.Server;
using GOKDOGANIHA.Core.Services.Telemetry;

namespace GOKDOGANIHA.Tests.Services;

public class CompetitionTelemetryValidatorTests
{
    [Fact]
    public void Accepts_document_compliant_packet()
    {
        var result = CompetitionTelemetryValidator.Validate(ValidPacket());
        Assert.True(result.IsValid, result.Message);
    }

    [Theory]
    [InlineData(-90, true)]
    [InlineData(90, true)]
    [InlineData(-90.01, false)]
    [InlineData(90.01, false)]
    public void Validates_pitch_range(double pitch, bool expected)
    {
        var result = CompetitionTelemetryValidator.Validate(ValidPacket() with { Dikilme = pitch });
        Assert.Equal(expected, result.IsValid);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(360, true)]
    [InlineData(-0.01, false)]
    [InlineData(360.01, false)]
    public void Validates_heading_range(double heading, bool expected)
    {
        var result = CompetitionTelemetryValidator.Validate(ValidPacket() with { Yonelme = heading });
        Assert.Equal(expected, result.IsValid);
    }

    [Fact]
    public void Rejects_entire_packet_when_one_field_is_invalid()
    {
        var result = CompetitionTelemetryValidator.Validate(ValidPacket() with { Batarya = 101 });
        Assert.False(result.IsValid);
        Assert.Contains("iha_batarya", result.Message);
    }

    [Fact]
    public void Lock_requires_non_zero_target_dimensions()
    {
        var result = CompetitionTelemetryValidator.Validate(ValidPacket() with
        {
            Kilitlenme = 1,
            HedefGenislik = 0,
            HedefYukseklik = 0
        });

        Assert.False(result.IsValid);
        Assert.Contains("hedef genişliği", result.Message);
    }

    [Fact]
    public void Rejects_non_vehicle_utc_time()
    {
        var result = CompetitionTelemetryValidator.Validate(ValidPacket() with
        {
            GpsSaati = new CompetitionTime(25, 0, 0, 0)
        });

        Assert.False(result.IsValid);
        Assert.Contains("gps_saati", result.Message);
    }

    private static TelemetryPacket ValidPacket() => new(
        TakimNumarasi: 7,
        Enlem: 41.508775,
        Boylam: 36.118335,
        Irtifa: 38,
        Dikilme: 7,
        Yonelme: 210,
        Yatis: -30,
        Hiz: 28,
        Batarya: 50,
        Otonom: 1,
        Kilitlenme: 1,
        HedefMerkezX: 300,
        HedefMerkezY: 230,
        HedefGenislik: 30,
        HedefYukseklik: 43,
        GpsSaati: new CompetitionTime(11, 38, 37, 654));
}
