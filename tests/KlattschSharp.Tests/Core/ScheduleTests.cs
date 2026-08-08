using KlattschSharp;

namespace KlattschSharp.Tests.Core;

public sealed class ScheduleTests
{
    [Fact]
    public void EmptyScheduleHasNoEvents()
    {
        var s = Schedule.Empty(48_000);
        Assert.Empty(s.Events);
        Assert.Equal(0UL, s.LastEventSample);
    }

    [Fact]
    public void FromMsEventsQuantizesAndSorts()
    {
        var s = Schedule.FromMsEvents(48_000, new[]
        {
            new MsEvent(100.0, new ParamUpdate { F0 = 220.0 }, 30.0),
            new MsEvent(0.0,   new ParamUpdate { F0 = 120.0 }, 35.0),
        });
        Assert.Equal(2, s.Events.Count);
        Assert.Equal(0UL, s.Events[0].AtSample);
        Assert.Equal(4800UL, s.Events[1].AtSample);
        Assert.Equal(1680u, s.Events[0].TransitionSamples);
    }

    [Fact]
    public void FromMsEventsClampsTransitionToAtLeastOneSample()
    {
        var s = Schedule.FromMsEvents(48_000, new[]
        {
            new MsEvent(0.0, default, 0.0),
        });
        Assert.Equal(1u, s.Events[0].TransitionSamples);
    }

    [Fact]
    public void FromMsEventsKeepsEmissionOrderForSameTimeEvents()
    {
        var s = Schedule.FromMsEvents(48_000, new[]
        {
            new MsEvent(50.0, new ParamUpdate { F0 = 1.0 }, 5.0),
            new MsEvent(0.0,  new ParamUpdate { F0 = 2.0 }, 5.0),
            new MsEvent(50.0, new ParamUpdate { F0 = 3.0 }, 5.0),
            new MsEvent(50.0, new ParamUpdate { F0 = 4.0 }, 5.0),
        });
        Assert.Equal(2.0, s.Events[0].Target.F0);
        Assert.Equal(1.0, s.Events[1].Target.F0);
        Assert.Equal(3.0, s.Events[2].Target.F0);
        Assert.Equal(4.0, s.Events[3].Target.F0);
    }

    [Fact]
    public void FromSampleEventsKeepsEmissionOrderForSameSampleEvents()
    {
        var s = Schedule.FromSampleEvents(48_000, new[]
        {
            new ScheduleEvent(100, new ParamUpdate { F0 = 1.0 }, 5),
            new ScheduleEvent(100, new ParamUpdate { F0 = 2.0 }, 5),
            new ScheduleEvent(0,   new ParamUpdate { F0 = 3.0 }, 5),
        });
        Assert.Equal(3.0, s.Events[0].Target.F0);
        Assert.Equal(1.0, s.Events[1].Target.F0);
        Assert.Equal(2.0, s.Events[2].Target.F0);
    }
}
