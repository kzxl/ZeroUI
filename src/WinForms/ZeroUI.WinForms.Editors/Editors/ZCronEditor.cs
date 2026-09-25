using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using ZeroUI.Core;

namespace ZeroUI.WinForms.Editors
{
    public class ZCronEditor : Control
    {
        public string CronExpression { get; set; } public bool ShowPreview { get; set; } public event EventHandler CronChanged;
    }
    
    [Obsolete("$alias is deprecated and will be removed in 5 release cycles. Please migrate to ZCronEditor instead.")]
    [ToolboxItem(false)]
    public class CronEditor : ZCronEditor { }
    
    [Obsolete("Zero$alias is deprecated and will be removed in 5 release cycles. Please migrate to ZCronEditor instead.")]
    [ToolboxItem(false)]
    public class ZeroCronEditor : ZCronEditor { }
}
