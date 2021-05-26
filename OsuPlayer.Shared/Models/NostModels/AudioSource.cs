using System.Collections.Generic;

namespace Milky.OsuPlayer.Shared.Models.NostModels
{
    public class AudioSourceInfo
    {
        //public AudioSource AudioSource { get; set; }
        //public float StartTime { get; set; }
        //public float Endtime { get; set; }
        //public bool InUse { get; set; }
        ///// <summary>
        ///// wont be stopped when all audio sources are in use
        ///// </summary>
        //public bool Protected { get; set; }
    }

    public static class KeysoundFilenameUtilities
    {
        private static Dictionary<int, string> _suffixDictionary;
        static KeysoundFilenameUtilities()
        {
            string sfxstr = "01_A-1 02_AS-1 03_B-1 04_C0 05_CS0 06_D0 07_DS0 08_E0 09_F0 10_FS0 11_G0 12_GS0 13_A0 " +
                            "14_AS0 15_B0 16_C1 17_CS1 18_D1 19_DS1 20_E1 21_F1 22_FS1 23_G1 24_GS1 25_A1 26_AS1 27_B1 28_C2 " +
                            "29_CS2 30_D2 31_DS2 32_E2 33_F2 34_FS2 35_G2 36_GS2 37_A2 38_AS2 39_B2 40_C3 41_CS3 42_D3 43_DS3 " +
                            "44_E3 45_F3 46_FS3 47_G3 48_GS3 49_A3 50_AS3 51_B3 52_C4 53_CS4 54_D4 55_DS4 56_E4 57_F4 58_FS4 " +
                            "59_G4 60_GS4 61_A4 62_AS4 63_B4 64_C5 65_CS5 66_D5 67_DS5 68_E5 69_F5 70_FS5 71_G5 72_GS5 73_A5 " +
                            "74_AS5 75_B5 76_C6 77_CS6 78_D6 79_DS6 80_E6 81_F6 82_FS6 83_G6 84_GS6 85_A6 86_AS6 87_B6 88_C7";
            var sfxs = sfxstr.Split(' ');
            _suffixDictionary = new Dictionary<int, string>();
            foreach (var sfx in sfxs)
            {
                _suffixDictionary.Add(int.Parse(sfx.Substring(0, 2)), sfx);
            }

        }
        //
        public static string GetFileSuffix(int scale)
        {
            return _suffixDictionary[scale];
        }
    }
}
