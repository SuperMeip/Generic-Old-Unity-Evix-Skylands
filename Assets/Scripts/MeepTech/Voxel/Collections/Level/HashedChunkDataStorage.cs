using MeepTech.Voxel.Collections.Storage;
using MeepTech.Voxel.Generation.Mesh;
using System.Collections.Generic;

namespace MeepTech.Voxel.Collections.Level {

  /// <summary>
  /// A chunk data storage method that uses hash maps/dictionaries
  /// </summary>
  class HashedChunkDataStorage : ChunkDataStorage {

    /// <summary>
    /// The active chunks, stored by coordinate location
    /// </summary>
    Dictionary<long, IVoxelStorage> loadedChunkVoxelData;

    /// <summary>
    /// the loaded chunk meshes
    /// </summary>
    Dictionary<long, IMesh> loadedChunkMeshes;

    /// <summary>
    /// The parent level
    /// </summary>
    ILevel level;

    /// <summary>
    /// Construct
    /// </summary>
    /// <param name="chunkBounds"></param>
    /// <param name="voxelSource"></param>
    public HashedChunkDataStorage(ILevel level) {
      this.level = level;
      loadedChunkVoxelData = new Dictionary<long, IVoxelStorage>();
      loadedChunkMeshes = new Dictionary<long, IMesh>();
    }

    /// <summary>
    /// Get the chunk from the hash map
    /// </summary>
    /// <param name="chunkLocation"></param>
    /// <returns></returns>
    public override IVoxelStorage getChunkVoxelData(Coordinate chunkLocation) {
      lock (loadedChunkVoxelData) {
        return chunkLocation.isWithin(Coordinate.Zero, level.chunkBounds)
        && loadedChunkVoxelData.ContainsKey(GetChunkHash(chunkLocation))
          ? loadedChunkVoxelData[GetChunkHash(chunkLocation)]
          : default;
      }
    }

    /// <summary>
    /// Set the given set of voxel data to the given chunk location
    /// </summary>
    /// <param name="chunkLocation"></param>
    /// <param name="voxelData"></param>
    public override void setChunkVoxelData(Coordinate chunkLocation, IVoxelStorage voxelData) {
      lock (loadedChunkVoxelData) {
        loadedChunkVoxelData[GetChunkHash(chunkLocation)] = voxelData;
      }
    }

    /// <summary>
    /// Remove the chunk at the given location
    /// </summary>
    /// <param name="chunkLocation"></param>
    public override void removeChunkVoxelData(Coordinate chunkLocation) {
      lock (loadedChunkVoxelData) {
        loadedChunkVoxelData.Remove(GetChunkHash(chunkLocation));
      }
    }

    /// <summary>
    /// Set the chunk mesh for the given location
    /// </summary>
    /// <param name="chunkLocation"></param>
    /// <param name="chunkMesh"></param>
    public override void setChunkMesh(Coordinate chunkLocation, IMesh chunkMesh) {
      lock (loadedChunkMeshes) {
        loadedChunkMeshes[GetChunkHash(chunkLocation)] = chunkMesh;
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="chunkLocation"></param>
    /// <param name="chunkMesh"></param>
    public override void removeChunkMesh(Coordinate chunkLocation) {
      lock (loadedChunkMeshes) {
        loadedChunkMeshes.Remove(GetChunkHash(chunkLocation));
      }
    }

    /// <summary>
    /// if this contains the chunk mesh key
    /// </summary>
    /// <param name="chunkLocation"></param>
    /// <param name="chunkMesh"></param>
    public override bool containsChunkMesh(Coordinate chunkLocation) {
      lock (loadedChunkMeshes) {
        return loadedChunkMeshes.ContainsKey(GetChunkHash(chunkLocation));
      }
    }

    /// <summary>
    /// Get the mesh for a chunk if one's loaded
    /// </summary>
    /// <param name="chunkLocation"></param>
    /// <returns></returns>
    public override IMesh getChunkMesh(Coordinate chunkLocation) {
      lock (loadedChunkMeshes) {
        return chunkLocation.isWithin(Coordinate.Zero, level.chunkBounds)
        && containsChunkMesh(chunkLocation)
          ? loadedChunkMeshes[GetChunkHash(chunkLocation)]
          : default;
      }
    }

    /// <summary>
    /// Get the hash key for the chunk's location
    /// todo: add property longHash to coordinate
    /// </summary>
    /// <returns></returns>
    protected static long GetChunkHash(Coordinate chunkLocation) {
      long hash = 0;
      hash |= ((short)chunkLocation.x);
      hash |= (((short)chunkLocation.y) << 16);
      hash |= (((short)chunkLocation.z) << 24);

      return hash;
    }
  }
}
