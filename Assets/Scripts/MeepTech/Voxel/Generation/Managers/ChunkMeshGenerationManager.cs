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

    /// <summary>
    /// An event indicating a chunk has finished generating it's mesh and is ready to render
    /// </summary>
    public struct ChunkMeshGenerationFinishedEvent : IEvent {

      /// <summary>
      /// The chunk location of the chunk that's finished generating it's mesh
      /// </summary>
      public Coordinate chunkLocation {
        get;
      }

      /// <summary>
      /// The name of this event
      /// </summary>
      public string name => "Chunk Mesh Has Finished Generating";

      /// <summary>
      /// Create a new event indicating a chunk has finished generating it's mesh
      /// </summary>
      /// <param name="chunkLocation"></param>
      public ChunkMeshGenerationFinishedEvent(Coordinate chunkLocation) {
        this.chunkLocation = chunkLocation;
      }
    }

    /// <summary>
    /// An event indicating a chunk has finished generating it's mesh and is ready to render
    /// </summary>
    public struct ChunkMeshGenerationSkippedEvent : IEvent {

      /// <summary>
      /// The chunk location of the chunk that's finished generating it's mesh
      /// </summary>
      public Coordinate chunkLocation {
        get;
      }

      /// <summary>
      /// The name of this event
      /// </summary>
      public string name => "Chunk Empty, No Mesh To Generate";

      /// <summary>
      /// Create a new event indicating a chunk has finished generating it's mesh
      /// </summary>
      /// <param name="chunkLocation"></param>
      public ChunkMeshGenerationSkippedEvent(Coordinate chunkLocation) {
        this.chunkLocation = chunkLocation;
      }
    }
  }
}
