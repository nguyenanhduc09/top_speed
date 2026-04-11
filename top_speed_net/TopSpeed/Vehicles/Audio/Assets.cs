using System;
using System.Collections.Generic;
using System.IO;
using TopSpeed.Common;
using TS.Audio;

namespace TopSpeed.Vehicles
{
    internal partial class Car
    {
        private Source CreateRequiredSound(string? path, bool looped = false, bool spatialize = true, bool allowHrtf = true)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException("Sound path not provided.");
            if (!File.Exists(path))
                throw new FileNotFoundException("Sound file not found.", path);
            return CreateVehicleSound(path!, looped, spatialize, allowHrtf);
        }

        private Source[] CreateRequiredSoundVariants(IReadOnlyList<string>? paths, string? fallbackSinglePath)
        {
            if (paths != null && paths.Count > 0)
            {
                var result = new Source[paths.Count];
                for (var i = 0; i < paths.Count; i++)
                    result[i] = CreateRequiredSound(paths[i]);
                return result;
            }

            return new[] { CreateRequiredSound(fallbackSinglePath) };
        }

        private Source[] CreateOptionalSoundVariants(IReadOnlyList<string>? paths, string? fallbackSinglePath)
        {
            if (paths != null && paths.Count > 0)
            {
                var items = new List<Source>();
                for (var i = 0; i < paths.Count; i++)
                {
                    var sound = TryCreateSound(paths[i]);
                    if (sound != null)
                        items.Add(sound);
                }
                return items.ToArray();
            }

            var single = TryCreateSound(fallbackSinglePath);
            return single == null ? Array.Empty<Source>() : new[] { single };
        }

        private Source SelectRandomCrashHandle()
        {
            if (_soundCrashVariants.Length == 0)
                return _soundCrash;
            return _soundCrashVariants[Algorithm.RandomInt(_soundCrashVariants.Length)];
        }

        private bool AnyBackfirePlaying()
        {
            for (var i = 0; i < _soundBackfireVariants.Length; i++)
            {
                if (_soundBackfireVariants[i].IsPlaying)
                    return true;
            }
            return false;
        }

        private void PlayRandomBackfire()
        {
            if (_soundBackfireVariants.Length == 0)
                return;
            _soundBackfire = _soundBackfireVariants[Algorithm.RandomInt(_soundBackfireVariants.Length)];
            _soundBackfire.Play(loop: false);
        }

        private void StopResetBackfireVariants()
        {
            for (var i = 0; i < _soundBackfireVariants.Length; i++)
            {
                if (_soundBackfireVariants[i].IsPlaying)
                    _soundBackfireVariants[i].Stop();
                _soundBackfireVariants[i].SeekToStart();
            }
        }

        private static void DisposeSoundVariants(Source[] sounds)
        {
            for (var i = 0; i < sounds.Length; i++)
            {
                sounds[i].Stop();
                sounds[i].Dispose();
            }
        }

        private Source? TryCreateSound(string? path, bool looped = false, bool spatialize = true, bool allowHrtf = true)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return null;
            return CreateVehicleSound(path!, looped, spatialize, allowHrtf);
        }

        private Source CreateVehicleSound(string path, bool looped, bool spatialize, bool allowHrtf)
        {
            var asset = _audio.LoadAsset(path, streamFromDisk: !looped);
            if (!spatialize)
            {
                return looped
                    ? _audio.CreateLoopingSource(asset, AudioEngineOptions.VehiclesBusName, useHrtf: false)
                    : _audio.CreateSource(asset, AudioEngineOptions.VehiclesBusName, useHrtf: false);
            }

            return looped
                ? _audio.CreateLoopingSpatialSource(asset, AudioEngineOptions.VehiclesBusName, allowHrtf)
                : _audio.CreateSpatialSource(asset, AudioEngineOptions.VehiclesBusName, allowHrtf);
        }

        private Source CreateTrackSurfaceLoop(string path)
        {
            var asset = _audio.LoadAsset(path, streamFromDisk: false);
            return _audio.CreateLoopingSource(asset, AudioEngineOptions.TrackBusName, useHrtf: false);
        }
    }
}

