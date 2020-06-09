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