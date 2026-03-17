using System;
using TopSpeed.Bots;
using TopSpeed.Data;
using TopSpeed.Protocol;
using Xunit;

namespace TopSpeed.Shared.Tests.Physics
{
    public sealed class BotPhysicsTests
    {
        [Fact]
        public void BotPhysics_AllOfficialVehicles_CanLaunchFromStandstill()
        {
            foreach (CarType carType in Enum.GetValues(typeof(CarType)))
            {
                if (carType == CarType.CustomVehicle)
                    continue;

                var config = BotPhysicsCatalog.Get(carType);
                var state = new BotPhysicsState
                {
                    PositionX = 0f,
                    PositionY = 0f,
                    SpeedKph = 0f,
                    LateralVelocityMps = 0f,
                    YawRateRad = 0f,
                    Gear = 1,
                    AutoShiftCooldownSeconds = 0f
                };

                var input = new BotPhysicsInput(
                    elapsedSeconds: 0.1f,
                    surface: TrackSurface.Asphalt,
                    throttle: 100,
                    brake: 0,
                    steering: 0);

                TopSpeed.Bots.BotPhysics.Step(config, ref state, input);

                Assert.True(state.SpeedKph > 0f, $"{carType} failed to launch from standstill.");
            }
        }
    }
}
