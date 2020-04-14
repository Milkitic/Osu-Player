using Milky.OsuPlayer.Data.Models;
using Milky.OsuPlayer.Presentation.Interaction;
using Milky.OsuPlayer.UserControls;
using Milky.OsuPlayer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace Milky.OsuPlayer.ViewModels
{
    public class CollectionViewModel : VmBase
    {
        private static readonly SafeDbOperator SafeDbOperator = new SafeDbOperator();

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
                    var col = SafeDbOperator.GetCollectionById(Id);
                    if (col == null) return;
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
