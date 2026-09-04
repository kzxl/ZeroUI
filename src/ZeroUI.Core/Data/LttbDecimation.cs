using System;
using System.Collections.Generic;

namespace ZeroUI.Core.Data
{
    /// <summary>
    /// Represents a 2D time-series data point (X: Timestamp or Index, Y: Value).
    /// </summary>
    public readonly struct TimePoint
    {
        public double X { get; }
        public double Y { get; }

        public TimePoint(double x, double y)
        {
            X = x;
            Y = y;
        }

        public override string ToString() => $"({X:0.##}, {Y:0.##})";
    }

    /// <summary>
    /// High-performance, zero-GC implementation of the Largest-Triangle-Three-Buckets (LTTB) algorithm.
    /// Downsamples massive time-series telemetry streams (e.g. 100,000+ points) down to screen pixel resolution (e.g. 1,000 points)
    /// while strictly preserving visual peaks, valleys, and waveform morphology without UI lag.
    /// </summary>
    public static class LttbDecimation
    {
        /// <summary>
        /// Downsamples the input points down to the target threshold count using LTTB.
        /// Writes results directly into the destination buffer to avoid heap allocations.
        /// </summary>
        /// <param name="data">Source time-series points sorted by X.</param>
        /// <param name="destination">Destination array where downsampled points will be written.</param>
        /// <param name="targetCount">Number of desired output points (must be >= 3 and <= destination.Length).</param>
        /// <returns>Actual number of points written into destination.</returns>
        public static int Downsample(IReadOnlyList<TimePoint> data, TimePoint[] destination, int targetCount)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            int dataLength = data.Count;
            if (targetCount >= dataLength || targetCount <= 2 || dataLength <= 2)
            {
                int copyCount = Math.Min(dataLength, destination.Length);
                for (int i = 0; i < copyCount; i++)
                {
                    destination[i] = data[i];
                }
                return copyCount;
            }

            int sampledIndex = 0;

            // Bucket size. Leave room for start and end data points
            double every = (double)(dataLength - 2) / (targetCount - 2);

            int a = 0;
            destination[sampledIndex++] = data[a]; // Always add the first point

            for (int i = 0; i < targetCount - 2; i++)
            {
                // Calculate point average for next bucket (bucket c)
                double avgX = 0;
                double avgY = 0;
                int avgRangeStart = (int)Math.Floor((i + 1) * every) + 1;
                int avgRangeEnd = (int)Math.Floor((i + 2) * every) + 1;
                avgRangeEnd = Math.Min(avgRangeEnd, dataLength);

                int avgRangeLength = avgRangeEnd - avgRangeStart;
                if (avgRangeLength > 0)
                {
                    for (int j = avgRangeStart; j < avgRangeEnd; j++)
                    {
                        avgX += data[j].X;
                        avgY += data[j].Y;
                    }
                    avgX /= avgRangeLength;
                    avgY /= avgRangeLength;
                }
                else
                {
                    avgX = data[dataLength - 1].X;
                    avgY = data[dataLength - 1].Y;
                }

                // Get the range for this bucket (bucket b)
                int rangeOffs = (int)Math.Floor(i * every) + 1;
                int rangeTo = (int)Math.Floor((i + 1) * every) + 1;

                // Point a
                double pointAx = data[a].X;
                double pointAy = data[a].Y;

                double maxArea = -1;
                int maxAreaIndex = rangeOffs;

                for (int j = rangeOffs; j < rangeTo && j < dataLength; j++)
                {
                    // Calculate triangle area over points: (point_a, data[j], avg_point)
                    double area = Math.Abs(
                        (pointAx - avgX) * (data[j].Y - pointAy) -
                        (pointAx - data[j].X) * (avgY - pointAy)
                    ) * 0.5;

                    if (area > maxArea)
                    {
                        maxArea = area;
                        maxAreaIndex = j;
                    }
                }

                destination[sampledIndex++] = data[maxAreaIndex];
                a = maxAreaIndex; // Next a is this bucket's chosen point
            }

            destination[sampledIndex++] = data[dataLength - 1]; // Always add the last point
            return sampledIndex;
        }
    }
}
