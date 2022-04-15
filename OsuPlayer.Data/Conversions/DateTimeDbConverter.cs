using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace OsuPlayer.Data.Conversions;

public sealed class DateTimeDbConverter : ValueConverter<DateTime?, long?>
{
    public DateTimeDbConverter()
        : base(v => v == default ? default : v.Value.Ticks,
            v => v == default ? default : new DateTime(v.Value))

    {
    }
}