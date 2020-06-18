using MeepTech.Events;
using MeepTech.GamingBasics;
using MeepTech.Jobs;
using System.Threading;

namespace MeepTech.Voxel.Collections.Level.Management {

  /// <summary>
  /// Resolution Apeture for activating/deactivating the meshed gameobject
  /// </summary>
  public class ActivateGameobjectResolutionAperture : ChunkResolutionAperture {

    /// <summary>
    /// The current parent job, in charge of generating meshes for chunks in the load queue
    /// </summary>
    readonly JActivateChunkObjects chunkObjectActivationJobManager;

    /// <summary>
    /// Construct
    /// </summary>
    /// <param name="level"></param>
    /// <param name="managedChunkRadius"></param>
    /// <param name="managedChunkHeight"></param>
    internal ActivateGameobjectResolutionAperture(int managedChunkRadius, int managedChunkHeight = 0)
    : base(managedChunkRadius, managedChunkHeight) {
      chunkObjectActivationJobManager = new JActivateChunkObjects(this);
    }

#if DEBUG
    /// <summary>
    /// Get the chunks that this apeture is waiting to load
    /// </summary>
    /// <returns></returns>
    public override Coordinate[] getQueuedChunks() {
      return chunkObjectActivationJobManager.getAllQueuedItems();
    }

    /// <summary>
    /// Get the chunks this apeture is loading/processing
    /// </summary>
    /// <returns></returns>
    public override Coordinate[] getProcessingChunks() {
      return chunkObjectActivationJobManager.getAllItemsWithRunningJobs();
    }
#endif

    /// <summary>
    /// add chunks t othe activation job manager queue
    /// </summary>
    /// <param name="chunkLocations"></param>
    protected override void addChunksToLoad(Coordinate[] chunkLocations) {
      chunkObjectActivationJobManager.enqueue(chunkLocations);
    }

    /// <summary>
    /// Add chunks to unload and send de-actvation notifications
    /// </summary>
    /// <param name="chunkLocations"></param>
    protected override void addChunksToUnload(Coordinate[] chunkLocations) {
      chunkObjectActivationJobManager.dequeue(chunkLocations);
      new Thread(() => {
        foreach (Coordinate chunkLocation in chunkLocations) {
          World.EventSystem.notifyChannelOf(
            new SetChunkObjectInactiveEvent(chunkLocation),
            Evix.EventSystems.WorldEventSystem.Channels.ChunkActivationUpdates
          );
        }
      }) { Name = "Deactivate Chunks Messenger" }.Start();
    }

    /// <summary>
    /// The job manager this manager uses
    /// </summary>
    class JActivateChunkObjects : ChunkQueueManagerJob<ActivateGameobjectResolutionAperture> {

      /// <summary>
      /// Create a new job, linked to the level
      /// </summary>
      /// <param name="level"></param>
      public JActivateChunkObjects(ActivateGameobjectResolutionAperture manager) : base(manager) {
        threadName = "Activate Chunk Object Manager";
      }

      /// <summary>
      /// get the child job given the values
      /// </summary>
      /// <param name="chunkLocation"></param>
      /// <param name="parentCancelationSources"></param>
      /// <returns></returns>
      protected override IThreadedJob getChildJob(Coordinate chunkLocation) {
        return new JActivateChunkObject(this, chunkLocation);
      }

