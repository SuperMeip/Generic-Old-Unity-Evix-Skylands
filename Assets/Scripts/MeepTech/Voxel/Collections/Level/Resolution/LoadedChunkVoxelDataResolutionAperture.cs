
using MeepTech.Jobs;
using MeepTech.Voxel.Collections.Storage;
using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace MeepTech.Voxel.Collections.Level.Management {
  class LoadedChunkVoxelDataResolutionAperture<VoxelStorageType> : ChunkResolutionAperture 
    where VoxelStorageType : IVoxelStorage {

    /// <summary>
    /// The save path for levels.
    /// </summary>
    static readonly string SavePath = Application.persistentDataPath + "/leveldata/";

    /// <summary>
    /// The current parent job, in charge of loading the chunks in the load queue
    /// </summary>
    readonly JLoadChunksFromFile chunkFileLoadQueueManagerJob;

    /// <summary>
    /// The current parent job, in charge of loading the chunks in the load queue
    /// </summary>
    readonly JUnloadChunks chunkUnloadToFileQueueManagerJob;

    /// <summary>
    /// The job used to generate chunks
    /// </summary>
    readonly JGenerateTerrainDataForChunks chunkGenerationJobManager;

    /// <summary>
    /// Construct
    /// </summary>
    /// <param name="level"></param>
    /// <param name="managedChunkRadius"></param>
    /// <param name="managedChunkHeight"></param>
    internal LoadedChunkVoxelDataResolutionAperture(int managedChunkRadius, int managedChunkHeight = 0) 
    : base (managedChunkRadius, managedChunkHeight) {
      chunkFileLoadQueueManagerJob = new JLoadChunksFromFile(this);
      chunkGenerationJobManager = new JGenerateTerrainDataForChunks(this);
      chunkUnloadToFileQueueManagerJob = new JUnloadChunks(this);
    }


#if DEBUG
    /// <summary>
    /// Get the chunks that this apeture is waiting to load
    /// </summary>
    /// <returns></returns>
    public override Coordinate[] GetQueuedChunks() {
      return chunkFileLoadQueueManagerJob.getAllQueuedItems()
        .Concat(chunkGenerationJobManager.getAllQueuedItems()).ToArray();
    }

    /// <summary>
    /// Get the chunks this apeture is loading/processing
    /// </summary>
    /// <returns></returns>
    public override Coordinate[] GetProcessingChunks() {
      return chunkFileLoadQueueManagerJob.getAllItemsWithRunningJobs()
        .Concat(chunkGenerationJobManager.getAllItemsWithRunningJobs()).ToArray();
    }
#endif

    /// <summary>
    /// Enqueue items
    /// </summary>
    /// <param name="chunkLocations"></param>
    protected override void addChunksToLoad(Coordinate[] chunkLocations) {
      chunkFileLoadQueueManagerJob.enqueue(chunkLocations);
      chunkUnloadToFileQueueManagerJob.dequeue(chunkLocations);
    }

    /// <summary>
    /// dequeue items
    /// </summary>
    /// <param name="chunkLocations"></param>
    protected override void addChunksToUnload(Coordinate[] chunkLocations) {
      chunkFileLoadQueueManagerJob.dequeue(chunkLocations);
      chunkGenerationJobManager.dequeue(chunkLocations);
      chunkUnloadToFileQueueManagerJob.enqueue(chunkLocations);
    }

    /// <summary>
    /// Only to be used by jobs
    /// Save a chunk to file
    /// </summary>
    /// <param name="chunkLocation"></param>
    internal void saveChunkDataToFile(Coordinate chunkLocation) {
      IVoxelChunk chunkData = level.getChunk(chunkLocation);
      if (!chunkData.isEmpty) {
        IFormatter formatter = new BinaryFormatter();
        checkForSaveDirectory();
        Stream stream = new FileStream(getChunkFileName(chunkLocation), FileMode.Create, FileAccess.Write, FileShare.None);
        formatter.Serialize(stream, chunkData.voxels);
        stream.Close();
      }
    }

    /// <summary>
    /// Get the voxeldata for a chunk location from file
    /// </summary>
    /// <param name="chunkLocation"></param>
    /// <returns></returns>
    internal VoxelStorageType getVoxelDataForChunkFromFile(Coordinate chunkLocation) {
      IFormatter formatter = new BinaryFormatter();
      Stream readStream = new FileStream(getChunkFileName(chunkLocation), FileMode.Open, FileAccess.Read, FileShare.Read) {
        Position = 0
      };
      var fileData = formatter.Deserialize(readStream);
      if (fileData is VoxelStorageType) {
        VoxelStorageType voxelData = (VoxelStorageType)fileData;
        voxelData.isLoaded = true;
        readStream.Close();
        return voxelData;
      }

      return default;
    }

    /// <summary>
    /// Get the file name a chunk is saved to based on it's location
    /// </summary>
    /// <param name="chunkLocation">the location of the chunk</param>
    /// <returns></returns>
    internal string getChunkFileName(Coordinate chunkLocation) {
      return getLevelSavePath() + chunkLocation.ToSaveString() + ".evxch";
    }

    /// <summary>
    /// Generate the chunk data for the chunk at the given location
    /// </summary>
    /// <param name="chunkLocation"></param>
    internal VoxelStorageType generateVoxelDataForChunk(Coordinate chunkLocation){
      VoxelStorageType voxelData = (VoxelStorageType)Activator.CreateInstance(typeof(VoxelStorageType), Chunk.Diameter);
      level.voxelSource.generateAllAt(chunkLocation, voxelData);
      voxelData.isLoaded = true;

      return voxelData;
    }

    /// <summary>
    /// Create the save file directory if it doesn't exist for the level yet
    /// </summary>
    void checkForSaveDirectory() {
      if (Directory.Exists(getLevelSavePath())) {
        return;
      }

      Directory.CreateDirectory(getLevelSavePath());
    }

    /// <summary>
    /// Get the save path for the level
    /// </summary>
    /// <returns></returns>
    string getLevelSavePath() {
      return SavePath + "/" + level.seed + "/";
    }

    /// <summary>
    /// A job to load all chunks from file for the loading queue
    /// </summary>
    class JLoadChunksFromFile : ChunkQueueManagerJob<LoadedChunkVoxelDataResolutionAperture<VoxelStorageType>> {

      /// <summary>
      /// Create a new job, linked to the level
      /// </summary>
      /// <param name="level"></pa
      public JLoadChunksFromFile(LoadedChunkVoxelDataResolutionAperture<VoxelStorageType> manager) : base(manager, 25) {
        threadName = "Load Chunk Data Manager";
      }

      /// <summary>
      /// Get the correct child job
      /// </summary>
      /// <returns></returns>
      protected override IThreadedJob getChildJob(Coordinate chunkColumnLocation) {
        return new JLoadChunkFromFile(this, chunkColumnLocation);
      }

      /// <summary>
      /// Override to shift generational items to their own queue
      /// </summary>
      /// <param name="chunkLocation"></param>
      /// <returns></returns>
      protected override bool isAValidQueueItem(Coordinate chunkLocation) {
        // if this doesn't have a loaded file, remove it from this queue and load it in the generation one
        if (chunkManager.isWithinManagedBounds(chunkLocation)) {
          if (!File.Exists(chunkManager.getChunkFileName(chunkLocation))) {
            chunkManager.chunkGenerationJobManager.enqueue(new Coordinate[] { chunkLocation });
            return false;
          }
        } else {
          return false;
        }

        return true;
      }

      /// <summary>
      /// sort items by the focus area they're in?
      /// </summary>
      protected override void sortQueue() {
        chunkManager.sortByFocusDistance(ref queue, 1.5f);
      }

      /// <summary>
      /// A Job for loading the data for a column of chunks into a level from file
      /// </summary>
      class JLoadChunkFromFile : QueueTaskChildJob<JLoadChunksFromFile, Coordinate> {

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="level"></param>
        /// <param name="chunkColumnLocation"></param>
        /// <param name="parentCancellationSources"></param>
        internal JLoadChunkFromFile(
          JLoadChunksFromFile jobManager,
          Coordinate chunkColumnLocation
        ) : base(chunkColumnLocation, jobManager) {
          threadName = "Load Column: " + chunkColumnLocation;
        }

        /// <summary>
        /// Threaded function, loads all the voxel data for this chunk
        /// </summary>
        protected override void doWork(Coordinate chunkLocation) {
          IVoxelChunk chunk = jobManager.chunkManager.level.getChunk(chunkLocation);
          if (chunk.isEmpty && !chunk.isLoaded) {
            VoxelStorageType voxelData = jobManager.chunkManager.getVoxelDataForChunkFromFile(chunkLocation);
            jobManager.chunkManager.level.chunkDataStorage.setChunkVoxelData(chunkLocation, voxelData);
          }
        }
      }
    }

    /// <summary>
    /// A job to load all chunks from the terrain generation method for the loading queue
    /// </summary>
    class JGenerateTerrainDataForChunks : ChunkQueueManagerJob<LoadedChunkVoxelDataResolutionAperture<VoxelStorageType>> {

      /// <summary>
      /// Create a new job, linked to the level
      /// </summary>
      /// <param name="level"></param>
      public JGenerateTerrainDataForChunks(LoadedChunkVoxelDataResolutionAperture<VoxelStorageType> manager) : base(manager, 25) {
        threadName = "Generate Chunk Manager";
      }

      /// <summary>
      /// Get the correct child job
      /// </summary>
      /// <param name="chunkColumnLocation"></param>
      /// <returns></returns>
      protected override IThreadedJob getChildJob(Coordinate chunkColumnLocation) {
        return new JGenerateTerrainDataForChunk(this, chunkColumnLocation);
      }

      /// <summary>
      /// remove empty chunks that are not in the load area from the load queue
      /// </summary>
      /// <param name="chunkLocation"></param>
      /// <returns></returns>
      protected override bool isAValidQueueItem(Coordinate chunkLocation) {
        if (chunkManager.isWithinManagedBounds(chunkLocation)) {
          return true;
        } else {
          return false;
        }
      }

      /// <summary>
      /// sort items by the focus area they're in?
      /// </summary>
      protected override void sortQueue() {
        chunkManager.sortByFocusDistance(ref queue, 1.5f);
      }

      /// <summary>
      /// A Job for generating a new column of chunks into a level
      /// </summary>
      class JGenerateTerrainDataForChunk : QueueTaskChildJob<JGenerateTerrainDataForChunks, Coordinate> {

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="level"></param>
        /// <param name="chunkLocation"></param>
        /// <param name="parentCancellationSources"></param>
        internal JGenerateTerrainDataForChunk(
          JGenerateTerrainDataForChunks jobManager,
          Coordinate chunkLocation
        ) : base(chunkLocation, jobManager) {
          threadName = "Generating voxels for chunk: " + chunkLocation;
        }

        /// <summary>
        /// Threaded function, loads all the voxel data for this chunk
        /// </summary>
        protected override void doWork(Coordinate chunkLocation) {
          // if the chunk is empty, lets try to fill it.
          IVoxelChunk chunk = jobManager.chunkManager.level.getChunk(chunkLocation);
          if (chunk.isEmpty && !chunk.isLoaded) {
            IVoxelStorage voxelData = jobManager.chunkManager.generateVoxelDataForChunk(chunkLocation);
            jobManager.chunkManager.level.chunkDataStorage.setChunkVoxelData(chunkLocation, voxelData);
          }
        }
      }
    }

    /// <summary>
    /// A job to un-load and serialize all chunks from the unloading queue
    /// </summary>
    class JUnloadChunks : ChunkQueueManagerJob<LoadedChunkVoxelDataResolutionAperture<VoxelStorageType>> {

      /// <summary>
      /// Create a new job, linked to the level
      /// </summary>
      /// <param name="level"></param>
      public JUnloadChunks(LoadedChunkVoxelDataResolutionAperture<VoxelStorageType> manager) : base(manager) {
        threadName = "Unload Chunk Manager";
      }

      /// <summary>
      /// Get the child job
      /// </summary>
      /// <param name="chunkColumnLocation"></param>
      /// <returns></returns>
      protected override IThreadedJob getChildJob(Coordinate chunkColumnLocation) {
        return new JUnloadChunkToFile(this, chunkColumnLocation);
      }

      /// <summary>
      /// If the chunk never finished loading, we should not save it
      /// </summary>
      /// <param name="chunkLocation"></param>
      /// <returns></returns>
      protected override bool isAValidQueueItem(Coordinate chunkLocation) {
        IVoxelChunk chunk = chunkManager.level.getChunk(chunkLocation);
        return chunk.isLoaded;
      }

      /// <summary>
      /// A Job for un-loading the data for a column of chunks into a serialized file
      /// </summary>
      class JUnloadChunkToFile : QueueTaskChildJob<JUnloadChunks, Coordinate> {

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
          threadName = "Unload chunk to file: " + queueItem.ToString();
        }

        /// <summary>
        /// Threaded function, serializes this chunks voxel data and removes it from the level
        /// </summary>
        protected override void doWork(Coordinate chunkLocation) {
          jobManager.chunkManager.saveChunkDataToFile(chunkLocation);
          jobManager.chunkManager.level.chunkDataStorage.removeChunkVoxelData(chunkLocation);
          jobManager.chunkManager.level.chunkDataStorage.removeChunkMesh(chunkLocation);
        }
      }
    }
  }
}
