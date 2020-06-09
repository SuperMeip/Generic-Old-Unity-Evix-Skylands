using System.Collections.Generic;
using UnityEngine;
using MeepTech.Voxel.Collections.Level;
using MeepTech.Events;
using System;
using Evix.EventSystems;
using MeepTech.Voxel;
using MeepTech.Voxel.Generation.Mesh;
using MeepTech.Voxel.Collections.Storage;
using Evix.Controllers.Unity;
using MeepTech.Voxel.Generation.Sources;
using MeepTech.Voxel.Generation.Managers;

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
    public static UnityDebugger Debugger {
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
    /// The objects this world is managing
    /// </summary>
    List<GameObject> gameObjects;

    /// <summary>
    /// Make a new world
    /// </summary>
    protected World() {
      gameObjects = new List<GameObject>();
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
    public static void InitializeTestWorld(UnityLevelController levelController, IVoxelSource terrainSource) {
      SetPlayer(new Player(), 1);
     
      // set up the level
      Coordinate chunkBounds = (1000, 20, 1000);
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
      levelController.level = activeLevel;
      EventSystem.subscribe(
        levelController,
        WorldEventSystem.Channels.TerrainGeneration
      );
      levelController.initialize();

      // initialize around a chunk
      Coordinate spawn = chunkBounds / 2;
      activeLevel.initializeAround(spawn);
    }
  }
}