using MeepTech.GamingBasics;
using MeepTech.Voxel;
using MeepTech.Voxel.Collections.Level;
using MeepTech.Voxel.Collections.Level.Management;
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

    ///// UNITY FUNCTIONS

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
            new Level.FocusChangedChunkLocationEvent(this),
            EventSystems.WorldEventSystem.Channels.LevelFocusUpdates,
            true
          );
          previousChunkLocation = chunkLocation;
        }
      }
    }

#if DEBUG

    void OnDrawGizmos() {
      // ignore gizmo if inactive
      if (!isActive) {
        return;
      }

      ILevel level = World.Current.activeLevel;
      Vector3 worldChunkLocation = ((chunkLocation * Chunk.Diameter) + (Chunk.Diameter / 2)).vec3;

      /// draw the chunk this focus is in
      Gizmos.color = new Color(1.0f, 0.64f, 0.0f);
      Gizmos.DrawWireCube(worldChunkLocation, new Vector3(Chunk.Diameter, Chunk.Diameter, Chunk.Diameter));
      worldChunkLocation -= new Vector3((Chunk.Diameter / 2), (Chunk.Diameter / 2), (Chunk.Diameter / 2));

      /// draw the active chunk area
      IChunkResolutionAperture activeAperture = level.getApetureForResolutionLayer(Level.FocusResolutionLayers.Visible);
      Gizmos.color = Color.green;
      Gizmos.DrawWireCube(worldChunkLocation, new Vector3(
        activeAperture.managedChunkRadius * 2,
        activeAperture.managedChunkHeightRadius * 2,
        activeAperture.managedChunkRadius * 2
      ) * Chunk.Diameter);

      /// draw the meshed chunk area
      IChunkResolutionAperture meshAperture = level.getApetureForResolutionLayer(Level.FocusResolutionLayers.Meshed);
      Gizmos.color = Color.blue;
      Gizmos.DrawWireCube(worldChunkLocation, new Vector3(
        meshAperture.managedChunkRadius * 2,
        meshAperture.managedChunkHeightRadius * 2,
        meshAperture.managedChunkRadius * 2
      ) * Chunk.Diameter);

      // draw all the chunks in the mnesh loading queue right now
      Gizmos.color = Color.blue;
      foreach (Coordinate loadingChunkLocation in meshAperture.getProcessingChunks()) {
        Gizmos.DrawWireCube(
          ((loadingChunkLocation * Chunk.Diameter) + (Chunk.Diameter / 2)).vec3,
          new Vector3(Chunk.Diameter, Chunk.Diameter, Chunk.Diameter)
        );
      }

      /// draw the meshed chunk area
      IChunkResolutionAperture loadedAperture = level.getApetureForResolutionLayer(Level.FocusResolutionLayers.Loaded);
      Gizmos.color = Color.yellow;
      Gizmos.DrawWireCube(worldChunkLocation, new Vector3(
        loadedAperture.managedChunkRadius * 2,
        loadedAperture.managedChunkHeightRadius * 2,
        loadedAperture.managedChunkRadius * 2
      ) * Chunk.Diameter);

      // draw all the chunks in the data loading queue right now
      Gizmos.color = Color.yellow;
      foreach(Coordinate loadingChunkLocation in loadedAperture.getProcessingChunks()) {
        Gizmos.DrawWireCube(
          ((loadingChunkLocation * Chunk.Diameter) + (Chunk.Diameter / 2)).vec3,
          new Vector3(Chunk.Diameter, Chunk.Diameter, Chunk.Diameter)
        );
      }
    }
#endif

    /// <summary>
    /// Set the world position of the focus. Also sets the chunk position.
    /// </summary>
    public void setPosition(Coordinate worldPosition) {
      transform.position = worldLocation = previousWorldLocation = worldPosition.vec3;
      chunkLocation = previousChunkLocation = worldLocation / Chunk.Diameter;
    }

    /// <summary>
    /// set the controller active
    /// </summary>
    public void setActive() {
      isActive = true;
    }
  }
}
