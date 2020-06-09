using MeepTech.Voxel.Collections.Storage;

namespace MeepTech.Voxel.Generation.Mesh {

  /// <summary>
  /// Interface for a mesh renderer for voxel volumes
  /// </summary>
  /// 
  public interface IVoxelMeshGenerator {

    /// <summary>
    /// Generate a vertex mesh from a set of voxel data
    /// </summary>
    /// <param name="blockData"></param>
    /// <param name="isoSurfaceLevel"></param>
    /// <returns></returns>
    IMesh generateMesh(IVoxelStorage blockData);
  }
}