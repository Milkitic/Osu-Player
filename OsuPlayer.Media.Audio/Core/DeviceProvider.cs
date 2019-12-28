using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Milky.OsuPlayer.Common.Configuration;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wave;
using OsuPlayer.Devices;

namespace Milky.OsuPlayer.Media.Audio.Core
{
    public static class DeviceProvider
    {
        private static readonly MMDeviceEnumerator MMDeviceEnumerator;
        private static readonly MMNotificationClient MmNotificationClient;
        private static IWavePlayer _currentDevice;

        private class MMNotificationClient : IMMNotificationClient
        {
            public MMNotificationClient()
            {
                //_realEnumerator.RegisterEndpointNotificationCallback();
                if (System.Environment.OSVersion.Version.Major < 6)
                {
                    throw new NotSupportedException("This functionality is only supported on Windows Vista or newer.");
                }
            }

            public void OnDeviceStateChanged(string deviceId, DeviceState newState)
            {
                CacheList = null;
                Console.WriteLine("OnDeviceStateChanged\n Device Id -->{0} : Device State {1}", deviceId, newState);
            }

            public void OnDeviceAdded(string pwstrDeviceId)
            {
                CacheList = null;
                Console.WriteLine("OnDeviceAdded --> " + pwstrDeviceId);
            }

            public void OnDeviceRemoved(string deviceId)
            {
                CacheList = null;
                Console.WriteLine("OnDeviceRemoved --> " + deviceId);
            }

            public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
            {
                CacheList = null;
                Console.WriteLine("OnDefaultDeviceChanged --> {0}", flow.ToString());
            }

            public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
            {
                //fmtid & pid are changed to formatId and propertyId in the latest version NAudio
                Console.WriteLine("OnPropertyValueChanged: formatId --> {0}  propertyId --> {1}", key.formatId.ToString(), key.propertyId.ToString());
            }
        }

        static DeviceProvider()
        {
            MMDeviceEnumerator = new MMDeviceEnumerator();
            MmNotificationClient = new MMNotificationClient();
            MMDeviceEnumerator.RegisterEndpointNotificationCallback(MmNotificationClient);
        }

        private static List<IDeviceInfo> CacheList { get; set; }

        public static IWavePlayer CreateOrGetDefaultDevice()
        {
            var play = AppSettings.Default.Play;
            return CreateDevice(play.DeviceInfo, play.DesiredLatency, play.IsExclusive);
        }

        public static IWavePlayer GetCurrentDevice()
        {
            return _currentDevice;
        }

        public static IWavePlayer CreateDevice(IDeviceInfo deviceInfo = null, int latency = 1, bool isExclusive = true)
        {
            //if (_currentDevice != null)
            //{
            //    return _currentDevice;
            //}

            var apartmentState = Thread.CurrentThread.GetApartmentState();
            if (apartmentState != ApartmentState.STA)
            {
                throw new Exception($"Need {ApartmentState.STA}, but actual {apartmentState}.");
            }

            if (deviceInfo is null)
            {
                Console.WriteLine("Device is null, use wasapi default.");
                deviceInfo = WasapiInfo.Default;
            }

            if (CacheList == null) EnumerateAvailableDevices().ToList();
            //if (CacheList == null)
            //{
            //    CacheList = EnumerateAvailableDevices().ToList();
            //}

            IWavePlayer device = null;
            if (!CacheList.Contains(deviceInfo))
            {
                if (deviceInfo is WasapiInfo wasapiInfo)
                {
                    var foundResult = CacheList.Where(k => k.OutputMethod == OutputMethod.Wasapi).Cast<WasapiInfo>()
                        .FirstOrDefault(k => k.DeviceId == wasapiInfo.DeviceId);
                    if (foundResult?.Device != null)
                    {
                        wasapiInfo.Device = foundResult.Device;
                    }
                    else
                    {
                        Console.WriteLine("Device not found, use wasapi default.");
                        device = new WasapiOut(AudioClientShareMode.Shared, 1);
                    }
                }
                else
                {
                    Console.WriteLine("Device not found, use wasapi default.");
                    device = new WasapiOut(AudioClientShareMode.Shared, 1);
                }
            }

            if (device is null)
            {
                switch (deviceInfo.OutputMethod)
                {
                    case OutputMethod.WaveOut:
                        var waveOut = (WaveOutInfo)deviceInfo;
                        device = new WaveOutEvent
                        {
                            DeviceNumber = waveOut.DeviceNumber,
                            DesiredLatency = latency
                        };
                        break;
                    case OutputMethod.DirectSound:
                        var dsOut = (DirectSoundOutInfo)deviceInfo;
                        device = new DirectSoundOut(dsOut.DeviceGuid, latency);
                        break;
                    case OutputMethod.Wasapi:
                        var wasapi = (WasapiInfo)deviceInfo;
                        if (wasapi.Equals(WasapiInfo.Default))
                        {
                            device = new WasapiOut(AudioClientShareMode.Shared, 1);
                        }
                        else
                        {
                            device = new WasapiOut(wasapi.Device,
                                isExclusive ? AudioClientShareMode.Exclusive : AudioClientShareMode.Shared, true,
                                latency);
                        }
                        break;
                    case OutputMethod.Asio:
                        var asio = (AsioOutInfo)deviceInfo;
                        device = new AsioOut(asio.FriendlyName);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            _currentDevice = device;
            return device;
        }

        public static IEnumerable<IDeviceInfo> EnumerateAvailableDevices()
        {
            if (CacheList != null)
            {
                foreach (var deviceInfo in CacheList)
                {
                    yield return deviceInfo;
                }

                yield break;
            }

            CacheList = new List<IDeviceInfo> { WasapiInfo.Default };
            yield return WasapiInfo.Default;

            for (int n = -1; n < WaveOut.DeviceCount; n++)
            {
                WaveOutInfo info = null;
                try
                {
                    var caps = WaveOut.GetCapabilities(n);
                    info = new WaveOutInfo(caps.ProductName, n);
                }
                catch (Exception ex)
                {
                    // ignored
                }

                if (info != null)
                {
                    CacheList.Add(info);
                    yield return info;
                }
            }

            foreach (var dev in DirectSoundOut.Devices)
            {
                DirectSoundOutInfo info = null;
                try
                {
                    info = new DirectSoundOutInfo(dev.Description, dev.Guid);
                }
                catch (Exception ex)
                {
                    // ignored
                }

                if (info != null)
                {
                    CacheList.Add(info);
                    yield return info;
                }
            }

            foreach (var wasapi in MMDeviceEnumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.All))
            {
                WasapiInfo info = null;
                try
                {
                    if (wasapi.DataFlow != DataFlow.Render || wasapi.State != DeviceState.Active) continue;
                    info = new WasapiInfo(wasapi.FriendlyName, wasapi.ID)
                    {
                        Device = wasapi
                    };
                }
                catch (Exception ex)
                {
                    // ignored
                }

                if (info != null)
                {
                    CacheList.Add(info);
                    yield return info;
                }
            }

            foreach (var asio in AsioOut.GetDriverNames())
            {
                AsioOutInfo info = null;
                try
                {
                    info = new AsioOutInfo(asio);
                }
                catch (Exception ex)
                {
                    // ignored
                }

                if (info != null)
                {
                    CacheList.Add(info);
                    yield return info;
                }
            }
        }
    }
}
