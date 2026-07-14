using AeroResponse.Simulation.Layouts;

namespace AeroResponse.Simulation.Instruments.VerticalSpeedIndicator;

public static class VSIMath
{
    public static VSIReading GetReading(
        int actualVerticalSpeed,
        double displayedVerticalSpeed,
        VerticalSpeedIndicatorLayout layout)
    {
        var clampedDisplay = Math.Clamp(
            displayedVerticalSpeed,
            layout.MinimumVerticalSpeed,
            layout.MaximumVerticalSpeed);

        var rotation = GetNeedleRotation(
            clampedDisplay,
            layout.CalibrationPoints);

        return new VSIReading(
            ActualVerticalSpeed: actualVerticalSpeed,
            DisplayedVerticalSpeed: clampedDisplay,
            NeedleRotation: rotation);
    }
    public static double GetNeedleRotation(
        double verticalSpeed,
        IReadOnlyList<VSICalibrationPoint> calibrationPoints)
    {
        if (calibrationPoints.Count == 0)
        {
            throw new InvalidOperationException(
                "No VSI calibration points were provided.");
        }

        var ordered = calibrationPoints
            .OrderBy(point => point.VerticalSpeed)
            .ToList();

        // Clamp to the instrument limits.
        if (verticalSpeed <= ordered.First().VerticalSpeed)
        {
            return ordered.First().Angle;
        }

        if (verticalSpeed >= ordered.Last().VerticalSpeed)
        {
            return ordered.Last().Angle;
        }

        // Find the surrounding calibration points.
        for (var index = 0; index < ordered.Count - 1; index++)
        {
            var lower = ordered[index];
            var upper = ordered[index + 1];

            if (verticalSpeed >= lower.VerticalSpeed &&
                verticalSpeed <= upper.VerticalSpeed)
            {
                var percentage =
                    (verticalSpeed - lower.VerticalSpeed) /
                    (upper.VerticalSpeed - lower.VerticalSpeed);

                return lower.Angle +
                    percentage *
                    (upper.Angle - lower.Angle);
            }
        }

        // Should never occur.
        return ordered.Last().Angle;
    }
    public static double ApplyLag(
        double currentDisplay,
        double targetVerticalSpeed,
        double elapsedSeconds,
        double lagSeconds)
    {
        if (lagSeconds <= 0)
        {
            return targetVerticalSpeed;
        }

        var response = 1.0 - Math.Exp(-elapsedSeconds / lagSeconds);

        return currentDisplay +
            (targetVerticalSpeed - currentDisplay) * response;
    }
}