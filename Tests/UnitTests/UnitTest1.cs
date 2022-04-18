using System.Text;
using OsuPlayer.Audio;
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
        public void Test1()
        {
            var playList = new PlayList();
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
    }
}