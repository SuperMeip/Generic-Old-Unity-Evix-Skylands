/// <summary>
/// Used for interfacing with blocks
/// </summary>
namespace MeepTech.Voxel {

  /// <summary>
  /// Used for creating voxel constants
  /// </summary>
  public abstract class Voxel {

    /// <summary>
    /// A class for storing the values of each type of block
    /// </summary>
    public abstract class Type : IVoxelType {

      /// <summary>
      /// The ID of the block
      /// </summary>
      public byte Id {
        get;
        protected set;
      }

      /// <summary>
      /// If this block type is solid block or not
      /// </summary>
      public bool IsSolid {
        get;
        protected set;
      } = true;

      /// <summary>
      /// How hard/solid this block is. 0 is air.
      /// </summary>
      public byte Density {
        get;
        protected set;
      } = 64;

      /// <summary>
      /// Make a new type
      /// </summary>
      /// <param name="id"></param>
      internal Type(byte id) {
        Id = id;
      }
    }
  }
}