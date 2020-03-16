using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Milky.WpfApi;

namespace Milky.OsuPlayer.Common
{
    public class SharedVm : ViewModelBase
    {
        public bool EnableVideo { get; set; } = true;
        //public bool EnableVideo { get; set; } = true;

        private static SharedVm _default;
        private static object _defaultLock = new object();

        public static SharedVm Default
        {
            get
            {
                lock (_defaultLock)
                {
                    return _default ?? (_default = new SharedVm());
                }
            }
        }

        private SharedVm()
        {
        }
    }
}
