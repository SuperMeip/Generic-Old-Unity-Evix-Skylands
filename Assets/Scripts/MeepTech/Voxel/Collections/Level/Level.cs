using System;
using MeepTech.Voxel.Collections.Storage;
using MeepTech.Voxel.Generation.Sources;
using MeepTech.Voxel.Generation.Mesh;
using UnityEngine;
using MeepTech.GamingBasics;
using System.Collections.Generic;
using MeepTech.Voxel.Collections.Level.Management;
using MeepTech.Events;

namespace MeepTech.Voxel.Collections.Level {

  /// <summary>
  /// A collection of chunks, making an enclosed world in game
  /// </summary>
  public class Level : ILevel {

    /// <summary>
    /// Resolutions for how loaded chunks 
    /// </summary>
    public enum FocusResolutionLayers { Loaded, Meshed, Visible };

    /// <summary>
    /// The overall bounds of the level, max x y and z
    /// </summary>
    public int seed {
      get;
    }

    /// <summary>
    /// The overall bounds of the level, max x y and z
    /// </summary>
    public Coordinate chunkBounds {
      get;
      protected set;
    }

    /// <summary>
    /// The chunk data storate
    /// </summary>
    internal IChunkDataStorage chunkDataStorage {
      get;
      private set;
    }

    /// <summary>
    /// The source of voxels for this level
    /// </summary>
    internal IVoxelSource voxelSource {
      get;
    }

    /// <summary>
    /// The voxel generator this level uses.
    /// </summary>
    internal IVoxelMeshGenerator meshGenerator {
      get;
    }

    /// <summary>
    /// The current highest assigned focus id.
    /// </summary>
    int currentMaxFocusID = -1;

    /// <summary>
    /// The chunk resolution Apertures for this level by FocusResolutionLayers type.
    /// </summary>
    IChunkResolutionAperture[] resolutionApertures;

    /// <summary>
    /// The foci this level is loaded around managed by assigned ID
    /// </summary>
    Dictionary<int, ILevelFocus> levelFociByID;

    /// <summary>
    /// Create a new level
    /// </summary>
    /// <param name="seed"></param>
    /// <param name="chunkBounds">the max x y and z chunk sizes of the world</param>
    Level(
      Coordinate chunkBounds,
      IVoxelSource voxelSource,
      IVoxelMeshGenerator meshGenerator,
      IChunkResolutionAperture[] resolutionApertures
    ) {
      this.chunkBounds   = chunkBounds;
      this.voxelSource = voxelSource;
      this.meshGenerator = meshGenerator;
      this.resolutionApertures = resolutionApertures;

      levelFociByID = new Dictionary<int, ILevelFocus>();

      /// subscribe all apetures to the terrain gen channel
      foreach (ChunkResolutionAperture resolutionAperture in resolutionApertures) {
        resolutionAperture.setLevel(this);
        World.EventSystem.subscribe(resolutionAperture, Evix.EventSystems.WorldEventSystem.Channels.LevelFocusUpdates);
      }

      seed = voxelSource.seed;
    }

    /// <summary>
    /// Create a new level using the given chunk storage type
    /// </summary>
    /// <typeparam name="ChunkDataStorageType"></typeparam>
    /// <param name="chunkBounds"></param>
    /// <param name="voxelSource"></param>
    /// <param name="meshGenerator"></param>
    /// <param name="chunkResolutionManagerTypes"></param>
    /// <returns></returns>
    public static Level Create<ChunkDataStorageType> (
      Coordinate chunkBounds,
      IVoxelSource voxelSource,
      IVoxelMeshGenerator meshGenerator,
      IChunkResolutionAperture[] chunkResolutionManagerTypes
    ) where ChunkDataStorageType : IChunkDataStorage {
      Level level = new Level(chunkBounds, voxelSource, meshGenerator, chunkResolutionManagerTypes);
      ChunkDataStorageType chunkDataStorage = (ChunkDataStorageType)Activator.CreateInstance(typeof(ChunkDataStorageType), level);
      level.chunkDataStorage = chunkDataStorage;

      return level;
    }

    /// <summary>
    /// Spawn a new level focus in this level
    /// </summary>
    /// <param name="newFocus"></param>
    public void spawnFocus(ILevelFocus newFocus) {
      levelFociByID[++currentMaxFocusID] = newFocus;
      World.EventSystem.notifyAllOf(
        new SpawnFocusEvent(newFocus)
      );
      newFocus.setActive();
    }

