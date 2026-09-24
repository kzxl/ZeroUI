using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ZeroUI.Core.Data;

namespace ZeroUI.Wpf.Design.Mock
{
    /// <summary>
    /// Design-time sample data provider for ZeroUI WPF controls.
    /// Used by Visual Studio XAML Designer and Blend to render rich preview states when explicitly configured.
    /// </summary>
    public static class DesignDataFactory
    {
        public class DesignTelemetryRecord
        {
            public int Id { get; set; }
            public string TagName { get; set; } = string.Empty;
            public double Value { get; set; }
            public string Unit { get; set; } = string.Empty;
            public string Status { get; set; } = "OK";
            public DateTime Timestamp { get; set; }
        }

        /// <summary>
        /// Creates a collection of sample SCADA telemetry records for design-time data binding.
        /// </summary>
        public static ObservableCollection<DesignTelemetryRecord> CreateSampleTelemetryData(int count = 10)
        {
            var list = new ObservableCollection<DesignTelemetryRecord>();
            var units = new[] { "bar", "°C", "rpm", "m³/h", "kW", "kPa" };
            var tags = new[] { "PT-101", "TT-204", "PUMP-01", "FT-302", "MOTOR-03", "LT-401" };
            var statuses = new[] { "OK", "Warning", "Normal", "Critical" };
            var random = new Random(42);

            for (int i = 1; i <= count; i++)
            {
                list.Add(new DesignTelemetryRecord
                {
                    Id = i,
                    TagName = tags[(i - 1) % tags.Length],
                    Value = Math.Round(20.0 + random.NextDouble() * 80.0, 2),
                    Unit = units[(i - 1) % units.Length],
                    Status = statuses[(i - 1) % statuses.Length],
                    Timestamp = DateTime.Now.AddMinutes(-i * 5)
                });
            }

            return list;
        }
    }
}
