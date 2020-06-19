using MeepTech.Voxel.Collections.Storage;
using MeepTech.Voxel.Generation.Mesh;

namespace MeepTech.Voxel.Collections.Level {

  /// <summary>
  /// A voxel storage wrapper aware of it's neighbors
  /// </summary>
  public interface IVoxelChunk : IVoxelStorage {

    /// <summary>
    /// if this chunk's neighbors are all finished loading
    /// </summary>
    bool neighborsAreLoaded {
      get;
    }

    /// <summary>
    /// if this chunk's neighbors' neighbors are all finished loading
    /// </summary>
    bool neighborsNeighborsAreLoaded {
      get;
    }

    /// <summary>
    /// If this chunk has loaded it's mesh data
    /// </summary>
    bool isMeshed {
      get;
    }

    /// <summary>
    /// The X, Y, Z of this chunk's level location
    /// (NOT the world location of the 0,0,0 of the chunk)
    /// </summary>
    Coordinate location {
      get;
    }

    /// <summary>
    /// get the voxel data for this chunk
    /// </summary>
    IVoxelStorage voxels {
      get;
    }

    /// <summary>
    /// The The generated mesh for this voxel data set
    /// </summary>
    IMesh mesh {
      get;
    }
  }
}