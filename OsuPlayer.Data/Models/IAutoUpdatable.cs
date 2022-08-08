using System;

namespace Milki.OsuPlayer.Data.Models;

public interface IAutoUpdatable
{
    DateTime UpdatedTime { get; set; }
}