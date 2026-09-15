using System;

namespace ZeroUI.WinForms.Editors
{
    // =========================================================================================
    // BACKWARD COMPATIBILITY SHIMS
    // These type declarations ensure that existing code referencing ZeroUI.WinForms.Editors
    // continues to compile without breaking changes.
    // =========================================================================================

    [Obsolete("Use ZeroUI.WinForms.Containers.StatisticCard instead.")]
    public class StatisticCard : ZeroUI.WinForms.Containers.StatisticCard { }

    [Obsolete("Use ZeroUI.WinForms.Navigation.PaginationControl instead.")]
    public class PaginationControl : ZeroUI.WinForms.Navigation.PaginationControl { }
}
