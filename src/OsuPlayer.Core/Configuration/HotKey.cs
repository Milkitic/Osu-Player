using System;
using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;
using Milki.Extensions.MouseKeyHook;

namespace OsuPlayer.Core.Configuration;

[JsonConverter(typeof(HotKeyConverter))]
// 01 01 00000025
// type_control+enable_key
public class HotKey
{
    public HotKeyType Type { get; set; }
    public HookKeys Key { get; set; }
    public bool Enabled { get; set; }
    public bool UseControlKey { get; set; }
    public bool UseAltKey { get; set; }
    public bool UseShiftKey { get; set; }
    [JsonIgnore]
    public Action Callback { get; set; }
}

public class HotKeyConverter : JsonConverter<HotKey>
{
    public override void Write(Utf8JsonWriter writer, HotKey value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        var bytes = new byte[8];
        bytes[0] = BitConverter.GetBytes(GetBit(value.Enabled) +
                                         (GetBit(value.UseControlKey) << 1) +
                                         (GetBit(value.UseAltKey) << 2) +
                                         (GetBit(value.UseShiftKey) << 3)
        )[0];
        bytes[1] = (byte)value.Type;
        var bytes1 = BitConverter.GetBytes((int)value.Key);
        bytes[2] = bytes1[0];
        bytes[3] = bytes1[1];
        bytes[4] = bytes1[2];
        bytes[5] = bytes1[3];
        var int64 = BitConverter.ToInt64(bytes, 0);
        writer.WriteNumberValue(int64);
    }

    private static int GetBit(bool value)
    {
        return value ? 1 : 0;
    }

    public override HotKey Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        var hotKey = new HotKey();
        var value = reader.TokenType == JsonTokenType.String
            ? Convert.ToInt64(reader.GetString())
            : reader.GetInt64();
        var bytes = BitConverter.GetBytes(value);
        var arr = new BitArray(new[] { bytes[0] });
        hotKey.Enabled = arr[0];
        hotKey.UseControlKey = arr[1];
        hotKey.UseAltKey = arr[2];
        hotKey.UseShiftKey = arr[3];
        hotKey.Type = (HotKeyType)bytes[1];
        var int32 = BitConverter.ToInt32(bytes, 2);
        hotKey.Key = (HookKeys)int32;
        return hotKey;
    }
}

public enum HotKeyType : byte
{
    TogglePlay, PrevSong, NextSong, VolumeUp, VolumeDown, SwitchFullMiniMode, AddCurrentToFav, SwitchLyricWindow
}
