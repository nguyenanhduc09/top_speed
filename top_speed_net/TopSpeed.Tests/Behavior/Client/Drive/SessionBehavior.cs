using System;
using System.Collections.Generic;
using TopSpeed.Drive.Session;
using Xunit;

namespace TopSpeed.Tests;

[Trait("Category", "Behavior")]
public sealed class SessionBehaviorTests
{
    [Fact]
    public void Update_Runs_Active_Subsystems_In_Order()
    {
        var calls = new List<string>();
        var fast = new RecordingSubsystem("fast", 10, () => calls.Add("fast"));
        var slow = new RecordingSubsystem("slow", 20, () => calls.Add("slow"));
        var session = new SessionBuilder(CreatePolicy(
                Phase.Running,
                Phase.Running,
                builder => builder.Add(
                    Phase.Running,
                    advanceProgressClock: true,
                    advanceRuntimeClock: true,
                    InputPolicy.Create(true, true, true),
                    PhaseDefinition.Subsystems(slow, fast),
                    Defaults.StandardCommands,
                    Defaults.NoExternalEvents,
                    new[] { Phase.Paused })))
            .AddSubsystems(slow, fast)
            .Build();

        session.Update(0.25f);

        calls.Should().Equal("fast", "slow");
    }

    [Fact]
    public void ApplyCommand_Ignores_Disallowed_Command()
    {
        var handled = 0;
        var session = new SessionBuilder(CreatePolicy(
                Phase.Running,
                Phase.Running,
                builder => builder.Add(
                    Phase.Running,
                    advanceProgressClock: true,
                    advanceRuntimeClock: true,
                    InputPolicy.Create(true, true, true),
                    Defaults.NoSubsystems,
                    Array.Empty<CommandId>(),
                    Defaults.NoExternalEvents,
                    Array.Empty<Phase>())))
            .AddCommandHandler(new HandlerId("test.command"), 100, (_, _) => handled++)
            .Build();

        session.ApplyCommand(new Command(Commands.Pause));

        handled.Should().Be(0);
        session.Context.Phase.Should().Be(Phase.Running);
    }

    [Fact]
    public void ApplyExternalEvent_Ignores_Disallowed_Event()
    {
        var handled = 0;
        var session = new SessionBuilder(CreatePolicy(
                Phase.Running,
                Phase.Running,
                builder => builder.Add(
                    Phase.Running,
                    advanceProgressClock: true,
                    advanceRuntimeClock: true,
                    InputPolicy.Create(true, true, true),
                    Defaults.NoSubsystems,
                    Defaults.StandardCommands,
                    Defaults.NoExternalEvents,
                    Array.Empty<Phase>())))
            .AddExternalEventHandler(new HandlerId("test.external"), 100, (_, _) => handled++)
            .Build();

        session.ApplyExternalEvent(new ExternalEvent(new ExternalEventId("test.denied")));

        handled.Should().Be(0);
    }

    [Fact]
    public void Pause_And_Resume_Use_Configured_Phases_And_Input_Policy()
    {
        var session = new SessionBuilder(CreatePolicy(
                Phase.Running,
                Phase.Running,
                builder =>
                {
                    builder.Add(
                        Phase.Running,
                        advanceProgressClock: true,
                        advanceRuntimeClock: true,
                        InputPolicy.Create(true, true, true),
                        Defaults.NoSubsystems,
                        Defaults.StandardCommands,
                        Defaults.NoExternalEvents,
                        new[] { Phase.Paused });
                    builder.Add(
                        Phase.Paused,
                        advanceProgressClock: false,
                        advanceRuntimeClock: true,
                        InputPolicy.Create(false, true, true),
                        Defaults.NoSubsystems,
                        Defaults.StandardCommands,
                        Defaults.NoExternalEvents,
                        new[] { Phase.Running });
                }))
            .Build();

        session.ApplyCommand(new Command(Commands.Pause));

        session.Context.Phase.Should().Be(Phase.Paused);
        session.Context.InputPolicy.AllowDrivingInput.Should().BeFalse();
        session.Context.InputPolicy.AllowAuxiliaryInput.Should().BeTrue();
        session.Context.InputPolicy.AllowHorn.Should().BeTrue();

        session.ApplyCommand(new Command(Commands.Resume));

        session.Context.Phase.Should().Be(Phase.Running);
        session.Context.InputPolicy.AllowDrivingInput.Should().BeTrue();
        session.Context.InputPolicy.AllowAuxiliaryInput.Should().BeTrue();
        session.Context.InputPolicy.AllowHorn.Should().BeTrue();
    }

