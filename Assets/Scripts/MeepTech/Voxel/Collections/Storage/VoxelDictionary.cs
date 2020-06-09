using System;
using System.Collections.Generic;

namespace MeepTech.Voxel.Collections.Storage {

  /// <summary>
  /// A collection of voxel data stored by point location in a dictionary
  /// </summary>
  public class VoxelDictionary : VoxelStorage {

    /// <summary>
    /// if this storage set is completely full of voxels
    /// </summary>
    public bool isFull {
      get => points.Count == bounds.x * bounds.y * bounds.z;
    }

    /// <summary>
    /// if there are no voxels in this storage object
    /// </summary>
    public override bool isEmpty
      => points == null || points.Count == 0;

    /// <summary>
    /// The collection of points, a byte representing the material the point is made of
    /// </summary>
    IDictionary<Coordinate, byte> points;

    /// <summary>
    /// Create a new marching point voxel dictionary of the given size
    /// </summary>
    /// <param name="bounds"></param>
    public VoxelDictionary(Coordinate bounds) : base(bounds) {
      points = new Dictionary<Coordinate, byte>();
    } //int version:
    public VoxelDictionary(int bound) : this(new Coordinate(bound)) { }

    /// <summary>
    /// Get the voxel at the location from the dictionary
    /// </summary>
    /// <param name="location"></param>
    /// <returns></returns>
    public override Voxel.Type get(Coordinate location) {
      points.TryGetValue(location, out byte value);
      return Terrain.Types.Get(value);
    }

    /// <summary>
    /// Overwrite the entire point at the given location
    /// </summary>
    /// <param name="location">the x,y,z of the voxel to set</param>
    /// <param name="newVoxelValue">The voxel data to set as a bitmask:
    ///   byte 1: the voxel type id
    ///   byte 2: the voxel vertex mask
    ///   byte 3 & 4: the voxel's scalar density float, compresed to a short
    /// </param>
    public override void set(Coordinate location, byte newVoxelValue) {
      if (location.isWithin(Coordinate.Zero, bounds)) {
        points[location] = newVoxelValue;
      } else {
        throw new IndexOutOfRangeException();
      }
    }
  }
}