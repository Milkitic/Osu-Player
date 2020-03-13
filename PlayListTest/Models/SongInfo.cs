namespace PlayListTest.Models
{
    public class SongInfo
    {
        public string Title { get; set; }
        public static SongInfo Logo { get; } = new SongInfo { Title = "好音乐，在。" };

        public override string ToString()
        {
            return Title;
        }
    }
}