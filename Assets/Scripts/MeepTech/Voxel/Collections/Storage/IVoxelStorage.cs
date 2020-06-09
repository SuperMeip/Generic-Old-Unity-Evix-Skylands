namespace MeepTech.Voxel.Collections.Storage {

  /// <summary>
  /// Interface for storing voxel
  /// </summary>
  public interface IVoxelStorage {

    /// <summary>
    /// if this voxel storage source is empty
    /// </summary>
    bool isEmpty {
      get;
    }

    /// <summary>
    /// if this block storage source has finished being
    ///    generated or loaded from file a file
    /// </summary>
    bool isLoaded {
      get;
      set;
    }

    /// <summary>
    /// The itteratable bounds of this collection of voxel, x, y, and z
    /// </summary>
    Coordinate bounds {
      get;
    }

    /// <summary>
    /// Get thevoxel type at the given x,y,z
    /// </summary>
    /// <param name="location">the x,y,z of the block/point data to get</param>
    /// <returns>The voxel type</returns>
    Voxel.Type get(Coordinate location);

    /// <summary>
    /// Update the point at the given location with a new voxel type, and potentially a new density value
    /// </summary>
    /// <param name="location">The xyz of the point to update</param>
    /// <param name="blockType">the type of voxel to set to</param>
    void set(Coordinate location, Voxel.Type newBlockType);
  }
}