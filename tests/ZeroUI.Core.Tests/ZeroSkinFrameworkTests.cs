using System;
using Xunit;
using ZeroUI.Core.Theme;

namespace ZeroUI.Core.Tests
{
    public class MockSkinnableControl : IZeroSkinnable
    {
        public bool UseDefaultSkin { get; set; } = true;
        public ZeroSkin? CustomSkin { get; set; }
        public ZeroSkin EffectiveSkin => ZeroSkinManager.ResolveSkin(this);
        public bool SkinApplied { get; private set; }
        public ZeroSkin? LastAppliedSkin { get; private set; }

        public void ApplySkin(ZeroSkin skin)
        {
            SkinApplied = true;
            LastAppliedSkin = skin;
            CustomSkin = skin;
            UseDefaultSkin = false;
        }
    }

    public class ZeroSkinFrameworkTests
    {
        [Fact]
        public void DefaultSkinnable_ResolvesTo_CurrentGlobalSkin()
        {
            var control = new MockSkinnableControl();

            Assert.True(control.UseDefaultSkin);
            Assert.Null(control.CustomSkin);
            Assert.Same(ZeroSkinManager.CurrentSkin, control.EffectiveSkin);
        }

        [Fact]
        public void LocalCustomSkin_Overrides_GlobalSkin_When_UseDefaultSkin_IsFalse()
        {
            var control = new MockSkinnableControl();
            var customSkin = ZeroSkinDefaults.CleanLight;

            control.CustomSkin = customSkin;
            control.UseDefaultSkin = false;

            Assert.False(control.UseDefaultSkin);
            Assert.Same(customSkin, control.EffectiveSkin);
        }

        [Fact]
        public void LocalCustomSkin_Ignored_If_UseDefaultSkin_IsTrue()
        {
            var control = new MockSkinnableControl();
            var customSkin = ZeroSkinDefaults.CleanLight;

            control.CustomSkin = customSkin;
            control.UseDefaultSkin = true;

            Assert.True(control.UseDefaultSkin);
            Assert.Same(ZeroSkinManager.CurrentSkin, control.EffectiveSkin);
        }

        [Fact]
        public void ApplySkin_SetsCustomSkin_And_DisablesDefaultSkin()
        {
            var control = new MockSkinnableControl();
            var emerald = ZeroSkinDefaults.EmeraldEnterprise;

            control.ApplySkin(emerald);

            Assert.True(control.SkinApplied);
            Assert.False(control.UseDefaultSkin);
            Assert.Same(emerald, control.CustomSkin);
            Assert.Same(emerald, control.EffectiveSkin);
        }

        [Fact]
        public void ResolveSkin_HandlesNullGracefully()
        {
            var resolved = ZeroSkinManager.ResolveSkin(null);
            Assert.Same(ZeroSkinManager.CurrentSkin, resolved);
        }
    }
}
