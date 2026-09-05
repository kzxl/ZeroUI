using System;
using System.Collections.Generic;
using System.Globalization;

namespace ZeroUI.Core.Localization
{
    /// <summary>
    /// Centralized Internationalization & Localization Coordinator for ZeroUI.
    /// Provides zero-allocation lookup for standard UI strings with built-in English and Vietnamese
    /// dictionaries, dynamic runtime language switching, and custom string overriding.
    /// </summary>
    public static class ZeroLocalizer
    {
        private static readonly Dictionary<ZeroStringId, string> English = new Dictionary<ZeroStringId, string>
        {
            [ZeroStringId.Ok] = "OK",
            [ZeroStringId.Cancel] = "Cancel",
            [ZeroStringId.Apply] = "Apply",
            [ZeroStringId.Close] = "Close",
            [ZeroStringId.Clear] = "Clear",
            [ZeroStringId.Reset] = "Reset",
            [ZeroStringId.Save] = "Save",
            [ZeroStringId.Search] = "Search...",
            [ZeroStringId.Loading] = "Loading...",
            [ZeroStringId.Refresh] = "Refresh",

            [ZeroStringId.CheckedComboPlaceholder] = "Select items...",
            [ZeroStringId.CheckedComboSummaryFormat] = "{0} items selected",
            [ZeroStringId.CheckedComboSelectAll] = "(Select All)",
            [ZeroStringId.TokenEditPlaceholder] = "Type and press Enter...",
            [ZeroStringId.DateEditPlaceholder] = "Select date...",
            [ZeroStringId.DateEditToday] = "Today",
            [ZeroStringId.DateEditClear] = "Clear",
            [ZeroStringId.ColorPickerPlaceholder] = "Select color...",

            [ZeroStringId.FilterOpAnd] = "AND",
            [ZeroStringId.FilterOpOr] = "OR",
            [ZeroStringId.FilterOpNotAnd] = "NOT AND",
            [ZeroStringId.FilterOpNotOr] = "NOT OR",
            [ZeroStringId.FilterEquals] = "Equals",
            [ZeroStringId.FilterNotEquals] = "Does not equal",
            [ZeroStringId.FilterContains] = "Contains",
            [ZeroStringId.FilterStartsWith] = "Starts with",
            [ZeroStringId.FilterEndsWith] = "Ends with",
            [ZeroStringId.FilterGreaterThan] = "Greater than",
            [ZeroStringId.FilterLessThan] = "Less than",
            [ZeroStringId.FilterIsNull] = "Is null",
            [ZeroStringId.FilterIsNotNull] = "Is not null",
            [ZeroStringId.FilterAddCondition] = "+ Condition",
            [ZeroStringId.FilterAddGroup] = "+ Group",

            [ZeroStringId.WizardBack] = "← Back",
            [ZeroStringId.WizardNext] = "Next →",
            [ZeroStringId.WizardFinish] = "Finish ✓",
            [ZeroStringId.WizardCancel] = "Cancel",
            [ZeroStringId.WizardStepTitleDefault] = "Step Title",
            [ZeroStringId.WizardValidationTitle] = "Step Validation",
            [ZeroStringId.WizardNoPages] = "No Pages",
            [ZeroStringId.WizardNoPagesDesc] = "Add pages to this wizard.",

            [ZeroStringId.PrintButton] = "🖨️ Print",
            [ZeroStringId.PrintStatusFormat] = "Page {0} of {1}",
            [ZeroStringId.ZoomFit] = "100%",

            [ZeroStringId.ValRequired] = "This field is required.",
            [ZeroStringId.ValRangeFormat] = "Value must be between {0} and {1}.",
            [ZeroStringId.ValEmail] = "Invalid email address format.",
            [ZeroStringId.ValPhone] = "Invalid phone number format.",
            [ZeroStringId.ValStringLengthFormat] = "Text length must be between {0} and {1} characters.",
            [ZeroStringId.ValInvalidFormat] = "Input format is invalid."
        };

