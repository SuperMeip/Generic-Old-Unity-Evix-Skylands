using MeepTech.Events;
using MeepTech.GamingBasics;
using MeepTech.Voxel.Collections.Level;
using MeepTech.Voxel.Collections.Storage;
using MeepTech.Voxel.Generation.Sources;
using System.Threading;

namespace MeepTech.Voxel.Generation.Managers {

  /// <summary>
  /// Job based chunk voxel generation
  /// </summary>
  /// <typeparam name="VoxelStorageType"></typeparam>
  class JobBasedChunkVoxelDataGenManager<VoxelStorageType> : ChunkVoxelDataGenerationManager<VoxelStorageType>
    where VoxelStorageType : VoxelStorage {

    /// <summary>
    /// The job used to generate chunks
    /// </summary>
    JGenerateChunks chunkGenerationJobManager;

#if DEBUG
    ///// MANAGER STATS
    int totalRequestsRecieved = 0;
    int chunksDroppedForOurOfFocus = 0;
    internal int requestsProcessedByJobs = 0;
    internal int requestsSucessfullyProcessedByJobs = 0;
    internal int alreadyNonEmptyChunksDropped = 0;
    internal int chunkDataGeneratedFromSource = 0;
    internal int generatedEmptyChunks = 0;
#endif

    /// <summary>
    /// Construct
    /// </summary>
    /// <param name="level">The level this manager is managing for</param>
    public JobBasedChunkVoxelDataGenManager(ILevel level, IChunkDataStorage chunkDataStorage, IVoxelSource voxelSource) : base(level, chunkDataStorage, voxelSource) {
      chunkGenerationJobManager = new JGenerateChunks(level, this);
    }

    /// <summary>
    /// Listen for events
    /// </summary>
    /// <param name="event"></param>
    /// <param name="origin"></param>
    public override void notifyOf(IEvent @event, IObserver origin = null) {
      switch(@event) {
        // if chunk data wasn't found in a file, lets generate it for them
        case ChunkDataNotFoundInFilesEvent cfdlmcdnfife:
          chunkGenerationJobManager.enQueue(new Coordinate[] { cfdlmcdnfife.chunkLocation });
#if DEBUG
          Interlocked.Increment(ref totalRequestsRecieved);
#endif
          break;
        default:
          return;
      }
      base.notifyOf(@event, origin);
    }

    /// <summary>
    /// Abort the manager jobs
    /// </summary>
    public override void killAll() {
      chunkGenerationJobManager.abort();
    }

#if DEBUG
    /// <summary>
    /// Manager stats
    /// </summary>
    /// <returns></returns>
    protected override (double, string)[] provideManagerStats() {
      return new (double, string)[] {
        (totalRequestsRecieved, "Total Requests Recieved"),
        (chunksDroppedForOurOfFocus, "Chunks Droped For Going Out Of Focus"),
        (requestsProcessedByJobs, "Total Jobs Assigned"),
        (requestsSucessfullyProcessedByJobs, "Sucessfully Processed Jobs"),
        (alreadyNonEmptyChunksDropped, "Pre-Non-Empty Chunks Dropped By Job"),
        (chunkDataGeneratedFromSource, "Total Chunks Generated From Source"),
        (generatedEmptyChunks, "Empty Chunks Generated"),
        (chunkGenerationJobManager.queueCount, "Currently Queued Items")
      };
    }
#endif

    /// <summary>
    /// A job to load all chunks from the loading queue
    /// </summary>
    class JGenerateChunks : ChunkQueueManagerJob<JobBasedChunkVoxelDataGenManager<VoxelStorageType>> {

      /// <summary>
      /// Create a new job, linked to the level
      /// </summary>
      /// <param name="level"></param>
      public JGenerateChunks(ILevel level, JobBasedChunkVoxelDataGenManager<VoxelStorageType> manager) : base(level, manager) {
        threadName = "Generate Chunk Manager";
      }

      /// <summary>
      /// Get the correct child job
      /// </summary>
      /// <param name="chunkColumnLocation"></param>
      /// <param name="parentCancellationSources"></param>
      /// <returns></returns>
      protected override QueueTaskChildJob<Coordinate> getChildJob(Coordinate chunkColumnLocation) {
        return new JGenerateChunk(this, chunkColumnLocation);
      }

      /// <summary>
      /// remove empty chunks that are not in the load area from the load queue
      /// </summary>
      /// <param name="chunkLocation"></param>
      /// <returns></returns>
      protected override bool isAValidQueueItem(Coordinate chunkLocation) {
        if (level.chunkIsWithinLoadedBounds(chunkLocation)) {
          return true;
        } else {
#if DEBUG
          Interlocked.Increment(ref manager.chunksDroppedForOurOfFocus);
#endif
          return false;
        }
      }

      /// <summary>
      /// A Job for generating a new column of chunks into a level
      /// </summary>
      class JGenerateChunk : QueueTaskChildJob<Coordinate> {

        /// <summary>
        /// The level we're loading for
        /// </summary>
        protected new JGenerateChunks jobManager;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="level"></param>
        /// <param name="chunkLocation"></param>
        /// <param name="parentCancellationSources"></param>
        internal JGenerateChunk(
          JGenerateChunks jobManager,
          Coordinate chunkLocation
        ) : base(chunkLocation, jobManager) {
          this.jobManager = jobManager;
          threadName = "Generating voxels for chunk: " + chunkLocation;
        }

        /// <summary>
        /// Threaded function, loads all the voxel data for this chunk
        /// </summary>
        protected override void doWork(Coordinate chunkLocation) {
#if DEBUG
          Interlocked.Increment(ref jobManager.manager.requestsProcessedByJobs);
#endif
          // if the chunk is empty, lets try to fill it.
          IVoxelChunk chunk = jobManager.level.getChunk(chunkLocation);
          if (chunk.isEmpty && !chunk.isLoaded) {
            VoxelStorageType voxelData = jobManager.manager.generateVoxelDataForChunk(chunkLocation);
            jobManager.manager.chunkDataStorage.setChunkVoxelData(chunkLocation, voxelData);
#if DEBUG
            Interlocked.Increment(ref jobManager.manager.chunkDataGeneratedFromSource);
#endif
            if (!voxelData.isEmpty) {
              World.EventSystem.notifyChannelOf(
                new ChunkDataLoadingFinishedEvent(chunkLocation),
                Evix.EventSystems.WorldEventSystem.Channels.TerrainGeneration
              );
            }
#if DEBUG
            else {
              Interlocked.Increment(ref jobManager.manager.generatedEmptyChunks);
            }
            Interlocked.Increment(ref jobManager.manager.requestsSucessfullyProcessedByJobs);
#endif
          }
#if DEBUG
          else {
            Interlocked.Increment(ref jobManager.manager.alreadyNonEmptyChunksDropped);
            //World.Debugger.log($"Tried to generate the voxels for a non empty chunk: {chunkLocation.ToString()}");
          }
#endif
        }
      }
    }
  }
}
