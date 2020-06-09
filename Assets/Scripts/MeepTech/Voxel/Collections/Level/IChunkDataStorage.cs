using MeepTech.Voxel.Collections.Storage;
using MeepTech.Voxel.Generation.Mesh;

namespace MeepTech.Voxel.Collections.Level {

  /// <summary>
  /// Manages storage for chunk mesh and voxel data for a Level
  /// </summary>
  public interface IChunkDataStorage {

    /// <summary>
    /// get the given set of voxel from the chunk location
    /// </summary>
    /// <param name="chunkLocation"></param>
    IVoxelStorage getChunkVoxelData(Coordinate chunkLocation);

    /// <summary>
    /// Set the given set of voxel data to the given chunk location
    /// </summary>
    /// <param name="chunkLocation"></param>
    /// <param name="voxelData"></param>
    void setChunkVoxelData(Coordinate chunkLocation, IVoxelStorage voxelData);

    /// <summary>C:\Users\super\Projects\Unity\VoxelMarchingCubeTerrain\Assets\Scripts\MeepTech\Voxel\
    /// Remove the chunk voxel data at the given location
    /// </summary>
    /// <param name="chunkLocation"></param>
    void removeChunkVoxelData(Coordinate chunkLocation);

    /// <summary>
    /// Get the mesh data for the given chunk
    /// </summary>
    /// <param name="chunkLocation"></param>
    /// <returns></returns>
    IMesh getChunkMesh(Coordinate chunkLocation);

    /// <summary>
    /// Set the chunk mesh for the given chunk location
    /// </summary>
    /// <param name="chunkLocation"></param>
    /// <param name="chunkMesh"></param>
    void setChunkMesh(Coordinate chunkLocation, IMesh chunkMesh);

    /// <summary>
    /// Clear the chunk mesh for the given chunk location
    /// </summary>
    /// <param name="chunkLocation"></param>
    /// <param name="chunkMesh"></param>
    void removeChunkMesh(Coordinate chunkLocation);

    /// <summary>
    /// if this contains a mesh for the given chunk.
    /// </summary>
    /// <param name="chunkLocation"></param>
    /// <param name="chunkMesh"></param>
    bool containsChunkMesh(Coordinate chunkLocation);
  }
}
