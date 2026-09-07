using System;
using System.Collections.Generic;

namespace ZeroUI.Core.Data
{
    /// <summary>
    /// Matching mode for multi-token search terms.
    /// </summary>
    public enum SearchTokenMatchMode
    {
        /// <summary>All tokens must be present in the target content (AND logic).</summary>
        All,

        /// <summary>At least one token must be present in the target content (OR logic).</summary>
        Any
    }

    /// <summary>
    /// High-performance multi-term search filtering and tokenization engine.
    /// Designed for high-capacity BOMs, inventory lookups, and fast multi-column filtering.
    /// </summary>
    public class SearchFilterEngine
    {
        private readonly List<string> _tokens = new List<string>();
        private SearchTokenMatchMode _matchMode = SearchTokenMatchMode.All;
        private string _rawQuery = string.Empty;

        /// <summary>
        /// Gets the current token matching mode (default is All / AND).
        /// </summary>
        public SearchTokenMatchMode MatchMode
        {
            get => _matchMode;
            set => _matchMode = value;
        }

        /// <summary>
        /// Gets the parsed search tokens.
        /// </summary>
        public IReadOnlyList<string> Tokens => _tokens;

        /// <summary>
        /// Gets the active query string.
        /// </summary>
        public string RawQuery => _rawQuery;

        /// <summary>
        /// Gets whether there is an active search filter.
        /// </summary>
        public bool HasFilter => _tokens.Count > 0;

        /// <summary>
        /// Sets the raw search query and parses it into distinct tokens.
        /// </summary>
        /// <param name="query">The raw user query string.</param>
        public void SetQuery(string? query)
        {
            _tokens.Clear();
            _rawQuery = query ?? string.Empty;

            if (string.IsNullOrWhiteSpace(_rawQuery))
                return;

            string[] parts = _rawQuery.Split(new[] { ' ', '\t', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                string token = parts[i].Trim();
                if (token.Length > 0 && !_tokens.Contains(token))
                {
                    _tokens.Add(token);
                }
            }
        }

        /// <summary>
        /// Evaluates whether a single target text matches the current search tokens.
        /// </summary>
        /// <param name="targetText">Target string to evaluate.</param>
        /// <returns>True if matching; otherwise false.</returns>
        public bool Matches(string? targetText)
        {
            if (_tokens.Count == 0) return true;
            if (string.IsNullOrEmpty(targetText)) return false;

            if (_matchMode == SearchTokenMatchMode.All)
            {
                for (int i = 0; i < _tokens.Count; i++)
                {
                    if (targetText!.IndexOf(_tokens[i], StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        return false;
                    }
                }
                return true;
            }
            else
            {
                for (int i = 0; i < _tokens.Count; i++)
                {
                    if (targetText!.IndexOf(_tokens[i], StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Evaluates whether a collection of column cell values for a row matches the current search tokens.
        /// </summary>
        /// <param name="columnValues">List or array of column string values.</param>
        /// <returns>True if the row matches; otherwise false.</returns>
        public bool MatchesAnyColumn(IReadOnlyList<string> columnValues)
        {
            if (_tokens.Count == 0) return true;
            if (columnValues == null || columnValues.Count == 0) return false;

            if (_matchMode == SearchTokenMatchMode.All)
            {
                // Each token must be found in at least one column
                for (int t = 0; t < _tokens.Count; t++)
                {
                    string token = _tokens[t];
                    bool found = false;
                    for (int c = 0; c < columnValues.Count; c++)
                    {
                        string val = columnValues[c];
                        if (val != null && val.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found) return false;
                }
                return true;
            }
            else
            {
                // Any token found in any column is a match
                for (int t = 0; t < _tokens.Count; t++)
                {
                    string token = _tokens[t];
                    for (int c = 0; c < columnValues.Count; c++)
                    {
                        string val = columnValues[c];
                        if (val != null && val.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Formats an enterprise count status string for status bar display.
        /// Example: "Showing 50 of 85,000 items (Filtered: 120)" or "85,000 items".
        /// </summary>
        /// <param name="visibleCount">Number of records currently visible in view or page.</param>
        /// <param name="filteredTotal">Total number of records matching current filter.</param>
        /// <param name="grandTotal">Grand total of records in underlying dataset.</param>
        /// <returns>Formatted status message.</returns>
        public string FormatStatusText(int visibleCount, int filteredTotal, int grandTotal)
        {
            if (!HasFilter || filteredTotal == grandTotal)
            {
                if (visibleCount < grandTotal)
                {
                    return string.Format("Showing {0:N0} of {1:N0} records", visibleCount, grandTotal);
                }
                return string.Format("{0:N0} records", grandTotal);
            }

            return string.Format("Showing {0:N0} of {1:N0} matching records (Total: {2:N0})",
                visibleCount, filteredTotal, grandTotal);
        }
    }
}
