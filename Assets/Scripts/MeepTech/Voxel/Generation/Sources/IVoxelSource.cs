using MeepTech.Voxel.Collections.Storage;

namespace MeepTech.Voxel.Generation.Sources {
  /// <summary>
  /// A source of voxels, usually using nosie
  /// </summary>
  public interface IVoxelSource {

    /// <summary>
    /// The seed for this voxel source
    /// </summary>
    int seed {
      get;
    }

    /// <summary>
    /// Generate all given voxels
    /// </summary>
    /// <param name="blockData"></param>
    void generateAll(IVoxelStorage blockData);

    /// <summary>
    /// Generate all given voxels, using a location offset
    /// </summary>
    /// <param name="location">the offset of the blocks</param>
    /// <param name="blockData">the empty block collection</param>
    void generateAllAt(Coordinate location, IVoxelStorage blockData);
  }
}