using MeepTech.Events;
using MeepTech.GamingBasics;
using MeepTech.Jobs;
using MeepTech.Voxel.Generation.Mesh;
using System.Linq;
using System.Threading;

namespace MeepTech.Voxel.Collections.Level.Management {

  /// <summary>
  /// A chunk resolution manager for the mesh data
  /// </summary>
  internal class LoadedChunkMeshDataResolutionAperture : ChunkResolutionAperture {

    /// <summary>
    /// The current parent job, in charge of generating meshes for chunks in the load queue
    /// </summary>
    JGenerateChunkMeshes chunkMeshGenQueueManagerJob;

    ///// CONSTRUCTORS

    /// <summary>
    /// Construct
    /// </summary>
    /// <param name="level"></param>
    /// <param name="managedChunkRadius"></param>
    /// <param name="managedChunkHeight"></param>
    internal LoadedChunkMeshDataResolutionAperture(int managedChunkRadius, int managedChunkHeight = 0)
    : base(managedChunkRadius, managedChunkHeight) {
      chunkMeshGenQueueManagerJob = new JGenerateChunkMeshes(this);
    }

    ///// PUBLIC FUNCTIONS

    /// <summary>
    /// Listen for notifications
    /// </summary>
    /// <param name="event"></param>
    /// <param name="origin"></param>
    public override void notifyOf(IEvent @event, IObserver origin = null) {
      switch (@event) {
        /// if a chunk is waiting on a mesh to be active, and it's not currently loading or loaded, lets load it.
        case ActivateGameobjectResolutionAperture.ChunkWaitingForActiveMissingMeshEvent swfamme:
          if (!getProcessingChunks().Concat(getQueuedChunks()).Contains(swfamme.chunkLocation)) {
            addChunksToLoad(new Coordinate[] { swfamme.chunkLocation });
          }
          break;
        default:
          base.notifyOf(@event, origin);
          break;
      }
    }

#if DEBUG
    /// <summary>
    /// Get the chunks that this apeture is waiting to load
    /// </summary>
    /// <returns></returns>
    public override Coordinate[] getQueuedChunks() {
      return chunkMeshGenQueueManagerJob.getAllQueuedItems();
    }

    /// <summary>
    /// Get the chunks this apeture is loading/processing
    /// </summary>
    /// <returns></returns>
    public override Coordinate[] getProcessingChunks() {
      return chunkMeshGenQueueManagerJob.getAllItemsWithRunningJobs();
    }
#endif

    ///// INTERNAL FUNCTIONS

    /// <summary>
    /// add chunks to this resolution layer
    /// </summary>
    /// <param name="chunkLocations"></param>
    protected override void addChunksToLoad(Coordinate[] chunkLocations) {
      chunkMeshGenQueueManagerJob.enqueue(chunkLocations);
    }

    /// <summary>
    /// remove chunks from this resolution layer
    /// </summary>
    /// <param name="chunkLocations"></param>
    protected override void addChunksToUnload(Coordinate[] chunkLocations) {
      chunkMeshGenQueueManagerJob.dequeue(chunkLocations);
      new Thread(() => {
        foreach (Coordinate chunkLocation in chunkLocations) {
          World.EventSystem.notifyChannelOf(
            new ChunkMeshMovedOutOfFocusEvent(chunkLocation),
            Evix.EventSystems.WorldEventSystem.Channels.ChunkActivationUpdates
          );
        }
      }) { Name = "De-Mesh Chunks Messenger" }.Start();
    }

    /// <summary>
    /// Generate the mesh for the voxeldata at the given chunk location
    /// </summary>
    /// <param name="chunkLocation"></param>
    /// <returns></returns>
    internal IMesh generateMeshDataForChunk(Coordinate chunkLocation) {
      IVoxelChunk chunk = level.getChunk(chunkLocation, false, true, true, true);
      if (!chunk.isEmpty) {
        return level.meshGenerator.generateMesh(chunk);
      }

      return new Mesh();
    }

    /// <summary>
    /// The job manager this manager uses
    /// </summary>
    class JGenerateChunkMeshes : ChunkQueueManagerJob<LoadedChunkMeshDataResolutionAperture> {

      /// <summary>
      /// Create a new job, linked to the level
      /// </summary>
      /// <param name="level"></param>
      public JGenerateChunkMeshes(LoadedChunkMeshDataResolutionAperture manager) : base(manager, 25) {
        threadName = "Generate Chunk Mesh Manager";
      }

      /// <summary>
      /// get the child job given the values
      /// </summary>
      /// <param name="chunkLocation"></param>
      /// <param name="parentCancelationSources"></param>
      /// <returns></returns>
      protected override IThreadedJob getChildJob(Coordinate chunkLocation) {
        return new JGenerateChunkMesh(this, chunkLocation);
      }

