using System;

namespace MeepTech.Voxel.Collections.Storage {

  /// <summary>
  /// A type of voxel storage that uses a 1D byte array
  /// </summary>
  public class VoxelFlatArray : VoxelStorage {
    /// <summary>
    /// The actual points
    /// </summary>
    byte[] points;

    /// <summary>
    /// If this storage is empty
    /// </summary>
    public override bool isEmpty 
      => points == null;

    /// <summary>
    /// make a new voxel data array
    /// </summary>
    /// <param name="bounds"></param>
    public VoxelFlatArray(Coordinate bounds) : base(bounds) {
      points = null;
    }

    /// <summary>
    /// make a new voxel data array
    /// </summary>
    /// <param name="bounds"></param>
    public VoxelFlatArray(int bound) : base(bound) {
      points = null;
    }
    
    /// <summary>
    /// Get the voxel at the given location
    /// </summary>
    /// <param name="location"></param>
    /// <returns></returns>
    public override Voxel.Type get(Coordinate location) {
      if (location.isWithin(Coordinate.Zero, bounds)) {
        if (points == null) {
          return Terrain.Types.Get(0);
        }
        return Terrain.Types.Get(
          points[location.x + bounds.x * (location.y + bounds.y * location.z)]
        );
      } else {
        throw new IndexOutOfRangeException();
      }
    }

    public override void set(Coordinate location, byte newVoxelType) {
      if (location.isWithin(Coordinate.Zero, bounds)) {
        if (points == null) {
          if (newVoxelType == 0) {
            return;
          }
          initPointArray();
        }
        points[location.x + bounds.x * (location.y + bounds.y * location.z)] = newVoxelType;
      } else {
        throw new IndexOutOfRangeException();
      }
    }

    void initPointArray() {
      points = new byte[bounds.x * bounds.y * bounds.z];
    }
  }
}
