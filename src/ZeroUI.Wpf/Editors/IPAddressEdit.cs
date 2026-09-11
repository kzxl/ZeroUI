using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ZeroUI.Core.Editors;

namespace ZeroUI.Wpf.Editors
{
    /// <summary>
    /// Industrial 4-octet segmented IPv4 address editor for ZeroUI.Wpf.
    /// Provides automatic octet cursor advancement on period (.) or space, backspace navigation,
    /// clipboard address parsing, and bidirectional IPAddress/string binding.
    /// </summary>
    public class IPAddressEdit : Control, IZeroEditor
    {
        public static readonly DependencyProperty AddressProperty =
            DependencyProperty.Register(
                nameof(Address),
                typeof(IPAddress),
                typeof(IPAddressEdit),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnAddressChanged));

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(IPAddressEdit),
                new FrameworkPropertyMetadata("0.0.0.0", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged));

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(string),
                typeof(IPAddressEdit),
                new FrameworkPropertyMetadata("0.0.0.0", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

        public static readonly DependencyProperty EditValueProperty =
            DependencyProperty.Register(
                nameof(EditValue),
                typeof(object),
                typeof(IPAddressEdit),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnEditValueChanged));

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(IPAddressEdit),
                new PropertyMetadata(new CornerRadius(5)));

        public static readonly DependencyProperty ReadOnlyProperty =
            DependencyProperty.Register(
                nameof(ReadOnly),
                typeof(bool),
                typeof(IPAddressEdit),
                new PropertyMetadata(false));

        public static readonly DependencyProperty IsModifiedProperty =
            DependencyProperty.Register(
                nameof(IsModified),
                typeof(bool),
                typeof(IPAddressEdit),
                new PropertyMetadata(false));

        public event EventHandler? EditValueChanged;

        public IPAddress? Address
        {
            get => (IPAddress?)GetValue(AddressProperty);
            set => SetValue(AddressProperty, value);
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public string Value
        {
            get => (string)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public object? EditValue
        {
            get => GetValue(EditValueProperty);
            set => SetValue(EditValueProperty, value);
        }

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public bool ReadOnly
        {
            get => (bool)GetValue(ReadOnlyProperty);
            set => SetValue(ReadOnlyProperty, value);
        }

        public bool IsModified
        {
            get => (bool)GetValue(IsModifiedProperty);
            set => SetValue(IsModifiedProperty, value);
        }

        private TextBox? _txtOctet1;
        private TextBox? _txtOctet2;
        private TextBox? _txtOctet3;
        private TextBox? _txtOctet4;
        private bool _isUpdating;

        static IPAddressEdit()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(IPAddressEdit), new FrameworkPropertyMetadata(typeof(IPAddressEdit)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _txtOctet1 = GetTemplateChild("PART_Octet1") as TextBox;
            _txtOctet2 = GetTemplateChild("PART_Octet2") as TextBox;
            _txtOctet3 = GetTemplateChild("PART_Octet3") as TextBox;
            _txtOctet4 = GetTemplateChild("PART_Octet4") as TextBox;

            SetupOctetBox(_txtOctet1, null, _txtOctet2);
            SetupOctetBox(_txtOctet2, _txtOctet1, _txtOctet3);
            SetupOctetBox(_txtOctet3, _txtOctet2, _txtOctet4);
            SetupOctetBox(_txtOctet4, _txtOctet3, null);

            UpdateOctetBoxesFromText(Text);
        }

        private void SetupOctetBox(TextBox? box, TextBox? prev, TextBox? next)
        {
            if (box == null) return;

            box.PreviewTextInput += (s, e) =>
            {
                if (ReadOnly) { e.Handled = true; return; }

                if (e.Text == "." || e.Text == " ")
                {
                    e.Handled = true;
                    if (next != null)
                    {
                        next.Focus();
                        next.SelectAll();
                    }
                    return;
                }

                foreach (char c in e.Text)
                {
                    if (!char.IsDigit(c))
                    {
                        e.Handled = true;
                        return;
                    }
                }
            };

            box.PreviewKeyDown += (s, e) =>
            {
                if (ReadOnly) return;

                if (e.Key == Key.Back && box.CaretIndex == 0 && box.SelectionLength == 0)
                {
                    if (prev != null)
                    {
                        prev.Focus();
                        prev.CaretIndex = prev.Text.Length;
                        e.Handled = true;
                    }
                }
                else if (e.Key == Key.Right && box.CaretIndex == box.Text.Length && next != null)
                {
                    next.Focus();
                    next.CaretIndex = 0;
                    e.Handled = true;
                }
                else if (e.Key == Key.Left && box.CaretIndex == 0 && prev != null)
                {
                    prev.Focus();
                    prev.CaretIndex = prev.Text.Length;
                    e.Handled = true;
                }
            };

            box.TextChanged += (s, e) =>
            {
                if (_isUpdating) return;

                // Value clamping between 0 and 255
                if (int.TryParse(box.Text, out int val))
                {
                    if (val > 255)
                    {
                        box.Text = "255";
                        box.CaretIndex = box.Text.Length;
                    }
                    else if (val < 0)
                    {
                        box.Text = "0";
                    }

                    // Auto advance if 3 digits or if value > 25 and another digit would exceed 255
                    if (box.Text.Length == 3 || (val >= 26 && box.Text.Length >= 2))
                    {
                        if (next != null && box.IsFocused)
                        {
                            next.Focus();
                            next.SelectAll();
                        }
                    }
                }

                SyncTextFromOctets();
            };

            // Clipboard paste interceptor
            DataObject.AddPastingHandler(box, (s, e) =>
            {
                if (ReadOnly) { e.CancelCommand(); return; }

                if (e.DataObject.GetDataPresent(DataFormats.Text))
                {
                    string pasteText = (string)e.DataObject.GetData(DataFormats.Text);
                    if (IPAddress.TryParse(pasteText.Trim(), out IPAddress? parsed))
                    {
                        e.CancelCommand();
                        SetIPAddress(parsed);
                    }
                }
            });
        }

        private void SyncTextFromOctets()
        {
            if (_isUpdating) return;

            string o1 = string.IsNullOrEmpty(_txtOctet1?.Text) ? "0" : _txtOctet1!.Text;
            string o2 = string.IsNullOrEmpty(_txtOctet2?.Text) ? "0" : _txtOctet2!.Text;
            string o3 = string.IsNullOrEmpty(_txtOctet3?.Text) ? "0" : _txtOctet3!.Text;
            string o4 = string.IsNullOrEmpty(_txtOctet4?.Text) ? "0" : _txtOctet4!.Text;

            string combined = $"{o1}.{o2}.{o3}.{o4}";
            _isUpdating = true;
            try
            {
                Text = combined;
                Value = combined;
                if (IPAddress.TryParse(combined, out var ip))
                {
                    Address = ip;
                    EditValue = ip;
                }
                else
                {
                    EditValue = combined;
                }
                IsModified = true;
                EditValueChanged?.Invoke(this, EventArgs.Empty);
            }
            finally
            {
                _isUpdating = false;
            }
        }

        private void UpdateOctetBoxesFromText(string text)
        {
            if (string.IsNullOrEmpty(text)) text = "0.0.0.0";
            var parts = text.Split('.');
            _isUpdating = true;
            try
            {
                if (_txtOctet1 != null) _txtOctet1.Text = parts.Length > 0 ? parts[0] : "0";
                if (_txtOctet2 != null) _txtOctet2.Text = parts.Length > 1 ? parts[1] : "0";
                if (_txtOctet3 != null) _txtOctet3.Text = parts.Length > 2 ? parts[2] : "0";
                if (_txtOctet4 != null) _txtOctet4.Text = parts.Length > 3 ? parts[3] : "0";
            }
            finally
            {
                _isUpdating = false;
            }
        }

        public void SetIPAddress(IPAddress address)
        {
            Address = address;
            var bytes = address.GetAddressBytes();
            if (bytes.Length == 4)
            {
                _isUpdating = true;
                try
                {
                    if (_txtOctet1 != null) _txtOctet1.Text = bytes[0].ToString();
                    if (_txtOctet2 != null) _txtOctet2.Text = bytes[1].ToString();
                    if (_txtOctet3 != null) _txtOctet3.Text = bytes[2].ToString();
                    if (_txtOctet4 != null) _txtOctet4.Text = bytes[3].ToString();
                    Text = address.ToString();
                    Value = Text;
                    EditValue = address;
                }
                finally
                {
                    _isUpdating = false;
                }
            }
        }

        private static void OnAddressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is IPAddressEdit edit && !edit._isUpdating && e.NewValue is IPAddress ip)
            {
                edit.SetIPAddress(ip);
            }
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is IPAddressEdit edit && !edit._isUpdating && e.NewValue is string s)
            {
                if (edit.Value != s) edit.Value = s;
                edit.UpdateOctetBoxesFromText(s);
            }
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is IPAddressEdit edit && !edit._isUpdating && e.NewValue is string s && edit.Text != s)
            {
                edit.Text = s;
            }
        }

        private static void OnEditValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not IPAddressEdit edit || edit._isUpdating) return;

            if (e.NewValue is IPAddress ip)
            {
                edit.SetIPAddress(ip);
            }
            else if (e.NewValue is string s && IPAddress.TryParse(s, out var parsed))
            {
                edit.SetIPAddress(parsed);
            }
        }

        public void Clear()
        {
            UpdateOctetBoxesFromText("0.0.0.0");
            SyncTextFromOctets();
            IsModified = false;
        }

        public void Reset()
        {
            Clear();
        }
    }
}
