using System.Drawing;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Milki.OsuPlayer.Data.Internal.Conversions;

internal sealed class PointDbConverter : ValueConverter<Point?, string?>
{
    public PointDbConverter()
        : base(v => v == default ? default : (string?)new PointConverter().ConvertTo(v, typeof(string)),
            v => v == default ? default : (Point?)new PointConverter().ConvertFrom(v))
    {
    }
}