        private static readonly Dictionary<ZeroStringId, string> Vietnamese = new Dictionary<ZeroStringId, string>
        {
            [ZeroStringId.Ok] = "Đồng ý",
            [ZeroStringId.Cancel] = "Hủy",
            [ZeroStringId.Apply] = "Áp dụng",
            [ZeroStringId.Close] = "Đóng",
            [ZeroStringId.Clear] = "Xóa",
            [ZeroStringId.Reset] = "Đặt lại",
            [ZeroStringId.Save] = "Lưu",
            [ZeroStringId.Search] = "Tìm kiếm...",
            [ZeroStringId.Loading] = "Đang tải...",
            [ZeroStringId.Refresh] = "Làm mới",

            [ZeroStringId.CheckedComboPlaceholder] = "Chọn mục...",
            [ZeroStringId.CheckedComboSummaryFormat] = "Đã chọn {0} mục",
            [ZeroStringId.CheckedComboSelectAll] = "(Chọn tất cả)",
            [ZeroStringId.TokenEditPlaceholder] = "Nhập và nhấn Enter...",
            [ZeroStringId.DateEditPlaceholder] = "Chọn ngày...",
            [ZeroStringId.DateEditToday] = "Hôm nay",
            [ZeroStringId.DateEditClear] = "Xóa",
            [ZeroStringId.ColorPickerPlaceholder] = "Chọn màu...",

            [ZeroStringId.FilterOpAnd] = "VÀ",
            [ZeroStringId.FilterOpOr] = "HOẶC",
            [ZeroStringId.FilterOpNotAnd] = "KHÔNG VÀ",
            [ZeroStringId.FilterOpNotOr] = "KHÔNG HOẶC",
            [ZeroStringId.FilterEquals] = "Bằng",
            [ZeroStringId.FilterNotEquals] = "Khác",
            [ZeroStringId.FilterContains] = "Chứa",
            [ZeroStringId.FilterStartsWith] = "Bắt đầu bằng",
            [ZeroStringId.FilterEndsWith] = "Kết thúc bằng",
            [ZeroStringId.FilterGreaterThan] = "Lớn hơn",
            [ZeroStringId.FilterLessThan] = "Nhỏ hơn",
            [ZeroStringId.FilterIsNull] = "Là rỗng (Null)",
            [ZeroStringId.FilterIsNotNull] = "Không rỗng",
            [ZeroStringId.FilterAddCondition] = "+ Điều kiện",
            [ZeroStringId.FilterAddGroup] = "+ Nhóm",

            [ZeroStringId.WizardBack] = "← Quay lại",
            [ZeroStringId.WizardNext] = "Tiếp theo →",
            [ZeroStringId.WizardFinish] = "Hoàn tất ✓",
            [ZeroStringId.WizardCancel] = "Hủy",
            [ZeroStringId.WizardStepTitleDefault] = "Tiêu đề bước",
            [ZeroStringId.WizardValidationTitle] = "Xác thực bước",
            [ZeroStringId.WizardNoPages] = "Chưa có trang",
            [ZeroStringId.WizardNoPagesDesc] = "Thêm các trang bước vào wizard này.",

            [ZeroStringId.PrintButton] = "🖨️ In ấn",
            [ZeroStringId.PrintStatusFormat] = "Trang {0} / {1}",
            [ZeroStringId.ZoomFit] = "100%",

            [ZeroStringId.ValRequired] = "Trường này là bắt buộc.",
            [ZeroStringId.ValRangeFormat] = "Giá trị phải nằm trong khoảng từ {0} đến {1}.",
            [ZeroStringId.ValEmail] = "Định dạng địa chỉ email không hợp lệ.",
            [ZeroStringId.ValPhone] = "Định dạng số điện thoại không hợp lệ.",
            [ZeroStringId.ValStringLengthFormat] = "Độ dài văn bản phải từ {0} đến {1} ký tự.",
            [ZeroStringId.ValInvalidFormat] = "Định dạng nhập liệu không hợp lệ."
        };

        private static readonly Dictionary<string, Dictionary<ZeroStringId, string>> CustomLanguages =
            new Dictionary<string, Dictionary<ZeroStringId, string>>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<ZeroStringId, string> Overrides =
            new Dictionary<ZeroStringId, string>();

        private static CultureInfo _currentCulture = CultureInfo.InvariantCulture;

        public static event EventHandler? CultureChanged;

        public static CultureInfo CurrentCulture
        {
            get => _currentCulture;
            set
            {
                if (_currentCulture != value)
                {
                    _currentCulture = value ?? CultureInfo.InvariantCulture;
                    CultureChanged?.Invoke(null, EventArgs.Empty);
                }
            }
        }

        public static void SetLanguage(string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
            {
                CurrentCulture = CultureInfo.InvariantCulture;
                return;
            }

            try
            {
                CurrentCulture = new CultureInfo(languageCode);
            }
            catch
            {
                CurrentCulture = CultureInfo.InvariantCulture;
            }
        }

        public static void SetLanguage(CultureInfo culture)
        {
            CurrentCulture = culture ?? CultureInfo.InvariantCulture;
        }

        /// <summary>
        /// Overrides a specific string ID with a custom application-defined text value.
        /// </summary>
        public static void Override(ZeroStringId id, string customText)
        {
            if (customText == null)
            {
                Overrides.Remove(id);
            }
            else
            {
                Overrides[id] = customText;
            }
            CultureChanged?.Invoke(null, EventArgs.Empty);
        }

        /// <summary>
        /// Resets all custom overrides.
        /// </summary>
        public static void ResetOverrides()
        {
            Overrides.Clear();
            CultureChanged?.Invoke(null, EventArgs.Empty);
        }

        /// <summary>
        /// Registers a full custom language dictionary (e.g. "ja", "de", "fr").
        /// </summary>
        public static void RegisterLanguage(string languageCode, IDictionary<ZeroStringId, string> dictionary)
        {
            if (string.IsNullOrWhiteSpace(languageCode) || dictionary == null) return;
            var dict = new Dictionary<ZeroStringId, string>(dictionary);
            CustomLanguages[languageCode] = dict;
        }

        /// <summary>
        /// Retrieves the localized string for the specified ID according to CurrentCulture.
        /// Falls back to English if missing from the active culture.
        /// </summary>
        public static string GetString(ZeroStringId id)
        {
            // 1. Check user-defined overrides
            if (Overrides.TryGetValue(id, out var overridden))
            {
                return overridden;
            }

            // 2. Check registered custom languages
            string lang = _currentCulture.TwoLetterISOLanguageName.ToLowerInvariant();
            if (CustomLanguages.TryGetValue(lang, out var customDict) && customDict.TryGetValue(id, out var customStr))
            {
                return customStr;
            }

            // 3. Check built-in Vietnamese
            if (lang == "vi")
            {
                if (Vietnamese.TryGetValue(id, out var viStr)) return viStr;
            }

            // 4. Fallback to built-in English
            if (English.TryGetValue(id, out var enStr))
            {
                return enStr;
            }

            return id.ToString();
        }

        /// <summary>
        /// Retrieves a localized formatted string with argument replacement.
        /// </summary>
        public static string GetFormattedString(ZeroStringId id, params object[] args)
        {
            string format = GetString(id);
            if (args == null || args.Length == 0) return format;
            try
            {
                return string.Format(_currentCulture, format, args);
            }
            catch
            {
                return format;
            }
        }
    }
}
