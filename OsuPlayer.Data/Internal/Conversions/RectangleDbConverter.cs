using System.Drawing;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Milki.OsuPlayer.Data.Internal.Conversions;

internal sealed class RectangleDbConverter : ValueConverter<Rectangle?, string?>
{
    public RectangleDbConverter()
        : base(v => v == default ? default : (string?)new RectangleConverter().ConvertTo(v, typeof(string)),
            v => v == default ? default : (Rectangle?)new RectangleConverter().ConvertFrom(v))
    {
    }
}