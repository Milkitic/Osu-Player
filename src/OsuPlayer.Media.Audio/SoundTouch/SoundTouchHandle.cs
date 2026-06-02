using Microsoft.Win32.SafeHandles;

namespace Milky.OsuPlayer.Media.Audio.SoundTouch;

internal sealed class SoundTouchHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    private SoundTouchHandle()
        : base(ownsHandle: true)
    {
    }

    protected override bool ReleaseHandle()
    {
        SoundTouchNativeMethods.DestroyInstance(handle);
        return true;
    }
}
