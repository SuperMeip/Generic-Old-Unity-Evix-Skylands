
using UnityEngine;

namespace MeepTech.Voxel {

  /// <summary>
  /// Extention to Voxel, for terrain 'block' types
  /// </summary>
  public abstract class Terrain : Voxel {

    /// <summary>
    /// A terrain voxel type
    /// </summary>
    public new abstract class Type : Voxel.Type {

      /// <summary>
      /// If this block type is solid block or not
      /// </summary>
      public Color Color {
        get;
        protected set;
      } = Color.white;

      /// <summary>
      /// Make a new type
      /// </summary>
      /// <param name="id"></param>
      internal Type(byte id) : base(id) { }
    }


    /// <summary>
    /// A class for manipulating block types
    /// </summary>
    public static class Types {

      /// <summary>
      /// Air, an empty block
      /// </summary>
      public static Type Air = new Air();

      /// <summary>
      /// Stone, a solid rock block
      /// </summary>
      public static Type Stone = new Stone();

      /// <summary>
      /// Stone, a solid rock block
      /// </summary>
      public static Type Dirt = new Dirt();

      /// <summary>
      /// Stone, a solid rock block
      /// </summary>
      public static Type Placeholder = new Placeholder();

      /// <summary>
      /// All block types by id
      /// </summary>
      public static Type[] All = {
        Air,
        Placeholder,
        Stone,
        Dirt
      };

      public static Type Get(byte id) {
        return All[id];
      }
    }
  }

  /// <summary>
  /// An air block, empty
  /// </summary>
  internal class Air : Terrain.Type {
    internal Air() : base(0) {
      Density = 0;
      IsSolid = false;
    }
  }

  /// <summary>
  /// An empty block that's not air.
  /// Counts as solid but doesn't render
  /// </summary>
  internal class Placeholder : Terrain.Type {
    internal Placeholder() : base(1) {
      Color = Color.black;
    }
  }

  /// <summary>
  /// Stone, a solid rock block
  /// </summary>
  internal class Stone : Terrain.Type {
    internal Stone() : base(2) {
      Density = 128;
      Color = Color.blue;
    }
  }

  /// <summary>
  /// Dirt. a sort of solid block
  /// </summary>
  internal class Dirt : Terrain.Type {

    internal Dirt() : base(3) {
      Density = 54;
      Color = Color.green;
    }
  }
}
