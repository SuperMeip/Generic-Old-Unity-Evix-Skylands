using MeepTech.Voxel.Collections.Level;
using Evix.EventSystems;
using MeepTech.Voxel;
using MeepTech.Voxel.Generation.Mesh;
using MeepTech.Voxel.Collections.Storage;
using MeepTech.Voxel.Generation.Sources;
using MeepTech.Voxel.Generation.Managers;
using Evix;

namespace MeepTech.GamingBasics {

  /// <summary>
  /// Za warudo
  /// </summary>
  public class World {

    /// <summary>
    /// The size of a voxel 'block', in world
    /// </summary>
    public const float BlockSize = 1.0f;

    /// <summary>
    /// The current world
    /// </summary>
    public static World Current {
      get; 
    } = new World();

    /// <summary>
    /// The debugger used to interface with unity debugging.
    /// </summary>
    public static IDebugger Debugger {
      get;
    } = new UnityDebugger();

    /// <summary>
    /// The debugger used to interface with unity debugging.
    /// </summary>
    public static WorldEventSystem EventSystem {
      get;
    } = new WorldEventSystem();

    /// <summary>
    /// The currently loaded level
    /// </summary>
    public static ILevel activeLevel {
      get;
      protected set;
    }

    /// <summary>
    /// the players in this world
    /// </summary>
    public Player[] players {
      get;
    }

    /// <summary>
    /// Make a new world
    /// </summary>
    protected World() {
      players = new Player[2];
    }

    /// <summary>
    /// Set the player
    /// </summary>
    /// <param name="playerNumber">The non 0 indexed player number to set</param>
    public static void SetPlayer(Player player, int playerNumber) {
      Current.players[playerNumber - 1] = player;
    }

    //////// TESTS

    /// <summary>
    /// start test world
    /// </summary>
    public static void InitializeTestWorld(ILevelController levelController, IVoxelSource terrainSource, ILevelFocus testFocus) {
      SetPlayer(new Player(), 1);
     
      // set up the level
      Coordinate chunkBounds = (1000, 5, 1000);
      activeLevel = new Level<
        VoxelFlatArray,
        HashedChunkDataStorage,
        MarchGenerator,
        JobBasedChunkFileDataLoadingManager<VoxelFlatArray>,
        JobBasedChunkVoxelDataGenManager<VoxelFlatArray>,
        JobBasedChunkMeshGenManager
      >(
        chunkBounds,
        terrainSource
      );

      // set up the level controller.
      EventSystem.subscribe(
        activeLevel,
        WorldEventSystem.Channels.TerrainGeneration
      );
      EventSystem.subscribe(
        levelController,
        WorldEventSystem.Channels.TerrainGeneration
      );
      levelController.initializeFor(activeLevel);

      // initialize around a chunk
      testFocus.spawn(chunkBounds * Chunk.Diameter/2);
      testFocus.setActive();
    }
  }
}