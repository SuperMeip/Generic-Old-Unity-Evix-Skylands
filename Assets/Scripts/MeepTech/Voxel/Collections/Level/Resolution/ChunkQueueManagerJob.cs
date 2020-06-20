using MeepTech.Jobs;

namespace MeepTech.Voxel.Collections.Level.Management {

  /// <summary>
  /// A base job for managing chunk work queues
  /// </summary>
  public abstract class ChunkQueueManagerJob<ChunkManagerType> : PriorityQueueManagerJob<float, Coordinate>
    where ChunkManagerType : IChunkResolutionAperture {

    /// <summary>
    /// The level we're loading for
    /// </summary>
    public ChunkManagerType chunkManager {
      get;
      protected set;
    }

    /// <summary>
    /// Create a new job, linked to the level via it's manager
    /// </summary>
    protected ChunkQueueManagerJob(ChunkManagerType manager) : base() {
      chunkManager = manager;
    }
  }
}