      /// <summary>
      /// remove empty chunks that have loaded from the mesh gen queue
      /// </summary>
      /// <param name="chunkLocation"></param>
      /// <returns></returns>
      protected override bool isAValidQueueItem(Coordinate chunkLocation) {
        if (!chunkManager.isWithinManagedBounds(chunkLocation)) {
          return false;
        }

        IVoxelChunk chunk = chunkManager.level.getChunk(chunkLocation, true);
        // the chunk can't be loaded and empty, or meshed with an empty mesh.
        if ((chunk.isLoaded && chunk.isEmpty) || (chunk.isMeshed && chunk.mesh.isEmpty)) {
          return false;
        }

        /// in this case, we want to make sure the mesh job didn't drop or loose our chunk
        // @TODO: cull this better, instead of isfull, maybe use a system to determine if it's visible to a player or not,
        // this notification should only be sent if a chunk needs to be rendered badly because it's missing.
        if (chunk.isLoaded && !chunk.isMeshed && !chunk.isEmpty && !chunk.isFull) {
          new Thread(() => {
            World.EventSystem.notifyChannelOf(
              new ChunkWaitingForActiveMissingMeshEvent(chunkLocation),
              Evix.EventSystems.WorldEventSystem.Channels.ChunkActivationUpdates
            );
          }) { Name = "Missing Mesh Messenger"}.Start();
        }

        return true;
      }

      /// <summary>
      /// Don't activate the chunk until a mesh is loaded for the loaded chunk
      /// </summary>
      /// <param name="queueItem"></param>
      /// <returns></returns>
      protected override bool itemIsReady(Coordinate chunkLocation) {
        IVoxelChunk chunk = chunkManager.level.getChunk(chunkLocation, true);
        return chunk.isLoaded && chunk.isMeshed && !chunk.mesh.isEmpty;
      }

      /// <summary>
      /// sort items by the focus area they're in?
      /// </summary>
      protected override void sortQueue() {
        chunkManager.sortByFocusDistance(ref queue);
      }

      /// <summary>
      /// Child job for doing work on the chunk
      /// </summary>
      protected class JActivateChunkObject : QueueTaskChildJob<JActivateChunkObjects, Coordinate> {

        /// <summary>
        /// Make a new job
        /// </summary>
        /// <param name="level"></param>
        /// <param name="chunkLocation"></param>
        internal JActivateChunkObject(
          JActivateChunkObjects jobManager,
          Coordinate chunkLocation
        ) : base(chunkLocation, jobManager) {
          threadName = "Tell Game Engine to activate Gameobject for Chunk: " + queueItem.ToString();
        }

        /// <summary>
        /// generate the chunk mesh if the level doesn't have it yet.
        /// </summary>
        protected override void doWork(Coordinate chunkLocation) {
          IVoxelChunk chunk = jobManager.chunkManager.level.getChunk(chunkLocation, true);
          if (chunk.isMeshed && !chunk.mesh.isEmpty) {
            World.EventSystem.notifyChannelOf(
              new SetChunkObjectActiveEvent(chunkLocation),
              Evix.EventSystems.WorldEventSystem.Channels.ChunkActivationUpdates
            );
          }
        }
      }
    }

    /// <summary>
    /// An event indicating a a chunk is ready to have it's gameobject set active in world.
    /// </summary>
    public struct SetChunkObjectActiveEvent : IEvent {

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
      public SetChunkObjectActiveEvent(Coordinate chunkLocation) {
        name = $"Setting Chunk Gameobject Object Active: {chunkLocation.ToString()}";
        this.chunkLocation = chunkLocation;
      }
    }

    /// <summary>
    /// An event indicating a a chunk is ready to have it's gameobject set inactive in world.
    /// </summary>
    public struct SetChunkObjectInactiveEvent : IEvent {

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
      public SetChunkObjectInactiveEvent(Coordinate chunkLocation) {
        name = $"Setting Chunk Gameobject Object Inactive: {chunkLocation.ToString()}";
        this.chunkLocation = chunkLocation;
      }
    }

    /// <summary>
    /// An event indicating a chunk is not empty, not solid, not meshed, and is waiting for activation.
    /// </summary>
    public struct ChunkWaitingForActiveMissingMeshEvent : IEvent {

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
      public ChunkWaitingForActiveMissingMeshEvent(Coordinate chunkLocation) {
        name = $"Active chunk: {chunkLocation.ToString()} is waiting for a mesh!";
        this.chunkLocation = chunkLocation;
      }
    }
  }
}
