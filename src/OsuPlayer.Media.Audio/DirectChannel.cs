using System.Collections.Generic;
using System.Threading.Tasks;
using Milki.Extensions.MixPlayer;
using Milki.Extensions.MixPlayer.Annotations;
using Milki.Extensions.MixPlayer.NAudioExtensions;
using Milki.Extensions.MixPlayer.Subchannels;

namespace Milky.OsuPlayer.Media.Audio
{
    public class DirectChannel : MultiElementsChannel
    {
        private readonly string _audioPath;
        private readonly int _delay;
        private readonly SampleControl _control;

        public DirectChannel(string audioPath, int delay, [NotNull] AudioPlaybackEngine engine, SampleControl control = null) : base(engine)
        {
            _audioPath = audioPath;
            _delay = delay;
            _control = control ?? new SampleControl();
        }

        public override Task<IEnumerable<SoundElement>> GetSoundElements()
        {
            return Task.FromResult<IEnumerable<SoundElement>>(new[]
            {
                SoundElement.Create(_delay, _control.Volume, _control.Balance, _audioPath)
            });
        }
    }
}