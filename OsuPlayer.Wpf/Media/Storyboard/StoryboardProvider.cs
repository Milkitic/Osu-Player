using OSharp.Beatmap;
using OSharp.Storyboard;
using OSharp.Storyboard.Management;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using Milky.OsuPlayer.Media.Storyboard.Layer;
using Milky.OsuPlayer.Media.Storyboard.Render;

namespace Milky.OsuPlayer.Media.Storyboard
{
    public class StoryboardProvider : IDisposable
    {
        public HwndRenderBase HwndRenderBase { get; }
        public string Directory { get; set; }
        public Timing StoryboardTiming { get; } = new Timing(0, new Stopwatch());

        //public StoryboardProvider(Window window)
        //{
        //    HwndRenderBase = new HwndRenderBase(window);
        //}

        public StoryboardProvider(FrameworkElement window)
        {
            HwndRenderBase = new HwndRenderBase(window);
        }

        public void LoadStoryboard(string dir, OsuFile osu)
        {
            ClearLayer();

            List<Element> backEle = null;
            List<Element> foreEle = null;
            Stopwatch sw = new Stopwatch();

            Console.WriteLine(@"Parsing..");
            sw.Start();
            OsuFileAnalyzer analyzer = new OsuFileAnalyzer(osu);
            string osbFile = Path.Combine(dir, analyzer.OsbFileName);
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
                var osb = ElementGroup.ParseFromFile(osbFile);
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
            LoadStoryboard(osuFile.Directory.FullName, OsuFile.ReadFromFile(osuPath));
        }

        private void ClearLayer()
        {
            HwndRenderBase.RemoveAllLayers();
        }

        private static void FillLayerList(ref List<Element> backEle, ref List<Element> foreEle, ElementGroup osb)
        {
            if (osb?.ElementList?.Count <= 0) return;
            var back = osb.ElementList.Where(k => k.Layer == LayerType.Background && k.IsWorthy);
            var fore = osb.ElementList.Where(k => k.Layer == LayerType.Foreground && k.IsWorthy);
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
