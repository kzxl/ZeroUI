using System;
using ZeroUI.Core.Data;

namespace ZeroUI.Core.DataGrid
{
    /// <summary>
    /// Decoupled metadata and formatting contract for dynamic in-place cell editors.
    /// Enables per-cell and per-column custom editor dispatch without coupling the data layer to UI windows.
    /// </summary>
    public interface IRepositoryItem
    {
        /// <summary>
        /// Logical name of the editor type (e.g., "TextEdit", "SpinEdit", "DateEdit", "ComboBoxEdit", "CheckEdit").
        /// </summary>
        string EditorTypeName { get; }

        /// <summary>
        /// Formats an underlying model value into display text.
        /// </summary>
        string FormatValue(object? rawValue);

        /// <summary>
        /// Parses raw user-edited text or input back into the target data type.
        /// </summary>
        bool ParseEditValue(ReadOnlySpan<char> text, out object? parsedValue);
    }
}
