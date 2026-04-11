using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using SteamAudio;

namespace TS.Audio
{
    public sealed partial class SteamAudioContext : IDisposable
    {
        internal sealed class ListenerState
        {
            public readonly IPL.Vector3 Right;
            public readonly IPL.Vector3 Up;
            public readonly IPL.Vector3 Ahead;
            public readonly IPL.Vector3 Origin;

            public ListenerState(IPL.Vector3 right, IPL.Vector3 up, IPL.Vector3 ahead, IPL.Vector3 origin)
            {
                Right = right;
                Up = up;
                Ahead = ahead;
                Origin = origin;
            }
        }

        public IPL.Context Context;
        public IPL.Hrtf Hrtf;
        public readonly int SampleRate;
        public readonly int FrameSize;
        public readonly int ReflectionOrder;
        public readonly int ReflectionChannels;
        public readonly int ReflectionIrSize;
        public readonly float ReflectionDurationSeconds;
        public readonly IPL.ReflectionEffectType ReflectionType;
        private volatile ListenerState _listenerState;
        internal ListenerState ListenerSnapshot => _listenerState;
        private IPL.Simulator _simulator;
        private IPL.Scene _scene;
        private readonly Dictionary<AudioSourceHandle, IPL.Source> _sources = new Dictionary<AudioSourceHandle, IPL.Source>();
        private readonly object _simLock = new object();

        public IDisposable AcquireSimulationLock()
        {
            return new SimulationLock(_simLock);
        }

        public SteamAudioContext(int sampleRate, int frameSize, string? hrtfSofaPath)
        {
            SampleRate = sampleRate;
            FrameSize = frameSize;
            ReflectionOrder = 0;
            ReflectionChannels = (ReflectionOrder + 1) * (ReflectionOrder + 1);
            ReflectionDurationSeconds = 0.5f;
            ReflectionIrSize = Math.Max(1, (int)Math.Ceiling(ReflectionDurationSeconds * SampleRate));
            ReflectionType = IPL.ReflectionEffectType.Parametric;
            _listenerState = CreateIdentityState();

            var contextSettings = new IPL.ContextSettings
            {
                Version = IPL.Version,
                LogCallback = null,
                AllocateCallback = null,
                FreeCallback = null,
                SimdLevel = IPL.SimdLevel.Avx2,
                Flags = 0
            };

            var error = IPL.ContextCreate(in contextSettings, out Context);
            if (error != IPL.Error.Success)
                throw new InvalidOperationException("Failed to create SteamAudio context: " + error);

            var hrtfSettings = new IPL.HrtfSettings
            {
                Type = string.IsNullOrWhiteSpace(hrtfSofaPath) ? IPL.HrtfType.Default : IPL.HrtfType.Sofa,
                SofaFileName = string.IsNullOrWhiteSpace(hrtfSofaPath) ? null : hrtfSofaPath,
                SofaData = IntPtr.Zero,
                SofaDataSize = 0,
                Volume = 1.0f,
                NormType = IPL.HrtfNormType.None
            };

            var audioSettings = new IPL.AudioSettings
            {
                SamplingRate = sampleRate,
                FrameSize = frameSize
            };

            error = IPL.HrtfCreate(Context, in audioSettings, in hrtfSettings, out Hrtf);
            if (error != IPL.Error.Success)
            {
                IPL.ContextRelease(ref Context);
                Context = default;
                throw new InvalidOperationException("Failed to create SteamAudio HRTF: " + error);
            }
        }

        public void SetScene(IPL.Scene scene)
        {
            if (scene.Handle == IntPtr.Zero || Context.Handle == IntPtr.Zero)
                return;

            lock (_simLock)
            {
                if (_simulator.Handle == IntPtr.Zero)
                    CreateSimulator();

                _scene = scene;

                if (_simulator.Handle != IntPtr.Zero)
                {
                    IPL.SimulatorSetScene(_simulator, scene);
                    IPL.SimulatorCommit(_simulator);
                }
            }
        }

        public void ClearScene()
        {
            lock (_simLock)
            {
                if (_simulator.Handle != IntPtr.Zero)
                {
                    IPL.SimulatorSetScene(_simulator, default);
                    IPL.SimulatorCommit(_simulator);
                }

                _scene = default;
            }
        }

        public void UpdateListener(Vector3 position, Vector3 forward, Vector3 up)
        {
            if (Context.Handle == IntPtr.Zero)
                return;

            var normForward = Vector3.Normalize(forward);
            var normUp = Vector3.Normalize(up);
            var right = Vector3.Normalize(Vector3.Cross(normUp, normForward));

            _listenerState = new ListenerState(
                ToIpl(right),
                ToIpl(normUp),
                new IPL.Vector3 { X = -normForward.X, Y = -normForward.Y, Z = -normForward.Z },
                ToIpl(position));
        }

        public void Dispose()
        {
            lock (_simLock)
            {
                foreach (var entry in _sources.Values)
                {
                    var source = entry;
                    if (source.Handle != IntPtr.Zero)
                    {
                        IPL.SourceRemove(source, _simulator);
                        IPL.SourceRelease(ref source);
                    }
                }
                _sources.Clear();

                if (_simulator.Handle != IntPtr.Zero)
                {
                    IPL.SimulatorRelease(ref _simulator);
                    _simulator = default;
                }
            }

            if (Hrtf.Handle != IntPtr.Zero)
            {
                IPL.HrtfRelease(ref Hrtf);
                Hrtf = default;
            }

            if (Context.Handle != IntPtr.Zero)
            {
                IPL.ContextRelease(ref Context);
                Context = default;
            }
        }

        public static IPL.Vector3 ToIpl(Vector3 v)
        {
            return new IPL.Vector3 { X = v.X, Y = v.Y, Z = v.Z };
        }

        private sealed class SimulationLock : IDisposable
        {
            private readonly object _lock;

            public SimulationLock(object simLock)
            {
                _lock = simLock;
                Monitor.Enter(_lock);
            }

            public void Dispose()
            {
                Monitor.Exit(_lock);
            }
        }

        private static ListenerState CreateIdentityState()
        {
            return new ListenerState(
                new IPL.Vector3 { X = 1, Y = 0, Z = 0 },
                new IPL.Vector3 { X = 0, Y = 1, Z = 0 },
                new IPL.Vector3 { X = 0, Y = 0, Z = -1 },
                new IPL.Vector3 { X = 0, Y = 0, Z = 0 });
        }
    }
}
