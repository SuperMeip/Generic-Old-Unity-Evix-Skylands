using MeepTech.Jobs;
using MeepTech.Voxel.Collections.Level;

namespace MeepTech.Voxel.Generation.Managers {

  /// <summary>
  /// A base job for managing chunk work queues
  /// </summary>
  public abstract class ChunkQueueManagerJob<ChunkManagerType> : QueueManagerJob<Coordinate> {

    /// <summary>
    /// The level we're loading for
    /// </summary>
    public ILevel level {
      get;
      protected set;
    }

    /// <summary>
    /// The level we're loading for
    /// </summary>
    public ChunkManagerType manager {
      get;
      protected set;
    }

    /// <summary>
    /// Create a new job, linked to the level
    /// </summary>
    /// <param name="level"></param>
    protected ChunkQueueManagerJob(ILevel level, ChunkManagerType manager, int maxJobsCount = 10) : base(maxJobsCount) {
      this.level = level;
      this.manager = manager;
    }
  }
}