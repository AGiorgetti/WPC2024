// Dapr Actors do not have built-in event sourcing support, so write your own
// using the any EventSourcing library you like the most.

// I'd suggest to design it so that even commands are expressed as messages
// that can be handled, rather than function methods.

// We can also extend it to support snapshots (maybe using the state manager?).

// We also need to implement a Projection mechanism to update the read model.

using Dapr.Actors.Runtime;
using NEventStore;

namespace DaprShared;

public abstract class EventSourcingActor<TState> : Actor
    where TState : EventSourcingActorState, new()
{
    private readonly IStoreEvents _store;
    private IEventStream? _stream;
    protected TState State { get; private set; } = new TState();

    public EventSourcingActor(ActorHost host, IStoreEvents store) : base(host)
    {
        _store = store;
    }

    private void CheckInvariants(TState state)
    {
        state.CheckInvariants();
    }

    /// <summary>
    /// Load events from storage / rebuild the state from events
    /// </summary>
    protected void ReHydrate()
    {
        var id = Id.GetId();
        _stream = _store.OpenStream(id, 0, int.MaxValue);
        foreach (var @event in _stream.CommittedEvents)
        {
            ApplyEvent(State, @event.Body);
        }
    }

    protected override Task OnActivateAsync()
    {
        ReHydrate();
        return base.OnActivateAsync();
    }

    protected override Task OnDeactivateAsync()
    {
        _stream?.Dispose();
        return base.OnDeactivateAsync();
    }

    protected abstract void ApplyEvent(TState state, dynamic @event);

    protected void RaiseEvent(object @event)
    {
        // to properly implement a DDD aggregate, we should:
        // clone the state
        // apply the event to the clone
        // check invariants
        // if invariants are not satisfied, throw an exception
        // if invariants are satisfied, persist the event, update the state
        var tentativeState = (TState)State.Clone();
        ApplyEvent(tentativeState, @event);
        CheckInvariants(tentativeState);

        // Persist the stream only if we have no exceptions
        _stream?.Add(new EventMessage { Body = @event });
        _stream?.CommitChanges(Guid.NewGuid());

        // swap state
        State = tentativeState;
    }

    protected void RaiseEvents(object[] events)
    {
        // to properly implement a DDD aggregate, we should:
        // clone the state
        // apply the event to the clone
        // check invariants
        // if invariants are not satisfied, throw an exception
        // if invariants are satisfied, persist the event, update the state
        var tentativeState = (TState)State.Clone();
        foreach (var @event in events)
        {
            ApplyEvent(tentativeState, @event);
        }
        CheckInvariants(tentativeState);

        // Persist the stream only if we have no exceptions
        foreach (var @event in events)
        {
            _stream?.Add(new EventMessage { Body = @event });
        }
        _stream?.CommitChanges(Guid.NewGuid());

        // swap state
        State = tentativeState;
    }
}