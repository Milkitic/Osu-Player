using System.Text;
using System.Threading.Tasks;
using OsuPlayer;
using OsuPlayer.Audio;
using OsuPlayer.Core;
using OsuPlayer.Shared;
using Xunit;
using Xunit.Abstractions;

namespace UnitTests
{
    public class UnitTest1
    {
        private readonly ITestOutputHelper _output;

        public UnitTest1(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void MemoryPlayList()
        {
            var playList = new MemoryPlayList();
            playList.Mode = PlaylistMode.Random;

            var paths = new[] { "0a", "1b", "2c", "3d", "4e", "5f", "6g" };
            playList.SetPathList(paths);
            playList.AppendPaths("u", "v", "w");
            playList.GetNextPath(Direction.Next, false);

            playList.AppendPaths("x", "y", "z");

            var sb = new StringBuilder();

            for (var i = 0; i < paths.Length * 2; i++)
            {
                var next = playList.GetNextPath(Direction.Next, false);
                sb.Append(next);
                sb.Append(' ');
            }

            sb.Remove(sb.Length - 1, 1);
            _output.WriteLine(sb.ToString());

            playList.SetPointerByPath("zz", true);

            playList.RemovePaths("zz");
        }

        [Fact]
        public void StandardizePath()
        {
            var path = PathUtils.StandardizePath(@"E:/Games/osu!\Songs\BmsToOsu/IIDX\29075\P -  (bms2osu) [lv.10].osu",
                @"E:\Games/osu!\Songs\");
        }

        [Fact]
        public async Task PlayController()
        {
            var appSettings = ConfigurationFactory.GetConfiguration<AppSettings>();
            var playController = new PlayController(appSettings);
            await playController.SwitchFile(@"E:/Games/osu!\Songs\BmsToOsu/IIDX\29075\P -  (bms2osu) [lv.10].osu", false);
            await playController.SwitchFile(@"E:\”Œœ∑◊ ¡œ\osu thing\beatmap bak\Laur - Vindication\Laur - Vindication (yf_bmp) [Extra].osu", false);

            var path = PathUtils.StandardizePath(@"E:/Games/osu!\Songs\BmsToOsu/IIDX\29075\P -  (bms2osu) [lv.10].osu",
                @"E:\Games/osu!\Songs\");
        }
    }
}