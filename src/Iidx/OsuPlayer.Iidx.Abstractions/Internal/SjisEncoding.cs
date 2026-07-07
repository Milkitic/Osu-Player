using System.Text;

namespace OsuPlayer.Iidx.Abstractions.Internal;

/// <summary>
/// Cached Shift-JIS (codepage 932) encoding. IIDX <c>music_data.bin</c>
/// stores romanized titles / BGA filenames / layers flags in Shift-JIS.
/// </summary>
internal static class SjisEncoding
{
    public static Encoding Instance { get; } = CreateInstance();

    private static Encoding CreateInstance()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(932);
    }
}
