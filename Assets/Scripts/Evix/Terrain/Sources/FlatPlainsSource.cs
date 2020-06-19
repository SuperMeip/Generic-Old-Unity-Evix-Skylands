using MeepTech.Voxel;
using MeepTech.Voxel.Generation.Sources;

namespace Evix.Terrain.Sources {
  public class FlatPlainsSource : VoxelSource {

    public float seaLevel = 30f;

    protected override float getNoiseValueAt(Coordinate location) {
      if (location.y > seaLevel) {
        return 0.0f;
      } else if (location.y == seaLevel) {
        return 3.0f;
      } else {
        return 2.0f;
      }
    }

    /// <summary>
    /// Get the voxel type for the density
    /// </summary>
    /// <param name="isoSurfaceDensityValue"></param>
    /// <returns></returns>
    protected override Voxel.Type getVoxelTypeFor(float isoSurfaceDensityValue, Coordinate location) {
      return TerrainBlock.Types.Get((byte)(int)isoSurfaceDensityValue);
    }
  }
}
