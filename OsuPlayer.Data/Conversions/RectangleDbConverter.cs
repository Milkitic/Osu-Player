using System.Drawing;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace OsuPlayer.Data.Conversions;

public sealed class RectangleDbConverter : ValueConverter<Rectangle?, string?>
{
    public RectangleDbConverter()
        : base(v => v == default ? default : (string?)new RectangleConverter().ConvertTo(v, typeof(string)),
            v => v == default ? default : (Rectangle?)new RectangleConverter().ConvertFrom(v))
    {
    }
}