
namespace MeepTech.Voxel.Generation.Sources {

  /// <summary>
  /// A wavy, hilly block source
  /// </summary>
  public class WaveSource : VoxelSource {

    public float xWaveFrequency = 0.1f;
    public float zWaveFrequency = 0.1f;
    public float smoothness = -1f;
    public float value4 = 1f;
    public float value5 = 0f;
    public float value6 = 1f;
    public float value7 = 10f;
    public float value8 = 20f;

    protected override float getNoiseValueAt(Coordinate location) {
      return location.y - noise.GetPerlin(location.x / xWaveFrequency, location.z / zWaveFrequency).GenMap(smoothness, value4, value5, value6) * 10 - 20;
    }
  }

  public static class WaveSourceUtiliy {

    /// <summary>
    /// Map values for terrain generation
    /// </summary>
    /// <param name="value"></param>
    /// <param name="x1"></param>
    /// <param name="y1"></param>
    /// <param name="x2"></param>
    /// <param name="y2"></param>
    /// <returns></returns>
    public static float GenMap(this float value, float x1, float y1, float x2, float y2) {
      return (value - x1) / (y1 - x1) * (y2 - x2) + x2;
    }
  }
}