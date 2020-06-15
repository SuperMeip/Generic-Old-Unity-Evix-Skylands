using MeepTech.Events;
using MeepTech.Voxel.Collections.Level;

namespace MeepTech.Voxel.Generation.Managers {
  /// <summary>
  /// Manages some aspect of chunks for levels.
  /// </summary>
  public abstract class ChunkManager : IChunkManager {

    /// <summary>
    /// The level this data load manager is working for
    /// </summary>
    protected ILevel level;

    /// <summary>
    /// The chunk data storage this is managing loading for
    /// </summary>
    internal IChunkDataStorage chunkDataStorage;

    /// <summary>
    /// Construct
    /// </summary>
    /// <param name="level">The level this manager is managing for</param>
    public ChunkManager(ILevel level, IChunkDataStorage chunkDataStorage) {
      this.level = level;
      this.chunkDataStorage = chunkDataStorage;
    }

    /// <summary>
    /// Stop all loading, jobs, management, etc.
    /// </summary>
    public abstract void killAll();

    /// <summary>
    /// Children should be observers
    /// </summary>
    /// <param name="event"></param>
    /// <param name="origin"></param>
    public virtual void notifyOf(IEvent @event, IObserver origin = null) {
      switch (@event) {
        case KillAllChunkManagementEventsEvent _:
          killAll();
          break;
        default:
          return;
      }
    }

#if DEBUG
    /// <summary>
    /// Provide your stats as an array
    /// </summary>
    /// <returns></returns>
    protected abstract (double, string)[] provideManagerStats();

    /// <summary>
    /// Get a readout of the current stats of this manager
    /// </summary>
    /// <returns></returns>
    public string getCurrentStats() {
      string statText = $"==================================\n" +
        $"{GetType().Name}\n" +
        "Stats:-----------------\n";
      foreach ((double, string) stat in provideManagerStats()) {
        statText += $"[{stat.Item2}]: {stat.Item1}\n";
      }
      statText += "==================================";

      return statText;
    }
#endif

    ///// EVENTS

    /// <summary>
    /// An event to kill all chunk managers
    /// </summary>
    public struct KillAllChunkManagementEventsEvent : IEvent {

      /// <summary>
      /// The name of this event
      /// </summary>
      public string name => "Killing all chunk managers";
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
      //public string name => "Chunk Voxel Data Has Finished Loading";
      public string name {
        get;
      }

      /// <summary>
      /// Create a new event indicating a chunk has finished generating it's mesh
      /// </summary>
      /// <param name="chunkLocation"></param>
      public ChunkDataLoadingFinishedEvent(Coordinate chunkLocation) {
        name = $"Chunk Voxel Data Has Finished Loading {chunkLocation.ToString()}";
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
      //public string name => "Chunk Data File Not Found";
      public string name {
        get;
      }

      /// <summary>
      /// Create a new event indicating a chunk has finished generating it's mesh
      /// </summary>
      /// <param name="chunkLocation"></param>
      public ChunkDataNotFoundInFilesEvent(Coordinate chunkLocation, bool isInError = false) {
        name = $"Chunk Data File Not Found {chunkLocation.ToString()}";
        this.chunkLocation = chunkLocation;
        this.isInError = isInError;
      }
    }

    /// <summary>
    /// An event indicating a chunk has finished generating it's mesh and is ready to render
    /// </summary>
    public struct ChunkMeshGenerationFinishedEvent : IEvent {

      /// <summary>
      /// The chunk location of the chunk that's finished generating it's mesh
      /// </summary>
      public Coordinate chunkLocation {
        get;
      }

      /// <summary>
      /// The name of this event
      /// </summary>
      //public string name => "Chunk Mesh Has Finished Generating";
      public string name {
        get;
      }

      /// <summary>
      /// Create a new event indicating a chunk has finished generating it's mesh
      /// </summary>
      /// <param name="chunkLocation"></param>
      public ChunkMeshGenerationFinishedEvent(Coordinate chunkLocation) {
        name = $"Chunk Mesh Has Finished Loading {chunkLocation.ToString()}";
        this.chunkLocation = chunkLocation;
      }
    }

    /// <summary>
    /// An event indicating a chunk has moved out of the zone we wish to render it's mesh in.
    /// </summary>
    public struct ChunkOutOfRenderZoneEvent : IEvent {

      /// <summary>
      /// The chunk location of the chunk that's finished generating it's mesh
      /// </summary>
      public Coordinate[] chunkLocation {
        get;
      }

      /// <summary>
      /// The name of this event
      /// </summary>
      //public string name => "Chunk Mesh has exited the render area";
      public string name {
        get;
      }

      /// <summary>
      /// Create a new event indicating a chunk has finished generating it's mesh
      /// </summary>
      /// <param name="chunkLocation"></param>
      public ChunkOutOfRenderZoneEvent(Coordinate[] chunkLocation) {
        name = $"Chunk has left visible render zone {chunkLocation.ToString()}";
        this.chunkLocation = chunkLocation;
      }
    }
  }
}