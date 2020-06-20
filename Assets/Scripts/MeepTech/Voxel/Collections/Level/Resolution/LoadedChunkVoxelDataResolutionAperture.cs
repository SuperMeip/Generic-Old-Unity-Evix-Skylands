
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
    readonly JGenerateTerrainDataForChunks chunkGenerationManagerJob;

    /// <summary>
    /// Construct
    /// </summary>
    /// <param name="level"></param>
    /// <param name="managedChunkRadius"></param>
    /// <param name="managedChunkHeight"></param>
    internal LoadedChunkVoxelDataResolutionAperture(int managedChunkRadius, int managedChunkHeight = 0) 
    : base (managedChunkRadius, managedChunkHeight) {
      chunkFileLoadQueueManagerJob = new JLoadChunksFromFile(this);
      chunkGenerationManagerJob = new JGenerateTerrainDataForChunks(this);
      chunkUnloadToFileQueueManagerJob = new JUnloadChunks(this);
    }


#if DEBUG
    /// <summary>
    /// Get the chunks that this apeture is waiting to load
    /// </summary>
    /// <returns></returns>
    public override Coordinate[] getQueuedChunks() {
      return chunkFileLoadQueueManagerJob.getAllQueuedItems()
        .Concat(chunkGenerationManagerJob.getAllQueuedItems()).ToArray();
    }

    /// <summary>
    /// Get the chunks this apeture is loading/processing
    /// </summary>
    /// <returns></returns>
    public override Coordinate[] getProcessingChunks() {
      return chunkFileLoadQueueManagerJob.getAllItemsWithRunningJobs()
        .Concat(chunkGenerationManagerJob.getAllItemsWithRunningJobs()).ToArray();
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
      chunkGenerationManagerJob.dequeue(chunkLocations);
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
      public JLoadChunksFromFile(LoadedChunkVoxelDataResolutionAperture<VoxelStorageType> manager) : base(manager) {
        threadName = "Load Chunk Data Manager";
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
            chunkManager.chunkGenerationManagerJob.enqueue(new Coordinate[] { chunkLocation });
            return false;
          }
        } else {
          return false;
        }

        return true;
      }

      /// <summary>
      /// sort items by the focus area they're in
      /// </summary>
      protected override float getPriority(Coordinate chunkLocation) {
        return chunkManager.getClosestFocusDistance(chunkLocation, 1.5f);
      }

      /// <summary>
      /// Try to load the voxel data from file
      /// </summary>
      /// <param name="queueItem"></param>
      protected override void childJob(Coordinate chunkLocation) {
        IVoxelChunk chunk = chunkManager.level.getChunk(chunkLocation);
        if (chunk.isEmpty && !chunk.isLoaded) {
          VoxelStorageType voxelData = chunkManager.getVoxelDataForChunkFromFile(chunkLocation);
          chunkManager.level.chunkDataStorage.setChunkVoxelData(chunkLocation, voxelData);
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
      public JGenerateTerrainDataForChunks(LoadedChunkVoxelDataResolutionAperture<VoxelStorageType> manager) : base(manager) {
        threadName = "Generate Chunk Manager";
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
      /// sort items by the focus area they're in
      /// </summary>
      protected override float getPriority(Coordinate chunkLocation) {
        return chunkManager.getClosestFocusDistance(chunkLocation, 1.5f);
      }

      /// <summary>
      /// Generate the voxel data for the chunk from the voxel source
      /// </summary>
      /// <param name="queueItem"></param>
      protected override void childJob(Coordinate chunkLocation) {
        // if the chunk is empty, lets try to fill it.
        IVoxelChunk chunk = chunkManager.level.getChunk(chunkLocation);
        if (chunk.isEmpty && !chunk.isLoaded) {
          IVoxelStorage voxelData = chunkManager.generateVoxelDataForChunk(chunkLocation);
          chunkManager.level.chunkDataStorage.setChunkVoxelData(chunkLocation, voxelData);
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
      /// If the chunk never finished loading, we should not save it
      /// </summary>
      /// <param name="chunkLocation"></param>
      /// <returns></returns>
      protected override bool isAValidQueueItem(Coordinate chunkLocation) {
        IVoxelChunk chunk = chunkManager.level.getChunk(chunkLocation);
        return chunk.isLoaded;
      }

      /// <summary>
      /// sort items by the focus area they're in
      /// </summary>
      protected override float getPriority(Coordinate chunkLocation) {
        return chunkManager.getClosestFocusDistance(chunkLocation, 1.5f);
      }

      /// <summary>
      /// Save the chunk data to file
      /// </summary>
      /// <param name="queueItem"></param>
      protected override void childJob(Coordinate chunkLocation) {
        chunkManager.saveChunkDataToFile(chunkLocation);
        chunkManager.level.chunkDataStorage.removeChunkVoxelData(chunkLocation);
        chunkManager.level.chunkDataStorage.removeChunkMesh(chunkLocation);
      }
    }
  }
}
