namespace MeepTech.Voxel.Collections.Level {
  /// <summary>
  /// An interface for a level, used to load the block data for a level around a player/focus point
  /// </summary>
  public interface ILevel {

    /// <summary>
    /// The overall bounds of the level, max x y and z
    /// </summary>
    int seed {
      get;
    }

    /// <summary>
    /// The overall bounds of the level, max x y and z
    /// </summary>
    Coordinate chunkBounds {
      get;
    }

    /// <summary>
    /// The current focus the level is using
    /// </summary>
    Coordinate focus {
      get;
    }

    /// <summary>
    /// The width of the active chunk area in chunks
    /// </summary>
    int meshedChunkDiameter {
      get;
    }

    /// <summary>
    /// The coordinates indicating the two chunks the extreems of what chunks are to be loaded from memmory:
    ///   0: south bottom west most loaded chunk
    ///   1: north top east most loaded chunk 
    /// </summary>
    Coordinate[] loadedChunkBounds {
      get;
    }

    /// <summary>
    /// The coordinates indicating the two chunks the extreems of what chunks are to be meshed.
    ///   0: south bottom west most loaded chunk
    ///   1: north top east most loaded chunk 
    /// </summary>
    Coordinate[] meshedChunkBounds {
      get;
    }

    /// <summary>
    /// Get the chunk at the given location (if it's loaded)
    /// </summary>
    /// <param name="chunkLocation">the location of the chunk to grab</param>
    /// <param name="withMeshes">get the chunk with it's mesh</param>
    /// <param name="withNeighbors">get the chunk with neighbors linked</param>
    /// <param name="withNeighborsNeighbors">get the neightbors of the neighbors as well</param>
    /// <returns>the chunk data or null if there's none loaded</returns>
    IVoxelChunk getChunk(Coordinate chunkLocation, bool withMesh = false, bool withNeighbors = false, bool withNeighborsNeighbors = false, bool fullNeighborEncasement = false);

    /// <summary>
    /// </summary>
    /// <param name="centerChunkLocation">the center point/focus of the loaded chunks, usually a player location</param>
    void initializeAround(Coordinate centerChunkLocation);

    /// <summary>
    /// Adjust the level's loaded focus to a new location
    /// </summary>
    /// <param name="newFocus"></param>
    void adjustFocusTo(Coordinate newFocus);

    /// <summary>
    /// Get if the given chunkLocation is loaded
    /// </summary>
    /// <param name="chunkLocation"></param>
    /// <returns></returns>
    bool chunkIsWithinLoadedBounds(Coordinate chunkLocation);

    /// <summary>
    /// Get if the given chunkLocation should be meshed
    /// </summary>
    /// <param name="chunkLocation"></param>
    /// <returns></returns>
    bool chunkIsWithinkMeshedBounds(Coordinate chunkLocation);

    /// <summary>
    /// Get stats for the managers used to build this level
    /// </summary>
    /// <returns></returns>
    string getManagerStats();
  }
}