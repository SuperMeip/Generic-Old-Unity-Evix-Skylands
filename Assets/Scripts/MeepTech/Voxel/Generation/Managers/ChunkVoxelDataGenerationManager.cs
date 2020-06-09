using MeepTech.Voxel.Collections.Level;
using MeepTech.Voxel.Collections.Storage;
using MeepTech.Voxel.Generation.Sources;
using System;

namespace MeepTech.Voxel.Generation.Managers {

  /// <summary>
  /// Manage the generation of voxel data for a chunk
  /// </summary>
  public abstract class ChunkVoxelDataGenerationManager<VoxelStorageType> : ChunkManager
    where VoxelStorageType : IVoxelStorage {

    /// <summary>
    /// The source to use to generate the voxels
    /// </summary>
    IVoxelSource voxelSource;

    /// <summary>
    /// Construct
    /// </summary>
    /// <param name="level">The level this manager is managing for</param>
    public ChunkVoxelDataGenerationManager(ILevel level, IChunkDataStorage chunkDataStorage, IVoxelSource voxelSource) : base(level, chunkDataStorage) {
      this.voxelSource = voxelSource;
    }

    /// <summary>
    /// Generate the chunk data for the chunk at the given location
    /// </summary>
    /// <param name="chunkLocation"></param>
    internal VoxelStorageType generateVoxelDataForChunk(Coordinate chunkLocation) {
      VoxelStorageType voxelData = (VoxelStorageType)Activator.CreateInstance(typeof(VoxelStorageType), Chunk.Diameter);
      voxelSource.generateAllAt(chunkLocation, voxelData);
      voxelData.isLoaded = true;

      return voxelData;
    }
  }
}
