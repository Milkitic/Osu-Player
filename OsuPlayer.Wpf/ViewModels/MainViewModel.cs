using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using DMSkin;
using DMSkin.WPF;
using DMSkin.WPF.API;

namespace Milkitic.OsuPlayer.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public ICommand NormalWindowCommand
        {
            get
            {
                return new DelegateCommand(obj =>
                {
                    //UI线程执行
                    Execute.OnUIThread(() =>
                    {
                      
                    });
                });
            }
        }
    }
}
