using System;
using System.Collections.Generic;
using System.Threading;
using SteamAudio;

namespace TS.Audio
{
    public sealed partial class SteamAudioContext
    {
        public void UpdateSimulation(IReadOnlyList<AudioSourceHandle> sources)
        {
            if (sources == null || sources.Count == 0)
                return;

            lock (_simLock)
            {
                var active = new HashSet<AudioSourceHandle>();
                foreach (var source in sources)
                {
                    if (source == null || !source.IsSpatialized || !source.UsesSteamAudio || !source.IsPlaying)
                        continue;
                    active.Add(source);
                }

                if (active.Count == 0)
                {
                    if (_sources.Count > 0)
                        RemoveInactiveSources(active);
                    return;
                }

                if (_simulator.Handle == IntPtr.Zero || _scene.Handle == IntPtr.Zero)
                {
                    if (_sources.Count > 0)
                        RemoveInactiveSources(new HashSet<AudioSourceHandle>());

                    foreach (var source in active)
                        ApplyRoomOnlyOutputs(source);

                    return;
                }

                foreach (var source in active)
                {
                    EnsureSource(source);
                    SetSourceInputs(source);
                }

                RemoveInactiveSources(active);

                var shared = BuildSharedInputs();
                var flags = IPL.SimulationFlags.Direct | IPL.SimulationFlags.Reflections;
                IPL.SimulatorSetSharedInputs(_simulator, flags, in shared);
                IPL.SimulatorCommit(_simulator);
                IPL.SimulatorRunDirect(_simulator);
                IPL.SimulatorRunReflections(_simulator);

                foreach (var source in active)
                {
                    if (!_sources.TryGetValue(source, out var simSource) || simSource.Handle == IntPtr.Zero)
                        continue;
                    var sourceFlags = IPL.SimulationFlags.Direct | IPL.SimulationFlags.Reflections;
                    IPL.SourceGetOutputs(simSource, sourceFlags, out var outputs);
                    ApplyDirectOutputs(source, in outputs.Direct);
                    ApplyReverbOutputs(source, in outputs.Reflections);
                }
            }
        }

        private void CreateSimulator()
        {
            var settings = new IPL.SimulationSettings
            {
                Flags = IPL.SimulationFlags.Direct | IPL.SimulationFlags.Reflections,
                SceneType = IPL.SceneType.Default,
                ReflectionType = ReflectionType,
                MaxNumOcclusionSamples = 32,
                MaxNumRays = 2048,
                NumDiffuseSamples = 64,
                MaxDuration = ReflectionDurationSeconds,
                MaxOrder = ReflectionOrder,
                MaxNumSources = 128,
                NumThreads = Math.Max(1, Environment.ProcessorCount - 1),
                RayBatchSize = 64,
                NumVisSamples = 8,
                SamplingRate = SampleRate,
                FrameSize = FrameSize
            };

            var error = IPL.SimulatorCreate(Context, in settings, out _simulator);
            if (error != IPL.Error.Success)
                _simulator = default;
        }

        private void EnsureSource(AudioSourceHandle source)
        {
            if (_sources.TryGetValue(source, out var existing) && existing.Handle != IntPtr.Zero)
                return;

            var settings = new IPL.SourceSettings
            {
                Flags = IPL.SimulationFlags.Direct | IPL.SimulationFlags.Reflections
            };

            var error = IPL.SourceCreate(_simulator, in settings, out var simSource);
            if (error != IPL.Error.Success)
                return;

            IPL.SourceAdd(simSource, _simulator);
            _sources[source] = simSource;
        }

        private void RemoveInactiveSources(HashSet<AudioSourceHandle> active)
        {
            if (_sources.Count == 0)
                return;

            var toRemove = new List<AudioSourceHandle>();
            foreach (var entry in _sources)
            {
                if (!active.Contains(entry.Key))
                    toRemove.Add(entry.Key);
            }

            foreach (var handle in toRemove)
            {
                if (_sources.TryGetValue(handle, out var simSource) && simSource.Handle != IntPtr.Zero)
                {
                    IPL.SourceRemove(simSource, _simulator);
                    IPL.SourceRelease(ref simSource);
                }
                _sources.Remove(handle);
            }
        }

        private void SetSourceInputs(AudioSourceHandle handle)
        {
            if (!_sources.TryGetValue(handle, out var source) || source.Handle != IntPtr.Zero == false)
                return;

            var spatial = handle.SpatialParams;
            var position = new IPL.Vector3
            {
                X = Volatile.Read(ref spatial.PosX),
                Y = Volatile.Read(ref spatial.PosY),
                Z = Volatile.Read(ref spatial.PosZ)
            };

            var coord = new IPL.CoordinateSpace3
            {
                Origin = position,
                Right = new IPL.Vector3 { X = 1f, Y = 0f, Z = 0f },
                Up = new IPL.Vector3 { X = 0f, Y = 1f, Z = 0f },
                Ahead = new IPL.Vector3 { X = 0f, Y = 0f, Z = 1f }
            };

            var inputs = new IPL.SimulationInputs
            {
                Flags = IPL.SimulationFlags.Direct | IPL.SimulationFlags.Reflections,
                DirectFlags = IPL.DirectSimulationFlags.Occlusion | IPL.DirectSimulationFlags.Transmission | IPL.DirectSimulationFlags.AirAbsorption,
                Source = coord,
                DistanceAttenuationModel = new IPL.DistanceAttenuationModel { Type = IPL.DistanceAttenuationModelType.Default, MinDistance = 1.0f },
                AirAbsorptionModel = new IPL.AirAbsorptionModel { Type = IPL.AirAbsorptionModelType.Default },
                Directivity = new IPL.Directivity { DipoleWeight = 0f, DipolePower = 1f },
                OcclusionType = IPL.OcclusionType.Raycast,
                OcclusionRadius = 0.5f,
                NumOcclusionSamples = 8,
                HybridReverbTransitionTime = 0.25f,
                HybridReverbOverlapPercent = 0.25f,
                NumTransmissionRays = 4
            };

            unsafe
            {
                inputs.ReverbScale[0] = 1.0f;
                inputs.ReverbScale[1] = 1.0f;
                inputs.ReverbScale[2] = 1.0f;
            }

            IPL.SourceSetInputs(source, inputs.Flags, in inputs);
        }

        private IPL.SimulationSharedInputs BuildSharedInputs()
        {
            var listener = _listenerState;
            return new IPL.SimulationSharedInputs
            {
                Listener = new IPL.CoordinateSpace3
                {
                    Origin = listener.Origin,
                    Right = listener.Right,
                    Up = listener.Up,
                    Ahead = listener.Ahead
                },
                NumRays = 2048,
                NumBounces = 2,
                Duration = ReflectionDurationSeconds,
                Order = ReflectionOrder,
                IrradianceMinDistance = 1.0f
            };
        }
    }
}
