using MeepTech.GamingBasics;
using MeepTech.Voxel;
using MeepTech.Voxel.Collections.Level;
using MeepTech.Voxel.Collections.Storage;
using MeepTech.Voxel.Generation.Managers;
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

    public float SeaLevel = 30.0f;
    public Vector3 levelSize = new Vector3(1000, 2, 1000);
    public int meshedChunkDiameter = 20;
    public int chunkLoadBuffer = 10;
    public int chunksBelowToMesh = 5;

    IVoxelSource voxelSource;

    // Start is called before the first frame update
    void Awake() {
      // set up the voxel source
      voxelSource = getConfiguredPlainSource();

      // set up the level
      Coordinate chunkBounds = levelSize;
      ILevel level = new Level<
        VoxelFlatArray,
        HashedChunkDataStorage,
        MarchGenerator,
        JobBasedChunkFileDataLoadingManager<VoxelFlatArray>,
        JobBasedChunkVoxelDataGenManager<VoxelFlatArray>,
        JobBasedChunkMeshGenManager
      >(
        chunkBounds,
        voxelSource,
        meshedChunkDiameter,
        chunkLoadBuffer,
        chunksBelowToMesh
      );

      levelController.initializeFor(level);
      World.InitializeTestWorld(levelController, currentFocus);
    }

    FlatPlainsSource getConfiguredPlainSource() {
      FlatPlainsSource plainsSource = new FlatPlainsSource();
      plainsSource.seaLevel = SeaLevel;

      return plainsSource;
    }
  }
}