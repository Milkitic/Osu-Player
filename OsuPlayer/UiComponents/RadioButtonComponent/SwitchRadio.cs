using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Milki.OsuPlayer.Common;
using Milki.OsuPlayer.Common.Configuration;
using Milki.OsuPlayer.Presentation.Dependency;

namespace Milki.OsuPlayer.UiComponents.RadioButtonComponent
{
    public class SwitchRadio : RadioButton
    {
        //protected Window HostWindow { get; private set; }
        private static readonly Dictionary<Type, FrameworkElement> PageMapping = new Dictionary<Type, FrameworkElement>();
        private Action<FrameworkElement> _loadedAction;

        protected FrameworkElement HostWindow { get; private set; }

        public SwitchRadio()
        {
            Loaded += (sender, e) =>
            {
                if (HostWindow != null)
                {
                    return;
                }

                //HostWindow = Window.GetWindow(this);
                HostWindow = this.FindParentObjects(typeof(Page), typeof(Window));
            };

            Checked += (sender, e) =>
            {
                if (HostWindow == null)
                {
                    return;
                }

                if (!string.IsNullOrWhiteSpace(Scope) && Scopes.ContainsKey(Scope))
                {
                    var others = Scopes[Scope].Where(k => k != this);
                    foreach (var switchRadio in others)
                    {
                        switchRadio.IsChecked = false;
                    }
                }

                if (string.IsNullOrWhiteSpace(TargetFrameControl))
                {
                    return;
                }

                if (HostWindow.FindName(TargetFrameControl) is Frame frame)
                {
                    var ui = (UIElement)frame.Content;
                    if (ui != null)
                    {
                        Storyboard.SetTarget(Da2, ui);
                        if (AppSettings.Default.Interface.MinimalMode)
                        {
                            OnSbOnCompleted(null, null);
                        }
                        else
                        {
                            FadeoutSb.Completed += OnSbOnCompleted;
                        }

                        FadeoutSb.Begin();

                        void OnSbOnCompleted(object obj, EventArgs args)
                        {
                            Navigate(frame);
                            //ui.BeginAnimation(OpacityProperty, null);
                            //var removeSb = new RemoveStoryboard { BeginStoryboardName = FadeoutSb.Name };
                            FadeoutSb.Completed -= OnSbOnCompleted;
                        }
                    }
                    else
                    {
                        Navigate(frame);
                    }
                    //var n = NavigationService.GetNavigationService(frame);
                    //frame.NavigationService.Navigate(new Uri($"{TargetPageType}?ExtraData={TargetPageData}", UriKind.Relative), TargetPageData);
                }
            };
        }

        public void CheckAndAction(Action<FrameworkElement> action)
        {
            if (IsChecked == true)
            {
                if (HostWindow.FindName(TargetFrameControl) is Frame frame)
                {
                    if (frame.Content != null)
                    {
                        action.Invoke(frame.Content as FrameworkElement);
                    }
                }
            }
            else
            {
                _loadedAction = action;
                IsChecked = true;
            }
        }

        private void Navigate(Frame frame)
        {
            FrameworkElement page;
            if (TargetPageSingleton && PageMapping.ContainsKey(TargetPageType))
            {
                page = PageMapping[TargetPageType];
            }
            else
            {
                page = (FrameworkElement)(TargetPageData == null
                    ? Activator.CreateInstance(TargetPageType)
                    : Activator.CreateInstance(TargetPageType, TargetPageData));
                if (TargetPageSingleton)
                {
                    PageMapping.Add(TargetPageType, page);
                }
            }

            _loadedAction?.Invoke(page);
            _loadedAction = null;
            var endOpacity = page.Opacity;
            var originTransform = page.RenderTransform;
            page.RenderTransformOrigin = new Point(0.5, 0.5);
            Storyboard.SetTarget(Da1, page);
            Storyboard.SetTarget(Ta1, page);
            Storyboard.SetTarget(Ta1Clone, page);
            if (page.RenderTransform.GetType() != typeof(ScaleTransform))
                page.RenderTransform = new ScaleTransform();
            frame.NavigationService.Navigate(page);

            FadeinSb.Completed += OnSbOnCompleted;
            FadeinSb.Begin();

            void OnSbOnCompleted(object sender, EventArgs e)
            {
                page.RenderTransform = originTransform;
                //page.BeginAnimation(OpacityProperty, null);
                //page.BeginAnimation(TranslateTransform.XProperty, null);
                //var removeSb = new RemoveStoryboard { BeginStoryboardName = FadeinSb.Name };
                FadeinSb.Completed -= OnSbOnCompleted;
            }
        }

        public object TargetPageData
        {
            get => GetValue(TargetPageDataProperty);
            set => SetValue(TargetPageDataProperty, value);
        }

        public static readonly DependencyProperty TargetPageDataProperty =
            DependencyProperty.Register(
                "TargetPageData",
                typeof(object),
                typeof(SwitchRadio)
            );

