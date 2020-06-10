using MeepTech.Events;
using MeepTech.Voxel.Collections.Level;
using MeepTech.Voxel.Generation.Mesh;

namespace MeepTech.Voxel.Generation.Managers {

  /// <summary>
  /// Base cunk manager for generating chunk meshes.
  /// </summary>
  public abstract class ChunkMeshGenerationManager : ChunkManager {

    /// <summary>
    /// The mesh generator this chunk generator will use
    /// </summary>
    IVoxelMeshGenerator meshGenerator;

    /// <summary>
    /// Construct
    /// </summary>
    /// <param name="level">The level this manager is managing for</param>
    public ChunkMeshGenerationManager(ILevel level, IChunkDataStorage chunkDataStorage, IVoxelMeshGenerator meshGenerator) : base(level, chunkDataStorage) {
      this.meshGenerator = meshGenerator;
    }

    /// <summary>
    /// Generate the mesh for the voxeldata at the given chunk location
    /// </summary>
    /// <param name="chunkLocation"></param>
    /// <returns></returns>
    internal IMesh generateMeshDataForChunk(Coordinate chunkLocation) {
      IVoxelChunk chunk = level.getChunk(chunkLocation, false, true, true, true);
      if (!chunk.isEmpty) {
        return meshGenerator.generateMesh(chunk);
      }

      return new Mesh.Mesh();
    }
  }
}
