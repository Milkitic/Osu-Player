using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using Milki.OsuPlayer.Configuration;
using Milki.OsuPlayer.Services;
using Milki.OsuPlayer.Utils;

namespace Milki.OsuPlayer.Pages.Settings;

/// <summary>
/// HotKeyPage.xaml 的交互逻辑
/// </summary>
public partial class HotKeyPage : Page
{
    //private readonly MainWindow _mainWindow;
    private bool _holdingCtrl, _holdingAlt, _holdingShift;

    private readonly Key[] _exceptKeys =
    {
        Key.System,
        Key.LeftCtrl, Key.RightCtrl,
        Key.LeftShift, Key.RightShift,
        Key.LeftAlt, Key.RightAlt
    };

    private readonly KeyHookService _keyHookService;

    public HotKeyPage()
    {
        _keyHookService = ServiceProviders.Default.GetService<KeyHookService>();
        InitializeComponent();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        GetAllHotKeyValue(FrameGrid);
    }

    private void TextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        //var type = (HotKeyType)((TextBox)sender).Tag;
        //_keyHookService.ConfigType = type;
    }

    private void TextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        //_keyHookService.ConfigType = null;
        //TextBox_KeyUp(sender, null);
    }

    private void TextBox_Keydown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.LeftCtrl:
            case Key.RightCtrl:
                _holdingCtrl = true;
                break;
            case Key.LeftShift:
            case Key.RightShift:
                _holdingShift = true;
                break;
            case Key.LeftAlt:
            case Key.RightAlt:
            case Key.System:
                _holdingAlt = true;
                break;
        }

        var textBox = (TextBox)sender;
        var strList = new List<string>();

        if (_holdingCtrl)
            strList.Add("Ctrl + ");
        if (_holdingShift)
            strList.Add("Shift + ");
        if (_holdingAlt)
            strList.Add("Alt + ");

        if (!_exceptKeys.Contains(e.Key))
        {
            strList.Add(e.Key.ConvertToString());
        }

        textBox.Text = string.Join("", strList);
    }

    private void TextBox_KeyUp(object sender, KeyEventArgs e)
    {
        if (e == null)
        {
            _holdingCtrl = false;
            _holdingShift = false;
            _holdingAlt = false;
        }
        else
        {
            switch (e.Key)
            {
                case Key.LeftCtrl:
                case Key.RightCtrl:
                    _holdingCtrl = false;
                    break;
                case Key.LeftShift:
                case Key.RightShift:
                    _holdingShift = false;
                    break;
                case Key.LeftAlt:
                case Key.RightAlt:
                    _holdingAlt = false;
                    break;
            }
        }

        var textBox = (TextBox)sender;
        GetHotKeyValue(textBox);
        AppSettings.SaveDefault();
    }

    private static void GetHotKeyValue(TextBox textBox)
    {
        //var hotKey = AppSettings.Default.HotKeys.First(k => k.Type == (HotKeyType)textBox.Tag);
        //var strList = new List<string>();

        //if (hotKey.UseControlKey)
        //    strList.Add("Ctrl");
        //if (hotKey.UseShiftKey)
        //    strList.Add("Shift");
        //if (hotKey.UseAltKey)
        //    strList.Add("Alt");
        //strList.Add(hotKey.Key.ConvertToString());

        //textBox.Text = string.Join(" + ", strList);
    }

    private static void GetAllHotKeyValue(DependencyObject obj)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
        {
            var childVisual = (Visual)VisualTreeHelper.GetChild(obj, i);
            if (childVisual is TextBox box)
                GetHotKeyValue(box);
        }
    }
}