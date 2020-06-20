using MeepTech;
using MeepTech.Voxel;
using MeepTech.Voxel.Generation.Sources;

namespace Evix.Terrain.Sources {
  class BumpyPlainsSource : VoxelSource {

    protected override void setUpNoise() {
      noise.SetNoiseType(MeepTech.Voxel.Generation.Sources.Noise.FastNoise.NoiseType.Cellular);
    }

    protected override float getNoiseValueAt(Coordinate location) {
      return noise.GetPerlin(location.x, location.z);
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
