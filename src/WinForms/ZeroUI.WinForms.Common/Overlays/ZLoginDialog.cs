using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZeroUI.Core.Input;
using ZeroUI.Core.Security;
using ZeroUI.WinForms.Input;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Overlays
{
    public enum LoginMode
    {
        Password = 0,
        Pin = 1,
        Badge = 2
    }

    /// <summary>
    /// Multi-modal enterprise industrial login dialog supporting Username/Password,
    /// touch PIN Numpad, and hardware RFID badge authentication.
    /// </summary>
    public class ZLoginDialog : Form
    {
        private readonly IAuthenticationProvider _authProvider;
        private readonly ISessionManager _session;

        private LoginMode _currentMode = LoginMode.Password;
        private TextBox _txtUsername = null!;
        private TextBox _txtPassword = null!;
        private TextBox _txtPin = null!;
        private Label _lblStatus = null!;
        private Panel _tabPasswordPanel = null!;
        private Panel _tabPinPanel = null!;
        private Panel _tabBadgePanel = null!;
        private ZVirtualKeyboard _pinNumpad = null!;
        private Rectangle _cardRect;
        private Rectangle _tabPassRect;
        private Rectangle _tabPinRect;
        private Rectangle _tabBadgeRect;

        public IUser? AuthenticatedUser { get; private set; }

        public ZLoginDialog(IAuthenticationProvider? authProvider = null, ISessionManager? session = null)
        {
            _authProvider = authProvider ?? new InMemoryUserStore();
            _session = session ?? SessionContext.Current;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = false;
            DoubleBuffered = true;
            KeyPreview = true;
            Size = new Size(460, 520);
            BackColor = Color.FromArgb(20, 24, 33);

            InitializeComponents();
        }

        private void InitializeComponents()
        {
            _cardRect = new Rectangle(0, 0, Width, Height);
            int contentW = Width - 60;

            // Tabs definition
            int tabY = 60;
            int tabH = 34;
            int tabW = contentW / 3;
            _tabPassRect = new Rectangle(30, tabY, tabW, tabH);
            _tabPinRect = new Rectangle(30 + tabW, tabY, tabW, tabH);
            _tabBadgeRect = new Rectangle(30 + tabW * 2, tabY, tabW, tabH);

            int panelY = 110;
            int panelH = 330;

            // Password Panel
            _tabPasswordPanel = new Panel
            {
                Location = new Point(30, panelY),
                Size = new Size(contentW, panelH),
                BackColor = Color.Transparent
            };

            var lblUser = new Label
            {
                Text = "Operator ID / Username",
                ForeColor = Color.FromArgb(148, 163, 184),
                Font = new Font(Font.FontFamily, 9f),
                Location = new Point(0, 10),
                AutoSize = true
            };
            _txtUsername = new TextBox
            {
                Font = new Font(Font.FontFamily, 12f),
                Location = new Point(0, 32),
                Size = new Size(contentW, 32),
                BackColor = Color.FromArgb(30, 36, 49),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Text = "admin"
            };

            var lblPass = new Label
            {
                Text = "Password",
                ForeColor = Color.FromArgb(148, 163, 184),
                Font = new Font(Font.FontFamily, 9f),
                Location = new Point(0, 80),
                AutoSize = true
            };
            _txtPassword = new TextBox
            {
                Font = new Font(Font.FontFamily, 12f),
                PasswordChar = '●',
                Location = new Point(0, 102),
                Size = new Size(contentW, 32),
                BackColor = Color.FromArgb(30, 36, 49),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Text = "admin"
            };
            _txtPassword.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; _ = ExecuteLoginAsync(); }
            };

            var btnSubmit = CreateActionButton("Sign In", Color.FromArgb(14, 165, 233), 0, 160, contentW, 42);
            btnSubmit.Click += (s, e) => _ = ExecuteLoginAsync();

            var btnCancel = CreateActionButton("Cancel", Color.FromArgb(51, 65, 85), 0, 215, contentW, 36);
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            _tabPasswordPanel.Controls.Add(lblUser);
            _tabPasswordPanel.Controls.Add(_txtUsername);
            _tabPasswordPanel.Controls.Add(lblPass);
            _tabPasswordPanel.Controls.Add(_txtPassword);
            _tabPasswordPanel.Controls.Add(btnSubmit);
            _tabPasswordPanel.Controls.Add(btnCancel);

            // PIN Panel
            _tabPinPanel = new Panel
            {
                Location = new Point(30, panelY),
                Size = new Size(contentW, panelH),
                BackColor = Color.Transparent,
                Visible = false
            };

            _txtPin = new TextBox
            {
                Font = new Font(Font.FontFamily, 16f, FontStyle.Bold),
                PasswordChar = '●',
                TextAlign = HorizontalAlignment.Center,
                Location = new Point(0, 0),
                Size = new Size(contentW, 36),
                BackColor = Color.FromArgb(30, 36, 49),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            _pinNumpad = new ZVirtualKeyboard
            {
                LayoutMode = VirtualKeyboardLayout.Numpad,
                TargetControl = _txtPin,
                Location = new Point(0, 45),
                Size = new Size(contentW, 260)
            };
            _pinNumpad.EnterPressed += (s, e) => _ = ExecuteLoginAsync();

            _tabPinPanel.Controls.Add(_txtPin);
            _tabPinPanel.Controls.Add(_pinNumpad);

            // Badge Panel
            _tabBadgePanel = new Panel
            {
                Location = new Point(30, panelY),
                Size = new Size(contentW, panelH),
                BackColor = Color.Transparent,
                Visible = false
            };

            var lblBadgeIcon = new Label
            {
                Text = "💳\r\n\r\nPresent RFID / NFC Badge\r\nOr Insert Hardware Dongle",
                Font = new Font(Font.FontFamily, 13f, FontStyle.Regular),
                ForeColor = Color.FromArgb(56, 189, 248),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            _tabBadgePanel.Controls.Add(lblBadgeIcon);

            // Status label
            _lblStatus = new Label
            {
                Text = "Please select authentication mode",
                ForeColor = Color.FromArgb(148, 163, 184),
                Font = new Font(Font.FontFamily, 9f),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(30, Height - 50),
                Size = new Size(contentW, 30),
                BackColor = Color.Transparent
            };

            Controls.Add(_tabPasswordPanel);
            Controls.Add(_tabPinPanel);
            Controls.Add(_tabBadgePanel);
            Controls.Add(_lblStatus);
        }

        private static Button CreateActionButton(string text, Color backColor, int x, int y, int w, int h)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(Control.DefaultFont.FontFamily, 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (_tabPassRect.Contains(e.Location)) SetMode(LoginMode.Password);
            else if (_tabPinRect.Contains(e.Location)) SetMode(LoginMode.Pin);
            else if (_tabBadgeRect.Contains(e.Location)) SetMode(LoginMode.Badge);
        }

        private void SetMode(LoginMode mode)
        {
            _currentMode = mode;
            _tabPasswordPanel.Visible = (mode == LoginMode.Password);
            _tabPinPanel.Visible = (mode == LoginMode.Pin);
            _tabBadgePanel.Visible = (mode == LoginMode.Badge);
            _lblStatus.Text = mode == LoginMode.Badge ? "Listening for badge swipe..." : "Ready";
            _lblStatus.ForeColor = Color.FromArgb(148, 163, 184);
            Invalidate();
        }

        public async Task<bool> ExecuteLoginAsync()
        {
            _lblStatus.Text = "Authenticating...";
            _lblStatus.ForeColor = Color.FromArgb(56, 189, 248);

            AuthResult result;
            if (_currentMode == LoginMode.Password)
            {
                result = await _authProvider.AuthenticatePasswordAsync(_txtUsername.Text.Trim(), _txtPassword.Text);
            }
            else
            {
                result = await _authProvider.AuthenticatePinAsync("admin", _txtPin.Text);
            }

            if (result.Succeeded && result.User != null)
            {
                AuthenticatedUser = result.User;
                _session.SetCurrentUser(result.User);
                DialogResult = DialogResult.OK;
                Close();
                return true;
            }
            else
            {
                _lblStatus.Text = result.ErrorMessage ?? "Authentication failed";
                _lblStatus.ForeColor = Color.FromArgb(239, 68, 68);
                return false;
            }
        }

        public async Task<bool> ExecuteBadgeLoginAsync(string badgeId)
        {
            _lblStatus.Text = $"Validating Badge '{badgeId}'...";
            var result = await _authProvider.AuthenticateBadgeAsync(badgeId);
            if (result.Succeeded && result.User != null)
            {
                AuthenticatedUser = result.User;
                _session.SetCurrentUser(result.User);
                DialogResult = DialogResult.OK;
                Close();
                return true;
            }
            else
            {
                _lblStatus.Text = result.ErrorMessage ?? "Badge not recognized";
                _lblStatus.ForeColor = Color.FromArgb(239, 68, 68);
                return false;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Outer border
            using (var borderPen = new Pen(Color.FromArgb(51, 65, 85), 1.5f))
            {
                g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }

            // Header Title
            using (var titleFont = new Font(Font.FontFamily, 14f, FontStyle.Bold))
            using (var subFont = new Font(Font.FontFamily, 8.5f))
            {
                g.DrawString("🔐 ZeroUI Operator Sign-In", titleFont, Brushes.White, 30, 16);
                g.DrawString("Role-Based Industrial Access Control", subFont, new SolidBrush(Color.FromArgb(148, 163, 184)), 32, 38);
            }

            // Draw Tabs
            DrawTab(g, "Password", _tabPassRect, _currentMode == LoginMode.Password);
            DrawTab(g, "PIN Code", _tabPinRect, _currentMode == LoginMode.Pin);
            DrawTab(g, "RFID Badge", _tabBadgeRect, _currentMode == LoginMode.Badge);
        }

        private static void DrawTab(Graphics g, string label, Rectangle rect, bool isActive)
        {
            Color back = isActive ? Color.FromArgb(14, 165, 233) : Color.FromArgb(30, 36, 49);
            Color text = isActive ? Color.White : Color.FromArgb(148, 163, 184);

            using (var brush = new SolidBrush(back))
            using (var pen = new Pen(Color.FromArgb(51, 65, 85), 1f))
            {
                g.FillRectangle(brush, rect);
                g.DrawRectangle(pen, rect);
            }

            using (var font = new Font(Control.DefaultFont.FontFamily, 8.5f, isActive ? FontStyle.Bold : FontStyle.Regular))
            using (var brush = new SolidBrush(text))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString(label, font, brush, rect, sf);
            }
        }

        public static bool ShowLogin(IAuthenticationProvider? authProvider = null, Form? owner = null)
        {
            using var dlg = new ZLoginDialog(authProvider);
            return dlg.ShowDialog(owner) == DialogResult.OK;
        }
    }
}
