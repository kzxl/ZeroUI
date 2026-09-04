using System;
using System.Collections.Generic;

namespace ZeroUI.Core.Data
{
    /// <summary>
    /// Represents a scheduled task item in a high-performance industrial Gantt timeline.
    /// </summary>
    public class GanttTaskItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public float Progress { get; set; } // 0.0f to 1.0f
        public List<int> PredecessorIds { get; } = new List<int>();
        public bool IsMilestone { get; set; }
        public string? AssignedResource { get; set; }
        public uint BarColor { get; set; } = 0xFF2563EB; // Primary Blue
        public int Level { get; set; } = 0;
        public bool IsExpanded { get; set; } = true;

        public TimeSpan Duration => EndDate - StartDate;

        public GanttTaskItem()
        {
        }

        public GanttTaskItem(int id, string name, DateTime start, DateTime end, float progress = 0.0f, bool isMilestone = false, string? resource = null)
        {
            Id = id;
            Name = name;
            StartDate = start;
            EndDate = end;
            Progress = progress;
            IsMilestone = isMilestone;
            AssignedResource = resource;
        }
    }
}
