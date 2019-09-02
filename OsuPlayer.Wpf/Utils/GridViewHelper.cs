using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace Milky.OsuPlayer.Utils
{
    public class GridViewHelper
    {
        private readonly ListView _playList;
        private RoutedEventHandler _handler;
        private Timer _dt;

        public GridViewHelper(ListView playList)
        {
            _playList = playList;
        }
        public void OnMouseDoubleClick(RoutedEventHandler handler)
        {
            _handler = handler;
            _playList.AddHandler(GridViewRowPresenter.MouseLeftButtonDownEvent,
                new RoutedEventHandler(MouseDown));
            _playList.AddHandler(GridViewRowPresenter.MouseLeftButtonUpEvent,
                new RoutedEventHandler(MouseUp));
            _playList.AddHandler(GridViewRowPresenter.MouseMoveEvent,
                new RoutedEventHandler(MouseMove));
        }

        private Timer SetNewTimer()
        {
            var newTimer = new Timer((obj) =>
            {
                _clickedOnce = false;
                _dt.Dispose();
            }, null, NativeUser32.GetDoubleClickTime(), NativeUser32.GetDoubleClickTime());
            return newTimer;
        }

        private void MouseMove(object sender, RoutedEventArgs e)
        {
            _mouseMoved = true;
        }


        private bool _mouseMoved = false;
        private bool _clickedOnce = false;
        private void MouseDown(object sender, RoutedEventArgs e)
        {
            //_mouseMoved = true;
        }

        private void MouseUp(object sender, RoutedEventArgs e)
        {
            if (_mouseMoved)
            {
                _mouseMoved = false;
                _clickedOnce = true;
                _dt = SetNewTimer();
                return;
            }

            if (!_clickedOnce)
            {
                _clickedOnce = true;
                _dt = SetNewTimer();
            }
            else
            {
                _dt.Dispose();
                _handler?.Invoke(sender, e);
                _clickedOnce = false;
            }
        }
    }
}