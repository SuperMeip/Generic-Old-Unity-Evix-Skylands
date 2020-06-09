using MeepTech.Events;
using MeepTech.Voxel.Collections.Level;
using MeepTech.Voxel.Collections.Storage;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace MeepTech.Voxel.Generation.Managers {

  /// <summary>
  /// Base class for a manager that loads chunks from files.
  /// </summary>
  public abstract class ChunkFileDataLoadingManager<VoxelStorageType> : ChunkManager
    where VoxelStorageType : IVoxelStorage {

    /// <summary>
    /// The save path for levels.
    /// </summary>
    readonly string SavePath = "/leveldata/";

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
      return SavePath + "/" + level.seed + "/" + chunkLocation.ToString() + ".evxch";
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
      VoxelStorageType voxelData = (VoxelStorageType)formatter.Deserialize(readStream);
      voxelData.isLoaded = true;
      readStream.Close();

      return voxelData;
    }

    /// <summary>
    /// An event indicating a chunk has finished generating it's mesh and is ready to render
    /// </summary>
    public struct ChunkDataLoadingFinishedEvent : IEvent {

      /// <summary>
      /// The chunk location of the chunk that's finished generating it's mesh
      /// </summary>
      public Coordinate chunkLocation {
        get;
      }

      /// <summary>
      /// The name of this event
      /// </summary>
      public string name => "Chunk Voxel Data Has Finished Loading";

      /// <summary>
      /// Create a new event indicating a chunk has finished generating it's mesh
      /// </summary>
      /// <param name="chunkLocation"></param>
      public ChunkDataLoadingFinishedEvent(Coordinate chunkLocation) {
        this.chunkLocation = chunkLocation;
      }
    }

    /// <summary>
    /// An event indicating a chunk has finished generating it's mesh and is ready to render
    /// </summary>
    public struct ChunkDataNotFoundInFilesEvent : IEvent {

      /// <summary>
      /// If this was the result of an error
      /// </summary>
      public bool isInError {
        get;
      }

      /// <summary>
      /// The chunk location of the chunk that's finished generating it's mesh
      /// </summary>
      public Coordinate chunkLocation {
        get;
      }

      /// <summary>
      /// The name of this event
      /// </summary>
      public string name => "Chunk Data File Not Found";

      /// <summary>
      /// Create a new event indicating a chunk has finished generating it's mesh
      /// </summary>
      /// <param name="chunkLocation"></param>
      public ChunkDataNotFoundInFilesEvent(Coordinate chunkLocation, bool isInError = false) {
        this.chunkLocation = chunkLocation;
        this.isInError = isInError;
      }
    }
  }
}