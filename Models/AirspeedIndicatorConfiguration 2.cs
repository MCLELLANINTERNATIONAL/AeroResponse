public record AirspeedIndicatorConfiguration(
    int MinimumSpeed,
    int MaximumSpeed,
    int WhiteArcStart,
    int WhiteArcEnd,
    int GreenArcStart,
    int GreenArcEnd,
    int YellowArcStart,
    int YellowArcEnd,
    int NeverExceedSpeed);