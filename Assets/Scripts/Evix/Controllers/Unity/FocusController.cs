using MeepTech.GamingBasics;
using MeepTech.Voxel;
using MeepTech.Voxel.Collections.Level;
using UnityEngine;

namespace Evix.Controllers.Unity {

  /// <summary>
  /// A player controller for use in unity
  /// </summary>
  public class FocusController : MonoBehaviour, ILevelFocus {

    /// <summary>
    /// If this player is active
    /// </summary>
    public bool isActive {
      get;
      private set;
    }

    /// <summary>
    /// The chunk location of this player
    /// </summary>
    public Coordinate chunkLocation {
      get;
      private set;
    }

    /// <summary>
    /// The previous chunk location of the character
    /// </summary>
    Coordinate previousChunkLocation;

    /// <summary>
    /// The world (voxel) location of this player
    /// </summary>
#if DEBUG
    [ReadOnly] public
#endif
    Vector3 worldLocation;

    /// <summary>
    /// the previous world location of the character
    /// </summary>
    Vector3 previousWorldLocation;

    void Update() {
      /// check to see if we should update the chunks
      if (!isActive) {
        return;
      }

      // if this is active and the world position has changed, check if the chunk has changed
      worldLocation = transform.position;
      if (worldLocation != previousWorldLocation) {
        previousWorldLocation = worldLocation;
        chunkLocation = worldLocation / Chunk.Diameter;
        if (!chunkLocation.Equals(previousChunkLocation)) {
          // if the chunk has changed, let everyone know this player is in a new chunk
          World.EventSystem.notifyChannelOf(
            new FocusChangedChunkLocationEvent(chunkLocation),
            EventSystems.WorldEventSystem.Channels.TerrainGeneration
          );
          previousChunkLocation = chunkLocation;
        }
      }
    }

    /// <summary>
    /// Spawn the player at the given location
    /// </summary>
    public void spawn(Coordinate spawnPoint) {
      transform.position = worldLocation = previousWorldLocation = spawnPoint.vec3;
      chunkLocation = previousChunkLocation = worldLocation / Chunk.Diameter;
      World.EventSystem.notifyAllOf(new SpawnFocusEvent(this));
    }

    /// <summary>
    /// set the controller active
    /// </summary>
    public void setActive() {
      isActive = true;
    }
  }
}
