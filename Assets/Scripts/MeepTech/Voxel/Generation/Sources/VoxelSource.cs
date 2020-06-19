using MeepTech.Voxel.Collections.Storage;

namespace MeepTech.Voxel.Generation.Sources {

  /// <summary>
  /// Base class for a voxel source
  /// </summary>
  public abstract class VoxelSource : IVoxelSource {

    /// <summary>
    /// Generated voxels for this source.
    /// @todod, make this instanced to make it thread safe?
    /// </summary>
    public static int VoxelsGenerated = 0;

    /// <summary>
    /// The generation seed
    /// </summary>
    public int seed {
      get;
    }

    /// <summary>
    /// The noise generator used for this voxel source
    /// </summary>
    protected Noise.FastNoise noise;

    /// <summary>
    /// Create a new voxel source
    /// </summary>
    public VoxelSource(int seed = 1234) {
      this.seed = seed;
      noise = new Noise.FastNoise(seed);
      setUpNoise();
    }

    /// <summary>
    /// Must be implimented, get the noise density float (0 -> 1) for a given point
    /// </summary>
    /// <param name="location">the x y z to get the iso density for</param>
    /// <returns></returns>
    protected abstract float getNoiseValueAt(Coordinate location);

    /// <summary>
    /// Function for setting up noise before generation
    /// </summary>
    protected virtual void setUpNoise() { }

    /// <summary>
    /// Get the voxel type for the density
    /// </summary>
    /// <param name="noiseValue"></param>
    /// <returns></returns>
    protected abstract Voxel.Type getVoxelTypeFor(float noiseValue, Coordinate location);

    /// <summary>
    /// Generate all the voxels in the given collection with this source
    /// </summary>
    /// <param name="voxelData"></param>
    public void generateAll(IVoxelStorage voxelData) {
      generateAllAt(Coordinate.Zero, voxelData);
    }

    /// <summary>
    /// Generate the given set of voxeldata at the given location offset
    /// </summary>
    /// <param name="location">The xyz to use as an offset for generating these voxels</param>
    /// <param name="voxelData">The voxel data to populate</param>
    public void generateAllAt(Coordinate location, IVoxelStorage voxelData) {
      Coordinate.Zero.until(voxelData.bounds, (coordinate) => {
        VoxelsGenerated++;
        Coordinate globalLocation = coordinate + (location * voxelData.bounds);
        float noiseValue = getNoiseValueAt(globalLocation);
        Voxel.Type newVoxelType = getVoxelTypeFor(noiseValue, globalLocation);
        if (newVoxelType != Voxel.Types.Empty) {
          voxelData.set(coordinate, newVoxelType);
        }
      });
    }
  }
}