    /// <summary>
    /// Get the chunk at the given location (if it's loaded)
    /// </summary>
    /// <param name="chunkLocation">the location of the chunk to grab</param>
    /// <param name="withMeshes">get the chunk with it's mesh</param>
    /// <param name="withNeighbors">get the chunk with neighbors linked</param>
    /// <returns>the chunk data or null if there's none loaded</returns>
    public IVoxelChunk getChunk(Coordinate chunkLocation, bool withMeshes = false, bool withNeighbors = false, bool withNeighborsNeighbors = false, bool fullNeighborEncasement = false) {
      // just get an empty chunk for this one if this is out of bounds
      if (!chunkLocation.isWithin(Coordinate.Zero, chunkBounds)) {
        return Chunk.GetEmptyChunk(withNeighbors);
      }

      IVoxelStorage voxels = chunkDataStorage.getChunkVoxelData(chunkLocation);
      IVoxelChunk[] neighbors = null;

      if (withNeighbors) {
        neighbors = new IVoxelChunk[Directions.All.Length];
        foreach (Directions.Direction direction in Directions.All) {
          Coordinate neighborLocation = chunkLocation + direction.Offset;
          neighbors[direction.Value] = getChunk(neighborLocation, withMeshes, withNeighborsNeighbors, fullNeighborEncasement);
        }
      }

      return new Chunk(voxels, neighbors, withMeshes ? chunkDataStorage.getChunkMesh(chunkLocation) : null);
    }

    /// <summary>
    /// Get the id for the given level focus
    /// </summary>
    /// <param name="focus"></param>
    /// <returns></returns>
    public int getFocusID(ILevelFocus focus) {
      foreach(KeyValuePair<int, ILevelFocus> storedFocus in levelFociByID) {
        if (storedFocus.Value == focus) {
          return storedFocus.Key;
        }
      }

      return -1;
    }

    /// <summary>
    /// Get a focus by it's id
    /// </summary>
    /// <param name="focusID"></param>
    /// <returns></returns>
    public ILevelFocus getFocusByID(int focusID) {
      return levelFociByID[focusID];
    }

    /// <summary>
    /// do something for each focus in the level
    /// </summary>
    /// <param name="action"></param>
    public void forEachFocus(Action<ILevelFocus> action) {
      foreach(ILevelFocus focus in levelFociByID.Values) {
        action(focus);
      }   
    }

    /// <summary>
    /// Check if the chunk is within the given resolution area
    /// </summary>
    /// <param name="chunkLocation"></param>
    /// <param name="focusResolutionLayer"></param>
    /// <returns></returns>
    public IChunkResolutionAperture getApetureForResolutionLayer(Level.FocusResolutionLayers resolutionLayer) {
      return resolutionApertures[(int)resolutionLayer];
    }

    /// <summary>
    /// An event for announcing when the player changes chunk locations
    /// </summary>
    public struct FocusChangedChunkLocationEvent : IEvent {

      /// <summary>
      /// The updated focus
      /// </summary>
      public ILevelFocus updatedFocus {
        get;
      }

      /// <summary>
      /// the name of this event
      /// </summary>
      public string name => "Focus Changed Chunk Locations";

      /// <summary>
      /// Make this kind of event
      /// </summary>
      /// <param name="newChunkLocation"></param>
      public FocusChangedChunkLocationEvent(ILevelFocus updatedFocus) {
        this.updatedFocus = updatedFocus;
      }
    }

    /// <summary>
    /// An event for announcing when a focus has spawned in the level
    /// </summary>
    public struct SpawnFocusEvent : IEvent {

      /// <summary>
      /// The world location the focus has spawned at
      /// </summary>
      public ILevelFocus spawnedFocus {
        get;
      }

      /// <summary>
      /// the name of this event
      /// </summary>
      public string name => "Focus Spawned";

      /// <summary>
      /// Make this kind of event
      /// </summary>
      /// <param name="newChunkLocation"></param>
      public SpawnFocusEvent(ILevelFocus spawnedFocus) {
        this.spawnedFocus = spawnedFocus;
      }
    }

    /// <summary>
    /// An event for announcing when a focus has left the level
    /// </summary>
    public struct FocusLeftEvent : IEvent {

      /// <summary>
      /// The world location the focus has spawned at
      /// </summary>
      public ILevelFocus abscondingFocus {
        get;
      }

      /// <summary>
      /// the name of this event
      /// </summary>
      public string name => "Focus Spawned";

      /// <summary>
      /// Make this kind of event
      /// </summary>
      /// <param name="newChunkLocation"></param>
      public FocusLeftEvent(ILevelFocus abscondingFocus) {
        this.abscondingFocus = abscondingFocus;
      }
    }
  }

  public static class Vector3LevelUtilities {

    /// <summary>
    /// convert a world vector 3 to a level chunk location
    /// </summary>
    /// <param name="location"></param>
    /// <returns></returns>
    public static Coordinate worldToChunkLocation(this Vector3 location) {
      return (
        (int)location.x / Chunk.Diameter,
        (int)location.y / Chunk.Diameter,
        (int)location.z / Chunk.Diameter
      );
    }

    /// <summary>
    /// convert a world vector 3 to a level chunk location
    /// </summary>
    /// <param name="location"></param>
    /// <returns></returns>
    public static Coordinate toCoordinate(this Vector3 location) {
      return (
        (int)location.x,
        (int)location.y,
        (int)location.z
      );
    }
  }
}