using MeepTech.Events;
using MeepTech.Voxel;
using UnityEngine;

namespace MeepTech.GamingBasics {
  
  /// <summary>
  /// A person playing the game
  /// </summary>
  public class Player {

    /// <summary>
    /// An event for announcing when the player changes chunk locations
    /// </summary>
    public struct ChangeChunkLocationEvent : IEvent {
      
      /// <summary>
      /// The new chunk location
      /// </summary>
      public Coordinate newChunkLocation {
        get;
      }

      /// <summary>
      /// the name of this event
      /// </summary>
      public string name => "Player Changed Chunk Locations";

      /// <summary>
      /// Make this kind of event
      /// </summary>
      /// <param name="newChunkLocation"></param>
      public ChangeChunkLocationEvent(Coordinate newChunkLocation) {
        this.newChunkLocation = newChunkLocation;
      }
    }

    /// <summary>
    /// An event for announcing when the player has spawned
    /// </summary>
    public struct SpawnEvent : IEvent {
      
      /// <summary>
      /// The world location the player has spawned at
      /// </summary>
      public Vector3 spawnLocation {
        get;
      }

      /// <summary>
      /// the name of this event
      /// </summary>
      public string name => "Player Spawned";

      /// <summary>
      /// Make this kind of event
      /// </summary>
      /// <param name="newChunkLocation"></param>
      public SpawnEvent(Vector3 spawnLocation) {
        this.spawnLocation = spawnLocation;
      }
    }
  }
}
