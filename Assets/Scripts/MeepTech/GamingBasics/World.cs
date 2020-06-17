﻿using MeepTech.Voxel.Collections.Level;
using Evix.EventSystems;
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
    public ILevel activeLevel {
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

    /// <summary>
    /// Set a level as the active level of the current world
    /// </summary>
    /// <param name="level"></param>
    public static void setActiveLevel(ILevel level) {
      Current.activeLevel = level;
    }
  }
}