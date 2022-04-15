using System.Drawing;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace OsuPlayer.Data.Conversions;

public sealed class PointDbConverter : ValueConverter<Point?, string?>
{
    public PointDbConverter()
        : base(v => v == default ? default : (string?)new PointConverter().ConvertTo(v, typeof(string)),
            v => v == default ? default : (Point?)new PointConverter().ConvertFrom(v))
    {
    }
}