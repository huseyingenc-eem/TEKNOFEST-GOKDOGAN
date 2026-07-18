using GOKDOGANIHA.Core.Models.Server;
using GOKDOGANIHA.Core.Services.Telemetry;

namespace GOKDOGANIHA.Tests.Services;

public class OpponentTelemetrySanitizerTests
{
    [Fact]
    public void Clean_preserves_every_documented_field_without_shifting_values()
    {
        var source = Position(
            team: 17,
            latitude: 41.5118256,
            longitude: 36.11993,
            altitude: 36.25,
            pitch: -8.5,
            heading: 127.75,
            roll: 19.5,
            speed: 41.25,
            ageMs: 467);

        var result = Assert.Single(OpponentTelemetrySanitizer.Clean([source]));

        Assert.Equal(17, result.TakimNumarasi);
        Assert.Equal(41.5118256, result.Enlem);
        Assert.Equal(36.11993, result.Boylam);
        Assert.Equal(36.25, result.Irtifa);
        Assert.Equal(-8.5, result.Dikilme);
        Assert.Equal(127.75, result.Yonelme);
        Assert.Equal(19.5, result.Yatis);
        Assert.Equal(41.25, result.Hiz);
        Assert.Equal(467, result.ZamanFarkiMs);
    }

    [Fact]
    public void Clean_rejects_dirty_fields_deduplicates_by_freshness_and_sorts_by_team()
    {
        var teamTwoOld = Position(team: 2, speed: 20, ageMs: 900);
        var teamTwoFresh = Position(team: 2, speed: 25, ageMs: 100);
        var positions = new[]
        {
            Position(team: 8, ageMs: 250),
            teamTwoOld,
            Position(team: 3, latitude: 91),
            Position(team: 4, pitch: -91),
            Position(team: 5, heading: 361),
            Position(team: 6, roll: double.NaN),
            Position(team: 7, speed: -1),
            Position(team: 9, altitude: double.PositiveInfinity),
            Position(team: 10, ageMs: 5_001),
            teamTwoFresh
        };

        var result = OpponentTelemetrySanitizer.Clean(positions);

        Assert.Equal([2, 8], result.Select(position => position.TakimNumarasi));
        Assert.Same(teamTwoFresh, result[0]);
        Assert.Equal(25, result[0].Hiz);
    }

    [Fact]
    public void Clean_handles_missing_list_without_throwing()
    {
        Assert.Empty(OpponentTelemetrySanitizer.Clean(null));
    }

    private static KonumBilgisi Position(
        int team,
        double latitude = 41.5,
        double longitude = 36.1,
        double altitude = 100,
        double pitch = 5,
        double heading = 180,
        double roll = -7,
        double speed = 30,
        int ageMs = 200)
        => new(team, latitude, longitude, altitude, pitch, heading, roll, speed, ageMs);
}
