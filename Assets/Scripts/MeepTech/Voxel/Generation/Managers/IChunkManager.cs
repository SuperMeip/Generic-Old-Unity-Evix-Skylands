using MeepTech.Events;

namespace MeepTech.Voxel.Generation.Managers {
  /// <summary>
  /// Manages the storage and loading/unloading of chunk data for levels.
  /// </summary>
  public interface IChunkManager : IObserver {

    /// <summary>
    /// Kill all loading or jobs managemed by this manager.
    /// </summary>
    void killAll();

#if DEBUG
    /// <summary>
    /// Get the current stats of the manager
    /// </summary>
    /// <returns></returns>
    string getCurrentStats();
#endif
  }
}
