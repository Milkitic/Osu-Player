namespace OsuPlayer.Shared.Models;

public sealed class WindowPoint
{
    public WindowPoint()
    {
    }

    public WindowPoint(double x, double y)
    {
        X = x;
        Y = y;
    }

    public double X { get; set; }

    public double Y { get; set; }
}
