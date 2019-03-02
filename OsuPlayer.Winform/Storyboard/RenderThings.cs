using System.Collections.Generic;
using Milkitic.OsbLib;
using Milkitic.OsbLib.Models;

namespace Milkitic.OsuPlayer.Storyboard
{
    public struct RenderThings
    {
        public int Index;
        public Element Elment;
        public IEnumerable<IEvent> Events;
    }
}