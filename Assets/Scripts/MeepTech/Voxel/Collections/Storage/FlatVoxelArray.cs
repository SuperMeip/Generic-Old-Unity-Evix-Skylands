using System;
using System.Runtime.Serialization;

namespace MeepTech.Voxel.Collections.Storage {

  /// <summary>
  /// A type of voxel storage that uses a 1D byte array
  /// </summary>
  [Serializable]
  public class FlatVoxelArray : VoxelStorage {

    /// <summary>
    /// If this storage is empty
    /// </summary>
    public override bool isEmpty
      => voxels == null;

    /// <summary>
    /// make a new voxel data array
    /// </summary>
    /// <param name="bounds"></param>
    public FlatVoxelArray(Coordinate bounds) : base(bounds) {
      voxels = null;
    }

    /// <summary>
    /// The actual points
    /// </summary>
    byte[] voxels;

    ///// CONSTRUCTORS

    /// <summary>
    /// make a new voxel data array
    /// </summary>
    /// <param name="bounds"></param>
    public FlatVoxelArray(int bound) : base(bound) {
      voxels = null;
    }

    /// <summary>
    /// Used for deserialization
    /// </summary>
    /// <param name="info"></param>
    /// <param name="context"></param>
    public FlatVoxelArray(SerializationInfo info, StreamingContext context) : base (info) {
      voxels = (byte[])info.GetValue("voxels", typeof(byte[]));
    }

    ///// PUBLIC FUNCTIONS
    
    /// <summary>
    /// Get the voxel at the given location
    /// </summary>
    /// <param name="location"></param>
    /// <returns></returns>
    public override Voxel.Type get(Coordinate location) {
      if (location.isWithin(Coordinate.Zero, bounds)) {
        if (voxels == null) {
          return Terrain.Types.Get(0);
        }
        return Terrain.Types.Get(
          voxels[location.x + bounds.x * (location.y + bounds.y * location.z)]
        );
      } else {
        throw new IndexOutOfRangeException();
      }
    }

    /// <summary>
    /// Set a voxel
    /// </summary>
    /// <param name="location"></param>
    /// <param name="newVoxelType"></param>
    public override void set(Coordinate location, byte newVoxelType) {
      if (location.isWithin(Coordinate.Zero, bounds)) {
        if (voxels == null) {
          if (newVoxelType == 0) {
            return;
          }
          initVoxelArray();
        }
        voxels[location.x + bounds.x * (location.y + bounds.y * location.z)] = newVoxelType;
      } else {
        throw new IndexOutOfRangeException();
      }
    }

    /// <summary>
    /// Get the data object for this serialized voxel array
    /// </summary>
    /// <param name="info"></param>
    /// <param name="context"></param>
    public override void GetObjectData(SerializationInfo info, StreamingContext context) {
      info.AddValue("bounds", bounds, typeof(Coordinate));
      info.AddValue("voxels", voxels, typeof(byte[]));
    }

    ///// SUB FUNCTIONS

    /// <summary>
    /// create a new voxel array
    /// </summary>
    void initVoxelArray() {
      voxels = new byte[bounds.x * bounds.y * bounds.z];
    }
  }
}
