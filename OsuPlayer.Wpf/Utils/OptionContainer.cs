using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Milkitic.OsuPlayer.Utils
{
    public class OptionContainer
    {
        private readonly List<object> _optionControls = new List<object>();

        public void Add(object optionControl)
        {
            var type = optionControl.GetType();
            PropertyInfo prop = type.GetProperties().FirstOrDefault(k => k.Name == "IsChecked");
            if (prop != null && (prop.PropertyType == typeof(bool?) || prop.PropertyType == typeof(bool)))
                _optionControls.Add(optionControl);
        }

        public void Add(params object[] optionControls)
        {
            foreach (var control in optionControls)
            {
                Add(control);
            }
        }

        public void Switch(dynamic optionControl)
        {
            if (_optionControls.FirstOrDefault(k => k == optionControl) == null)
            {
                Add(optionControl);
            }

            IEnumerable<dynamic> items = _optionControls.Where(k => k != optionControl);
            foreach (var controls in items)
            {
                controls.IsChecked = false;
            }
            dynamic control = _optionControls.FirstOrDefault(k => k == optionControl);
            if (control != null)
                control.IsChecked = true;
        }
    }
}
