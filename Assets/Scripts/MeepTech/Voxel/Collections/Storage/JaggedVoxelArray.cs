using System;
using System.Runtime.Serialization;

namespace MeepTech.Voxel.Collections.Storage {

  /// <summary>
  /// Jagged array dynamic block storage
  /// </summary>
  [Serializable]
  public class JaggedVoxelArray : VoxelStorage {

    /// <summary>
    /// if this is empty
    /// </summary>
    public override bool isEmpty
      => voxels == null;

    /// <summary>
    /// Not implimented yet. I may not use this collection
    /// </summary>
    public override bool isFull
      => voxelCount == bounds.x * bounds.y * bounds.z;

    /// <summary>
    /// block data
    /// </summary>
    byte[][][] voxels;

    /// <summary>
    /// the current number of solid voxels
    /// </summary>
    int voxelCount = 0;

    ///// CONSTRUCTORS

    /// <summary>
    /// make a new blockdata array
    /// </summary>
    /// <param name="bounds"></param>
    public JaggedVoxelArray(Coordinate bounds) : base(bounds) {
      voxels = null;
    }

    /// <summary>
    /// make a new blockdata array
    /// </summary>
    /// <param name="bounds"></param>
    public JaggedVoxelArray(int bound) : base(bound) {
      voxels = null;
    }

    /// <summary>
    /// Used for deserialization
    /// </summary>
    /// <param name="info"></param>
    /// <param name="context"></param>
    public JaggedVoxelArray(SerializationInfo info, StreamingContext context) : base(info) {
      voxels = (byte[][][])info.GetValue("voxels", typeof(byte[][][]));
    }

    ///// PUBLIC FUNCTIONS

    /// <summary>
    /// get a point
    /// </summary>
    /// <param name="location"></param>
    /// <returns></returns>
    public override Voxel.Type get(Coordinate location) {
      if (location.isWithin(Coordinate.Zero, bounds)) {
        return Terrain.Types.Get(tryToGetValue(location));
      }
      throw new IndexOutOfRangeException();
    }

    /// <summary>
    /// set a point's voxel value
    /// </summary>
    /// <param name="location"></param>
    /// <param name="newVoxelType"></param>
    public override void set(Coordinate location, byte newVoxelType) {
      if (location.isWithin(Coordinate.Zero, bounds)) {
        setValue(location, newVoxelType);
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
      info.AddValue("voxels", voxels, typeof(byte[][][]));
    }

    ///// SUB FUNCTIONS

    /// <summary>
    /// Set the value at the given point
    /// </summary>
    /// <param name="location"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    void setValue(Coordinate location, byte value) {
      // if the block value is zero and we'd need to resize the array to store it:
      //  just don't it's empty.
      if (voxels == null) {
        if (value != 0) {
          initilizeJaggedArray(location.x + 1);
        } else return;
      }

      // If this is beyond our current X, resize the x array
      if (voxels.Length <= location.x) {
        if (value != 0) {
          Array.Resize(ref voxels, location.x + 1);
        } else return;
      }

      // if there's no Y array at the X location, add one
      if (voxels[location.x] == null) {
        if (value != 0) {
          voxels[location.x] = new byte[location.y + 1][];
        } else return;
      }

      // if the Y array is too small, resize it
      if (voxels[location.x].Length <= location.y) {
        if (value != 0) {
          Array.Resize(ref voxels[location.x], location.y + 1);
        } else return;
      }

      // if there's no Z array at our location, add one
      if (voxels[location.x][location.y] == null) {
        if (value != 0) {
          voxels[location.x][location.y] = new byte[location.z + 1];
        } else return;
      }

      // if the Z array is too small, resize it
      if (voxels[location.x][location.y].Length <= location.z) {
        if (value != 0) {
          Array.Resize(ref voxels[location.x][location.y], location.z + 1);
        } else return;
      }

      /// set the block value
      voxels[location.x][location.y][location.z] = value;
    }

    /// <summary>
    /// Get the data at a location
    /// </summary>
    /// <param name="location"></param>
    /// <returns></returns>
    byte tryToGetValue(Coordinate location) {
      return (byte)(voxels != null
        ? location.x < voxels.Length
          ? location.y < voxels[location.x].Length
            ? location.z < voxels[location.x][location.y].Length
              ? voxels[location.x][location.y][location.z]
              : 0
            : 0
          : 0
        : 0
      );
    }

    /// <summary>
    /// Create the first row of the jagged array
    /// </summary>
    /// <param name="x"></param>
    void initilizeJaggedArray(int x = -1) {
      voxels = new byte[x == -1 ? bounds.x : x][][];
    }
  }
}