    [Fact]
    public void Update_Advances_Progress_And_Runtime_Clocks_Per_Phase()
    {
        var session = new SessionBuilder(CreatePolicy(
                Phase.Running,
                Phase.Running,
                builder =>
                {
                    builder.Add(
                        Phase.Running,
                        advanceProgressClock: true,
                        advanceRuntimeClock: true,
                        InputPolicy.Create(true, true, true),
                        Defaults.NoSubsystems,
                        Defaults.StandardCommands,
                        Defaults.NoExternalEvents,
                        new[] { Phase.Paused });
                    builder.Add(
                        Phase.Paused,
                        advanceProgressClock: false,
                        advanceRuntimeClock: true,
                        InputPolicy.Create(false, true, true),
                        Defaults.NoSubsystems,
                        Defaults.StandardCommands,
                        Defaults.NoExternalEvents,
                        new[] { Phase.Running });
                }))
            .Build();

        session.Update(1.5f);
        session.ApplyCommand(new Command(Commands.Pause));
        session.Update(2.0f);

        session.Context.ProgressSeconds.Should().BeApproximately(1.5f, 0.0001f);
        session.Context.RuntimeSeconds.Should().BeApproximately(3.5f, 0.0001f);
    }

    [Fact]
    public void Runtime_Scheduled_Event_Fires_While_Progress_Clock_Is_Frozen()
    {
        var fired = 0;
        var session = new SessionBuilder(CreatePolicy(
                Phase.Paused,
                Phase.Running,
                builder =>
                {
                    builder.Add(
                        Phase.Running,
                        advanceProgressClock: true,
                        advanceRuntimeClock: true,
                        InputPolicy.Create(true, true, true),
                        Defaults.NoSubsystems,
                        Defaults.StandardCommands,
                        Defaults.NoExternalEvents,
                        new[] { Phase.Paused });
                    builder.Add(
                        Phase.Paused,
                        advanceProgressClock: false,
                        advanceRuntimeClock: true,
                        InputPolicy.Create(false, true, true),
                        Defaults.NoSubsystems,
                        Defaults.StandardCommands,
                        Defaults.NoExternalEvents,
                        new[] { Phase.Running });
                }))
            .AddEventHandler(new HandlerId("test.runtimeEvent"), 100, (_, sessionEvent) =>
            {
                if (sessionEvent.Id == Events.PlaySound)
                    fired++;
            })
            .Build();

        session.QueueEvent(new Event(Events.PlaySound), 1.0f, Clock.Runtime);
        session.Update(0.5f);
        session.Update(0.6f);
        session.Update(0f);

        fired.Should().Be(1);
        session.Context.ProgressSeconds.Should().Be(0f);
        session.Context.RuntimeSeconds.Should().BeApproximately(1.1f, 0.0001f);
    }

    private static Policy CreatePolicy(Phase initialPhase, Phase resumeFallbackPhase, Action<PolicyBuilder> configure)
    {
        var builder = new PolicyBuilder(initialPhase, resumeFallbackPhase);
        configure(builder);
        return builder.Build();
    }

    private sealed class RecordingSubsystem : Subsystem
    {
        private readonly Action _update;

        public RecordingSubsystem(string name, int order, Action update)
            : base(name, order)
        {
            _update = update;
        }

        public override void Update(SessionContext context, float elapsed)
        {
            _update();
        }
    }
}
