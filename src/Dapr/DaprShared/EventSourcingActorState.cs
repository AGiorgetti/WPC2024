// Dapr Actors do not have built-in event sourcing support, so write your own
// using the any EventSourcing library you like the most.

// if we want, we can design it so that even commands are expressed as messages
// that can be handled, rather than function methods.

// we can also extend it to support snapshots (maybe using the state manager?).

namespace DaprShared;

// In a real production implementation state should be immutable and updated only by "ApplyEvent" calls

public abstract class EventSourcingActorState: ICloneable
{
    public abstract object Clone();

    public abstract void CheckInvariants();
}
