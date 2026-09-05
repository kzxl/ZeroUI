using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Windows.Forms;
using ZeroUI.WinForms.Editors;
using ZeroUI.WinForms.Overlays;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Reporting
{
    /// <summary>
    /// Modern Enterprise Print Preview and Page Layout control for ZeroUI WinForms.
    /// Provides vector page rendering, interactive zoom (Fit Page, Fit Width, 25%-400%),
    /// multi-page navigation, and direct printing dispatch for warehouse tickets, barcodes, and reports.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Reporting & Documents")]
    [DefaultProperty("Document")]
    [Description("Enterprise vector print preview control with toolbar, zoom, and printer dispatch")]
    public class DocumentPreviewControl : Control
    {
        private PrintDocument? _document;
        private readonly PrintPreviewControl _previewControl;
        private readonly Panel _toolbarPanel;

        private readonly SimpleButton _btnPrint;
        private readonly SimpleButton _btnPrevPage;
        private readonly SimpleButton _btnNextPage;
        private readonly SimpleButton _btnZoomIn;
        private readonly SimpleButton _btnZoomOut;
        private readonly SimpleButton _btnFitPage;
        private readonly Label _lblPageInfo;

        [Category("Printing")]
        public PrintDocument? Document
        {
            get => _document;
            set
            {
                _document = value;
                _previewControl.Document = value;
                UpdatePageInfo();
            }
        }

        [Category("Printing")]
        public double Zoom
        {
            get => _previewControl.Zoom;
            set
            {
                _previewControl.Zoom = Math.Max(0.2, Math.Min(4.0, value));
                UpdatePageInfo();
            }
        }

        public int StartPage
        {
            get => _previewControl.StartPage;
            set
            {
                _previewControl.StartPage = Math.Max(0, value);
                UpdatePageInfo();
            }
        }

        public DocumentPreviewControl()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;
            BackColor = ZeroTheme.Colors.Background;
            Size = new Size(680, 520);

            // Native PrintPreviewControl
            _previewControl = new PrintPreviewControl
            {
                Dock = DockStyle.Fill,
                BackColor = ZeroTheme.Colors.Background,
                AutoZoom = true
            };
            Controls.Add(_previewControl);

            // 1. Toolbar Panel
            _toolbarPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = ZeroTheme.Colors.Surface
            };
            _toolbarPanel.Paint += (s, e) =>
            {
                using (var pen = new Pen(ZeroTheme.Colors.Border))
                {
                    e.Graphics.DrawLine(pen, 0, _toolbarPanel.Height - 1, _toolbarPanel.Width, _toolbarPanel.Height - 1);
                }
            };

            // Buttons
            _btnPrint = new SimpleButton { Text = "🖨 Print", Width = 80, Height = 30, Location = new Point(10, 7) };
            _btnPrint.Click += (s, e) => Print();

            _btnPrevPage = new SimpleButton { Text = "◀", Width = 34, Height = 30, Location = new Point(100, 7) };
            _btnPrevPage.Click += (s, e) => { if (_previewControl.StartPage > 0) _previewControl.StartPage--; UpdatePageInfo(); };

            _lblPageInfo = new Label
            {
                Text = "Page 1",
                AutoSize = false,
                Width = 70,
                Height = 30,
                Location = new Point(138, 7),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = ZeroTheme.Colors.TextPrimary,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };

            _btnNextPage = new SimpleButton { Text = "▶", Width = 34, Height = 30, Location = new Point(212, 7) };
            _btnNextPage.Click += (s, e) => { _previewControl.StartPage++; UpdatePageInfo(); };

            _btnZoomOut = new SimpleButton { Text = "−", Width = 34, Height = 30, Location = new Point(260, 7) };
            _btnZoomOut.Click += (s, e) => Zoom = _previewControl.Zoom * 0.8;

            _btnZoomIn = new SimpleButton { Text = "+", Width = 34, Height = 30, Location = new Point(298, 7) };
            _btnZoomIn.Click += (s, e) => Zoom = _previewControl.Zoom * 1.25;

            _btnFitPage = new SimpleButton { Text = "Fit Page", Width = 70, Height = 30, Location = new Point(336, 7) };
            _btnFitPage.Click += (s, e) => _previewControl.AutoZoom = true;

            _toolbarPanel.Controls.Add(_btnPrint);
            _toolbarPanel.Controls.Add(_btnPrevPage);
            _toolbarPanel.Controls.Add(_lblPageInfo);
            _toolbarPanel.Controls.Add(_btnNextPage);
            _toolbarPanel.Controls.Add(_btnZoomOut);
            _toolbarPanel.Controls.Add(_btnZoomIn);
            _toolbarPanel.Controls.Add(_btnFitPage);

            Controls.Add(_toolbarPanel);

            // Theme Sync
            ZeroTheme.ThemeChanged += (s, e) =>
            {
                BackColor = ZeroTheme.Colors.Background;
                _toolbarPanel.BackColor = ZeroTheme.Colors.Surface;
                _previewControl.BackColor = ZeroTheme.Colors.Background;
                _lblPageInfo.ForeColor = ZeroTheme.Colors.TextPrimary;
                _toolbarPanel.Invalidate();
            };
        }

        public void Print()
        {
            if (_document == null) return;
            using (var dlg = new PrintDialog { Document = _document, UseEXDialog = true })
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    _document.Print();
                }
            }
        }

        private void UpdatePageInfo()
        {
            _lblPageInfo.Text = $"Page {_previewControl.StartPage + 1}";
        }

        public void InvalidatePreview()
        {
            _previewControl.InvalidatePreview();
            UpdatePageInfo();
        }
    }

    /// <summary>
    /// Legacy alias for DocumentPreviewControl.
    /// Preserved for 100% backward compatibility.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Reporting & Documents")]
    [DefaultProperty("Document")]
    [Description("Legacy alias for DocumentPreviewControl")]
    public class ZeroPrintPreview : DocumentPreviewControl
    {
    }
}
