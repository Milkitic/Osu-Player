namespace OsuPlayer.Iidx.Abstractions;

/// <summary>
/// IIDX radar (difficulty profile) tuple: notes / peak density / scratch /
/// soflan (BPM changes) / charge notes / chord density.
/// </summary>
public readonly struct IidxRadarData
{
    public IidxRadarData(int notes, int peak, int scratch, int soflan, int charge, int chord)
    {
        Notes = notes;
        Peak = peak;
        Scratch = scratch;
        Soflan = soflan;
        Charge = charge;
        Chord = chord;
    }

    public int Notes { get; }
    public int Peak { get; }
    public int Scratch { get; }
    public int Soflan { get; }
    public int Charge { get; }
    public int Chord { get; }
}