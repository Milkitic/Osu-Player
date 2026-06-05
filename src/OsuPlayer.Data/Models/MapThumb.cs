using System;

namespace OsuPlayer.Data.Models;

public class MapThumb
{
    public string Id { get; set; }
    public Guid MapId { get; set; }
    public string ThumbPath { get; set; }
}