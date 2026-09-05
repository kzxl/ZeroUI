using System;
using System.Globalization;
using Xunit;
using ZeroUI.Core.Data;
using ZeroUI.Core.Localization;

namespace ZeroUI.Core.Tests
{
    public class ZeroLocalizerTests : IDisposable
    {
        public ZeroLocalizerTests()
        {
            // Reset to English before each test
            ZeroLocalizer.ResetOverrides();
            ZeroLocalizer.SetLanguage("en");
        }

        public void Dispose()
        {
            // Ensure clean state after tests
            ZeroLocalizer.ResetOverrides();
            ZeroLocalizer.SetLanguage("en");
        }

        [Fact]
        public void DefaultLanguage_IsEnglish()
        {
            Assert.Equal("OK", ZeroLocalizer.GetString(ZeroStringId.Ok));
            Assert.Equal("Cancel", ZeroLocalizer.GetString(ZeroStringId.Cancel));
            Assert.Equal("Select items...", ZeroLocalizer.GetString(ZeroStringId.CheckedComboPlaceholder));
            Assert.Equal("This field is required.", ZeroLocalizer.GetString(ZeroStringId.ValRequired));
        }

        [Fact]
        public void SwitchLanguage_ToVietnamese_ReturnsCorrectTranslations()
        {
            ZeroLocalizer.SetLanguage("vi");

            Assert.Equal("Đồng ý", ZeroLocalizer.GetString(ZeroStringId.Ok));
            Assert.Equal("Hủy", ZeroLocalizer.GetString(ZeroStringId.Cancel));
            Assert.Equal("Chọn mục...", ZeroLocalizer.GetString(ZeroStringId.CheckedComboPlaceholder));
            Assert.Equal("VÀ", ZeroLocalizer.GetString(ZeroStringId.FilterOpAnd));
            Assert.Equal("HOẶC", ZeroLocalizer.GetString(ZeroStringId.FilterOpOr));
            Assert.Equal("Hoàn tất ✓", ZeroLocalizer.GetString(ZeroStringId.WizardFinish));
            Assert.Equal("Trường này là bắt buộc.", ZeroLocalizer.GetString(ZeroStringId.ValRequired));
        }

        [Fact]
        public void GetFormattedString_FormatsArgumentsProperly()
        {
            ZeroLocalizer.SetLanguage("en");
            string pageEn = ZeroLocalizer.GetFormattedString(ZeroStringId.PrintStatusFormat, 2, 5);
            Assert.Equal("Page 2 of 5", pageEn);

            ZeroLocalizer.SetLanguage("vi");
            string summaryVi = ZeroLocalizer.GetFormattedString(ZeroStringId.CheckedComboSummaryFormat, 4);
            Assert.Equal("Đã chọn 4 mục", summaryVi);
        }

        [Fact]
        public void Override_CustomString_OverridesTranslationAndRaisesEvent()
        {
            bool eventFired = false;
            EventHandler handler = (s, e) => eventFired = true;
            ZeroLocalizer.CultureChanged += handler;

            try
            {
                ZeroLocalizer.Override(ZeroStringId.Ok, "Custom Confirm");
                Assert.True(eventFired);
                Assert.Equal("Custom Confirm", ZeroLocalizer.GetString(ZeroStringId.Ok));
            }
            finally
            {
                ZeroLocalizer.CultureChanged -= handler;
            }
        }

        [Fact]
        public void FilterCriteriaExtensions_ProvideLocalizedStrings()
        {
            ZeroLocalizer.SetLanguage("vi");

            Assert.Equal("VÀ", FilterGroupOperator.And.GetLocalizedName());
            Assert.Equal("HOẶC", FilterGroupOperator.Or.GetLocalizedName());
            Assert.Equal("KHÔNG VÀ", FilterGroupOperator.NotAnd.GetLocalizedName());
            Assert.Equal("Bằng", FilterComparisonOperator.Equals.GetLocalizedName());
            Assert.Equal("Chứa", FilterComparisonOperator.Contains.GetLocalizedName());
            Assert.Equal("Khác", FilterComparisonOperator.NotEquals.GetLocalizedName());

            ZeroLocalizer.SetLanguage("en");
            Assert.Equal("AND", FilterGroupOperator.And.GetLocalizedName());
            Assert.Equal("OR", FilterGroupOperator.Or.GetLocalizedName());
            Assert.Equal("Equals", FilterComparisonOperator.Equals.GetLocalizedName());
        }

        [Fact]
        public void Fallback_ForUnsupportedLanguage_DefaultsToEnglish()
        {
            ZeroLocalizer.SetLanguage(new CultureInfo("fr-FR"));
            Assert.Equal("Cancel", ZeroLocalizer.GetString(ZeroStringId.Cancel));
            Assert.Equal("OK", ZeroLocalizer.GetString(ZeroStringId.Ok));
        }
    }
}
