using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Milky.WpfApi.Collections
{
    public sealed class NumberableObservableCollection<T> : ObservableCollection<T> where T : NumberableModel
    {
        public NumberableObservableCollection()
        {
            CollectionChanged += OnCollectionChanged;
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add ||
                e.Action == NotifyCollectionChangedAction.Replace)
            {
                for (int i = e.NewStartingIndex; i < Count; i++)
                {
                    this[i].Index = i;
                }
            }
            else
            {
                for (var i = 0; i < this.Count; i++)
                {
                    var item = this[i];
                    item.Index = i;
                }
            }
        }

        ~NumberableObservableCollection()
        {
            CollectionChanged -= OnCollectionChanged;
        }
    }
}
