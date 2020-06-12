using MeepTech.Events;
using MeepTech.Voxel.Collections.Level;
using MeepTech.Voxel.Collections.Storage;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace MeepTech.Voxel.Generation.Managers {

  /// <summary>
  /// Base class for a manager that loads chunks from files.
  /// </summary>
  public abstract class ChunkFileDataLoadingManager<VoxelStorageType> : ChunkManager
    where VoxelStorageType : IVoxelStorage {

    /// <summary>
    /// The save path for levels.
    /// </summary>
    static readonly string SavePath = Application.persistentDataPath + "/leveldata/";

    /// <summary>
    /// Construct
    /// </summary>
    /// <param name="level">The level this manager is managing for</param>
    public ChunkFileDataLoadingManager(ILevel level, IChunkDataStorage chunkDataStorage) : base (level, chunkDataStorage) {}

    /// <summary>
    /// Add chunks that we want this manager to load into storage from file.
    /// </summary>
    /// <param name="chunkLocations"></param>
    public abstract void addChunksToLoad(Coordinate[] chunkLocations);

    /// <summary>
    /// Add chunks we want this manager to unload to file storage.
    /// </summary>
    /// <param name="chunkLocations"></param>
    public abstract void addChunksToUnload(Coordinate[] chunkLocations);

    /// <summary>
    /// Get the file name a chunk is saved to based on it's location
    /// </summary>
    /// <param name="chunkLocation">the location of the chunk</param>
    /// <returns></returns>
    protected string getChunkFileName(Coordinate chunkLocation) {
      return getLevelSavePath() + chunkLocation.ToSaveString() + ".evxch";
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
      Stream readStream = new FileStream(getChunkFileName(chunkLocation), FileMode.Open, FileAccess.Read, FileShare.Read);
      readStream.Position = 0;
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
    /// Create the save file directory if it doesn't exist for the level yet
    /// </summary>
    void checkForSaveDirectory() {
      if (Directory.Exists(getLevelSavePath())) {
        return;
      }

      Directory.CreateDirectory(getLevelSavePath());
    }


    string getLevelSavePath() {
      return SavePath + "/" + level.seed + "/";
    }
  }
}