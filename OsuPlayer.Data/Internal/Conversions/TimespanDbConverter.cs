using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Milki.OsuPlayer.Data.Internal.Conversions;

internal sealed class TimespanDbConverter : ValueConverter<TimeSpan?, long?>
{
    public TimespanDbConverter()
        : base(v => v == default ? default : v.Value.Ticks,
            v => v == default ? default : TimeSpan.FromTicks(v.Value))

    {
    }
}