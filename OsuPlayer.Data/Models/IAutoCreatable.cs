using System;

namespace Milki.OsuPlayer.Data.Models;

public interface IAutoCreatable
{
    DateTime CreateTime { get; set; }
}