using System;
using System.Collections.Generic;
using Milky.OsuPlayer.Data.Models;

namespace Milky.OsuPlayer.Services
{
    public interface IExportService
    {
        bool IsTaskBusy { get; }
        event EventHandler TaskSuccess;
        void QueueEntry(Beatmap entry);
        void QueueEntries(IEnumerable<Beatmap> entries);
    }
}
