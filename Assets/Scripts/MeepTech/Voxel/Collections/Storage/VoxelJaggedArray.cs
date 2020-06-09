using System;

namespace MeepTech.Voxel.Collections.Storage {

  /// <summary>
  /// Jagged array dynamic block storage
  /// </summary>
  public class VoxelJaggedArray : VoxelStorage {

    /// <summary>
    /// block data
    /// </summary>
    byte[][][] points;

    /// <summary>
    /// if this is empty
    /// </summary>
    public override bool isEmpty
      => points == null;

    /// <summary>
    /// make a new blockdata array
    /// </summary>
    /// <param name="bounds"></param>
    public VoxelJaggedArray(Coordinate bounds) : base(bounds) {
      points = null;
    }

    /// <summary>
    /// make a new blockdata array
    /// </summary>
    /// <param name="bounds"></param>
    public VoxelJaggedArray(int bound) : base(bound) {
      points = null;
    }

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
    /// Set the value at the given point
    /// </summary>
    /// <param name="location"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    void setValue(Coordinate location, byte value) {
      // if the block value is zero and we'd need to resize the array to store it:
      //  just don't it's empty.
      if (points == null) {
        if (value != 0) {
          initilizeJaggedArray(location.x + 1);
        } else return;
      }

      // If this is beyond our current X, resize the x array
      if (points.Length <= location.x) {
        if (value != 0) {
          Array.Resize(ref points, location.x + 1);
        } else return;
      }

      // if there's no Y array at the X location, add one
      if (points[location.x] == null) {
        if (value != 0) {
          points[location.x] = new byte[location.y + 1][];
        } else return;
      }

      // if the Y array is too small, resize it
      if (points[location.x].Length <= location.y) {
        if (value != 0) {
          Array.Resize(ref points[location.x], location.y + 1);
        } else return;
      }

      // if there's no Z array at our location, add one
      if (points[location.x][location.y] == null) {
        if (value != 0) {
          points[location.x][location.y] = new byte[location.z + 1];
        } else return;
      }

      // if the Z array is too small, resize it
      if (points[location.x][location.y].Length <= location.z) {
        if (value != 0) {
          Array.Resize(ref points[location.x][location.y], location.z + 1);
        } else return;
      }

      /// set the block value
      points[location.x][location.y][location.z] = value;
    }

    /// <summary>
    /// Get the data at a location
    /// </summary>
    /// <param name="location"></param>
    /// <returns></returns>
    byte tryToGetValue(Coordinate location) {
      return (byte)(points != null
        ? location.x < points.Length
          ? location.y < points[location.x].Length
            ? location.z < points[location.x][location.y].Length
              ? points[location.x][location.y][location.z]
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
      points = new byte[x == -1 ? bounds.x : x][][];
    }
  }
}