using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace OsuPlayer.Data.Conversions;

public sealed class TimespanDbConverter : ValueConverter<TimeSpan?, long?>
{
    public TimespanDbConverter()
        : base(v => v == default ? default : v.Value.Ticks,
            v => v == default ? default : TimeSpan.FromTicks(v.Value))

    {
    }
}