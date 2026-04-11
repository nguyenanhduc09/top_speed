using System;
using MiniAudioEx.Core.AdvancedAPI;
using MiniAudioEx.Native;

namespace TS.Audio
{
    internal sealed class SourceGraph : IDisposable
    {
        private readonly AudioOutput _output;
        private readonly AudioBus _bus;
        private readonly ma_sound_group_ptr _group;
        private readonly AudioSourceSpatialParams _spatial;
        private readonly bool _spatialize;
        private readonly bool _useHrtf;
        private SteamAudioSpatializer? _spatializer;
        private MaEffectNode? _effectNode;
        private bool _disposed;

        public bool UsesHrtf => _useHrtf;

        public SourceGraph(AudioOutput output, AudioBus bus, ma_sound_group_ptr group, AudioSourceSpatialParams spatial, bool spatialize, bool useHrtf)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _group = group;
            _spatial = spatial ?? throw new ArgumentNullException(nameof(spatial));
            _spatialize = spatialize;
            _useHrtf = _spatialize && useHrtf && output.SteamAudio != null;
            _spatializer = output.SteamAudio != null
                ? new SteamAudioSpatializer(output.SteamAudio, output.PeriodSizeInFrames, output.TrueStereoHrtf, output.DownmixMode)
                : null;
        }

        public void Configure()
        {
            var groupNode = new ma_node_ptr(_group.pointer);
            MiniAudioNative.ma_node_detach_all_output_buses(groupNode);

            if (_useHrtf)
            {
                if (_effectNode == null)
                {
                    _effectNode = new MaEffectNode();
                    var init = _effectNode.Initialize(_output.Runtime.EngineHandle, (uint)_output.SampleRate, (uint)_output.Channels);
                    if (init != ma_result.success)
                        throw new InvalidOperationException("Failed to initialize HRTF effect node: " + init);

                    _effectNode.Process += OnHrtfProcess;
                    _effectNode.AttachOutputBus(0, _bus.NodeHandle, 0);
                }

                MiniAudioNative.ma_sound_group_set_spatialization_enabled(_group, 0);
                MiniAudioNative.ma_node_attach_output_bus(groupNode, 0, _effectNode.NodeHandle, 0);
                return;
            }

            MiniAudioNative.ma_sound_group_set_spatialization_enabled(_group, _spatialize ? 1u : 0u);
            MiniAudioNative.ma_node_attach_output_bus(groupNode, 0, _bus.NodeHandle, 0);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _effectNode?.Dispose();
            _effectNode = null;
            _spatializer?.Dispose();
            _spatializer = null;
        }

        private void OnHrtfProcess(MaEffectNode sender, NativeArray<float> framesIn, uint frameCountIn, NativeArray<float> framesOut, ref uint frameCountOut, uint channels)
        {
            if (_disposed)
                return;

            if (_spatializer == null)
            {
                framesIn.CopyTo(framesOut);
                return;
            }

            _spatializer.Process(framesIn, frameCountIn, framesOut, ref frameCountOut, channels, _spatial);
        }
    }
}
