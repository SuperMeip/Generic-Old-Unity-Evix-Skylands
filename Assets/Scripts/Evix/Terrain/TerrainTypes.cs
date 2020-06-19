
using MeepTech.Voxel;
using UnityEngine;

namespace Evix.Terrain {

  /// <summary>
  /// Extention to Voxel, for terrain 'block' types
  /// </summary>
  public abstract class TerrainBlock : Voxel {

    /// <summary>
    /// A terrain voxel type
    /// </summary>
    public new abstract class Type : Voxel.Type {

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
      internal Type(byte id) : base(id) { }
    }

    /// <summary>
    /// A class for manipulating block types
    /// </summary>
    public new class Types {

      /// <summary>
      /// Air, an empty block
      /// </summary>
      public static Type Air {
        get;
      } = new Air();

      /// <summary>
      /// Stone, a solid rock block
      /// </summary>
      public static Type Stone {
        get;
      } = new Stone();

      /// <summary>
      /// Stone, a solid rock block
      /// </summary>
      public static Type Dirt {
        get;
      } = new Dirt();

      /// <summary>
      /// Stone, a solid rock block
      /// </summary>
      public static Type Placeholder {
        get;
      } = new Placeholder();

      /// <summary>
      /// All block types by id
      /// </summary>
      public static Type[] All {
        get;
      } = {
        Air,
        Placeholder,
        Stone,
        Dirt
      };

      /// <summary>
      /// Get the type by id
      /// </summary>
      /// <param name="id"></param>
      /// <returns></returns>
      public static Voxel.Type Get(byte id) {
        return All[id];
      }
    }
  }

  /// <summary>
  /// An air block, empty
  /// </summary>
  internal class Air : TerrainBlock.Type {
    internal Air() : base(0) {
      Density = 0;
      IsSolid = false;
      Color = System.Drawing.Color.Transparent;
    }
  }

  /// <summary>
  /// An empty block that's not air.
  /// Counts as solid but doesn't render
  /// </summary>
  internal class Placeholder : TerrainBlock.Type {
    internal Placeholder() : base(1) {
      Color = System.Drawing.Color.Purple;
    }
  }

  /// <summary>
  /// Stone, a solid rock block
  /// </summary>
  internal class Stone : TerrainBlock.Type {
    internal Stone() : base(2) {
      Density = 128;
      Color = System.Drawing.Color.LightSlateGray;
    }
  }

  /// <summary>
  /// Dirt. a sort of solid block
  /// </summary>
  internal class Dirt : TerrainBlock.Type {
    internal Dirt() : base(3) {
      Density = 54;
      Color = System.Drawing.Color.Brown;
    }
  }

  /// <summary>
  /// Convert a system color to a unity color
  /// </summary>
  public static class ConvertToUnityColor {
    public static Color ToUnityColor(this System.Drawing.Color input) {
      return new Color32(input.R, input.G, input.B, input.A);
    }
  }
}
