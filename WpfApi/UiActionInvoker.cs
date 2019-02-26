using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Milky.WpfApi
{
    public class UiActionInvoker
    {
        private readonly SynchronizationContext _uiContext;

        public UiActionInvoker(SynchronizationContext uiContext)
        {
            _uiContext = uiContext;
        }

        public void Invoke(Action action)
        {
            action?.OnUiThread(_uiContext);
        }
    }
}
