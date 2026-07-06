using System;
using KeyAsio.Core.Audio;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace OsuPlayer.Media.Audio;

public static class OsuPlayerMediaAudioServiceCollectionExtensions
{
    /// <summary>
    /// Registers the audio module that <c>OsuPlayer.Media.Audio</c> ships. This is
    /// a thin replacement for <see cref="DependencyInjectionExtensions.AddAudioModule"/>
    /// that swaps the default KeyAsio <see cref="AudioEngine"/> for
    /// <see cref="OsuPlayerAudioEngine"/>, so the SDL-specific device-description
    /// rewrites live in OsuPlayer instead of inside KeyAsio.
    /// </summary>
    public static IServiceCollection AddOsuPlayerAudioModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAudioModule();
        services.Replace(ServiceDescriptor.Singleton<IPlaybackEngine, OsuPlayerAudioEngine>());
        return services;
    }
}
