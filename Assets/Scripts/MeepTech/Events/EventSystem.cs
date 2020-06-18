﻿using MeepTech.GamingBasics;
using System;
using System.Collections.Generic;

namespace MeepTech.Events {

  /// <summary>
  /// An event system with channels, to be extended for each needed use.
  /// </summary>
  public abstract class EventSystem<ChannelList> : IEventSystem<ChannelList>
    where ChannelList : struct, Enum {

    /// <summary>
    /// all observers currently listening
    /// </summary>
    List<IObserver> allListeners;

    /// <summary>
    /// All the listeners who are subscribed to a specific channel are stored here.
    /// </summary>
    List<IObserver>[] listenersByChannel;

    /// <summary>
    /// Debug Mode.
    /// </summary>
    bool debugMode = false;

    /// <summary>
    /// Constructor
    /// </summary>
    public EventSystem() {
      allListeners = new List<IObserver>();
      /// Set up the channels based on the enum
      int channelCount = Enum.GetValues(typeof(ChannelList)).Length;
      listenersByChannel = new List<IObserver>[channelCount];
      for (int i = 0; i < channelCount; i++) {
        listenersByChannel[i] = new List<IObserver>();
      }
    }

    /// <summary>
    /// Subscribe to the listener list.
    /// </summary>
    public void subscribe(IObserver newListener, ChannelList? channelToSubscribeTo = null) {
      allListeners.Add(newListener);
      if (channelToSubscribeTo != null) {
        subscribeToChannel(newListener, Convert.ToInt32(channelToSubscribeTo));
      }
    }

    /// <summary>
    /// Notify all listening observers of an event
    /// </summary>
    /// <param name="event">The event to notify all listening observers of</param>
    /// <param name="origin">(optional) the osurce of the event</param>
    public void notifyAllOf(IEvent @event, IObserver origin = null) {
      if (debugMode) {
        World.Debugger.log($"Notifiying ALL of {@event.name}");
      }
      foreach (IObserver observer in allListeners) {
        observer.notifyOf(@event, origin);
      }
    }

    /// <summary>
    /// Notify all listening observers of an event
    /// </summary>
    /// <param name="event">The event to notify all listening observers of</param>
    /// <param name="origin">(optional) the osurce of the event</param>
    public void notifyChannelOf(IEvent @event, ChannelList channelToNotify, IObserver origin = null) {
      int channelNumber = Convert.ToInt32(channelToNotify);
      if (channelNumber < listenersByChannel.Length && channelNumber > 0) {
        if (debugMode) {
          World.Debugger.log($"Notifiying channel: {channelToNotify} of {@event.name}");
        }
        foreach (IObserver observer in listenersByChannel[channelNumber]) {
          observer.notifyOf(@event, origin);
        }
      } else ThrowMissingChannelException(channelNumber);
    }

    /// <summary>
    /// Wrapper for adding to the channel subscriber list.
    /// </summary>
    /// <param name="newListener"></param>
    /// <param name="channelToSubscribeTo"></param>
    void subscribeToChannel(IObserver newListener, int channelToSubscribeTo) {
      if (channelToSubscribeTo < listenersByChannel.Length && channelToSubscribeTo > 0) {
        listenersByChannel[channelToSubscribeTo].Add(newListener);
      } else ThrowMissingChannelException(channelToSubscribeTo);
    }

    /// <summary>
    /// Throw a missing channel exception
    /// </summary>
    /// <param name="missingChannel"></param>
    static void ThrowMissingChannelException(int missingChannel) {
      throw new System.IndexOutOfRangeException($"Event System does not have a chanel {missingChannel}!");
    }
  }
}