        public string Scope
        {
            get => (string)GetValue(ScopeProperty);
            set => SetValue(ScopeProperty, value);
        }

        public static readonly DependencyProperty ScopeProperty =
            DependencyProperty.Register(
                "Scope",
                typeof(string),
                typeof(SwitchRadio),
                new PropertyMetadata(null, OnScopeChanged)
            );

        private static void OnScopeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var oldVal = (string)e.OldValue;
            var newVal = (string)e.NewValue;
            var obj = (SwitchRadio)d;
            if (!string.IsNullOrWhiteSpace(oldVal))
            {
                if (Scopes.ContainsKey(oldVal))
                {
                    Scopes[oldVal].Remove(obj);
                    if (Scopes[oldVal].Count == 0)
                    {
                        Scopes.Remove(oldVal);
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(newVal))
            {
                if (!Scopes.ContainsKey(newVal))
                {
                    Scopes.Add(newVal, new List<SwitchRadio>());
                }

                Scopes[newVal].Add(obj);
            }
        }

        public ControlTemplate IconTemplate
        {
            get => (ControlTemplate)GetValue(IconTemplateProperty);
            set => SetValue(IconTemplateProperty, value);
        }

        public static readonly DependencyProperty IconTemplateProperty =
            DependencyProperty.Register(
                "IconTemplate",
                typeof(ControlTemplate),
                typeof(SwitchRadio),
                null
            );

        public Thickness CornerRadius
        {
            get => (Thickness)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                "CornerRadius",
                typeof(Thickness),
                typeof(SwitchRadio),
                new PropertyMetadata(new Thickness(0))
            );

        public Thickness IconMargin
        {
            get => (Thickness)GetValue(IconMarginProperty);
            set => SetValue(IconMarginProperty, value);
        }

        public static readonly DependencyProperty IconMarginProperty =
            DependencyProperty.Register(
                "IconMargin",
                typeof(Thickness),
                typeof(SwitchRadio),
                new PropertyMetadata(new Thickness(0, 0, 8, 0))
            );

        public Orientation IconOrientation
        {
            get => (Orientation)GetValue(IconOrientationProperty);
            set => SetValue(IconOrientationProperty, value);
        }

        public static readonly DependencyProperty IconOrientationProperty =
            DependencyProperty.Register(
                "IconOrientation",
                typeof(Orientation),
                typeof(SwitchRadio),
                new PropertyMetadata(Orientation.Horizontal)
            );

        public double IconSize
        {
            get => (double)GetValue(IconSizeProperty);
            set => SetValue(IconSizeProperty, value);
        }

        public static readonly DependencyProperty IconSizeProperty =
            DependencyProperty.Register(
                "IconSize",
                typeof(double),
                typeof(SwitchRadio),
                new PropertyMetadata(24d)
            );

        public Brush IconColor
        {
            get => (Brush)GetValue(IconColorProperty);
            set => SetValue(IconColorProperty, value);
        }

        public static readonly DependencyProperty IconColorProperty =
            DependencyProperty.Register(
                "IconColor",
                typeof(Brush),
                typeof(SwitchRadio),
                new PropertyMetadata(null)
            );

        public Type TargetPageType
        {
            get => (Type)GetValue(TargetPageTypeProperty);
            set => SetValue(TargetPageTypeProperty, value);
        }

        public static readonly DependencyProperty TargetPageTypeProperty =
            DependencyProperty.Register(
                "TargetPageType",
                typeof(Type),
                typeof(SwitchRadio)
            );

        public string TargetFrameControl
        {
            get => (string)GetValue(TargetFrameControlProperty);
            set => SetValue(TargetFrameControlProperty, value);
        }

        public static readonly DependencyProperty TargetFrameControlProperty =
            DependencyProperty.Register(
                "TargetFrameControl",
                typeof(string),
                typeof(SwitchRadio)
            );

        public bool TargetPageSingleton
        {
            get => (bool)GetValue(TargetPageSingletonProperty);
            set => SetValue(TargetPageSingletonProperty, value);
        }

        public static readonly DependencyProperty TargetPageSingletonProperty =
            DependencyProperty.Register(
                "TargetPageSingleton",
                typeof(bool),
                typeof(SwitchRadio)
            );

        public Brush MouseOverBackground
        {
            get => (Brush)GetValue(MouseOverBackgroundProperty);
            set => SetValue(MouseOverBackgroundProperty, value);
        }

        public Brush MouseOverForeground
        {
            get => (Brush)GetValue(MouseOverForegroundProperty);
            set => SetValue(MouseOverForegroundProperty, value);
        }

        public Brush MouseOverIconColor
        {
            get => (Brush)GetValue(MouseOverIconColorProperty);
            set => SetValue(MouseOverIconColorProperty, value);
        }

        public Brush MouseDownBackground
        {
            get => (Brush)GetValue(MouseDownBackgroundProperty);
            set => SetValue(MouseDownBackgroundProperty, value);
        }

        public Brush MouseDownForeground
        {
            get => (Brush)GetValue(MouseDownForegroundProperty);
            set => SetValue(MouseDownForegroundProperty, value);
        }
        public Brush MouseDownIconColor
        {
            get => (Brush)GetValue(MouseDownIconColorProperty);
            set => SetValue(MouseDownIconColorProperty, value);
        }

        public Brush CheckedBackground
        {
            get => (Brush)GetValue(CheckedBackgroundProperty);
            set => SetValue(CheckedBackgroundProperty, value);
        }

        public Brush CheckedForeground
        {
            get => (Brush)GetValue(CheckedForegroundProperty);
            set => SetValue(CheckedForegroundProperty, value);
        }

        public Brush CheckedIconColor
        {
            get => (Brush)GetValue(CheckedIconColorProperty);
            set => SetValue(CheckedIconColorProperty, value);
        }

        public static readonly DependencyProperty MouseOverBackgroundProperty = DependencyProperty.Register("MouseOverBackground", typeof(Brush), typeof(SwitchRadio), new PropertyMetadata(default(Brush)));
        public static readonly DependencyProperty MouseOverForegroundProperty = DependencyProperty.Register("MouseOverForeground", typeof(Brush), typeof(SwitchRadio), new PropertyMetadata(default(Brush)));
        public static readonly DependencyProperty MouseDownBackgroundProperty = DependencyProperty.Register("MouseDownBackground", typeof(Brush), typeof(SwitchRadio), new PropertyMetadata(default(Brush)));
        public static readonly DependencyProperty MouseDownForegroundProperty = DependencyProperty.Register("MouseDownForeground", typeof(Brush), typeof(SwitchRadio), new PropertyMetadata(default(Brush)));
        public static readonly DependencyProperty MouseDownIconColorProperty = DependencyProperty.Register("MouseDownIconColor", typeof(Brush), typeof(SwitchRadio), new PropertyMetadata(default(Brush)));
        public static readonly DependencyProperty CheckedBackgroundProperty = DependencyProperty.Register("CheckedBackground", typeof(Brush), typeof(SwitchRadio), new PropertyMetadata(default(Brush)));
        public static readonly DependencyProperty CheckedForegroundProperty = DependencyProperty.Register("CheckedForeground", typeof(Brush), typeof(SwitchRadio), new PropertyMetadata(default(Brush)));
        public static readonly DependencyProperty MouseOverIconColorProperty = DependencyProperty.Register("MouseOverIconColor", typeof(Brush), typeof(SwitchRadio), new PropertyMetadata(default(Brush)));
        public static readonly DependencyProperty CheckedIconColorProperty = DependencyProperty.Register("CheckedIconColor", typeof(Brush), typeof(SwitchRadio), new PropertyMetadata(default(Brush)));

        private static Dictionary<string, List<SwitchRadio>> Scopes { get; } =
            new Dictionary<string, List<SwitchRadio>>();

        static SwitchRadio()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SwitchRadio), new FrameworkPropertyMetadata(typeof(SwitchRadio)));

