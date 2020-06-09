using MeepTech.Events;
using MeepTech.GamingBasics;
using MeepTech.Voxel.Collections.Level;
using MeepTech.Voxel.Collections.Storage;
using MeepTech.Voxel.Generation.Mesh;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using static MeepTech.Voxel.Generation.Managers.ChunkMeshGenerationManager;

namespace MeepTech.Voxel.Generation.Managers {

  /// <summary>
  /// A chunk mesh generation manager that uses threaded jobs
  /// </summary>
  public class JobBasedChunkMeshGenManager : ChunkMeshGenerationManager {

    /// <summary>
    /// The current parent job, in charge of generating meshes for chunks in the load queue
    /// </summary>
    JGenerateChunkMeshes chunkMeshGenQueueManagerJob;

    ///// MANAGER STATS
    int totalRequestsRecieved = 0;
    internal int chunksDroppedForOurOfFocus = 0;
    internal int chunksDroppedForBeingEmpty = 0;
    internal int requestsProcessedByJobs = 0;
    internal int requestsSucessfullyProcessedByJobs = 0;
    internal int jobsDroppedMeshAlreadyExists = 0;
    internal int chunkMeshesGeneraged = 0;
    internal int generatedEmptyMeshes = 0;

    /// <summary>
    /// construct
    /// </summary>
    public JobBasedChunkMeshGenManager(ILevel level, IChunkDataStorage chunkDataStorage, IVoxelMeshGenerator meshGenerator) : base(level, chunkDataStorage, meshGenerator) {
      chunkMeshGenQueueManagerJob = new JGenerateChunkMeshes(level, this);
    }

    /// <summary>
    /// Listen for events
    /// </summary>
    /// <param name="event"></param>
    /// <param name="origin"></param>
    public override void notifyOf(IEvent @event, IObserver origin = null) {
      switch (@event) {
        // if chunk data wasn't found in a file, lets generate it for them
        case ChunkFileDataLoadingManager<VoxelFlatArray>.ChunkDataLoadingFinishedEvent cfdlmcdlfe:
          chunkMeshGenQueueManagerJob.enQueue(new Coordinate[] { cfdlmcdlfe.chunkLocation });
          Interlocked.Increment(ref totalRequestsRecieved);
          break;
        default:
          return;
      }
    }

    /// <summary>
    /// Manager stats
    /// </summary>
    /// <returns></returns>
    protected override (double, string)[] provideManagerStats() {
      return new (double, string)[] {
        (totalRequestsRecieved, "Total Requests Recieved"),
        (chunksDroppedForOurOfFocus, "Chunks Droped For Going Out Of Focus"),
        (chunksDroppedForBeingEmpty, "Chunks Droped For Being Loaded But Empty"),
        (requestsProcessedByJobs, "Total Jobs Assigned"),
        (requestsSucessfullyProcessedByJobs, "Sucessfully Processed Jobs"),
        (jobsDroppedMeshAlreadyExists, "Existing Chunk Mesh; Dropped By Job"),
        (chunkMeshesGeneraged, "Total Chunk Meshes Generated"),
        (generatedEmptyMeshes, "Empty Chunk Meshes Generated"),
        (chunkMeshGenQueueManagerJob.queueCount, "Currently Queued Items")
      };
    }
  }

  /// <summary>
  /// The job manager this manager uses
  /// </summary>
  class JGenerateChunkMeshes : ChunkQueueManagerJob<JobBasedChunkMeshGenManager> {

    /// <summary>
    /// Create a new job, linked to the level
    /// </summary>
    /// <param name="level"></param>
    public JGenerateChunkMeshes(ILevel level, JobBasedChunkMeshGenManager manager) : base(level, manager) {
      threadName = "Generate Chunk Mesh Manager";
    }

    /// <summary>
    /// get the child job given the values
    /// </summary>
    /// <param name="chunkLocation"></param>
    /// <param name="parentCancelationSources"></param>
    /// <returns></returns>
    protected override QueueTaskChildJob<Coordinate> getChildJob(Coordinate chunkLocation) {
      return new JGenerateChunkMesh(this, chunkLocation);
    }

    /// <summary>
    /// remove empty chunks that have loaded from the mesh gen queue
    /// </summary>
    /// <param name="chunkLocation"></param>
    /// <returns></returns>
    protected override bool isAValidQueueItem(Coordinate chunkLocation) {
      if (!level.chunkIsWithinLoadedBounds(chunkLocation)) {
        Interlocked.Increment(ref manager.chunksDroppedForOurOfFocus);
        return false;
      }
      IVoxelChunk chunk = level.getChunk(chunkLocation);
      // the chunk can't be loaded and empty, we'll generate nothing.
      if ((chunk.isLoaded && chunk.isEmpty)) {
        Interlocked.Increment(ref manager.chunksDroppedForBeingEmpty);
        return false;
      }

      return true;
    }

    /// <summary>
    /// Don't generate a mesh until a chunk's data is loaded
    /// </summary>
    /// <param name="queueItem"></param>
    /// <returns></returns>
    protected override bool itemIsReady(Coordinate chunkLocation) {
      if (level.chunkIsWithinkMeshedBounds(chunkLocation)) {
        IVoxelChunk chunk = level.getChunk(chunkLocation, false, true, true, true);
        return chunk.isLoaded && chunk.neighborsNeighborsAreLoaded;
      } else return false;
    }

    /// <summary>
    /// Sort the queue by distance from the focus of the level
    /// </summary>
    protected override void sortQueue() {
      Coordinate[] sortedQueue = queue.OrderBy(o => o.distance(level.focus)).ToArray();
      queue = new ConcurrentQueue<Coordinate>(sortedQueue);
    }

    /// <summary>
    /// Child job for doing work on the chunk columns
    /// </summary>
    protected class JGenerateChunkMesh : QueueTaskChildJob<Coordinate> {

      /// <summary>
      /// The level we're loading for
      /// </summary>
      protected new JGenerateChunkMeshes jobManager;

      /// <summary>
      /// Make a new job
      /// </summary>
      /// <param name="level"></param>
      /// <param name="chunkLocation"></param>
      internal JGenerateChunkMesh(
        JGenerateChunkMeshes jobManager,
        Coordinate chunkLocation
      ) : base(chunkLocation, jobManager) {
        this.jobManager = jobManager;
        threadName = "Generate Mesh on Chunk: " + queueItem.ToString();
      }

      /// <summary>
      /// generate the chunk mesh if the level doesn't have it yet.
      /// </summary>
      protected override void doWork(Coordinate chunkLocation) {
        Interlocked.Increment(ref jobManager.manager.requestsProcessedByJobs);
        if (!jobManager.manager.chunkDataStorage.containsChunkMesh(chunkLocation)) {
          IMesh mesh = jobManager.manager.generateMeshDataForChunk(chunkLocation);
          Interlocked.Increment(ref jobManager.manager.chunkMeshesGeneraged);
          if (!mesh.isEmpty) {
            jobManager.manager.chunkDataStorage.setChunkMesh(chunkLocation, mesh);
            World.EventSystem.notifyChannelOf(
              new ChunkMeshGenerationFinishedEvent(chunkLocation),
              Evix.EventSystems.WorldEventSystem.Channels.TerrainGeneration
            );
          } else {
            Interlocked.Increment(ref jobManager.manager.generatedEmptyMeshes);
          }
          Interlocked.Increment(ref jobManager.manager.requestsSucessfullyProcessedByJobs);
        } else {
          Interlocked.Increment(ref jobManager.manager.jobsDroppedMeshAlreadyExists);
        }
      }
    }
  }
}