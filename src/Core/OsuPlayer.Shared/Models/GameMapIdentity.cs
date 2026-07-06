using System;
using OsuPlayer.Shared;

namespace OsuPlayer.Shared.Models;

/// <summary>
/// Identifies a beatmap/music entry across platforms. Extends <see cref="MapIdentity"/>
/// with a <see cref="SourceGame"/> discriminator so osu! and IIDX entries never collide.
/// </summary>
public readonly struct GameMapIdentity : IMapIdentifiable, IEquatable<GameMapIdentity>
{
    public GameMapIdentity(string folderName, string version, bool inOwnDb, SourceGame sourceGame)
    {
        FolderName = folderName;
        Version = version;
        InOwnDb = inOwnDb;
        SourceGame = sourceGame;
    }

    public string FolderName { get; }
    public string Version { get; }
    public bool InOwnDb { get; }
    public SourceGame SourceGame { get; }

    public MapIdentity GetIdentity() => new(FolderName, Version, InOwnDb);

    public GameMapIdentity WithSource(SourceGame source) => new(FolderName, Version, InOwnDb, source);

    public bool Equals(GameMapIdentity other) =>
        FolderName == other.FolderName
        && Version == other.Version
        && InOwnDb == other.InOwnDb
        && SourceGame == other.SourceGame;

    public override bool Equals(object? obj) => obj is GameMapIdentity other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(FolderName, Version, InOwnDb, SourceGame);

    public override string ToString() =>
        $"{SourceGame}: [\"{FolderName}\",\"{Version}\"]{(InOwnDb ? " (local)" : "")}";

    public static GameMapIdentity FromOsu(IMapIdentifiable map) =>
        new(map.FolderName, map.Version, map.InOwnDb, SourceGame.Osu);
}