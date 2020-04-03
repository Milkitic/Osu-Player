using Milky.OsuPlayer.Control;
using Milky.OsuPlayer.Data;
using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Presentation.Interaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Milky.OsuPlayer.UserControls;

namespace Milky.OsuPlayer.ViewModels
{
    public class CollectionViewModel : VmBase
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
                Locked = collection.LockedBool
            };

        public static IEnumerable<CollectionViewModel> CopyFrom(IEnumerable<Collection> collection)
            => collection.Select(CopyFrom);
    }
}
