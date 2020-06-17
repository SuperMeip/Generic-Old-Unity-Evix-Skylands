using Evix.EventSystems;
using MeepTech.GamingBasics;
using MeepTech.Voxel.Collections.Level;
using MeepTech.Voxel.Collections.Level.Management;
using MeepTech.Voxel.Collections.Storage;
using MeepTech.Voxel.Generation.Mesh;
using MeepTech.Voxel.Generation.Sources;
using UnityEngine;

namespace Evix.Controllers.Unity {

  public class TestWorldController : MonoBehaviour {

    /// <summary>
    /// The current object to focus on.
    /// </summary>
    public FocusController currentFocus;

    /// <summary>
    /// The controller for the active level.
    /// </summary>
    public LevelController levelController;


    ///// SETUP VARS
    public float SeaLevel = 30.0f;
    public Vector3 levelSize = new Vector3(1000, 2, 1000);

    public int activeChunkRadius = 10;
    public int activeChunkHeightOverride = 0;

    public int meshedChunkBuffer = 10;
    public int meshedChunkBufferHeightOverride = 0;

    public int loadedChunkBuffer = 10;
    public int loadedChunkHeightBufferOverride = 0;

    void Awake() {
      // set up player 1
      World.SetPlayer(new Player(), 1);

      // set up the level
      ILevel level = Level.Create<HashedChunkDataStorage>(
        levelSize,
        getConfiguredPlainSource(),
        new MarchGenerator(),
        new IChunkResolutionAperture[] {
          new LoadedChunkVoxelDataResolutionAperture<FlatVoxelArray>(
            activeChunkRadius + meshedChunkBuffer + loadedChunkBuffer,
            activeChunkHeightOverride + meshedChunkBufferHeightOverride + loadedChunkHeightBufferOverride
          ),
          new LoadedChunkMeshDataResolutionAperture(
            activeChunkRadius + meshedChunkBuffer,
            activeChunkHeightOverride + meshedChunkBufferHeightOverride
          ),
          new ActivateGameobjectResolutionAperture(activeChunkRadius, activeChunkHeightOverride)
        }
      );

      // set up the level controller
      World.EventSystem.subscribe(
        levelController,
        WorldEventSystem.Channels.ChunkActivationUpdates
      );
      levelController.initializeFor(level);
      World.setActiveLevel(levelController.level);

      // initialize the focus
      currentFocus.setPosition((level.chunkBounds - (0, 1, 0)) / 2 * Chunk.Diameter);
      level.spawnFocus(currentFocus);
    }

    FlatPlainsSource getConfiguredPlainSource() {
      FlatPlainsSource plainsSource = new FlatPlainsSource();
      plainsSource.seaLevel = SeaLevel;

      return plainsSource;
    }
  }
}