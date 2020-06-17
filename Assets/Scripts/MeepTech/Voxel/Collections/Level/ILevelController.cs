using MeepTech.Events;

namespace MeepTech.Voxel.Collections.Level {

  /// <summary>
  /// Used to control a level in the game space
  /// </summary>
  public interface ILevelController: IObserver {

    /// <summary>
    /// The level this is controlling
    /// </summary>
    ILevel  level {
      get;
    }

    /// <summary>
    /// Initilize this controller for the given level
    /// </summary>
    /// <param name="level"></param>
    void initializeFor(ILevel  level);
  }
}
