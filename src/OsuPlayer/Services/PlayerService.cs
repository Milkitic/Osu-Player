#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Milki.OsuPlayer.Audio;

namespace Milki.OsuPlayer.Services;

public class PlayerService
{
    private readonly PlayListService _playListService;

    public PlayerService(PlayListService playListService)
    {
        _playListService = playListService;
    }

    public OsuMixPlayer? ActiveMixPlayer { get; private set; }


}