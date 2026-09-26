using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using ZeroUI.Core.Input;
using ZeroUI.Core.Security;
using ZeroUI.Wpf.Input;

namespace ZeroUI.Wpf.Overlays
{
    /// <summary>
    /// Multi-modal enterprise industrial login dialog in WPF supporting Username/Password,
    /// touch PIN Numpad, and hardware RFID badge authentication.
    /// </summary>
    public class ZLoginDialog : Window
    {
        private readonly IAuthenticationProvider _authProvider;
        private readonly ISessionManager _session;

        private TextBox _txtUsername = null!;
        private PasswordBox _txtPassword = null!;
        private PasswordBox _txtPin = null!;
        private TextBlock _lblStatus = null!;
        private StackPanel _panelPassword = null!;
        private StackPanel _panelPin = null!;
        private StackPanel _panelBadge = null!;
        private Button _tabPass = null!;
        private Button _tabPin = null!;
        private Button _tabBadge = null!;

        public IUser? AuthenticatedUser { get; private set; }

        public ZLoginDialog(IAuthenticationProvider? authProvider = null, ISessionManager? session = null)
        {
            _authProvider = authProvider ?? new InMemoryUserStore();
            _session = session ?? SessionContext.Current;

            WindowStyle = WindowStyle.None;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;
            Width = 460;
            Height = 520;
            Background = new SolidColorBrush(Color.FromRgb(20, 24, 33));

            BuildUI();
        }

        private void BuildUI()
        {
            var root = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                BorderThickness = new Thickness(1.5),
                Padding = new Thickness(24)
            };

            var stack = new StackPanel();

            // Header
            var title = new TextBlock
            {
                Text = "🔐 ZeroUI Operator Sign-In",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 4)
            };

            var subtitle = new TextBlock
            {
                Text = "Role-Based Industrial Access Control",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                Margin = new Thickness(0, 0, 0, 16)
            };

            // Tabs
            var tabsGrid = new UniformGrid { Rows = 1, Columns = 3, Margin = new Thickness(0, 0, 0, 16) };
            _tabPass = CreateTabButton("Password", true, () => SwitchTab(0));
            _tabPin = CreateTabButton("PIN Code", false, () => SwitchTab(1));
            _tabBadge = CreateTabButton("RFID Badge", false, () => SwitchTab(2));
            tabsGrid.Children.Add(_tabPass);
            tabsGrid.Children.Add(_tabPin);
            tabsGrid.Children.Add(_tabBadge);

            // Container for panels
            var contentContainer = new Grid { Height = 300 };

