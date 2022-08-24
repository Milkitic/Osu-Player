using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Milki.OsuPlayer.Wpf;

namespace Milki.OsuPlayer.UiComponents;

public enum NavigateAnimation
{
    Regular, ScrollLeft, ScrollRight, ScrollUp, ScrollDown
}

public class AnimatedFrameGrid : Grid
{
    internal readonly Border Border;

    public AnimatedFrameGrid()
    {
        Border = new Border();
        Children.Add(Border);
    }
}

public partial class AnimatedFrame : FrameEx
{
    private readonly ScaleTransform _scaleTransformInstance = new();
    private readonly TranslateTransform _translateTransformInstance = new();

    private readonly Storyboard _fadeInStoryboard;
    private readonly Storyboard _fadeOutStoryboard;

    private readonly DoubleAnimation _fadeInScaleX;
    private readonly DoubleAnimation _fadeInScaleY;
    private readonly DoubleAnimation _fadeInTranslateY;

    public Type MainPageType { get; set; }

    public AnimatedFrame(Type mainPageType) : this()
    {
        MainPageType = mainPageType;
    }

    public AnimatedFrame()
    {
        InitializeComponent();
        _fadeInStoryboard = (Storyboard)FindResource("FadeInStoryboard");

        _fadeInScaleX = (DoubleAnimation)_fadeInStoryboard.Children[1];
        _fadeInScaleY = (DoubleAnimation)_fadeInStoryboard.Children[2];
        _fadeInTranslateY = (DoubleAnimation)_fadeInStoryboard.Children[3];

        _fadeOutStoryboard = (Storyboard)FindResource("FadeOutStoryboard");

        Navigated += AnimatedFrame_Navigated;
    }

    public void AnimateNavigate(object content, NavigateAnimation navigateAnimation =
        NavigateAnimation.Regular)
    {
        if (content == Content) return;
        if (Content != null)
        {
            if (Content is ContentPresenter cp)
            {
                if (content == cp.Content) return;
            }

            Storyboard.SetTarget(_fadeOutStoryboard, Content as DependencyObject);
            _fadeOutStoryboard.Completed += OnSbOnCompleted;
            _fadeOutStoryboard.Begin();

            void OnSbOnCompleted(object obj, EventArgs args)
            {
                if (content != null)
                    InnerAnimateNavigate(content);
                _fadeOutStoryboard.Completed -= OnSbOnCompleted;
            }
        }
        else
        {
            InnerAnimateNavigate(content);
        }
    }

    public void AnimateNavigateBack()
    {
        var ui = (UIElement?)Content;
        if (ui != null)
        {
            _fadeOutStoryboard.Completed += OnSbOnCompleted;
            _fadeOutStoryboard.Begin();

            void OnSbOnCompleted(object obj, EventArgs args)
            {
                InnerAnimateNavigateBack();
                _fadeOutStoryboard.Completed -= OnSbOnCompleted;
            }
        }
        else
        {
            InnerAnimateNavigateBack();
        }
    }

    private void AnimatedFrame_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
    {
        EnsureContentTransform(Content);

        _fadeInStoryboard.Completed += OnSbOnCompleted;
        _fadeInStoryboard.Begin();

        void OnSbOnCompleted(object obj, EventArgs args)
        {
            _fadeInStoryboard.Completed -= OnSbOnCompleted;
        }

        if (Content?.GetType() == MainPageType)
        {
            ClearHistory();
        }
    }

    private void InnerAnimateNavigate(object uiElement)
    {
        NavigationService.Navigate(uiElement);
    }

    private void InnerAnimateNavigateBack()
    {
        NavigationService.GoBack();
    }

    private void ClearHistory()
    {
        if (!CanGoBack && !CanGoForward)
        {
            return;
        }

        var entry = RemoveBackEntry();
        while (entry != null)
        {
            Console.WriteLine("Removed " + entry.Name);
            entry = RemoveBackEntry();
        }
    }

    private void EnsureContentTransform(object content)
    {
        if (content is not UIElement uiElement)
        {
            uiElement = new ContentPresenter { Content = content };
            Content = uiElement;
        }

        Storyboard.SetTarget(_fadeInStoryboard, uiElement);

        uiElement.RenderTransformOrigin = new Point(0.5, 0.5);
        if (uiElement.RenderTransform is TransformGroup group)
        {
            if (!group.Children.Contains(_scaleTransformInstance))
            {
                group.Children.Add(_scaleTransformInstance);
                BindPropertyScalePath(group.Children.Count - 1);
            }

            if (!group.Children.Contains(_translateTransformInstance))
            {
                group.Children.Add(_translateTransformInstance);
                BindPropertyTranslatePath(group.Children.Count - 1);
            }
        }
        else
        {
            var transformGroup = new TransformGroup
            {
                Children = { _scaleTransformInstance, _translateTransformInstance }
            };
            if (uiElement.RenderTransform != null && uiElement.RenderTransform != Transform.Identity)
            {
                transformGroup.Children.Insert(0, uiElement.RenderTransform);
            }

            uiElement.RenderTransform = transformGroup;

            BindPropertyScalePath(transformGroup.Children.Count - 2);
            BindPropertyTranslatePath(transformGroup.Children.Count - 1);
        }
    }

    private void BindPropertyScalePath(int count)
    {
        Storyboard.SetTargetProperty(_fadeInScaleX,
            new PropertyPath($"RenderTransform.Children[{count}].ScaleX"));
        Storyboard.SetTargetProperty(_fadeInScaleY,
            new PropertyPath($"RenderTransform.Children[{count}].ScaleY"));
    }

    private void BindPropertyTranslatePath(int count)
    {
        Storyboard.SetTargetProperty(_fadeInTranslateY,
            new PropertyPath($"RenderTransform.Children[{count}].Y"));
    }
}