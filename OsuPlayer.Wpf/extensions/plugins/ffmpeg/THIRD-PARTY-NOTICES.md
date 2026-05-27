# FFmpeg Plugin Third-Party Notices

This directory contains native binaries used by Osu Player through FFME.
Keep this notice and the referenced license files with any release package
that includes the files under `extensions/plugins/ffmpeg`.

## Components

### FFmpeg

- Files:
  - `win-x64/avcodec-61.dll`
  - `win-x64/avdevice-61.dll`
  - `win-x64/avfilter-10.dll`
  - `win-x64/avformat-61.dll`
  - `win-x64/avutil-59.dll`
  - `win-x64/ffmpeg.exe`
  - `win-x64/ffplay.exe`
  - `win-x64/ffprobe.exe`
  - `win-x64/swresample-5.dll`
  - `win-x64/swscale-8.dll`
  - `win-x86/avcodec-61.dll`
  - `win-x86/avdevice-61.dll`
  - `win-x86/avfilter-10.dll`
  - `win-x86/avformat-61.dll`
  - `win-x86/avutil-59.dll`
  - `win-x86/ffmpeg.exe`
  - `win-x86/ffplay.exe`
  - `win-x86/ffprobe.exe`
  - `win-x86/swresample-5.dll`
  - `win-x86/swscale-8.dll`
- Version reported by the binaries: `n7.0.2-188-g2e503a9b94-20250711`
- Upstream project: https://ffmpeg.org/
- Source repository: https://git.ffmpeg.org/ffmpeg.git
- Git mirror: https://github.com/FFmpeg/FFmpeg
- License reported by `ffmpeg -L`: GNU Lesser General Public License,
  version 3 or later.
- Included license text: `COPYING.LGPLv3.txt`
- Local changes: none to the FFmpeg binary files.

The distributed FFmpeg build is a shared-library build and does not report
`--enable-gpl` or `--enable-nonfree` in its configuration. Osu Player loads
the FFmpeg libraries from this directory at runtime; users may replace these
DLLs with compatible FFmpeg builds for the same architecture.

### OpenH264

- Files:
  - `win-x64/openh264-2.6.0-win64.dll`
  - `win-x86/openh264-2.6.0-win32.dll`
- Version: 2.6.0
- Upstream project: https://www.openh264.org/
- Source repository: https://github.com/cisco/openh264
- Binary downloads:
  - http://ciscobinary.openh264.org/openh264-2.6.0-win64.dll.bz2
  - http://ciscobinary.openh264.org/openh264-2.6.0-win32.dll.bz2
- Included license text: `OPENH264-BINARY-LICENSE.txt`
- Local changes: none to the OpenH264 binary files.

Checksums for the included OpenH264 binaries:

| File | MD5 | SHA-256 |
| --- | --- | --- |
| `win-x64/openh264-2.6.0-win64.dll` | `CE1282F5845F56761954282E4992730D` | `2076CB5675EC6C1A4C70E7A2A322552F547B6EEED649D6DFCD9E02A543B24691` |
| `win-x86/openh264-2.6.0-win32.dll` | `95FFDB2717EDFD876D7A575AF06B472F` | `B0098DB6ACBD290A1FE13997D61D461E7327E39B42BF868DB41FAF498B7621A2` |

## FFmpeg Build Configuration

The following configuration strings are copied from `ffmpeg.exe -version`.

### win-x64

