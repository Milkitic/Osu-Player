using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Milky.OsuPlayer.Presentation.ObjectModel
{
    public sealed class NumberableObservableCollection<T> : ObservableCollection<T> where T : NumberableModel
    {
        public NumberableObservableCollection(IEnumerable<T> items) : this()
        {
            AddRange(items);
        }

        public NumberableObservableCollection()
        {
            CollectionChanged += OnCollectionChanged;
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add ||
                e.Action == NotifyCollectionChangedAction.Replace)
            {
                RenumberFrom(e.NewStartingIndex);
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                RenumberFrom(e.OldStartingIndex);
            }
            else
            {
                RenumberFrom(0);
            }
        }

        private void RenumberFrom(int startIndex)
        {
            if (Count == 0)
            {
                return;
            }

            if (startIndex < 0 || startIndex >= Count)
            {
                startIndex = 0;
            }

            for (var i = startIndex; i < Count; i++)
            {
                this[i].Index = i;
            }
        }

        public void AddRange(IEnumerable<T> items)
        {
            foreach (var beatmapDataModel in items)
            {
                Add(beatmapDataModel);
            }
        }

        ~NumberableObservableCollection()
        {
            CollectionChanged -= OnCollectionChanged;
        }
    }
}