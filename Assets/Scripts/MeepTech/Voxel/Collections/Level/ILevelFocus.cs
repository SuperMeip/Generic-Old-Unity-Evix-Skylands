using MeepTech.Events;

namespace MeepTech.Voxel.Collections.Level {

  public interface ILevelFocus {

    /// <summary>
    /// Get if this focus is active
    /// </summary>
    bool isActive {
      get;
    }

    /// <summary>
    /// The chunk location of this focus
    /// </summary>
    Coordinate chunkLocation {
      get;
    }

    /// <summary>
    /// set the focus active;
    /// </summary>
    void setActive();
  }
}