      /// <summary>
      /// remove empty chunks that have loaded from the mesh gen queue
      /// </summary>
      /// <param name="chunkLocation"></param>
      /// <returns></returns>
      protected override bool isAValidQueueItem(Coordinate chunkLocation) {
        if (!chunkManager.isWithinManagedBounds(chunkLocation)) {
          World.Debugger.log($"{threadName} dropped {chunkLocation} due to it being out of bounds");
          return false;
        }

        IVoxelChunk chunk = chunkManager.level.getChunk(chunkLocation);
        // the chunk can't be loaded and empty, we'll generate nothing.
        if (chunk.isLoaded && chunk.isEmpty) {
          World.Debugger.log($"{threadName} dropped {chunkLocation} due to it being loaded and empty");
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
        IVoxelChunk chunk = chunkManager.level.getChunk(chunkLocation, false, true, true, true);
        return chunk.isLoaded && chunk.neighborsNeighborsAreLoaded;
      }

      /// <summary>
      /// sort items by the focus area they're in?
      /// </summary>
      protected override void sortQueue() {
        chunkManager.sortByFocusDistance(ref queue);
      }

      /// <summary>
      /// Child job for doing work on the chunk columns
      /// </summary>
      protected class JGenerateChunkMesh : QueueTaskChildJob<JGenerateChunkMeshes, Coordinate> {

        /// <summary>
        /// Make a new job
        /// </summary>
        /// <param name="level"></param>
        /// <param name="chunkLocation"></param>
        internal JGenerateChunkMesh(
          JGenerateChunkMeshes jobManager,
          Coordinate chunkLocation
        ) : base(chunkLocation, jobManager) {
          threadName = "Generate Mesh on Chunk: " + queueItem.ToString();
        }

        /// <summary>
        /// generate the chunk mesh if the level doesn't have it yet.
        /// </summary>
        protected override void doWork(Coordinate chunkLocation) {
          // if we don't have a mesh yet, generate one
          if (!jobManager.chunkManager.level.chunkDataStorage.containsChunkMesh(chunkLocation)) {
            IMesh mesh = jobManager.chunkManager.generateMeshDataForChunk(chunkLocation);
            jobManager.chunkManager.level.chunkDataStorage.setChunkMesh(chunkLocation, mesh);
            World.EventSystem.notifyChannelOf(
              new ChunkMeshLoadingFinishedEvent(chunkLocation),
              Evix.EventSystems.WorldEventSystem.Channels.ChunkActivationUpdates
            );
          // if we already have a mesh, just send off the finished loading notification
          } else {
            World.EventSystem.notifyChannelOf(
              new ChunkMeshLoadingFinishedEvent(chunkLocation),
              Evix.EventSystems.WorldEventSystem.Channels.ChunkActivationUpdates
            );
          }
        }
      }
    }

    /// <summary>
    /// An event indicating a a chunk's mesh is stored and loaded
    /// </summary>
    public struct ChunkMeshLoadingFinishedEvent : IEvent {

      /// <summary>
      /// The chunk location of the chunk
      /// </summary>
      public Coordinate chunkLocation {
        get;
      }

      /// <summary>
      /// The name of this event
      /// </summary>
      public string name {
        get;
      }

      /// <summary>
      /// Create a new event indicating a chunk is ready to have it's gameobject set active in world.
      /// </summary>
      /// <param name="chunkLocation"></param>
      public ChunkMeshLoadingFinishedEvent(Coordinate chunkLocation) {
        name = $"Mesh loaded for chunk : {chunkLocation.ToString()}";
        this.chunkLocation = chunkLocation;
      }
    }

    /// <summary>
    /// An event indicating a a chunk has moved out of the focus area in which we retain it's mesh data
    /// </summary>
    public struct ChunkMeshMovedOutOfFocusEvent : IEvent {

      /// <summary>
      /// The chunk location of the chunk
      /// </summary>
      public Coordinate chunkLocation {
        get;
      }

      /// <summary>
      /// The name of this event
      /// </summary>
      public string name {
        get;
      }

      /// <summary>
      /// Create a new event indicating a chunk is ready to have it's gameobject set inactive in world.
      /// </summary>
      /// <param name="chunkLocation"></param>
      public ChunkMeshMovedOutOfFocusEvent(Coordinate chunkLocation) {
        name = $"Chunk has left mesh resolution aperture area : {chunkLocation.ToString()}";
        this.chunkLocation = chunkLocation;
      }
    }
  }
}
