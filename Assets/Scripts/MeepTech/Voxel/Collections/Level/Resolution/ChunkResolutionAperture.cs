using MeepTech.Events;
using MeepTech.GamingBasics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MeepTech.Voxel.Collections.Level.Management {

  /// <summary>
  /// An apeture/area of resolution manager for how much a chunk is loaded.
  /// </summary>
  public abstract class ChunkResolutionAperture : IChunkResolutionAperture {

    /// <summary>
    /// The managed chunk area radius, X and Z. Height may be different.
    /// </summary>
    public int managedChunkRadius {
      get;
      private set;
    }

    /// <summary>
    /// The managed chunk area height
    /// </summary>
    public int managedChunkHeightRadius {
      get;
      private set;
    }

    /// <summary>
    /// The level this resolution manager is dealing with
    /// </summary>
    public Level level {
      get;
      private set;
    }

    /// <summary>
    /// The bounds this resolution manager manages currently ordered by focus id.
    /// </summary>
    Dictionary<int, Coordinate[]> managedChunkBoundsByFocusID;

    ///// CONSTRUCTORS

    /// <summary>
    /// Default constructor
    /// </summary>
    /// <param name="level"></param>
    /// <param name="managedChunkRadius"></param>
    /// <param name="managedChunkHeight"></param>
    protected ChunkResolutionAperture(int managedChunkRadius, int managedChunkHeight = 0) {
      this.managedChunkRadius = managedChunkRadius;
      managedChunkHeightRadius = managedChunkHeight == 0 ? managedChunkRadius : managedChunkHeight;
      managedChunkBoundsByFocusID = new Dictionary<int, Coordinate[]>();
    }

    ///// PUBLIC FUNCTIONS

    /// <summary>
    /// Check if the location is within any of the managed bounds
    /// </summary>
    /// <param name="chunkLocation"></param>
    /// <returns></returns>
    public bool isWithinManagedBounds(Coordinate chunkLocation) {
      foreach(Coordinate[] bounds in managedChunkBoundsByFocusID.Values) {
        if (chunkLocation.isWithin(bounds)) {
          return true;
        }
      }

      return false;
    }

    /// <summary>
    /// Get the managed bounds for the given focus
    /// </summary>
    /// <param name="focus"></param>
    /// <returns></returns>
    public Coordinate[] getManagedBoundsForFocus(ILevelFocus focus) {
      int focusID = level.getFocusID(focus);
      return managedChunkBoundsByFocusID[focusID];
    }

    /// <summary>
    /// Notifications this manager works off of
    /// </summary>
    /// <param name="event"></param>
    /// <param name="origin"></param>
    public void notifyOf(IEvent @event, IObserver origin = null) {
      if (level == null) {
        return;
      }

      switch (@event) {
        // when a focus spawns in the level
        case Level.SpawnFocusEvent fse:
          initilizeFocus(fse.spawnedFocus);
          break;
        // When the focus moves to a new chunk, adjust the loaded level data around it
        case Level.FocusChangedChunkLocationEvent fccle:
          adjustFocus(fccle.updatedFocus);
          break;
        case Level.FocusLeftEvent fle:
          removeFocus(fle.abscondingFocus);
          break;
        default:
          return;
      }
    }

#if DEBUG
    /// <summary>
    /// Get the chunks that this apeture is waiting to load
    /// </summary>
    /// <returns></returns>
    public abstract Coordinate[] GetQueuedChunks();

    /// <summary>
    /// Get the chunks this apeture is loading/processing
    /// </summary>
    /// <returns></returns>
    public abstract Coordinate[] GetProcessingChunks();
#endif

    ///// INTERNAL FUNCTIONS

    /// <summary>
    /// Set the level this appeture is attached to
    /// </summary>
    /// <param name="level"></param>
    internal void setLevel(Level level) {
      this.level = level;

      /// cull the apeture bounds to the max level sizes
      managedChunkRadius = Math.Min(Math.Min(level.chunkBounds.x, level.chunkBounds.z) / 2, managedChunkRadius);
      managedChunkHeightRadius = Math.Min(level.chunkBounds.y / 2, managedChunkHeightRadius);
    }

    /// <summary>
    /// enqueue new locations for this resolution manager to load
    /// </summary>
    /// <param name="chunkLocations"></param>
    protected abstract void addChunksToLoad(Coordinate[] chunkLocations);

    /// <summary>
    /// try to remove certain chunks from this resolution manager
    /// </summary>
    /// <param name="chunkLocations"></param>
    protected abstract void addChunksToUnload(Coordinate[] chunkLocations);

    /// <summary>
    /// Sort the list of chunk locations by their distance to the level's focuses.
    /// </summary>
    /// <param name="chunkLocations"></param>
    /// <returns></returns>
    protected void sortByFocusDistance(ref List<Coordinate> chunkLocations) {
      chunkLocations = chunkLocations.OrderBy(chunkLocation => {
        float closestFocusDistance = float.MaxValue;
        level.forEachFocus(focus => {
          float focusDistance = focus.chunkLocation.distance(chunkLocation);
          closestFocusDistance = focusDistance < closestFocusDistance ? focusDistance : closestFocusDistance;
        });

        return closestFocusDistance;
      }).ToList();
    }

    ///// SUB FUNCTIONS

    /// <summary>
    /// Initilize a new focus point
    /// </summary>
    /// <param name="newFocalPoint"></param>
    void initilizeFocus(ILevelFocus newFocalPoint) {
      int focusID = level.getFocusID(newFocalPoint);
      if (focusID < 0) {
        World.Debugger.logError($"Chunk manager {GetType()} tried to load a new focus of type {newFocalPoint.GetType()} that isn't registered in the level");
        return;
      }

      Coordinate[] managedChunkBounds = getManagedChunkBounds(newFocalPoint);
      managedChunkBoundsByFocusID[focusID] = managedChunkBounds;

      Coordinate[] chunksToLoad = Coordinate.GetAllPointsBetween(managedChunkBounds[0], managedChunkBounds[1]);
      addChunksToLoad(chunksToLoad);
    }

    /// <summary>
    /// Remove a focal point from management by this level
    /// </summary>
    /// <param name="focusToRemove"></param>
    void removeFocus(ILevelFocus focusToRemove) {
      int focusID = level.getFocusID(focusToRemove);
      if (focusID < 0) {
        World.Debugger.logError($"Chunk manager {GetType()} tried to load a new focus of type {focusToRemove.GetType()} that isn't registered in the level");
        return;
      }

      Coordinate[] chunksToUnLoad = Coordinate.GetAllPointsBetween(managedChunkBoundsByFocusID[focusID][0], managedChunkBoundsByFocusID[focusID][1]);
      managedChunkBoundsByFocusID.Remove(focusID);
      addChunksToUnload(chunksToUnLoad);
    }

    /// <summary>
    /// Adjust the bounds and resolution loading for the given focus.
    /// </summary>
    /// <param name="focus"></param>
    void adjustFocus(ILevelFocus focus) {
      int focusID = level.getFocusID(focus);
      if (focusID < 0) {
        World.Debugger.logError($"Chunk manager {GetType()} tried to load a new focus of type {focus.GetType()} that isn't registered in the level");
        return;
      }

      Coordinate[] newManagedChunkBounds = getManagedChunkBounds(focus);
      Coordinate[] newChunksToLoad = Coordinate.GetPointDiff(newManagedChunkBounds, managedChunkBoundsByFocusID[focusID]);
      Coordinate[] oldChunksToUnload = Coordinate.GetPointDiff(managedChunkBoundsByFocusID[focusID], newManagedChunkBounds);
      managedChunkBoundsByFocusID[focusID] = newManagedChunkBounds;

      addChunksToLoad(newChunksToLoad);
      addChunksToUnload(oldChunksToUnload);
    }

    /// <summary>
    /// Get the managed chunk bounds for the given focus.
    /// </summary>
    /// <param name="focus"></param>
    /// <returns></returns>
    Coordinate[] getManagedChunkBounds(ILevelFocus focus) {
      Coordinate focusLocation = focus.chunkLocation;
      return new Coordinate[] {
        (
          Math.Max(focusLocation.x - managedChunkRadius, 0),
          Math.Max(focusLocation.y - managedChunkHeightRadius, 0),
          Math.Max(focusLocation.z - managedChunkRadius, 0)
        ),
        (
          Math.Min(focusLocation.x + managedChunkRadius, level.chunkBounds.x),
          Math.Min(focusLocation.x + managedChunkHeightRadius, level.chunkBounds.y),
          Math.Min(focusLocation.z + managedChunkRadius, level.chunkBounds.z)
        )
      };
    }
  }
}
