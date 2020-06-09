using MeepTech.Voxel.Collections.Storage;
using MeepTech.Voxel.Generation.Mesh;

namespace MeepTech.Voxel.Collections.Level {

  /// <summary>
  /// Base clase for chunk data storage. Voxel and mesh
  /// </summary>
  public abstract class ChunkDataStorage : IChunkDataStorage {

    /// <summary>
    /// Set the given voxeldata to the given chunk location in this level's active storage/memmory
    /// </summary>
    /// <param name="chunkLocation"></param>
    /// <param name="voxelData"></param>
    public abstract void setChunkVoxelData(Coordinate chunkLocation, IVoxelStorage voxelData);

    /// <summary>
    /// Remove/nullify data for the chunk at the given location
    /// </summary>
    /// <param name="chunkLocation"></param>
    public abstract void removeChunkVoxelData(Coordinate chunkLocation);

    /// <summary>
    /// Return if this level contains a loaded chunk mesh for the given chunk
    /// </summary>
    /// <param name="chunkLocation"></param>
    /// <returns></returns>
    public abstract bool containsChunkMesh(Coordinate chunkLocation);

    /// <summary>
    /// add the loaded chunk mesh data to this level
    /// </summary>
    /// <param name="chunkLocation"></param>
    public abstract void setChunkMesh(Coordinate chunkLocation, IMesh chunkMesh);

    /// <summary>
    /// Remove/nullify data for the loaded chunk mesh at the given location
    /// </summary>
    /// <param name="chunkLocation"></param>
    public abstract void removeChunkMesh(Coordinate chunkLocation);

    /// <summary>
    /// get the chunk's voxel data for the given location
    /// </summary>
    /// <param name="chunkLocation"></param>
    /// <returns></returns>
    public abstract IVoxelStorage getChunkVoxelData(Coordinate chunkLocation);

    /// <summary>
    /// Return the chunk mesh loaded for the given chunk
    /// </summary>
    /// <param name="chunkLocation"></param>
    /// <returns></returns>
    public abstract IMesh getChunkMesh(Coordinate chunkLocation);
  }
}
