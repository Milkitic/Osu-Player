using System;
using System.Collections.Generic;
using OsuPlayer.Data.Models;

namespace OsuPlayer.Services;

public interface IExportService
{
    bool IsTaskBusy { get; }
    event EventHandler TaskSuccess;
    void QueueEntry(Beatmap entry);
    void QueueEntries(IEnumerable<Beatmap> entries);
}
