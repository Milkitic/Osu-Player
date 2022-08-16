using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Milki.OsuPlayer.Data.Internal.Conversions;

internal sealed class DateTimeDbConverter : ValueConverter<DateTime?, long?>
{
    public DateTimeDbConverter()
        : base(v => v == default ? default : v.Value.Ticks,
            v => v == default ? default : new DateTime(v.Value))

    {
    }
}