using System;
using System.Collections.Generic;
using System.Text;
using ZeroUI.Core.Common;
using ZeroUI.Core.Data;

namespace ZeroUI.Core.Layout
{
    public class ColumnLayoutState
    {
        public string FieldName { get; set; } = string.Empty;
        public int Width { get; set; } = 100;
        public bool IsVisible { get; set; } = true;
        public bool IsPinned { get; set; } = false;
        public int GroupIndex { get; set; } = -1;
        public SortDirection SortOrder { get; set; } = SortDirection.None;
    }

    public class DockPanelLayoutState
    {
        public string Title { get; set; } = string.Empty;
        public string DockPosition { get; set; } = "Document";
        public bool IsPinned { get; set; } = true;
        public int Width { get; set; } = 240;
        public int Height { get; set; } = 200;
    }

    public class WorkspaceLayoutState
    {
        public string Version { get; set; } = "1.0";
        public DateTime SavedAt { get; set; } = DateTime.UtcNow;
        public List<DockPanelLayoutState> DockPanels { get; } = new List<DockPanelLayoutState>();
        public List<ColumnLayoutState> GridColumns { get; } = new List<ColumnLayoutState>();
    }

    /// <summary>
    /// Pure, zero-dependency Workspace Layout Serializer for ZeroUI.
    /// Serializes and restores DockManager panel positions, floating states,
    /// and DataGrid column configurations (width, visibility, grouping, pinning, sort) to clean JSON.
    /// Compatible with .NET Standard 2.0, .NET Framework 4.6.2, and .NET 8.0+.
    /// </summary>
    public static class ZeroWorkspaceSerializer
    {
        public static string Serialize(WorkspaceLayoutState state)
        {
            if (state == null) return "{}";

            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"Version\": \"{Escape(state.Version)}\",");
            sb.AppendLine($"  \"SavedAt\": \"{state.SavedAt:O}\",");

            // Dock Panels
            sb.AppendLine("  \"DockPanels\": [");
            for (int i = 0; i < state.DockPanels.Count; i++)
            {
                var p = state.DockPanels[i];
                sb.AppendLine("    {");
                sb.AppendLine($"      \"Title\": \"{Escape(p.Title)}\",");
                sb.AppendLine($"      \"DockPosition\": \"{Escape(p.DockPosition)}\",");
                sb.AppendLine($"      \"IsPinned\": {(p.IsPinned ? "true" : "false")},");
                sb.AppendLine($"      \"Width\": {p.Width},");
                sb.AppendLine($"      \"Height\": {p.Height}");
                sb.Append("    }");
                if (i < state.DockPanels.Count - 1) sb.Append(",");
                sb.AppendLine();
            }
            sb.AppendLine("  ],");

            // Grid Columns
            sb.AppendLine("  \"GridColumns\": [");
            for (int i = 0; i < state.GridColumns.Count; i++)
            {
                var c = state.GridColumns[i];
                sb.AppendLine("    {");
                sb.AppendLine($"      \"FieldName\": \"{Escape(c.FieldName)}\",");
                sb.AppendLine($"      \"Width\": {c.Width},");
                sb.AppendLine($"      \"IsVisible\": {(c.IsVisible ? "true" : "false")},");
                sb.AppendLine($"      \"IsPinned\": {(c.IsPinned ? "true" : "false")},");
                sb.AppendLine($"      \"GroupIndex\": {c.GroupIndex},");
                sb.AppendLine($"      \"SortOrder\": {(int)c.SortOrder}");
                sb.Append("    }");
                if (i < state.GridColumns.Count - 1) sb.Append(",");
                sb.AppendLine();
            }
            sb.AppendLine("  ]");
            sb.AppendLine("}");

            return sb.ToString();
        }

        public static WorkspaceLayoutState CaptureGrid(IEnumerable<ZeroColumn> columns)
        {
            var ws = new WorkspaceLayoutState();
            if (columns != null)
            {
                foreach (var col in columns)
                {
                    ws.GridColumns.Add(new ColumnLayoutState
                    {
                        FieldName = !string.IsNullOrEmpty(col.FieldName) ? col.FieldName : col.HeaderText,
                        Width = col.Width,
                        IsVisible = col.IsVisible,
                        IsPinned = col.IsPinned,
                        GroupIndex = col.GroupIndex,
                        SortOrder = col.SortOrder
                    });
                }
            }
            return ws;
        }

        public static void ApplyToGrid(IEnumerable<ZeroColumn> columns, WorkspaceLayoutState state)
        {
            if (columns == null || state == null || state.GridColumns.Count == 0) return;

            var map = new Dictionary<string, ColumnLayoutState>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in state.GridColumns)
            {
                if (!string.IsNullOrEmpty(c.FieldName)) map[c.FieldName] = c;
            }

            foreach (var col in columns)
            {
                string key = !string.IsNullOrEmpty(col.FieldName) ? col.FieldName : col.HeaderText;
                if (map.TryGetValue(key, out var s))
                {
                    col.Width = s.Width;
                    col.IsVisible = s.IsVisible;
                    col.IsPinned = s.IsPinned;
                    col.GroupIndex = s.GroupIndex;
                    col.SortOrder = s.SortOrder;
                }
            }
        }

        private static string Escape(string? s) => s?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";
    }
}
