using System;
using MeepTech.Voxel.Collections.Storage;
using MeepTech.Voxel.Generation.Sources;
using MeepTech.Voxel.Generation.Mesh;
using UnityEngine;
using MeepTech.Voxel.Generation.Managers;
using MeepTech.GamingBasics;

namespace MeepTech.Voxel.Collections.Level {

  /// <summary>
  /// A collection of chunks, making an enclosed world in game
  /// </summary>
  public class Level<
    VoxelStorageType,
    ChunkDataStorageType,
    MeshGeneratorType,
    ChunkFileDataLoadingManagerType,
    ChunkVoxelDataGenerationManagerType,
    ChunkMeshGenerationManagerType
  > : ILevel
    where VoxelStorageType : IVoxelStorage
    where ChunkDataStorageType : IChunkDataStorage
    where MeshGeneratorType : IVoxelMeshGenerator
    where ChunkFileDataLoadingManagerType : ChunkFileDataLoadingManager<VoxelStorageType>
    where ChunkVoxelDataGenerationManagerType : ChunkVoxelDataGenerationManager<VoxelStorageType>
    where ChunkMeshGenerationManagerType : ChunkMeshGenerationManager {

    /// <summary>
    /// The width of the active chunk area in chunks
    /// </summary>
    public int meshedChunkDiameter {
      get;
    } = 15;

    /// <summary>
    /// The buffer diameter around rendered chunks to also load into memmory
    /// </summary>
    public int chunkLoadBuffer {
      get;
    } = 5;

    /// <summary>
    /// How many chunks down to load (temp);
    /// </summary>
    public int chunksBelowToMesh {
      get;
    } = 5;

    /// <summary>
    /// The width of the active chunk area in chunks
    /// </summary>
    public int loadedChunkDiameter {
      get => meshedChunkDiameter + chunkLoadBuffer;
    }

    /// <summary>
    /// The width of the active chunk area in chunks
    /// </summary>
    public int chunksBelowToLoad {
      get => chunksBelowToMesh + chunkLoadBuffer;
    }

    /// <summary>
    /// The height of the active chunk area in chunks
    /// </summary>
    public int LoadedChunkHeight {
      get => chunkBounds.y;
    }

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
    /// The current center of all loaded chunks, usually based on player location
    /// </summary>
    public Coordinate focus {
      get;
      protected set;
    }

    /// <summary>
    /// The coordinates indicating the two chunks the extreems of what chunks are to be loaded from memmory:
    ///   0: south bottom west most loaded chunk
    ///   1: north top east most loaded chunk 
    /// </summary>
    public Coordinate[] loadedChunkBounds {
      get;
      protected set;
    }

    /// <summary>
    /// The coordinates indicating the two chunks the extreems of what chunks are to be meshed.
    ///   0: south bottom west most loaded chunk
    ///   1: north top east most loaded chunk 
    /// </summary>
    public Coordinate[] meshedChunkBounds {
      get;
      protected set;
    }

    /// <summary>
    /// Manager in charge of loading and unloading chunk data from files
    /// </summary>
    protected ChunkFileDataLoadingManagerType chunkFileDataLoadingManager;

    /// <summary>
    /// Manager in charge of generatig new chunk meshes.
    /// </summary>
    protected ChunkMeshGenerationManagerType chunkMeshGenerationManager;

    /// <summary>
    /// The manager that handles generating the voxels for a chunk
    /// </summary>
    protected ChunkVoxelDataGenerationManagerType chunkVoxelDataGenerationManager;

    /// <summary>
    /// The chunk data storate
    /// </summary>
    IChunkDataStorage chunkDataStorage;

    /// <summary>
    /// Create a new level
    /// </summary>
    /// <param name="seed"></param>
    /// <param name="chunkBounds">the max x y and z chunk sizes of the world</param>
    public Level(
      Coordinate chunkBounds,
      IVoxelSource voxelSource
    ) {
      this.chunkBounds   = chunkBounds;
      IVoxelMeshGenerator voxelMeshGenerator = (MeshGeneratorType)Activator.CreateInstance(typeof(MeshGeneratorType));
      chunkDataStorage = (ChunkDataStorageType)Activator.CreateInstance(typeof(ChunkDataStorageType), this);
      chunkFileDataLoadingManager = (ChunkFileDataLoadingManagerType)Activator.CreateInstance(typeof(ChunkFileDataLoadingManagerType), this, chunkDataStorage);
      chunkVoxelDataGenerationManager = (ChunkVoxelDataGenerationManagerType)Activator.CreateInstance(typeof(ChunkVoxelDataGenerationManagerType), this, chunkDataStorage, voxelSource);
      chunkMeshGenerationManager = (ChunkMeshGenerationManagerType)Activator.CreateInstance(typeof(ChunkMeshGenerationManagerType), this, chunkDataStorage, voxelMeshGenerator);
      World.EventSystem.subscribe(chunkFileDataLoadingManager, Evix.EventSystems.WorldEventSystem.Channels.TerrainGeneration);
      World.EventSystem.subscribe(chunkVoxelDataGenerationManager, Evix.EventSystems.WorldEventSystem.Channels.TerrainGeneration);
      World.EventSystem.subscribe(chunkMeshGenerationManager, Evix.EventSystems.WorldEventSystem.Channels.TerrainGeneration);
      seed = voxelSource.seed;
    }

    /// <summary>
    /// initialize this level with the center of loaded chunks fouced on the given location
    /// </summary>
    /// <param name="centerChunkLocation">the center point/focus of the loaded chunks, usually a player location</param>
    public void initializeAround(Coordinate centerChunkLocation) {
      focus = centerChunkLocation;
      loadedChunkBounds = getLoadedChunkBounds(focus);
      meshedChunkBounds = getMeshedChunkBounds(focus);
      Coordinate[] chunksToLoad = Coordinate.GetAllPointsBetween(loadedChunkBounds[0], loadedChunkBounds[1]);
      chunkFileDataLoadingManager.addChunksToLoad(chunksToLoad);
      Debug.Log($"adding {chunksToLoad.Length} chunks to the loading queue");
    }

    /// <summary>
    /// Move the focus/central loaded point of the level by one chunk in the given direction
    /// </summary>
    /// <param name="newFocus">The new focal chunkLocation</param>
    public void adjustFocusTo(Coordinate newFocus) {
      /* Coordinate[] newLoadedChunkBounds   = getLoadedChunkBounds(newFocus);
       Coordinate[] newRenderedChunkBounds = getMeshedChunkBounds(newFocus);
       Coordinate[] chunkColumnsToLoad     = Coordinate.GetPointDiff(newLoadedChunkBounds, loadedChunkBounds);
       Coordinate[] chunkColumnsToUnload   = Coordinate.GetPointDiff(loadedChunkBounds, newLoadedChunkBounds);
       Coordinate[] chunkColumnsToRender   = Coordinate.GetPointDiff(newRenderedChunkBounds, meshedChunkBounds);
       // @TODO: chunkColumnsToDeRender

       // queue the collected values
       addChunkColumnsToLoadingQueue(chunkColumnsToLoad);
       addChunkColumnsToUnloadingQueue(chunkColumnsToUnload);*/
      //addChunksToMeshGenQueue(chunkColumnsToRender);
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
      if (!chunkIsWithinLoadedBounds(chunkLocation)) {
        return Chunk.getEmptyChunk(withNeighbors);
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
    /// Get if the given chunkLocation is loaded
    /// </summary>
    /// <param name="chunkLocation"></param>
    /// <returns></returns>
   public  bool chunkIsWithinLoadedBounds(Coordinate chunkLocation) {
      return chunkLocation.isWithin(loadedChunkBounds);
    }

    /// <summary>
    /// Get if the given chunkLocation should be meshed
    /// </summary>
    /// <param name="chunkLocation"></param>
    /// <returns></returns>
    public bool chunkIsWithinkMeshedBounds(Coordinate chunkLocation) {
      return chunkLocation.isWithin(meshedChunkBounds);
    }

    /// <summary>
    /// Stop all running chunk managers
    /// </summary>
    public void stopAllManagers() {
      chunkFileDataLoadingManager.killAll();
      chunkVoxelDataGenerationManager.killAll();
      chunkMeshGenerationManager.killAll();
    }

    /// <summary>
    /// Get the loaded chunk bounds for a given focus point.
    /// </summary>
    /// <param name="centerLocation"></param>
    Coordinate[] getLoadedChunkBounds(Coordinate centerLocation) {
      return new Coordinate[] {
        (
          Math.Max(centerLocation.x - loadedChunkDiameter / 2, 0),
          Math.Max(centerLocation.y - chunksBelowToLoad, 0),
          Math.Max(centerLocation.z - loadedChunkDiameter / 2, 0)
        ),
        (
          Math.Min(centerLocation.x + loadedChunkDiameter / 2, chunkBounds.x),
          chunkBounds.y,
          Math.Min(centerLocation.z + loadedChunkDiameter / 2, chunkBounds.z)
        )
      };
    }

    /// <summary>
    /// Get the rendered chunk bounds for a given center point.
    /// Always trims to X,0,Z
    /// </summary>
    /// <param name="centerLocation"></param>
    Coordinate[] getMeshedChunkBounds(Coordinate centerLocation) {
      return new Coordinate[] {
        (
          Math.Max(centerLocation.x - meshedChunkDiameter / 2, 0),
          Math.Max(centerLocation.y - chunksBelowToMesh, 0),
          Math.Max(centerLocation.z - meshedChunkDiameter / 2, 0)
        ),
        (
          Math.Min(centerLocation.x + meshedChunkDiameter / 2, chunkBounds.x),
          chunkBounds.y,
          Math.Min(centerLocation.z + meshedChunkDiameter / 2, chunkBounds.z)
        )
      };
    }

#if DEBUG
    /// <summary>
    /// Get the stats of all the managers this level uses
    /// </summary>
    /// <returns></returns>
    public string getManagerStats() {
      return chunkFileDataLoadingManager.getCurrentStats() + '\n'
        + chunkVoxelDataGenerationManager.getCurrentStats() + '\n'
        + chunkMeshGenerationManager.getCurrentStats();
    }
#endif
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