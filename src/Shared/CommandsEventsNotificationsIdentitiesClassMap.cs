using MongoDB.Bson.Serialization;

namespace Shared;

/// <summary>
/// <para>
/// MongoDB mapping used for:
/// - commands
/// - events
/// - messages
/// - anything that implement <see cref="IMessage"/>
/// </para>
/// <para>it will use the normal MongoDB mapping for discriminators (ClassType.Name)</para>
/// </summary>
public class CommandsEventsNotificationsIdentitiesClassMap : BsonClassMap
{
    /// <summary>
    /// Constructor
    /// </summary>
    public CommandsEventsNotificationsIdentitiesClassMap(Type classType)
        : base(classType)
    {
        AutoMap();
    }
}