using MeepTech.Jobs;
using MeepTech.Voxel.Generation.Mesh;
using System.Linq;

namespace MeepTech.Voxel.Collections.Level.Management {

  /// <summary>
  /// A chunk resolution manager for the mesh data
  /// </summary>
  internal class LoadedChunkMeshDataResolutionAperture : ChunkResolutionAperture {

    /// <summary>
    /// The current parent job, in charge of generating meshes for chunks in the load queue
    /// </summary>
    JGenerateChunkMeshes chunkMeshGenQueueManagerJob;

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

#if DEBUG
    /// <summary>
    /// Get the chunks that this apeture is waiting to load
    /// </summary>
    /// <returns></returns>
    public override Coordinate[] GetQueuedChunks() {
      return chunkMeshGenQueueManagerJob.getAllQueuedItems();
    }

    /// <summary>
    /// Get the chunks this apeture is loading/processing
    /// </summary>
    /// <returns></returns>
    public override Coordinate[] GetProcessingChunks() {
      return chunkMeshGenQueueManagerJob.getAllItemsWithRunningJobs();
    }
#endif

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
      public JGenerateChunkMeshes(LoadedChunkMeshDataResolutionAperture manager) : base(manager, 50) {
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
          return false;
        }

        IVoxelChunk chunk = chunkManager.level.getChunk(chunkLocation);
        // the chunk can't be loaded and empty, we'll generate nothing.
        if (chunk.isLoaded && chunk.isEmpty) {
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
          if (!jobManager.chunkManager.level.chunkDataStorage.containsChunkMesh(chunkLocation)) {
            IMesh mesh = jobManager.chunkManager.generateMeshDataForChunk(chunkLocation);
            if (!mesh.isEmpty) {
              jobManager.chunkManager.level.chunkDataStorage.setChunkMesh(chunkLocation, mesh);
            }
          }
        }
      }
    }
  }
}
