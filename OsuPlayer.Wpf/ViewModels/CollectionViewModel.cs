using osu_database_reader.Components.Beatmaps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Milky.OsuPlayer.Common.Data;
using Milky.OsuPlayer.Data;
using Milky.OsuPlayer.Pages;
using Milky.WpfApi;
using Milky.WpfApi.Commands;
using Collection = Milky.OsuPlayer.Common.Data.EF.Model.Collection;

namespace Milky.OsuPlayer.ViewModels
{
    public class CollectionViewModel : ViewModelBase
    {
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
                    var entry = (BeatmapEntry) obj;
                    var col = DbOperate.GetCollectionById(Id);
                    await SelectCollectionPage.AddToCollectionAsync(col, entry);
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
