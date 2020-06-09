using System;

namespace MeepTech.Events {

  /// <summary>
  /// An event system capabple of sending out notifications
  /// </summary>
  public interface IEventSystem<ChannelList>
    where ChannelList : struct, Enum {

    /// <summary>
    /// Subscribe to the listener list.
    /// </summary>
    void subscribe(IObserver newListener, ChannelList? channelToSubscribeTo = null);

    /// <summary>
    /// Notify all listening observers of an event
    /// </summary>
    /// <param name="event">The event to notify all listening observers of</param>
    /// <param name="origin">(optional) the osurce of the event</param>
    void notifyAllOf(IEvent @event, IObserver origin = null);

    /// <summary>
    /// Notify all listening observers of an event
    /// </summary>
    /// <param name="event">The event to notify all listening observers of</param>
    /// <param name="origin">(optional) the osurce of the event</param>
    void notifyChannelOf(IEvent @event, ChannelList channelToNotify, IObserver origin = null);
  }
}