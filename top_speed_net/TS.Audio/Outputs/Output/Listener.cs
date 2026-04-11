using System.Numerics;
using MiniAudioEx.Native;

namespace TS.Audio
{
    public sealed partial class AudioOutput
    {
        public void UpdateListener(Vector3 position, Vector3 forward, Vector3 up, Vector3 velocity)
        {
            _listenerPosition = position;
            _listenerVelocity = velocity;

            var pos = ToMaVec3(position);
            var dir = ToMaVec3(forward);
            var vel = ToMaVec3(velocity);
            var upVec = new ma_vec3f { x = up.X, y = up.Y, z = up.Z };
            _runtime.UpdateListener(pos, dir, upVec, vel);

            _steamAudio?.UpdateListener(position, forward, up);
        }

        public void SetRoomAcoustics(RoomAcoustics acoustics)
        {
            _roomAcoustics = acoustics;
            AudioSourceHandle[] snapshot;
            lock (_sourceLock)
                snapshot = _sources.ToArray();

            for (var i = 0; i < snapshot.Length; i++)
                snapshot[i].SetRoomAcoustics(_roomAcoustics);
        }

        private static ma_vec3f ToMaVec3(Vector3 value)
        {
            return new ma_vec3f { x = value.X, y = value.Y, z = -value.Z };
        }
    }
}