            // Panel 1: Password
            _panelPassword = new StackPanel();
            _panelPassword.Children.Add(new TextBlock { Text = "Operator ID / Username", Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)), Margin = new Thickness(0, 8, 0, 4) });
            _txtUsername = new TextBox
            {
                Text = "admin",
                Height = 36,
                FontSize = 14,
                Background = new SolidColorBrush(Color.FromRgb(30, 36, 49)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                VerticalContentAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 12)
            };
            _panelPassword.Children.Add(_txtUsername);

            _panelPassword.Children.Add(new TextBlock { Text = "Password", Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)), Margin = new Thickness(0, 0, 0, 4) });
            _txtPassword = new PasswordBox
            {
                Password = "admin",
                Height = 36,
                FontSize = 16,
                Background = new SolidColorBrush(Color.FromRgb(30, 36, 49)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                VerticalContentAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            _txtPassword.KeyDown += (s, e) => { if (e.Key == Key.Enter) _ = ExecuteLoginAsync(0); };
            _panelPassword.Children.Add(_txtPassword);

            var btnSignIn = new Button
            {
                Content = "Sign In",
                Height = 42,
                Background = new SolidColorBrush(Color.FromRgb(14, 165, 233)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 0, 8)
            };
            btnSignIn.Click += (s, e) => _ = ExecuteLoginAsync(0);
            _panelPassword.Children.Add(btnSignIn);

            var btnCancel = new Button
            {
                Content = "Cancel",
                Height = 36,
                Background = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            btnCancel.Click += (s, e) => { DialogResult = false; Close(); };
            _panelPassword.Children.Add(btnCancel);

            // Panel 2: PIN
            _panelPin = new StackPanel { Visibility = Visibility.Collapsed };
            _txtPin = new PasswordBox
            {
                Height = 38,
                FontSize = 20,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Background = new SolidColorBrush(Color.FromRgb(30, 36, 49)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                Margin = new Thickness(0, 0, 0, 8)
            };
            var numpad = new ZVirtualKeyboard
            {
                LayoutMode = VirtualKeyboardLayout.Numpad,
                TargetElement = _txtPin,
                Height = 240
            };
            numpad.EnterPressed += (s, e) => _ = ExecuteLoginAsync(1);
            _panelPin.Children.Add(_txtPin);
            _panelPin.Children.Add(numpad);

            // Panel 3: Badge
            _panelBadge = new StackPanel { Visibility = Visibility.Collapsed, VerticalAlignment = VerticalAlignment.Center };
            _panelBadge.Children.Add(new TextBlock
            {
                Text = "💳\n\nPresent RFID / NFC Badge\nOr Insert Hardware Dongle",
                FontSize = 16,
                Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                TextAlignment = TextAlignment.Center
            });

            contentContainer.Children.Add(_panelPassword);
            contentContainer.Children.Add(_panelPin);
            contentContainer.Children.Add(_panelBadge);

            // Status label
            _lblStatus = new TextBlock
            {
                Text = "Ready",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 16, 0, 0)
            };

            stack.Children.Add(title);
            stack.Children.Add(subtitle);
            stack.Children.Add(tabsGrid);
            stack.Children.Add(contentContainer);
            stack.Children.Add(_lblStatus);

            root.Child = stack;
            Content = root;
        }

        private Button CreateTabButton(string label, bool isActive, Action onClick)
        {
            var btn = new Button
            {
                Content = label,
                Height = 32,
                FontWeight = isActive ? FontWeights.Bold : FontWeights.Normal,
                Foreground = isActive ? Brushes.White : new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                Background = isActive ? new SolidColorBrush(Color.FromRgb(14, 165, 233)) : new SolidColorBrush(Color.FromRgb(30, 36, 49)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };
            btn.Click += (s, e) => onClick();
            return btn;
        }

        private void SwitchTab(int index)
        {
            _panelPassword.Visibility = (index == 0) ? Visibility.Visible : Visibility.Collapsed;
            _panelPin.Visibility = (index == 1) ? Visibility.Visible : Visibility.Collapsed;
            _panelBadge.Visibility = (index == 2) ? Visibility.Visible : Visibility.Collapsed;

            UpdateTabButton(_tabPass, index == 0);
            UpdateTabButton(_tabPin, index == 1);
            UpdateTabButton(_tabBadge, index == 2);

            _lblStatus.Text = index == 2 ? "Waiting for badge swipe..." : "Ready";
            _lblStatus.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184));
        }

        private static void UpdateTabButton(Button btn, bool isActive)
        {
            btn.FontWeight = isActive ? FontWeights.Bold : FontWeights.Normal;
            btn.Foreground = isActive ? Brushes.White : new SolidColorBrush(Color.FromRgb(148, 163, 184));
            btn.Background = isActive ? new SolidColorBrush(Color.FromRgb(14, 165, 233)) : new SolidColorBrush(Color.FromRgb(30, 36, 49));
        }

        public async Task<bool> ExecuteLoginAsync(int mode)
        {
            _lblStatus.Text = "Authenticating...";
            _lblStatus.Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248));

            AuthResult result;
            if (mode == 0)
            {
                result = await _authProvider.AuthenticatePasswordAsync(_txtUsername.Text.Trim(), _txtPassword.Password);
            }
            else
            {
                result = await _authProvider.AuthenticatePinAsync("admin", _txtPin.Password);
            }

            if (result.Succeeded && result.User != null)
            {
                AuthenticatedUser = result.User;
                _session.SetCurrentUser(result.User);
                try { DialogResult = true; } catch (InvalidOperationException) { }
                Close();
                return true;
            }
            else
            {
                _lblStatus.Text = result.ErrorMessage ?? "Authentication failed";
                _lblStatus.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
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
                try { DialogResult = true; } catch (InvalidOperationException) { }
                Close();
                return true;
            }
            else
            {
                _lblStatus.Text = result.ErrorMessage ?? "Badge not recognized";
                _lblStatus.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                return false;
            }
        }

        public static bool? ShowLogin(IAuthenticationProvider? authProvider = null, Window? owner = null)
        {
            var dlg = new ZLoginDialog(authProvider) { Owner = owner };
            return dlg.ShowDialog();
        }
    }
}