            FadeinSb = new Storyboard { Name = "FadeinSb" };
            Da1 = new DoubleAnimation
            {
                From = 0,
                To = 1,
                EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut },
                BeginTime = TimeSpan.Zero,
                Duration = CommonUtils.GetDuration(TimeSpan.FromMilliseconds(300))
            };
            Storyboard.SetTargetProperty(Da1, new PropertyPath(OpacityProperty));

            Ta1 = new DoubleAnimation
            {
                From = 0.95,
                To = 1,
                EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut },
                BeginTime = TimeSpan.Zero,
                Duration = CommonUtils.GetDuration(TimeSpan.FromMilliseconds(300))
            };
            Ta1Clone = Ta1.Clone();
            Storyboard.SetTargetProperty(Ta1, new PropertyPath("RenderTransform.ScaleX"));
            Storyboard.SetTargetProperty(Ta1Clone, new PropertyPath("RenderTransform.ScaleY"));

            FadeinSb.Children.Add(Da1);
            FadeinSb.Children.Add(Ta1);
            FadeinSb.Children.Add(Ta1Clone);

            FadeoutSb = new Storyboard { Name = "FadeoutSb" };
            Da2 = new DoubleAnimation
            {
                From = 1,
                To = 0,
                EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut },
                BeginTime = TimeSpan.Zero,
                Duration = CommonUtils.GetDuration(TimeSpan.FromMilliseconds(100))
            };
            FadeoutSb.Children.Add(Da2);
            Storyboard.SetTargetProperty(Da2, new PropertyPath(OpacityProperty));
        }

        private static readonly DoubleAnimation Da1;
        private static readonly DoubleAnimation Ta1;
        private static readonly DoubleAnimation Ta1Clone;
        private static readonly DoubleAnimation Da2;
        private static readonly Storyboard FadeoutSb;
        private static readonly Storyboard FadeinSb;
    }
}
