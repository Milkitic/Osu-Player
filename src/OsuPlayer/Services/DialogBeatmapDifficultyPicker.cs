#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;
using OsuPlayer.Data.Models;
using OsuPlayer.UiComponents.FrontDialogComponent;
using OsuPlayer.UserControls;

namespace OsuPlayer.Services;

public sealed class DialogBeatmapDifficultyPicker : IBeatmapDifficultyPicker
{
    public Task<Beatmap?> PickAsync(IReadOnlyList<Beatmap> beatmaps)
    {
        if (beatmaps.Count == 0)
        {
            return Task.FromResult<Beatmap?>(null);
        }

        var completion = new TaskCompletionSource<Beatmap?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var control = new DiffSelectControl(beatmaps);

        control.BeatmapSelected += (_, beatmap) =>
        {
            completion.TrySetResult(beatmap);
            FrontDialogOverlay.Default.RaiseOk();
        };

        FrontDialogOverlay.Default.ShowContent(
            control,
            DialogOptionFactory.DiffSelectOptions,
            cancelAction: (_, _) => completion.TrySetResult(null));

        return completion.Task;
    }
}
