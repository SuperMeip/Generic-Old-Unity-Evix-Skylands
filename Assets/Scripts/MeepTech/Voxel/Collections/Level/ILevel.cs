using MeepTech.Events;
using MeepTech.Voxel.Collections.Level.Management;
using System;

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
    /// Get the chunk at the given location (if it's loaded)
    /// </summary>
    /// <param name="chunkLocation">the location of the chunk to grab</param>
    /// <param name="withMeshes">get the chunk with it's mesh</param>
    /// <param name="withNeighbors">get the chunk with neighbors linked</param>
    /// <param name="withNeighborsNeighbors">get the neightbors of the neighbors as well</param>
    /// <returns>the chunk data or null if there's none loaded</returns>
    IVoxelChunk getChunk(Coordinate chunkLocation, bool withMesh = false, bool withNeighbors = false, bool withNeighborsNeighbors = false, bool fullNeighborEncasement = false);

    /// <summary>
    /// Add a new focus into this level
    /// </summary>
    /// <param name=""></param>
    void spawnFocus(ILevelFocus newFocus);

    /// <summary>
    /// Get the id the level is using for the given focus
    /// </summary>
    /// <param name="focus"></param>
    /// <returns></returns>
    int getFocusID(ILevelFocus focus);

    /// <summary>
    /// Get the level focus based on it's id.
    /// </summary>
    /// <param name="focus"></param>
    /// <returns></returns>
    ILevelFocus getFocusByID(int focusID);

    /// <summary>
    /// performa an action on each focus
    /// </summary>
    /// <param name=""></param>
    void forEachFocus(Action<ILevelFocus> action);

    /// <summary>
    /// Get the interface for the aperture this level is using for the given chunk resolution layer
    /// </summary>
    /// <returns></returns>
    IChunkResolutionAperture getApetureForResolutionLayer(Level.FocusResolutionLayers resolutionLayer);
  }
}