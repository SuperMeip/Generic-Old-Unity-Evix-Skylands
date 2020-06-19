using Evix.Terrain;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MeepTech.Voxel.Collections.Storage {

  /// <summary>
  /// A collection of voxel data stored by point location in a dictionary
  /// </summary> 
  [Serializable]
  public class VoxelDictionary : VoxelStorage {

    /// <summary>
    /// if this storage set is completely full of voxels
    /// </summary>
    public override bool isFull {
      get => voxels.Count == bounds.x * bounds.y * bounds.z;
    }

    /// <summary>
    /// if there are no voxels in this storage object
    /// </summary>
    public override bool isEmpty
      => voxels == null || voxels.Count == 0;

    /// <summary>
    /// The collection of points, a byte representing the material the point is made of
    /// </summary>
    IDictionary<Coordinate, byte> voxels;

    ///// CONSTRUCTORS  

    /// <summary>
    /// Create a new marching point voxel dictionary of the given size
    /// </summary>
    /// <param name="bounds"></param>
    public VoxelDictionary(Coordinate bounds) : base(bounds) {
      voxels = new Dictionary<Coordinate, byte>();
    } //int version:
    public VoxelDictionary(int bound) : this(new Coordinate(bound)) { }

    /// <summary>
    /// Used for deserialization
    /// </summary>
    /// <param name="info"></param>
    /// <param name="context"></param>
    public VoxelDictionary(SerializationInfo info, StreamingContext context) : base(info) {
      voxels = (IDictionary<Coordinate, byte>)info.GetValue("voxels", typeof(IDictionary<Coordinate, byte>));
    }

    ///// PUBLIC FUNCTIONS

    /// <summary>
    /// Get the voxel at the location from the dictionary
    /// </summary>
    /// <param name="location"></param>
    /// <returns></returns>
    public override Voxel.Type get(Coordinate location) {
      voxels.TryGetValue(location, out byte value);
      return TerrainBlock.Types.Get(value);
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
        voxels[location] = newVoxelValue;
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
      info.AddValue("voxels", voxels, typeof(IDictionary<Coordinate, byte>));
    }
  }
}