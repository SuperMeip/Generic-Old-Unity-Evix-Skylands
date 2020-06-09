namespace MeepTech.Voxel {

  /// <summary>
  /// USed for manipulating generic voxeltypes
  /// </summary>
  interface IVoxelType {

    /// <summary>
    /// The ID of the voxel
    /// </summary>
    byte Id {
      get;
    }

    /// <summary>
    /// If this voxel type is solid or not
    /// </summary>
    bool IsSolid {
      get;
    }

    /// <summary>
    /// How hard/solid this voxel is.
    /// 0 is invisible, 1 is fully solid.
    /// </summary>
    byte Density {
      get;
    }
  }
}