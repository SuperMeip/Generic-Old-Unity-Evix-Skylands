using MeepTech.Events;

namespace MeepTech.Voxel.Generation.Managers {
  /// <summary>
  /// Manages the storage and loading/unloading of chunk data for levels.
  /// </summary>
  public interface IChunkManager : IObserver {

    /// <summary>
    /// Get the current stats of the manager
    /// </summary>
    /// <returns></returns>
    string getCurrentStats();
  }
}
