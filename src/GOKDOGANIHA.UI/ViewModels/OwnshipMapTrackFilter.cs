namespace GOKDOGANIHA.UI.ViewModels;

/// <summary>
/// Harita görselleştirmesi için GPS sıçrama kapısı, One Euro konum filtresi
/// ve dairesel yer-izi yumuşatması. Uçuş kontrolünde kullanılmaz.
/// </summary>
internal sealed class OwnshipMapTrackFilter
{
    private const double EarthRadiusMeters = 6_371_000.0;
    private const double MinimumCourseSpeedMps = 1.0;
    private static readonly TimeSpan ResetAfter = TimeSpan.FromSeconds(10);

    private readonly OneEuroScalarFilter _northFilter = new(minCutoff: 1.0, beta: 0.025, derivativeCutoff: 1.0);
    private readonly OneEuroScalarFilter _eastFilter = new(minCutoff: 1.0, beta: 0.025, derivativeCutoff: 1.0);
    private bool _initialized;
    private double _originLatitude;
    private double _originLongitude;
    private double _lastAcceptedLatitude;
    private double _lastAcceptedLongitude;
    private DateTime _lastAcceptedUtc;
    private bool _hasSmoothedCourse;
    private double _courseX;
    private double _courseY;
    private DateTime _lastCourseUtc;

    public OwnshipMapSample Apply(
        double latitude,
        double longitude,
        double headingDeg,
        double? groundTrackDeg,
        double groundSpeedMps,
        double? gpsHdop,
        DateTime sampleUtc)
    {
        if (!_initialized || sampleUtc - _lastAcceptedUtc > ResetAfter || sampleUtc <= _lastAcceptedUtc)
            return Initialize(latitude, longitude, headingDeg, groundTrackDeg, groundSpeedMps, sampleUtc);

        var elapsedSeconds = Math.Max(0.02, (sampleUtc - _lastAcceptedUtc).TotalSeconds);
        var movementMeters = DistanceMeters(
            _lastAcceptedLatitude,
            _lastAcceptedLongitude,
            latitude,
            longitude);

        // EKF innovation denetiminin harita-katmanı karşılığı: yeni ölçümün
        // beklenen hıza göre ulaşılabilir bir zarf içinde kalması gerekir.
        var hdopAllowance = gpsHdop is > 0 and < 20 ? gpsHdop.Value * 3.0 : 6.0;
        var plausibleSpeed = Math.Clamp(Math.Max(groundSpeedMps, 10.0), 10.0, 100.0);
        var allowedMovement = Math.Max(12.0, (plausibleSpeed * 1.8 + 15.0) * elapsedSeconds + hdopAllowance);
        if (movementMeters > allowedMovement)
            return new OwnshipMapSample(0, 0, CurrentCourse(headingDeg), Accepted: false);

        var previousLatitude = _lastAcceptedLatitude;
        var previousLongitude = _lastAcceptedLongitude;
        _lastAcceptedLatitude = latitude;
        _lastAcceptedLongitude = longitude;
        _lastAcceptedUtc = sampleUtc;

        ToLocalMeters(latitude, longitude, out var north, out var east);
        var filteredNorth = _northFilter.Apply(north, sampleUtc);
        var filteredEast = _eastFilter.Apply(east, sampleUtc);
        FromLocalMeters(filteredNorth, filteredEast, out var filteredLatitude, out var filteredLongitude);

        var desiredCourse = SelectCourse(
            headingDeg,
            groundTrackDeg,
            groundSpeedMps,
            previousLatitude,
            previousLongitude,
            latitude,
            longitude,
            movementMeters);
        var smoothedCourse = SmoothCourse(desiredCourse, sampleUtc);
        return new OwnshipMapSample(filteredLatitude, filteredLongitude, smoothedCourse, Accepted: true);
    }

    public void Reset()
    {
        _initialized = false;
        _hasSmoothedCourse = false;
        _northFilter.Reset();
        _eastFilter.Reset();
    }

    private OwnshipMapSample Initialize(
        double latitude,
        double longitude,
        double headingDeg,
        double? groundTrackDeg,
        double groundSpeedMps,
        DateTime sampleUtc)
    {
        Reset();
        _initialized = true;
        _originLatitude = latitude;
        _originLongitude = longitude;
        _lastAcceptedLatitude = latitude;
        _lastAcceptedLongitude = longitude;
        _lastAcceptedUtc = sampleUtc;
        _northFilter.Apply(0, sampleUtc);
        _eastFilter.Apply(0, sampleUtc);

        var initialCourse = groundSpeedMps >= MinimumCourseSpeedMps && IsAngle(groundTrackDeg)
            ? Normalize(groundTrackDeg!.Value)
            : Normalize(headingDeg);
        SetCourse(initialCourse, sampleUtc);
        return new OwnshipMapSample(latitude, longitude, initialCourse, Accepted: true);
    }

    private double SelectCourse(
        double headingDeg,
        double? groundTrackDeg,
        double groundSpeedMps,
        double previousLatitude,
        double previousLongitude,
        double latitude,
        double longitude,
        double movementMeters)
    {
        // En güvenilir sıra: hız vektörü/COG, yeterli mesafedeki GPS izi,
        // son olarak düşük hızda gövde heading'i.
        if (groundSpeedMps >= MinimumCourseSpeedMps && IsAngle(groundTrackDeg))
            return Normalize(groundTrackDeg!.Value);
        if (groundSpeedMps >= MinimumCourseSpeedMps && movementMeters >= 3.0)
            return InitialBearingDegrees(previousLatitude, previousLongitude, latitude, longitude);
        return Normalize(headingDeg);
    }

