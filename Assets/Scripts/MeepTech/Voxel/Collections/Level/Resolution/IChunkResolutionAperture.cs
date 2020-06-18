using MeepTech.Events;

namespace MeepTech.Voxel.Collections.Level.Management {

  /// <summary>
  /// An aperture (loaded area around something) that Manages the loaded resolution of chunks for a given level within it's area.
  /// Resolution is how loaded a chunk's data is within a given area.
  /// </summary>
  public interface IChunkResolutionAperture : IObserver {

    /// <summary>
    /// The height of the managed chunk area (Y)
    /// </summary>
    int managedChunkHeightRadius {
      get;
    }

    /// <summary>
    /// The radius (in chunks) from each focus that this resolution manager manages. For X and Z
    /// </summary>
    int managedChunkRadius {
      get;
    }

    /// <summary>
    /// Get if the given chunk is within the bounds of this manager
    /// </summary>
    /// <param name="chunkLocation"></param>
    /// <returns></returns>
    bool isWithinManagedBounds(Coordinate chunkLocation);

    /// <summary>
    /// Get the managed bounds for the given focus
    /// </summary>
    /// <param name="focus"></param>
    /// <returns></returns>
    Coordinate[] getManagedBoundsForFocus(ILevelFocus focus);

#if DEBUG
    /// <summary>
    /// Used for debugging, get which chunks are currently queued
    /// </summary>
    /// <returns></returns>
    Coordinate[] getQueuedChunks();

    /// <summary>
    /// Used for debugging, get which chunks are currently being worked on/loaded by this apeture
    /// </summary>
    /// <returns></returns>
    Coordinate[] getProcessingChunks();
#endif
  }
}