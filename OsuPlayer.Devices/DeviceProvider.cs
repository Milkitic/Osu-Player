using Milky.OsuPlayer.Presentation.Interaction;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OsuPlayer.Devices
{
    public static class DeviceProvider
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly MMDeviceEnumerator MMDeviceEnumerator;
        private static readonly MMNotificationClient MmNotificationClient;
        private static IWavePlayer _currentDevice;

        private class MMNotificationClient : IMMNotificationClient
        {
            private static readonly NLog.Logger InnerLogger = NLog.LogManager.GetCurrentClassLogger();
            public MMNotificationClient()
            {
                //_realEnumerator.RegisterEndpointNotificationCallback();
                if (Environment.OSVersion.Version.Major < 6)
                {
                    throw new NotSupportedException("This functionality is only supported on Windows Vista or newer.");
                }
            }

            public void OnDeviceStateChanged(string deviceId, DeviceState newState)
            {
                CacheList = null;
                InnerLogger.Debug("OnDeviceStateChanged\n Device Id -->{0} : Device State {1}", deviceId, newState);
            }

            public void OnDeviceAdded(string pwstrDeviceId)
            {
                CacheList = null;
                InnerLogger.Debug("OnDeviceAdded --> " + pwstrDeviceId);
            }

            public void OnDeviceRemoved(string deviceId)
            {
                CacheList = null;
                InnerLogger.Debug("OnDeviceRemoved --> " + deviceId);
            }

            public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
            {
                CacheList = null;
                InnerLogger.Info("OnDefaultDeviceChanged --> {0}", flow.ToString());
            }

            public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
            {
                //fmtid & pid are changed to formatId and propertyId in the latest version NAudio
                InnerLogger.Debug("OnPropertyValueChanged: formatId --> {0}  propertyId --> {1}", key.formatId.ToString(), key.propertyId.ToString());
            }
        }

        static DeviceProvider()
        {
            MMDeviceEnumerator = new MMDeviceEnumerator();
            MmNotificationClient = new MMNotificationClient();
            MMDeviceEnumerator.RegisterEndpointNotificationCallback(MmNotificationClient);
        }

        private static List<IDeviceInfo> CacheList { get; set; }

        public static IWavePlayer GetCurrentDevice()
        {
            return _currentDevice;
        }

        public static IWavePlayer CreateDevice(out IDeviceInfo actualDeviceInfo, IDeviceInfo deviceInfo = null, int latency = 1, bool isExclusive = true)
        {
            //if (_currentDevice != null)
            //{
            //    return _currentDevice;
            //}

            bool useDefault = false;
            if (deviceInfo is null)
            {
                deviceInfo = GetDefaultDeviceInfo();
                useDefault = true;
            }

            if (CacheList == null) EnumerateAvailableDevices().ToList();
            //if (CacheList == null)
            //{
            //    CacheList = EnumerateAvailableDevices().ToList();
            //}

            IWavePlayer device = null;
            if (!useDefault && !CacheList.Contains(deviceInfo))
            {
                if (deviceInfo is WasapiInfo wasapiInfo)
                {
                    var foundResult = CacheList
                        .Where(k => k.OutputMethod == OutputMethod.Wasapi)
                        .Cast<WasapiInfo>()
                        .FirstOrDefault(k => k.DeviceId == wasapiInfo.DeviceId);
                    if (foundResult?.Device != null)
                    {
                        wasapiInfo.Device = foundResult.Device;
                    }
                    else
                    {
                        deviceInfo = GetDefaultDeviceInfo();
                    }
                }
                else
                {
                    deviceInfo = GetDefaultDeviceInfo();
                }
            }
            Execute.OnUiThread(() =>
            {
                try
                {
                    switch (deviceInfo.OutputMethod)
                    {
                        case OutputMethod.WaveOut:
                            var waveOut = (WaveOutInfo)deviceInfo;

                            device = new WaveOutEvent
                            {
                                DeviceNumber = waveOut.DeviceNumber,
                                DesiredLatency = Math.Max(latency, 100)
                            };

                            break;
                        case OutputMethod.DirectSound:
                            var dsOut = (DirectSoundOutInfo)deviceInfo;
                            if (dsOut.Equals(DirectSoundOutInfo.Default))
                            {
                                device = new DirectSoundOut(40);
                            }
                            else
                            {
                                device = new DirectSoundOut(dsOut.DeviceGuid, Math.Max(latency, 40));
                            }
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
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error on create device.");
                    device?.Dispose();
                    deviceInfo = DirectSoundOutInfo.Default;
                    device = new DirectSoundOut(40);
                }
            });

            _currentDevice = device;
            actualDeviceInfo = deviceInfo;
            return device;
        }

        private static IDeviceInfo GetDefaultDeviceInfo()
        {
            IDeviceInfo deviceInfo;
            if (MMDeviceEnumerator.HasDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia))
            {
                deviceInfo = WasapiInfo.Default;
                Logger.Info("The output device in app's config was not detected in this system, use WASAPI default.");
            }
            else
            {
                deviceInfo = DirectSoundOutInfo.Default;
                Logger.Warn("The output device in app's config was not detected " +
                            "or no output device detected in this system, use DirectSoundOut default!!!");
            }

            return deviceInfo;
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
