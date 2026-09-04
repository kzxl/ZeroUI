using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;

namespace ZeroUI.Wpf.Theme
{
    /// <summary>
    /// Modern Fluent ControlTemplates and implicit Styles for WPF standard controls.
    /// Overrides default Windows Aero chrome across all controls (Buttons, ComboBox, ScrollBar, CheckBox, Radio, TextBox, ToolTip, ContextMenu, Tabs).
    /// </summary>
    public static class ZeroWpfStyles
    {
        private static ResourceDictionary? _stylesDict;

        public static ResourceDictionary Dictionary => _stylesDict ??= LoadStylesDictionary();

        public static Style ButtonStyle => (Style)Dictionary[typeof(Button)];
        public static Style ComboBoxStyle => (Style)Dictionary[typeof(ComboBox)];
        public static Style ComboBoxItemStyle => (Style)Dictionary[typeof(ComboBoxItem)];
        public static Style ScrollBarStyle => (Style)Dictionary[typeof(ScrollBar)];
        public static Style ScrollViewerStyle => (Style)Dictionary[typeof(ScrollViewer)];
        public static Style CheckBoxStyle => (Style)Dictionary[typeof(CheckBox)];
        public static Style RadioButtonStyle => (Style)Dictionary[typeof(RadioButton)];
        public static Style TextBoxStyle => (Style)Dictionary[typeof(TextBox)];
        public static Style ToolTipStyle => (Style)Dictionary[typeof(ToolTip)];
        public static Style ContextMenuStyle => (Style)Dictionary[typeof(ContextMenu)];
        public static Style MenuItemStyle => (Style)Dictionary[typeof(MenuItem)];
        public static Style TabControlStyle => (Style)Dictionary[typeof(TabControl)];
        public static Style TabItemStyle => (Style)Dictionary[typeof(TabItem)];

        public static void ApplyStyles(Application? app = null)
        {
            var targetApp = app ?? Application.Current;
            if (targetApp == null) return;

            ZeroWpfTheme.UpdateApplicationResources();

            var dict = Dictionary;
            if (!targetApp.Resources.MergedDictionaries.Contains(dict))
            {
                targetApp.Resources.MergedDictionaries.Add(dict);
            }
        }