    private double SmoothCourse(double desiredCourse, DateTime sampleUtc)
    {
        if (!_hasSmoothedCourse)
        {
            SetCourse(desiredCourse, sampleUtc);
            return desiredCourse;
        }

        var dt = Math.Clamp((sampleUtc - _lastCourseUtc).TotalSeconds, 0.02, 2.0);
        // 0,45 s zaman sabiti dönüşü akıcı yapar; sin/cos karışımı
        // 359° -> 1° geçişinin yanlış yönden dolaşmasını engeller.
        var alpha = 1.0 - Math.Exp(-dt / 0.45);
        var radians = desiredCourse * Math.PI / 180.0;
        _courseX = (1.0 - alpha) * _courseX + alpha * Math.Cos(radians);
        _courseY = (1.0 - alpha) * _courseY + alpha * Math.Sin(radians);
        _lastCourseUtc = sampleUtc;
        return Normalize(Math.Atan2(_courseY, _courseX) * 180.0 / Math.PI);
    }

    private void SetCourse(double course, DateTime sampleUtc)
    {
        var radians = course * Math.PI / 180.0;
        _courseX = Math.Cos(radians);
        _courseY = Math.Sin(radians);
        _lastCourseUtc = sampleUtc;
        _hasSmoothedCourse = true;
    }

    private double CurrentCourse(double fallbackHeading)
        => _hasSmoothedCourse
            ? Normalize(Math.Atan2(_courseY, _courseX) * 180.0 / Math.PI)
            : Normalize(fallbackHeading);

    private void ToLocalMeters(double latitude, double longitude, out double north, out double east)
    {
        north = (latitude - _originLatitude) * Math.PI / 180.0 * EarthRadiusMeters;
        east = (longitude - _originLongitude) * Math.PI / 180.0 * EarthRadiusMeters
               * Math.Cos(_originLatitude * Math.PI / 180.0);
    }

    private void FromLocalMeters(double north, double east, out double latitude, out double longitude)
    {
        latitude = _originLatitude + north / EarthRadiusMeters * 180.0 / Math.PI;
        longitude = _originLongitude + east
            / (EarthRadiusMeters * Math.Cos(_originLatitude * Math.PI / 180.0))
            * 180.0 / Math.PI;
    }

    private static bool IsAngle(double? value) => value is { } angle && double.IsFinite(angle);

    private static double DistanceMeters(double lat1, double lng1, double lat2, double lng2)
    {
        var lat1Rad = lat1 * Math.PI / 180.0;
        var lat2Rad = lat2 * Math.PI / 180.0;
        var deltaLat = (lat2 - lat1) * Math.PI / 180.0;
        var deltaLng = (lng2 - lng1) * Math.PI / 180.0;
        var a = Math.Sin(deltaLat / 2.0) * Math.Sin(deltaLat / 2.0)
                + Math.Cos(lat1Rad) * Math.Cos(lat2Rad)
                * Math.Sin(deltaLng / 2.0) * Math.Sin(deltaLng / 2.0);
        return EarthRadiusMeters * 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));
    }

    private static double InitialBearingDegrees(double lat1, double lng1, double lat2, double lng2)
    {
        var lat1Rad = lat1 * Math.PI / 180.0;
        var lat2Rad = lat2 * Math.PI / 180.0;
        var deltaLng = (lng2 - lng1) * Math.PI / 180.0;
        var y = Math.Sin(deltaLng) * Math.Cos(lat2Rad);
        var x = Math.Cos(lat1Rad) * Math.Sin(lat2Rad)
                - Math.Sin(lat1Rad) * Math.Cos(lat2Rad) * Math.Cos(deltaLng);
        return Normalize(Math.Atan2(y, x) * 180.0 / Math.PI);
    }

    private static double Normalize(double angle)
    {
        if (!double.IsFinite(angle)) return 0;
        var normalized = angle % 360.0;
        return normalized < 0 ? normalized + 360.0 : normalized;
    }

    private sealed class OneEuroScalarFilter(double minCutoff, double beta, double derivativeCutoff)
    {
        private bool _initialized;
        private double _lastRaw;
        private double _lastFiltered;
        private double _lastDerivative;
        private DateTime _lastUtc;

        public double Apply(double value, DateTime sampleUtc)
        {
            if (!_initialized)
            {
                _initialized = true;
                _lastRaw = _lastFiltered = value;
                _lastUtc = sampleUtc;
                return value;
            }

            var dt = Math.Clamp((sampleUtc - _lastUtc).TotalSeconds, 0.02, 2.0);
            var derivative = (value - _lastRaw) / dt;
            var derivativeAlpha = Alpha(derivativeCutoff, dt);
            var filteredDerivative = derivativeAlpha * derivative + (1.0 - derivativeAlpha) * _lastDerivative;
            var cutoff = minCutoff + beta * Math.Abs(filteredDerivative);
            var valueAlpha = Alpha(cutoff, dt);
            var filtered = valueAlpha * value + (1.0 - valueAlpha) * _lastFiltered;

            _lastRaw = value;
            _lastFiltered = filtered;
            _lastDerivative = filteredDerivative;
            _lastUtc = sampleUtc;
            return filtered;
        }

        public void Reset() => _initialized = false;

        private static double Alpha(double cutoff, double dt)
        {
            var tau = 1.0 / (2.0 * Math.PI * cutoff);
            return 1.0 / (1.0 + tau / dt);
        }
    }
}

internal readonly record struct OwnshipMapSample(
    double Latitude,
    double Longitude,
    double Heading,
    bool Accepted);
