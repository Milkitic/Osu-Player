using Milkitic.OsbLib;
using Milkitic.OsbLib.Extension;
using Milkitic.OsuLib;
using Milkitic.OsuPlayer.Storyboard.Layer;
using Milkitic.OsuPlayer.Storyboard.Render;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Milkitic.OsuPlayer.Storyboard
{
    internal class StoryboardProvider : IDisposable
    {
        public HwndRenderBase HwndRenderBase { get; }
        public string Directory { get; set; }
        public Timing StoryboardTiming { get; } = new Timing(0, new Stopwatch());

        public StoryboardProvider(Control control)
        {
            HwndRenderBase = new HwndRenderBase(control);
        }

        public void LoadStoryboard(string dir, OsuFile osu)
        {
            ClearLayer();

            List<Element> backEle = null;
            List<Element> foreEle = null;
            Stopwatch sw = new Stopwatch();

            Console.WriteLine(@"Parsing..");
            sw.Start();
            string osbFile = Path.Combine(dir, osu.OsbFileName);
            if (osu.Events.ElementGroup != null)
            {
                var osb = osu.Events.ElementGroup;

                sw.Restart();
                osb.Expand();
                Console.WriteLine($@"Osu's osb expanded done in {sw.ElapsedMilliseconds} ms");

                FillLayerList(ref backEle, ref foreEle, osb);
            }

            if (File.Exists(osbFile))
            {
                sw.Restart();
                var osb = ElementGroup.Parse(osbFile);
                Console.WriteLine($@"Parse osb done in {sw.ElapsedMilliseconds} ms");

                sw.Restart();
                osb.Expand();
                Console.WriteLine($@"Osb expanded done in {sw.ElapsedMilliseconds} ms");

                FillLayerList(ref backEle, ref foreEle, osb);
            }

            sw.Stop();
            Directory = dir;
            if (backEle == null && foreEle == null)
                return;
            StoryboardTiming.Reset();
            if (foreEle != null) backEle?.AddRange(foreEle);
            HwndRenderBase.AddLayers(new CustomLayer[]
            {
                new StoryboardLayer(HwndRenderBase.RenderTarget, backEle ?? foreEle, StoryboardTiming),
                new FpsLayer(HwndRenderBase.RenderTarget),
            });
            StoryboardTiming.Start();
        }

        public void LoadStoryboard(string osuPath)
        {
            var osuFile = new FileInfo(osuPath);
            LoadStoryboard(osuFile.Directory.FullName, new OsuFile(osuPath));
        }

        private void ClearLayer()
        {
            HwndRenderBase.RemoveAllLayers();
        }

        private static void FillLayerList(ref List<Element> backEle, ref List<Element> foreEle, ElementGroup osb)
        {
            if (osb?.ElementList?.Count <= 0) return;
            var back = osb.ElementList.Where(k => k.Layer == Milkitic.OsbLib.Enums.LayerType.Background && k.IsValid);
            var fore = osb.ElementList.Where(k => k.Layer == Milkitic.OsbLib.Enums.LayerType.Foreground && k.IsValid);
            if (back.Any())
            {
                if (backEle == null) backEle = new List<Element>();
                backEle.AddRange(back);
            }
            if (fore.Any())
            {
                if (foreEle == null) foreEle = new List<Element>();
                foreEle.AddRange(fore);
            }
        }

        public void Dispose()
        {
            HwndRenderBase.Dispose();
        }
    }
}