        private static ResourceDictionary LoadStylesDictionary()
        {
            const string xaml = @"
<ResourceDictionary xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">

    <!-- 1. SLIM FLUENT SCROLLBAR -->
    <ControlTemplate x:Key=""ZeroVerticalScrollBar"" TargetType=""{x:Type ScrollBar}"">
        <Grid Width=""8"" Background=""Transparent"">
            <Track x:Name=""PART_Track"" IsDirectionReversed=""True"">
                <Track.Thumb>
                    <Thumb>
                        <Thumb.Template>
                            <ControlTemplate TargetType=""{x:Type Thumb}"">
                                <Border x:Name=""thumbBorder""
                                        Background=""{DynamicResource ZeroUI.ScrollThumb}""
                                        CornerRadius=""4""
                                        Margin=""1,0,1,0""
                                        SnapsToDevicePixels=""True"" />
                                <ControlTemplate.Triggers>
                                    <Trigger Property=""IsMouseOver"" Value=""True"">
                                        <Setter TargetName=""thumbBorder"" Property=""Background"" Value=""{DynamicResource ZeroUI.ScrollThumbHover}"" />
                                    </Trigger>
                                </ControlTemplate.Triggers>
                            </ControlTemplate>
                        </Thumb.Template>
                    </Thumb>
                </Track.Thumb>
            </Track>
        </Grid>
    </ControlTemplate>

    <ControlTemplate x:Key=""ZeroHorizontalScrollBar"" TargetType=""{x:Type ScrollBar}"">
        <Grid Height=""8"" Background=""Transparent"">
            <Track x:Name=""PART_Track"" IsDirectionReversed=""False"">
                <Track.Thumb>
                    <Thumb>
                        <Thumb.Template>
                            <ControlTemplate TargetType=""{x:Type Thumb}"">
                                <Border x:Name=""thumbBorder""
                                        Background=""{DynamicResource ZeroUI.ScrollThumb}""
                                        CornerRadius=""4""
                                        Margin=""0,1,0,1""
                                        SnapsToDevicePixels=""True"" />
                                <ControlTemplate.Triggers>
                                    <Trigger Property=""IsMouseOver"" Value=""True"">
                                        <Setter TargetName=""thumbBorder"" Property=""Background"" Value=""{DynamicResource ZeroUI.ScrollThumbHover}"" />
                                    </Trigger>
                                </ControlTemplate.Triggers>
                            </ControlTemplate>
                        </Thumb.Template>
                    </Thumb>
                </Track.Thumb>
            </Track>
        </Grid>
    </ControlTemplate>

    <Style TargetType=""{x:Type ScrollBar}"">
        <Setter Property=""SnapsToDevicePixels"" Value=""True"" />
        <Setter Property=""OverridesDefaultStyle"" Value=""True"" />
        <Style.Triggers>
            <Trigger Property=""Orientation"" Value=""Vertical"">
                <Setter Property=""Width"" Value=""8"" />
                <Setter Property=""Template"" Value=""{StaticResource ZeroVerticalScrollBar}"" />
            </Trigger>
            <Trigger Property=""Orientation"" Value=""Horizontal"">
                <Setter Property=""Height"" Value=""8"" />
                <Setter Property=""Template"" Value=""{StaticResource ZeroHorizontalScrollBar}"" />
            </Trigger>
        </Style.Triggers>
    </Style>

    <Style TargetType=""{x:Type ScrollViewer}"">
        <Setter Property=""OverridesDefaultStyle"" Value=""True"" />
        <Setter Property=""Template"">
            <Setter.Value>
                <ControlTemplate TargetType=""{x:Type ScrollViewer}"">
                    <Grid>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width=""*"" />
                            <ColumnDefinition Width=""Auto"" />
                        </Grid.ColumnDefinitions>
                        <Grid.RowDefinitions>
                            <RowDefinition Height=""*"" />
                            <RowDefinition Height=""Auto"" />
                        </Grid.RowDefinitions>
                        <ScrollContentPresenter Grid.Column=""0"" Grid.Row=""0"" Margin=""{TemplateBinding Padding}"" />
                        <ScrollBar x:Name=""PART_VerticalScrollBar""
                                   Grid.Column=""1"" Grid.Row=""0""
                                   Value=""{TemplateBinding VerticalOffset}""
                                   Maximum=""{TemplateBinding ScrollableHeight}""
                                   ViewportSize=""{TemplateBinding ViewportHeight}""
                                   Visibility=""{TemplateBinding ComputedVerticalScrollBarVisibility}"" />
                        <ScrollBar x:Name=""PART_HorizontalScrollBar""
                                   Grid.Column=""0"" Grid.Row=""1""
                                   Orientation=""Horizontal""
                                   Value=""{TemplateBinding HorizontalOffset}""
                                   Maximum=""{TemplateBinding ScrollableWidth}""
                                   ViewportSize=""{TemplateBinding ViewportWidth}""
                                   Visibility=""{TemplateBinding ComputedHorizontalScrollBarVisibility}"" />
                    </Grid>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- 2. BUTTON STYLE -->
    <Style TargetType=""{x:Type Button}"">
        <Setter Property=""SnapsToDevicePixels"" Value=""True"" />
        <Setter Property=""OverridesDefaultStyle"" Value=""True"" />
        <Setter Property=""Background"" Value=""{DynamicResource ZeroUI.BgInput}"" />
        <Setter Property=""Foreground"" Value=""{DynamicResource ZeroUI.TextPrimary}"" />
        <Setter Property=""BorderBrush"" Value=""{DynamicResource ZeroUI.BorderDefault}"" />
        <Setter Property=""BorderThickness"" Value=""1"" />
        <Setter Property=""Padding"" Value=""12,5,12,5"" />
        <Setter Property=""FontSize"" Value=""12"" />
        <Setter Property=""Cursor"" Value=""Hand"" />
        <Setter Property=""Template"">
            <Setter.Value>
                <ControlTemplate TargetType=""{x:Type Button}"">
                    <Border x:Name=""btnBorder""
                            Background=""{TemplateBinding Background}""
                            BorderBrush=""{TemplateBinding BorderBrush}""
                            BorderThickness=""{TemplateBinding BorderThickness}""
                            CornerRadius=""5""
                            Padding=""{TemplateBinding Padding}""
                            SnapsToDevicePixels=""True"">
                        <ContentPresenter HorizontalAlignment=""Center""
                                          VerticalAlignment=""Center""
                                          RecognizesAccessKey=""True"" />
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property=""IsMouseOver"" Value=""True"">
                            <Setter TargetName=""btnBorder"" Property=""Background"" Value=""{DynamicResource ZeroUI.BgHover}"" />
                            <Setter TargetName=""btnBorder"" Property=""BorderBrush"" Value=""{DynamicResource ZeroUI.PrimaryAccent}"" />
                        </Trigger>
                        <Trigger Property=""IsPressed"" Value=""True"">
                            <Setter TargetName=""btnBorder"" Property=""Background"" Value=""{DynamicResource ZeroUI.BgActive}"" />
                        </Trigger>
                        <Trigger Property=""IsEnabled"" Value=""False"">
                            <Setter TargetName=""btnBorder"" Property=""Background"" Value=""{DynamicResource ZeroUI.BgDisabled}"" />
                            <Setter TargetName=""btnBorder"" Property=""BorderBrush"" Value=""{DynamicResource ZeroUI.BorderSubtle}"" />
                            <Setter Property=""Foreground"" Value=""{DynamicResource ZeroUI.TextMuted}"" />
                            <Setter Property=""Opacity"" Value=""0.5"" />
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- 3. COMBOBOX ITEM STYLE -->
    <Style TargetType=""{x:Type ComboBoxItem}"">
        <Setter Property=""SnapsToDevicePixels"" Value=""True"" />
        <Setter Property=""OverridesDefaultStyle"" Value=""True"" />
        <Setter Property=""Foreground"" Value=""{DynamicResource ZeroUI.TextPrimary}"" />
        <Setter Property=""FontSize"" Value=""12"" />
        <Setter Property=""Cursor"" Value=""Hand"" />
        <Setter Property=""Template"">
            <Setter.Value>
                <ControlTemplate TargetType=""{x:Type ComboBoxItem}"">
                    <Border x:Name=""itemBorder""
                            Background=""Transparent""
                            CornerRadius=""4""
                            Margin=""2,1,2,1""
                            Padding=""8,5,8,5""
                            SnapsToDevicePixels=""True"">
                        <ContentPresenter />
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property=""IsMouseOver"" Value=""True"">
                            <Setter TargetName=""itemBorder"" Property=""Background"" Value=""{DynamicResource ZeroUI.BgHover}"" />
                            <Setter Property=""Foreground"" Value=""{DynamicResource ZeroUI.TextPrimary}"" />
                        </Trigger>
                        <Trigger Property=""IsSelected"" Value=""True"">
                            <Setter TargetName=""itemBorder"" Property=""Background"" Value=""{DynamicResource ZeroUI.SelectionBackground}"" />
                            <Setter Property=""Foreground"" Value=""{DynamicResource ZeroUI.SelectionForeground}"" />
                            <Setter Property=""FontWeight"" Value=""SemiBold"" />
                        </Trigger>
                        <Trigger Property=""IsEnabled"" Value=""False"">
                            <Setter Property=""Foreground"" Value=""{DynamicResource ZeroUI.TextMuted}"" />
                            <Setter Property=""Opacity"" Value=""0.5"" />
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- 4. COMBOBOX STYLE -->
    <Style TargetType=""{x:Type ComboBox}"">
        <Setter Property=""SnapsToDevicePixels"" Value=""True"" />
        <Setter Property=""OverridesDefaultStyle"" Value=""True"" />
        <Setter Property=""Background"" Value=""{DynamicResource ZeroUI.BgInput}"" />
        <Setter Property=""Foreground"" Value=""{DynamicResource ZeroUI.TextPrimary}"" />
        <Setter Property=""BorderBrush"" Value=""{DynamicResource ZeroUI.BorderDefault}"" />
        <Setter Property=""BorderThickness"" Value=""1"" />
        <Setter Property=""FontSize"" Value=""12"" />
        <Setter Property=""ScrollViewer.HorizontalScrollBarVisibility"" Value=""Auto"" />
        <Setter Property=""ScrollViewer.VerticalScrollBarVisibility"" Value=""Auto"" />
        <Setter Property=""ItemContainerStyle"" Value=""{DynamicResource {x:Type ComboBoxItem}}"" />
        <Setter Property=""Template"">
            <Setter.Value>
                <ControlTemplate TargetType=""{x:Type ComboBox}"">
                    <Grid>
                        <ToggleButton x:Name=""toggleBtn""
                                      Focusable=""False""
                                      IsChecked=""{Binding IsDropDownOpen, Mode=TwoWay, RelativeSource={RelativeSource TemplatedParent}}""
                                      ClickMode=""Press"">
                            <ToggleButton.Template>
                                <ControlTemplate TargetType=""{x:Type ToggleButton}"">
                                    <Border x:Name=""tBorder""
                                            Background=""{DynamicResource ZeroUI.BgInput}""
                                            BorderBrush=""{DynamicResource ZeroUI.BorderDefault}""
                                            BorderThickness=""1""
                                            CornerRadius=""5""
                                            SnapsToDevicePixels=""True"">
                                        <Grid>
                                            <Grid.ColumnDefinitions>
                                                <ColumnDefinition Width=""*"" />
                                                <ColumnDefinition Width=""22"" />
                                            </Grid.ColumnDefinitions>
                                            <Path x:Name=""arrow""
                                                  Grid.Column=""1""
                                                  Data=""M 0 0 L 4 4 L 8 0 Z""
                                                  Fill=""{DynamicResource ZeroUI.TextMuted}""
                                                  HorizontalAlignment=""Center""
                                                  VerticalAlignment=""Center"" />
                                        </Grid>
                                    </Border>
                                    <ControlTemplate.Triggers>
                                        <Trigger Property=""IsMouseOver"" Value=""True"">
                                            <Setter TargetName=""tBorder"" Property=""Background"" Value=""{DynamicResource ZeroUI.BgHover}"" />
                                            <Setter TargetName=""tBorder"" Property=""BorderBrush"" Value=""{DynamicResource ZeroUI.PrimaryAccent}"" />
                                            <Setter TargetName=""arrow"" Property=""Fill"" Value=""{DynamicResource ZeroUI.PrimaryAccent}"" />
                                        </Trigger>
                                        <Trigger Property=""IsChecked"" Value=""True"">
                                            <Setter TargetName=""tBorder"" Property=""BorderBrush"" Value=""{DynamicResource ZeroUI.PrimaryAccent}"" />
                                            <Setter TargetName=""arrow"" Property=""Fill"" Value=""{DynamicResource ZeroUI.PrimaryAccent}"" />
                                        </Trigger>
                                    </ControlTemplate.Triggers>
                                </ControlTemplate>
                            </ToggleButton.Template>
                        </ToggleButton>
                        <ContentPresenter x:Name=""ContentSite""
                                          IsHitTestVisible=""False""
                                          Content=""{TemplateBinding SelectionBoxItem}""
                                          ContentTemplate=""{TemplateBinding SelectionBoxItemTemplate}""
                                          ContentTemplateSelector=""{TemplateBinding ItemTemplateSelector}""
                                          VerticalAlignment=""Center""
                                          HorizontalAlignment=""Left""
                                          Margin=""8,2,22,2"">
                            <ContentPresenter.Resources>
                                <Style TargetType=""{x:Type TextBlock}"">
                                    <Setter Property=""Foreground"" Value=""{DynamicResource ZeroUI.TextPrimary}"" />
                                </Style>
                            </ContentPresenter.Resources>
                        </ContentPresenter>
                        <Popup x:Name=""PART_Popup""
                               Placement=""Bottom""
                               IsOpen=""{TemplateBinding IsDropDownOpen}""
                               AllowsTransparency=""True""
                               Focusable=""False""
                               PopupAnimation=""Slide"">
                            <Grid x:Name=""DropDown""
                                  SnapsToDevicePixels=""True""
                                  MinWidth=""{TemplateBinding ActualWidth}""
                                  MaxHeight=""{TemplateBinding MaxDropDownHeight}"">
                                <Border x:Name=""DropDownBorder""
                                        Background=""{DynamicResource ZeroUI.BgCard}""
                                        BorderBrush=""{DynamicResource ZeroUI.BorderDefault}""
                                        BorderThickness=""1""
                                        CornerRadius=""6""
                                        Margin=""0,3,0,6""
                                        Padding=""3"">
                                    <Border.Effect>
                                        <DropShadowEffect BlurRadius=""10"" ShadowDepth=""3"" Direction=""270"" Opacity=""0.4"" Color=""#000000"" />
                                    </Border.Effect>
                                    <ScrollViewer SnapsToDevicePixels=""True"">
                                        <StackPanel IsItemsHost=""True"" KeyboardNavigation.DirectionalNavigation=""Contained"" />
                                    </ScrollViewer>
                                </Border>
                            </Grid>
                        </Popup>
                    </Grid>
                    <ControlTemplate.Triggers>
                        <Trigger Property=""IsEnabled"" Value=""False"">
                            <Setter Property=""Opacity"" Value=""0.5"" />
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- 5. CHECKBOX STYLE -->
    <Style TargetType=""{x:Type CheckBox}"">
        <Setter Property=""SnapsToDevicePixels"" Value=""True"" />
        <Setter Property=""OverridesDefaultStyle"" Value=""True"" />
        <Setter Property=""Foreground"" Value=""{DynamicResource ZeroUI.TextPrimary}"" />
        <Setter Property=""FontSize"" Value=""12.5"" />
        <Setter Property=""Cursor"" Value=""Hand"" />
        <Setter Property=""Template"">
            <Setter.Value>
                <ControlTemplate TargetType=""{x:Type CheckBox}"">
                    <StackPanel Orientation=""Horizontal"" VerticalAlignment=""Center"">
                        <Border x:Name=""chkBorder""
                                Width=""18"" Height=""18""
                                Background=""{DynamicResource ZeroUI.BgInput}""
                                BorderBrush=""{DynamicResource ZeroUI.BorderDefault}""
                                BorderThickness=""1""
                                CornerRadius=""4""
                                VerticalAlignment=""Center""
                                SnapsToDevicePixels=""True"">
                            <Path x:Name=""chkPath""
                                  Data=""M 3 8 L 7 12 L 13 4""
                                  Stroke=""White""
                                  StrokeThickness=""2""
                                  StrokeStartLineCap=""Round""
                                  StrokeEndLineCap=""Round""
                                  StrokeLineJoin=""Round""
                                  Visibility=""Collapsed""
                                  HorizontalAlignment=""Center""
                                  VerticalAlignment=""Center"" />
                        </Border>
                        <ContentPresenter Margin=""8,0,0,0""
                                          VerticalAlignment=""Center""
                                          RecognizesAccessKey=""True"" />
                    </StackPanel>
                    <ControlTemplate.Triggers>
                        <Trigger Property=""IsMouseOver"" Value=""True"">
                            <Setter TargetName=""chkBorder"" Property=""BorderBrush"" Value=""{DynamicResource ZeroUI.PrimaryAccent}"" />
                            <Setter TargetName=""chkBorder"" Property=""Background"" Value=""{DynamicResource ZeroUI.BgHover}"" />
                        </Trigger>
                        <Trigger Property=""IsChecked"" Value=""True"">
                            <Setter TargetName=""chkBorder"" Property=""Background"" Value=""{DynamicResource ZeroUI.PrimaryAccent}"" />
                            <Setter TargetName=""chkBorder"" Property=""BorderBrush"" Value=""{DynamicResource ZeroUI.PrimaryAccent}"" />
                            <Setter TargetName=""chkPath"" Property=""Visibility"" Value=""Visible"" />
                        </Trigger>
                        <Trigger Property=""IsEnabled"" Value=""False"">
                            <Setter TargetName=""chkBorder"" Property=""Background"" Value=""{DynamicResource ZeroUI.BgDisabled}"" />
                            <Setter TargetName=""chkBorder"" Property=""BorderBrush"" Value=""{DynamicResource ZeroUI.BorderSubtle}"" />
                            <Setter Property=""Foreground"" Value=""{DynamicResource ZeroUI.TextMuted}"" />
                            <Setter Property=""Opacity"" Value=""0.5"" />
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- 6. RADIOBUTTON STYLE -->
    <Style TargetType=""{x:Type RadioButton}"">
        <Setter Property=""SnapsToDevicePixels"" Value=""True"" />
        <Setter Property=""OverridesDefaultStyle"" Value=""True"" />
        <Setter Property=""Foreground"" Value=""{DynamicResource ZeroUI.TextPrimary}"" />
        <Setter Property=""FontSize"" Value=""12.5"" />
        <Setter Property=""Cursor"" Value=""Hand"" />
        <Setter Property=""Template"">
            <Setter.Value>
                <ControlTemplate TargetType=""{x:Type RadioButton}"">
                    <StackPanel Orientation=""Horizontal"" VerticalAlignment=""Center"">
                        <Border x:Name=""radioBorder""
                                Width=""18"" Height=""18""
                                Background=""{DynamicResource ZeroUI.BgInput}""
                                BorderBrush=""{DynamicResource ZeroUI.BorderDefault}""
                                BorderThickness=""1""
                                CornerRadius=""9""
                                VerticalAlignment=""Center""
                                SnapsToDevicePixels=""True"">
                            <Ellipse x:Name=""radioDot""
                                     Width=""8"" Height=""8""
                                     Fill=""{DynamicResource ZeroUI.PrimaryAccent}""
                                     Visibility=""Collapsed""
                                     HorizontalAlignment=""Center""
                                     VerticalAlignment=""Center"" />
                        </Border>
                        <ContentPresenter Margin=""8,0,0,0""
                                          VerticalAlignment=""Center""
                                          RecognizesAccessKey=""True"" />
                    </StackPanel>
                    <ControlTemplate.Triggers>
                        <Trigger Property=""IsMouseOver"" Value=""True"">
                            <Setter TargetName=""radioBorder"" Property=""BorderBrush"" Value=""{DynamicResource ZeroUI.PrimaryAccent}"" />
                            <Setter TargetName=""radioBorder"" Property=""Background"" Value=""{DynamicResource ZeroUI.BgHover}"" />
                        </Trigger>
                        <Trigger Property=""IsChecked"" Value=""True"">
                            <Setter TargetName=""radioBorder"" Property=""BorderBrush"" Value=""{DynamicResource ZeroUI.PrimaryAccent}"" />
                            <Setter TargetName=""radioDot"" Property=""Visibility"" Value=""Visible"" />
                        </Trigger>
                        <Trigger Property=""IsEnabled"" Value=""False"">
                            <Setter TargetName=""radioBorder"" Property=""Background"" Value=""{DynamicResource ZeroUI.BgDisabled}"" />
                            <Setter TargetName=""radioBorder"" Property=""BorderBrush"" Value=""{DynamicResource ZeroUI.BorderSubtle}"" />
                            <Setter Property=""Foreground"" Value=""{DynamicResource ZeroUI.TextMuted}"" />
                            <Setter Property=""Opacity"" Value=""0.5"" />
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- 7. TEXTBOX STYLE -->
    <Style TargetType=""{x:Type TextBox}"">
        <Setter Property=""SnapsToDevicePixels"" Value=""True"" />
        <Setter Property=""OverridesDefaultStyle"" Value=""True"" />
        <Setter Property=""Background"" Value=""{DynamicResource ZeroUI.BgInput}"" />
        <Setter Property=""Foreground"" Value=""{DynamicResource ZeroUI.TextPrimary}"" />
        <Setter Property=""BorderBrush"" Value=""{DynamicResource ZeroUI.BorderDefault}"" />
        <Setter Property=""BorderThickness"" Value=""1"" />
        <Setter Property=""Padding"" Value=""10,6,10,6"" />
        <Setter Property=""FontSize"" Value=""12.5"" />
        <Setter Property=""CaretBrush"" Value=""{DynamicResource ZeroUI.PrimaryAccent}"" />
        <Setter Property=""SelectionBrush"" Value=""{DynamicResource ZeroUI.SelectionBackground}"" />
        <Setter Property=""SelectionOpacity"" Value=""0.6"" />
        <Setter Property=""Template"">
            <Setter.Value>
                <ControlTemplate TargetType=""{x:Type TextBox}"">
                    <Border x:Name=""txtBorder""
                            Background=""{TemplateBinding Background}""
                            BorderBrush=""{TemplateBinding BorderBrush}""
                            BorderThickness=""{TemplateBinding BorderThickness}""
                            CornerRadius=""5""
                            Padding=""{TemplateBinding Padding}""
                            SnapsToDevicePixels=""True"">
                        <ScrollViewer x:Name=""PART_ContentHost"" Focusable=""False"" HorizontalScrollBarVisibility=""Hidden"" VerticalScrollBarVisibility=""Hidden"" />
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property=""IsMouseOver"" Value=""True"">
                            <Setter TargetName=""txtBorder"" Property=""BorderBrush"" Value=""{DynamicResource ZeroUI.BorderFocus}"" />
                        </Trigger>
                        <Trigger Property=""IsKeyboardFocused"" Value=""True"">
                            <Setter TargetName=""txtBorder"" Property=""BorderBrush"" Value=""{DynamicResource ZeroUI.BorderFocus}"" />
                        </Trigger>
                        <Trigger Property=""IsEnabled"" Value=""False"">
                            <Setter TargetName=""txtBorder"" Property=""Background"" Value=""{DynamicResource ZeroUI.BgDisabled}"" />
                            <Setter TargetName=""txtBorder"" Property=""BorderBrush"" Value=""{DynamicResource ZeroUI.BorderSubtle}"" />
                            <Setter Property=""Foreground"" Value=""{DynamicResource ZeroUI.TextMuted}"" />
                            <Setter Property=""Opacity"" Value=""0.5"" />
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- 8. TOOLTIP STYLE -->
    <Style TargetType=""{x:Type ToolTip}"">
        <Setter Property=""SnapsToDevicePixels"" Value=""True"" />
        <Setter Property=""OverridesDefaultStyle"" Value=""True"" />
        <Setter Property=""Background"" Value=""{DynamicResource ZeroUI.BgCard}"" />
        <Setter Property=""Foreground"" Value=""{DynamicResource ZeroUI.TextPrimary}"" />
        <Setter Property=""BorderBrush"" Value=""{DynamicResource ZeroUI.BorderDefault}"" />
        <Setter Property=""BorderThickness"" Value=""1"" />
        <Setter Property=""Padding"" Value=""8,4,8,4"" />
        <Setter Property=""FontSize"" Value=""12"" />
        <Setter Property=""Template"">
            <Setter.Value>
                <ControlTemplate TargetType=""{x:Type ToolTip}"">
                    <Border Background=""{TemplateBinding Background}""
                            BorderBrush=""{TemplateBinding BorderBrush}""
                            BorderThickness=""{TemplateBinding BorderThickness}""
                            CornerRadius=""5""
                            Padding=""{TemplateBinding Padding}""
                            SnapsToDevicePixels=""True"">
                        <Border.Effect>
                            <DropShadowEffect BlurRadius=""8"" ShadowDepth=""2"" Direction=""270"" Opacity=""0.4"" Color=""#000000"" />
                        </Border.Effect>
                        <ContentPresenter Content=""{TemplateBinding Content}"" />
                    </Border>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- 9. CONTEXTMENU & MENUITEM STYLE -->
    <Style TargetType=""{x:Type ContextMenu}"">
        <Setter Property=""SnapsToDevicePixels"" Value=""True"" />
        <Setter Property=""OverridesDefaultStyle"" Value=""True"" />
        <Setter Property=""Background"" Value=""{DynamicResource ZeroUI.BgCard}"" />
        <Setter Property=""BorderBrush"" Value=""{DynamicResource ZeroUI.BorderDefault}"" />
        <Setter Property=""BorderThickness"" Value=""1"" />
        <Setter Property=""Padding"" Value=""4"" />
        <Setter Property=""Template"">
            <Setter.Value>
                <ControlTemplate TargetType=""{x:Type ContextMenu}"">
                    <Border Background=""{TemplateBinding Background}""
                            BorderBrush=""{TemplateBinding BorderBrush}""
                            BorderThickness=""{TemplateBinding BorderThickness}""
                            CornerRadius=""6""
                            Padding=""{TemplateBinding Padding}""
                            SnapsToDevicePixels=""True"">
                        <Border.Effect>
                            <DropShadowEffect BlurRadius=""10"" ShadowDepth=""3"" Direction=""270"" Opacity=""0.4"" Color=""#000000"" />
                        </Border.Effect>
                        <StackPanel IsItemsHost=""True"" KeyboardNavigation.DirectionalNavigation=""Cycle"" />
                    </Border>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <Style TargetType=""{x:Type MenuItem}"">
        <Setter Property=""SnapsToDevicePixels"" Value=""True"" />
        <Setter Property=""OverridesDefaultStyle"" Value=""True"" />
        <Setter Property=""Foreground"" Value=""{DynamicResource ZeroUI.TextPrimary}"" />
        <Setter Property=""FontSize"" Value=""12.5"" />
        <Setter Property=""Padding"" Value=""10,6,10,6"" />
        <Setter Property=""Cursor"" Value=""Hand"" />
        <Setter Property=""Template"">
            <Setter.Value>
                <ControlTemplate TargetType=""{x:Type MenuItem}"">
                    <Border x:Name=""itemBorder""
                            Background=""Transparent""
                            CornerRadius=""4""
                            Padding=""{TemplateBinding Padding}""
                            SnapsToDevicePixels=""True"">
                        <Grid>
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width=""Auto"" SharedSizeGroup=""Icon"" />
                                <ColumnDefinition Width=""*"" />
                                <ColumnDefinition Width=""Auto"" SharedSizeGroup=""Shortcut"" />
                            </Grid.ColumnDefinitions>
                            <ContentPresenter x:Name=""Icon""
                                              Grid.Column=""0""
                                              Content=""{TemplateBinding Icon}""
                                              Margin=""0,0,8,0""
                                              VerticalAlignment=""Center"" />
                            <ContentPresenter Grid.Column=""1""
                                              Content=""{TemplateBinding Header}""
                                              VerticalAlignment=""Center""
                                              RecognizesAccessKey=""True"" />
                            <TextBlock Grid.Column=""2""
                                       Text=""{TemplateBinding InputGestureText}""
                                       Foreground=""{DynamicResource ZeroUI.TextMuted}""
                                       Margin=""16,0,0,0""
                                       VerticalAlignment=""Center"" />
                        </Grid>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property=""IsHighlighted"" Value=""True"">
                            <Setter TargetName=""itemBorder"" Property=""Background"" Value=""{DynamicResource ZeroUI.BgHover}"" />
                            <Setter Property=""Foreground"" Value=""{DynamicResource ZeroUI.TextPrimary}"" />
                        </Trigger>
                        <Trigger Property=""IsEnabled"" Value=""False"">
                            <Setter Property=""Foreground"" Value=""{DynamicResource ZeroUI.TextMuted}"" />
                            <Setter Property=""Opacity"" Value=""0.5"" />
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- 10. TAB CONTROL & TAB ITEM STYLE -->
    <Style TargetType=""{x:Type TabControl}"">
        <Setter Property=""SnapsToDevicePixels"" Value=""True"" />
        <Setter Property=""OverridesDefaultStyle"" Value=""True"" />
        <Setter Property=""Background"" Value=""Transparent"" />
        <Setter Property=""Template"">
            <Setter.Value>
                <ControlTemplate TargetType=""{x:Type TabControl}"">
                    <Grid>
                        <Grid.RowDefinitions>
                            <RowDefinition Height=""Auto"" />
                            <RowDefinition Height=""*"" />
                        </Grid.RowDefinitions>
                        <Border Grid.Row=""0""
                                Background=""{DynamicResource ZeroUI.BgCard}""
                                BorderBrush=""{DynamicResource ZeroUI.BorderDefault}""
                                BorderThickness=""0,0,0,1""
                                Padding=""12,0,12,0"">
                            <TabPanel IsItemsHost=""True"" />
                        </Border>
                        <Border Grid.Row=""1""
                                Background=""{TemplateBinding Background}"">
                            <ContentPresenter ContentSource=""SelectedContent"" />
                        </Border>
                    </Grid>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <Style TargetType=""{x:Type TabItem}"">
        <Setter Property=""SnapsToDevicePixels"" Value=""True"" />
        <Setter Property=""OverridesDefaultStyle"" Value=""True"" />
        <Setter Property=""Foreground"" Value=""{DynamicResource ZeroUI.TextSecondary}"" />
        <Setter Property=""FontSize"" Value=""13"" />
        <Setter Property=""FontWeight"" Value=""Normal"" />
        <Setter Property=""Cursor"" Value=""Hand"" />
        <Setter Property=""Padding"" Value=""16,10,16,10"" />
        <Setter Property=""Template"">
            <Setter.Value>
                <ControlTemplate TargetType=""{x:Type TabItem}"">
                    <Border x:Name=""tabBorder""
                            Background=""Transparent""
                            BorderThickness=""0,0,0,2""
                            BorderBrush=""Transparent""
                            Padding=""{TemplateBinding Padding}""
                            SnapsToDevicePixels=""True"">
                        <ContentPresenter ContentSource=""Header""
                                          HorizontalAlignment=""Center""
                                          VerticalAlignment=""Center"" />
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property=""IsMouseOver"" Value=""True"">
                            <Setter TargetName=""tabBorder"" Property=""Background"" Value=""{DynamicResource ZeroUI.BgHover}"" />
                            <Setter Property=""Foreground"" Value=""{DynamicResource ZeroUI.TextPrimary}"" />
                        </Trigger>
                        <Trigger Property=""IsSelected"" Value=""True"">
                            <Setter TargetName=""tabBorder"" Property=""BorderBrush"" Value=""{DynamicResource ZeroUI.PrimaryAccent}"" />
                            <Setter Property=""Foreground"" Value=""{DynamicResource ZeroUI.PrimaryAccent}"" />
                            <Setter Property=""FontWeight"" Value=""SemiBold"" />
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

</ResourceDictionary>";

            return (ResourceDictionary)XamlReader.Parse(xaml);
        }
    }
}
