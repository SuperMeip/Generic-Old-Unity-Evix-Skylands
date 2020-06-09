using System;

namespace MeepTech.Voxel.Generation.Sources {
  /// <summary>
  /// Voxel source for a single sphere
  /// </summary>
  public class SphereSource : VoxelSource {

    /// <summary>
    /// the radius of the sphere to generate
    /// </summary>
    int sphereRadius;

    /// <summary>
    /// the radius of the sphere to generate
    /// </summary>
    Coordinate sphereCenter;

    public SphereSource(int sphereRadius = 10, Coordinate sphereCenter = default) : base() {
      this.sphereRadius = sphereRadius;
      this.sphereCenter = sphereCenter;
    }

    /// <summary>
    /// Get values for sphere distance
    /// </summary>
    /// <param name="coordinate"></param>
    /// <returns></returns>
    protected override float getNoiseValueAt(Coordinate coordinate) {
      float distance = Math.Abs(sphereCenter.distance(coordinate));
      return RangeUtilities.ClampToFloat(distance, 0, (int)((sphereRadius) * 1.5f));
    }
  }
}