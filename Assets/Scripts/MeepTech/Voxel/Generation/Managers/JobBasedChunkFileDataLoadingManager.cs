using MeepTech.Voxel.Collections.Level;
using MeepTech.Voxel.Collections.Storage;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using MeepTech.GamingBasics;
using Evix.EventSystems;
using System.Threading;

namespace MeepTech.Voxel.Generation.Managers {

  /// <summary>
  /// A manager (message handler + doer) for loading chunks fom files.
  /// </summary>
  /// <typeparam name="VoxelStorageType"></typeparam>
  public class JobBasedChunkFileDataLoadingManager<VoxelStorageType> : ChunkFileDataLoadingManager<VoxelStorageType>
    where VoxelStorageType : IVoxelStorage {

    /// <summary>
    /// The current parent job, in charge of loading the chunks in the load queue
    /// </summary>
    JLoadChunks chunkLoadQueueManagerJob;

    /// <summary>
    /// The current parent job, in charge of loading the chunks in the load queue
    /// </summary>
    JUnloadChunks chunkUnloadQueueManagerJob;

#if DEBUG
    ///// MANAGER STATS
    int totalRequestsRecieved = 0;
    int chunksDroppedForOurOfFocus = 0;
    internal int requestsProcessedByJobs = 0;
    internal int requestsSucessfullyProcessedByJobs = 0;
    internal int alreadyNonEmptyChunksDropped = 0;
    internal int chunkDataLoadedFromFiles = 0;
    internal int loadedEmptyChunkDataFiles = 0;
    int chunkDataFilesNotFound = 0;
    int chunkFileNotFoundNotificationsSent = 0;
#endif

    /// <summary>
    /// construct
    /// </summary>
    public JobBasedChunkFileDataLoadingManager(ILevel level, IChunkDataStorage chunkDataStorage) : base (level, chunkDataStorage) {
      chunkLoadQueueManagerJob = new JLoadChunks(level, this);
      chunkUnloadQueueManagerJob = new JUnloadChunks(level, this);
    }

    /// <summary>
    /// Add a list of chunks that we want to load from file
    /// </summary>
    /// <param name="chunkLocations"></param>
    public override void addChunksToLoad(Coordinate[] chunkLocations) {
      new Thread(() => {
#if DEBUG
        Interlocked.Add(ref totalRequestsRecieved, chunkLocations.Length);
#endif
        chunkLoadQueueManagerJob.enQueue(chunkLocations);
        chunkUnloadQueueManagerJob.deQueue(chunkLocations);
      }) { Name = "Add Chunks To File Loading Queue" }.Start();
    }

    /// <summary>
    /// Adda list of chunks we want to unload to file storage
    /// </summary>
    /// <param name="chunkLocations"></param>
    public override void addChunksToUnload(Coordinate[] chunkLocations) {
      new Thread(() => {
        chunkUnloadQueueManagerJob.enQueue(chunkLocations);
        chunkLoadQueueManagerJob.deQueue(chunkLocations);
      }) { Name = "Add Chunks To File Un-Loading Queue" }.Start();
    }

    /// <summary>
    /// Abort the manager jobs
    /// </summary>
    public override void killAll() {
      chunkLoadQueueManagerJob.abort();
      chunkUnloadQueueManagerJob.abort();
    }

#if DEBUG
    /// <summary>
    /// This manager's stats
    /// </summary>
    /// <returns></returns>
    protected override (double, string)[] provideManagerStats() {
      return new (double, string)[] {
        (totalRequestsRecieved, "Total Requests Recieved"),
        (requestsProcessedByJobs, "Total Jobs Assigned"),
        (chunksDroppedForOurOfFocus, "Chunks Droped For Going Out Of Focus"),
        (requestsSucessfullyProcessedByJobs, "Sucessfully Processed Jobs"),
        (alreadyNonEmptyChunksDropped, "Pre-Non-Empty Chunks Dropped By Job"),
        (chunkDataLoadedFromFiles, "Total Chunk Data Files Loaded"),
        (loadedEmptyChunkDataFiles, "Empty Chunk Data Files Loaded"),
        (chunkDataFilesNotFound, "Save Data File Not Found For Chunks"),
        (chunkFileNotFoundNotificationsSent, "File Not Found Notifications Sent"),
        (chunkLoadQueueManagerJob.queueCount, "Currently Queued Items")
      };
    }
#endif

    /// <summary>
    /// A job to load all chunks from the loading queue
    /// </summary>
    class JLoadChunks : ChunkQueueManagerJob<JobBasedChunkFileDataLoadingManager<VoxelStorageType>> {

      /// <summary>
      /// Create a new job, linked to the level
      /// </summary>
      /// <param name="level"></pa
      public JLoadChunks(ILevel level, JobBasedChunkFileDataLoadingManager<VoxelStorageType> manager) : base(level, manager) {
        threadName = "Load Chunk Manager";
      }

      /// <summary>
      /// Get the correct child job
      /// </summary>
      /// <returns></returns>
      protected override QueueTaskChildJob<Coordinate> getChildJob(Coordinate chunkColumnLocation) {
        return new JLoadChunksFromFile(this, chunkColumnLocation);
      }

      /// <summary>
      /// Override to shift generational items to their own queue
      /// </summary>
      /// <param name="chunkLocation"></param>
      /// <returns></returns>
      protected override bool isAValidQueueItem(Coordinate chunkLocation) {
        return false;
        // if this doesn't have a loaded file, remove it from this queue and load it in the generation one
        /*if (level.chunkIsWithinLoadedBounds(chunkLocation)) {
          if (!File.Exists(manager.getChunkFileName(chunkLocation))) {
#if DEBUG
            Interlocked.Increment(ref manager.chunkDataFilesNotFound);
#endif
            return false;
          }
        } else {
#if DEBUG
          Interlocked.Increment(ref manager.chunksDroppedForOurOfFocus);
#endif
          return false;
        }

        return base.isAValidQueueItem(chunkLocation);*/
      }

      /// <summary>
      /// Do something on an invalid item before we toss it out
      /// </summary>
      /// <param name="chunkLocation"></param>
      protected override void onQueueItemInvalid(Coordinate chunkLocation) {
        if (level.chunkIsWithinLoadedBounds(chunkLocation)) {
          World.EventSystem.notifyChannelOf(
            new ChunkDataNotFoundInFilesEvent(chunkLocation),
            WorldEventSystem.Channels.TerrainGeneration
          );
#if DEBUG
          Interlocked.Increment(ref manager.chunkFileNotFoundNotificationsSent);
#endif
        }
      }

      /// <summary>
      /// Sort the queue by distance from the focus of the level
      /// </summary>
      protected override void sortQueue() {
        queue = queue.OrderBy(o => o.distance(level.focus.chunkLocation)).ToList();
      }

      /// <summary>
      /// A Job for loading the data for a column of chunks into a level from file
      /// </summary>
      class JLoadChunksFromFile : QueueTaskChildJob<Coordinate> {

        /// <summary>
        /// The level we're loading for
        /// </summary>
        protected new JLoadChunks jobManager;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="level"></param>
        /// <param name="chunkColumnLocation"></param>
        /// <param name="parentCancellationSources"></param>
        internal JLoadChunksFromFile(
          JLoadChunks jobManager,
          Coordinate chunkColumnLocation
        ) : base(chunkColumnLocation, jobManager) {
          this.jobManager = jobManager;
          threadName = "Load Column: " + chunkColumnLocation;
        }

        /// <summary>
        /// Threaded function, loads all the voxel data for this chunk
        /// </summary>
        protected override void doWork(Coordinate chunkLocation) {
#if DEBUG
          Interlocked.Increment(ref jobManager.manager.requestsProcessedByJobs);
#endif
          if (jobManager.level.getChunk(chunkLocation).isEmpty) {
            VoxelStorageType voxelData = jobManager.manager.getVoxelDataForChunkFromFile(chunkLocation);
            jobManager.manager.chunkDataStorage.setChunkVoxelData(chunkLocation, voxelData);
#if DEBUG
            Interlocked.Increment(ref jobManager.manager.chunkDataLoadedFromFiles);
#endif
            if (voxelData != null && !voxelData.isEmpty) {
              World.EventSystem.notifyChannelOf(
                new ChunkDataLoadingFinishedEvent(chunkLocation),
                WorldEventSystem.Channels.TerrainGeneration
            );
            } else {
#if DEBUG
              Interlocked.Increment(ref jobManager.manager.loadedEmptyChunkDataFiles);
#endif
            }
#if DEBUG
            Interlocked.Increment(ref jobManager.manager.requestsSucessfullyProcessedByJobs);
#endif
          } else {
#if DEBUG
            Interlocked.Increment(ref jobManager.manager.alreadyNonEmptyChunksDropped);
            //World.Debugger.log($"Tried to load the voxels for a non-empty chunk: {chunkLocation.ToString()}");
#endif
          }
        }
      }
    }

    /// <summary>
    /// A job to un-load and serialize all chunks from the unloading queue
    /// </summary>
    class JUnloadChunks : ChunkQueueManagerJob<JobBasedChunkFileDataLoadingManager<VoxelStorageType>> {

      /// <summary>
      /// Create a new job, linked to the level
      /// </summary>
      /// <param name="level"></param>
      public JUnloadChunks(ILevel level, JobBasedChunkFileDataLoadingManager<VoxelStorageType> manager) : base(level, manager) {
        threadName = "Unload Chunk Manager";
      }

      /// <summary>
      /// Get the child job
      /// </summary>
      /// <param name="chunkColumnLocation"></param>
      /// <param name="parentCancellationSources"></param>
      /// <returns></returns>
      protected override QueueTaskChildJob<Coordinate> getChildJob(Coordinate chunkColumnLocation) {
        return new JUnloadChunkToFile(this, chunkColumnLocation);
      }

      /// <summary>
      /// A Job for un-loading the data for a column of chunks into a serialized file
      /// </summary>
      class JUnloadChunkToFile : QueueTaskChildJob<Coordinate> {

        /// <summary>
        /// The level we're loading for
        /// </summary>
        protected new JUnloadChunks jobManager;

        /// <summary>
        /// Make a new job
        /// </summary>
        /// <param name="level"></param>
        /// <param name="chunkColumnLocation"></param>
        /// <param name="resourcePool"></param>
        internal JUnloadChunkToFile(
          JUnloadChunks jobManager,
          Coordinate chunkColumnLocation
        ) : base(chunkColumnLocation, jobManager) {
          this.jobManager = jobManager;
          threadName = "Unload chunk to file: " + queueItem.ToString();
        }

        /// <summary>
        /// Threaded function, serializes this chunks voxel data and removes it from the level
        /// </summary>
        protected override void doWork(Coordinate chunkLocation) {
          jobManager.manager.saveChunkDataToFile(chunkLocation);
          jobManager.manager.chunkDataStorage.removeChunkVoxelData(chunkLocation);
          jobManager.manager.chunkDataStorage.removeChunkMesh(chunkLocation);
        }
      }
    }
  }
}
