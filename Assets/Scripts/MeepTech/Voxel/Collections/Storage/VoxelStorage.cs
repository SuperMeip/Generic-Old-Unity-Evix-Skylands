using System;
using System.Runtime.Serialization;

namespace MeepTech.Voxel.Collections.Storage {

  /// <summary>
  /// Base class for simple voxel storage
  /// </summary>
  [Serializable]
  public abstract class VoxelStorage : IVoxelStorage, ISerializable {

    /// <summary>
    /// The itteratable bounds of this collection of voxels, x, y, and z
    /// </summary>
    public Coordinate bounds {
      get;
      protected set;
    }

    /// <summary>
    /// if this storage set is empty of voxels
    /// </summary>
    public abstract bool isEmpty {
      get;
    }

    /// <summary>
    /// if this storage set is empty of voxels
    /// </summary>
    public bool isLoaded {
      get;
      set;
    } = false;

    /// <summary>
    /// Generic base constructor
    /// </summary>
    /// <param name="bounds"></param>
    public VoxelStorage(Coordinate bounds) {
      setBounds(bounds);
    }

    /// <summary>
    /// Used for deserialization base
    /// </summary>
    /// <param name="info"></param>
    /// <param name="context"></param>
    protected VoxelStorage(SerializationInfo info) {
      bounds = (Coordinate)info.GetValue("bounds", typeof(Coordinate));
    }

    /// <summary>
    /// Base constructor all same bounds
    /// </summary>
    /// <param name="bound">x,y,and z's shared max bound</param>
    public VoxelStorage(int bound) : this(new Coordinate(bound)) { }

    /// <summary>
    /// Get the voxel data as an int bitmask at the given x,y,z
    /// </summary>
    /// <param name="location">the x,y,z of the voxel/point data to get</param>
    /// <returns>the voxel type</returns>
    public abstract Voxel.Type get(Coordinate location);

    /// <summary>
    /// Overwrite the entire voxel at the given location
    /// </summary>
    /// <param name="location">the x,y,z of the voxel to set</param>
    /// <param name="newVoxelType">the new type to set for the given voxel</param>
    public abstract void set(Coordinate location, byte newVoxelType);

    /// <summary>
    /// Serialization
    /// </summary>
    /// <param name="info"></param>
    /// <param name="context"></param>
    public abstract void GetObjectData(SerializationInfo info, StreamingContext context);

    /// <summary>
    /// Overwrite the entire voxel at the given location
    /// </summary>
    /// <param name="location">the x,y,z of the voxel to set</param>
    /// <param name="newVoxelType">the new type to set for the given voxel</param>
    public void set(Coordinate location, Voxel.Type newVoxelType) {
      set(location, newVoxelType.Id);
    }

    /// <summary>
    /// set the bounds based on a provided x y and z
    /// </summary>
    /// <param name="newBounds"></param>
    protected virtual void setBounds(Coordinate newBounds) {
      bounds = newBounds;
    }
  }
}