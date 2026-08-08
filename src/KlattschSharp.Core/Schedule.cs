using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace KlattschSharp;

/// <summary>
/// Time-sorted, sample-rate-bound parameter event schedule consumed by
/// FormantSynth.
/// </summary>
public sealed class Schedule
{
    private readonly ScheduleEvent[] _events;
    private readonly ReadOnlyCollection<ScheduleEvent> _eventsView;

    /// <summary>Sample rate the event times are bound to, in Hz.</summary>
    public uint SampleRate { get; }

    /// <summary>Events in apply order.</summary>
    public IReadOnlyList<ScheduleEvent> Events => _eventsView;

    /// <summary>Sample time of the final event. 0 when the schedule is empty.</summary>
    public ulong LastEventSample => _events.Length == 0 ? 0UL : _events[_events.Length - 1].AtSample;

    private Schedule(uint sampleRate, ScheduleEvent[] events)
    {
        SampleRate = sampleRate;
        _events = events;
        _eventsView = Array.AsReadOnly(events);
    }

    /// <summary>An event-free schedule at <paramref name="sampleRate"/>.</summary>
    public static Schedule Empty(uint sampleRate) => new(sampleRate, Array.Empty<ScheduleEvent>());

    /// <summary>Build from sample-time events, stable-sorted by time.</summary>
    public static Schedule FromSampleEvents(uint sampleRate, IEnumerable<ScheduleEvent> events)
    {
        // Stable sort: same-sample events apply in emission order
        var arr = events.OrderBy(e => e.AtSample).ToArray();
        return new Schedule(sampleRate, arr);
    }

    /// <summary>Quantize millisecond-keyed events to sample times and build, stable-sorted by time.</summary>
    public static Schedule FromMsEvents(uint sampleRate, IEnumerable<MsEvent> msEvents)
    {
        double sr = sampleRate;
        var list = new List<ScheduleEvent>();
        foreach (var m in msEvents)
        {
            ulong atSample = (ulong)Math.Max(0.0, Math.Floor(m.AtMs * sr / 1000.0));
            long transRaw = (long)Math.Floor(m.TransitionMs * sr / 1000.0);
            uint transitionSamples = (uint)Math.Max(1L, transRaw);
            list.Add(new ScheduleEvent(atSample, m.Target, transitionSamples));
        }
        var arr = list.OrderBy(e => e.AtSample).ToArray();
        return new Schedule(sampleRate, arr);
    }

    /// <summary>Backing array.</summary>
    internal ScheduleEvent[] EventsArray => _events;
}