```text
--prefix=/ffbuild/prefix --pkg-config-flags=--static --pkg-config=pkg-config --cross-prefix=x86_64-w64-mingw32- --arch=x86_64 --target-os=mingw32 --enable-version3 --disable-debug --enable-shared --disable-static --disable-w32threads --enable-pthreads --enable-iconv --enable-zlib --enable-libfribidi --enable-gmp --enable-libxml2 --enable-lzma --enable-fontconfig --enable-libharfbuzz --enable-libfreetype --enable-libvorbis --enable-opencl --disable-libpulse --enable-libvmaf --disable-libxcb --disable-xlib --enable-amf --enable-libaom --enable-libaribb24 --disable-avisynth --enable-chromaprint --enable-libdav1d --disable-libdavs2 --disable-libdvdread --disable-libdvdnav --disable-libfdk-aac --enable-ffnvcodec --enable-cuda-llvm --disable-frei0r --enable-libgme --enable-libkvazaar --enable-libaribcaption --enable-libass --enable-libbluray --enable-libjxl --enable-libmp3lame --enable-libopus --enable-librist --enable-libssh --enable-libtheora --enable-libvpx --enable-libwebp --enable-libzmq --enable-lv2 --enable-libvpl --enable-openal --enable-libopencore-amrnb --enable-libopencore-amrwb --enable-libopenh264 --enable-libopenjpeg --enable-libopenmpt --enable-librav1e --disable-librubberband --enable-schannel --enable-sdl2 --enable-libsnappy --enable-libsoxr --enable-libsrt --disable-libsvtav1 --enable-libtwolame --enable-libuavs3d --disable-libdrm --enable-vaapi --disable-libvidstab --enable-vulkan --enable-libshaderc --enable-libplacebo --disable-libx264 --disable-libx265 --disable-libxavs2 --disable-libxvid --enable-libzimg --enable-libzvbi --extra-cflags='-DLIBTWOLAME_STATIC -Wno-int-conversion' --extra-cxxflags= --extra-libs=-lgomp --extra-ldflags=-pthread --extra-ldexeflags= --cc=x86_64-w64-mingw32-gcc --cxx=x86_64-w64-mingw32-g++ --ar=x86_64-w64-mingw32-gcc-ar --ranlib=x86_64-w64-mingw32-gcc-ranlib --nm=x86_64-w64-mingw32-gcc-nm --extra-version=20250711
```

### win-x86

```text
--prefix=/ffbuild/prefix --pkg-config-flags=--static --pkg-config=pkg-config --cross-prefix=i686-w64-mingw32- --arch=i686 --target-os=mingw32 --enable-version3 --disable-debug --enable-shared --disable-static --disable-w32threads --enable-pthreads --enable-iconv --enable-zlib --enable-libfribidi --enable-gmp --enable-libxml2 --enable-lzma --enable-fontconfig --enable-libharfbuzz --enable-libfreetype --enable-libvorbis --enable-opencl --disable-libpulse --enable-libvmaf --disable-libxcb --disable-xlib --enable-amf --enable-libaom --enable-libaribb24 --disable-avisynth --enable-chromaprint --enable-libdav1d --disable-libdavs2 --disable-libdvdread --disable-libdvdnav --disable-libfdk-aac --enable-ffnvcodec --enable-cuda-llvm --disable-frei0r --enable-libgme --enable-libkvazaar --enable-libaribcaption --enable-libass --enable-libbluray --enable-libjxl --enable-libmp3lame --enable-libopus --enable-librist --enable-libssh --enable-libtheora --enable-libvpx --enable-libwebp --enable-libzmq --enable-lv2 --enable-libvpl --enable-openal --enable-libopencore-amrnb --enable-libopencore-amrwb --enable-libopenh264 --enable-libopenjpeg --enable-libopenmpt --disable-librav1e --disable-librubberband --enable-schannel --enable-sdl2 --enable-libsnappy --enable-libsoxr --enable-libsrt --disable-libsvtav1 --enable-libtwolame --disable-libuavs3d --disable-libdrm --enable-vaapi --disable-libvidstab --enable-vulkan --enable-libshaderc --enable-libplacebo --disable-libx264 --disable-libx265 --disable-libxavs2 --disable-libxvid --enable-libzimg --enable-libzvbi --extra-cflags='-DLIBTWOLAME_STATIC -Wno-int-conversion' --extra-cxxflags= --extra-libs=-lgomp --extra-ldflags=-pthread --extra-ldexeflags= --cc=i686-w64-mingw32-gcc --cxx=i686-w64-mingw32-g++ --ar=i686-w64-mingw32-gcc-ar --ranlib=i686-w64-mingw32-gcc-ranlib --nm=i686-w64-mingw32-gcc-nm --extra-version=20250711
```

## Release Checklist

When publishing a package that includes this directory:

1. Keep this file, `COPYING.LGPLv3.txt`, `COPYING.GPLv3.txt`, and
   `OPENH264-BINARY-LICENSE.txt` with the FFmpeg binaries.
2. Do not remove or rename the FFmpeg DLLs in a way that prevents users from
   replacing them with compatible builds.
3. Publish or link to the corresponding FFmpeg source and build information.
   At minimum, include the FFmpeg version string and configuration above.
4. Keep the OpenH264 binary license with the OpenH264 DLLs.
5. Re-run `ffmpeg.exe -L` and update this notice if the FFmpeg binaries are
   replaced.
