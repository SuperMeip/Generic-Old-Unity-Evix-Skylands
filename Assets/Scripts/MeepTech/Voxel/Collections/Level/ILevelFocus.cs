using MeepTech.Events;

namespace MeepTech.Voxel.Collections.Level {

  public interface ILevelFocus {

    /// <summary>
    /// Get if this focus is active
    /// </summary>
    bool isActive {
      get;
    }

    /// <summary>
    /// The chunk location of this focus
    /// </summary>
    Coordinate chunkLocation {
      get;
    }

    /// <summary>
    /// Spawn the focus at a given point in the level.
    /// Using level/world location, not chunk location
    /// </summary>
    /// <param name="spawnPoint"></param>
    void spawn(Coordinate spawnPoint);

    /// <summary>
    /// set the focus active;
    /// </summary>
    void setActive();
  }

  /// <summary>
  /// An event for announcing when the player changes chunk locations
  /// </summary>
  public struct FocusChangedChunkLocationEvent : IEvent {

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
    public FocusChangedChunkLocationEvent(Coordinate newChunkLocation) {
      this.newChunkLocation = newChunkLocation;
    }
  }

  /// <summary>
  /// An event for announcing when the player has spawned
  /// </summary>
  public struct SpawnFocusEvent : IEvent {

    /// <summary>
    /// The world location the focus has spawned at
    /// </summary>
    public ILevelFocus spawnedFocalPoint {
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
    public SpawnFocusEvent(ILevelFocus spawnedFocalPoint) {
      this.spawnedFocalPoint = spawnedFocalPoint;
    }
  }
}