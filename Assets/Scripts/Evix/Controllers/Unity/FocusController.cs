using MeepTech.GamingBasics;
using MeepTech.Voxel;
using MeepTech.Voxel.Collections.Level;
using UnityEngine;

namespace Evix.Controllers.Unity {

  /// <summary>
  /// A player controller for use in unity
  /// </summary>
  public class FocusController : MonoBehaviour {

    /// <summary>
    /// The world (voxel) location of this player
    /// </summary>
    [ReadOnly] public Vector3 worldLocation;

    /// <summary>
    /// The chunk location of this player
    /// </summary>
    [ReadOnly] public Coordinate chunkLocation;

    /// <summary>
    /// If this player is active
    /// </summary>
    [ReadOnly] public bool isActive;

    /// <summary>
    /// The previous chunk location of the character
    /// </summary>
    Coordinate previousChunkLocation;

    /// <summary>
    /// the previous world location of the character
    /// </summary>
    Vector3 previousWorldLocation;

    void Update() {
      /// check to see if we should update the chunks
      if (isActive) {
        // if this is active and the world position has changed, check if the chunk has changed
        worldLocation = transform.position;
        if (worldLocation != previousWorldLocation) {
          previousWorldLocation = worldLocation;
          chunkLocation = worldLocation / Chunk.Diameter;
          if (!chunkLocation.Equals(previousChunkLocation)) {
            // if the chunk has changed, let everyone know this player is in a new chunk
            World.EventSystem.notifyChannelOf(
              new Player.ChangeChunkLocationEvent(chunkLocation),
              EventSystems.WorldEventSystem.Channels.TerrainGeneration
            );
            previousChunkLocation = chunkLocation;
          }
        }
      }
    }

    /// <summary>
    /// Spawn the player at the given location
    /// </summary>
    public void spawn(Coordinate spawnPoint) {
      transform.position = worldLocation = spawnPoint.vec3;
      World.EventSystem.notifyAllOf(new Player.SpawnEvent(spawnPoint.vec3));
      isActive = true;
    }
  }
}
