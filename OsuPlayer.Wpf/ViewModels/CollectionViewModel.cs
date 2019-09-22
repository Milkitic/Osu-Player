using Milky.OsuPlayer.Common.Data;
using Milky.OsuPlayer.Common.Data.EF.Model;
using Milky.OsuPlayer.Pages;
using Milky.WpfApi;
using Milky.WpfApi.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Milky.OsuPlayer.Control;
using Collection = Milky.OsuPlayer.Common.Data.EF.Model.V1.Collection;

namespace Milky.OsuPlayer.ViewModels
{
    public class CollectionViewModel : ViewModelBase
    {
        private AppDbOperator _appDbOperator = new AppDbOperator();

        public string Id { get; set; }
        public string Name { get; set; }
        public int Index { get; set; }
        public string ImagePath { get; set; }
        public string Description { get; set; }
        public DateTime CreateTime { get; set; }
        public bool Locked { get; set; }

        public ICommand SelectCommand
        {
            get
            {
                return new DelegateCommand(async obj =>
                {
                    var entries = (IList<Beatmap>)obj;
                    var col = _appDbOperator.GetCollectionById(Id);
                    await SelectCollectionControl.AddToCollectionAsync(col, entries);
                });
            }
        }

        public static CollectionViewModel CopyFrom(Collection collection)
            => new CollectionViewModel
            {
                Id = collection.Id,
                Name = collection.Name,
                Index = collection.Index,
                ImagePath = collection.ImagePath,
                Description = collection.Description,
                CreateTime = collection.CreateTime,
                Locked = collection.Locked
            };

        public static IEnumerable<CollectionViewModel> CopyFrom(IEnumerable<Collection> collection)
            => collection.Select(CopyFrom);
    }
}
