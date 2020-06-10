using MeepTech.Voxel.Collections.Storage;
using UnityEngine;
using System.Collections.Concurrent;
using MeepTech.Voxel.Collections.Level;
using MeepTech.Events;
using MeepTech;
using MeepTech.GamingBasics;
using static MeepTech.Voxel.Generation.Managers.ChunkMeshGenerationManager;
using MeepTech.Voxel.Generation.Managers;
using System.Collections.Generic;

namespace Evix.Controllers.Unity {

  /// <summary>
  /// Used to control a level in the game world
  /// </summary>
  public class LevelController : MonoBehaviour, IObserver {

    /// <summary>
    /// The current object to focus on.
    /// </summary>
    public FocusController currentFocus;

    /// <summary>
    /// The prefab used to render a chunk in unity.
    /// </summary>
    public GameObject chunkObjectPrefab;

    /// <summary>
    /// The level is loaded enough for the manager to begin working
    /// </summary>
    [HideInInspector] public bool isLoaded;

    /// <summary>
    /// The level this is managing
    /// </summary>
    [HideInInspector] public ILevel level {
      get;
      private set;
    }

    [HideInInspector] public ConcurrentDictionary<Vector3, bool> chunkControllerDeActivationTokens {
      get;
      private set;
    }

    /// <summary>
    /// The pool of prefabs
    /// </summary>
    ChunkController[] chunkControllerPool;

    /// <summary>
    /// Chunk controllers waiting for assignement and activation
    /// </summary>
    ConcurrentBag<ChunkController> chunkControllersAssignedChunks;

    /// <summary>
    /// Chunk controllers waiting for assignement and activation
    /// </summary>
    List<ChunkController> chunkControllerActivationWaitQueue;

    ///// UNITY FUNCTIONS

    void Update() {
      if (isLoaded) {
        // add any chunks that were assigned controllers asycly.
        queueNewlyAssignedChunkControllers();
        // while this is loaded, go through the chunk activation queue and activate or attach meshes to chunks, doing both in the same frame
        // can cause lag in unity.
        if (chunkControllerActivationWaitQueue.Count > 0) {
          foreach (ChunkController assignedChunkController in chunkControllerActivationWaitQueue) {
            // if the chunk is not meshed yet, mesh it with the assigned chunk data
            if (!assignedChunkController.isMeshed) {
              assignedChunkController.updateMeshWithChunkData();
              // if the chunk is meshed, but not yet active:
            } else if (!assignedChunkController.gameObject.activeSelf) {
              if (chunkIsWithinActiveBounds(assignedChunkController.chunkLocation)) {
                assignedChunkController.setObjectActive();
                chunkControllerActivationWaitQueue.Remove(assignedChunkController);
              // make sure to remove still hidden items from the queue when we move away, and to de-mesh them.
              } else if (!level.chunkIsWithinLoadedBounds(assignedChunkController.chunkLocation.toCoordinate())) {
                assignedChunkController.deactivateAndClear();
                chunkControllerActivationWaitQueue.Remove(assignedChunkController);
              }
            }
          }
        }
      }
    }

#if DEBUG
    ///// GUI FUNCTIONS

    void OnDrawGizmos() {
      // ignore gizmo if we have no level to draw
      if (!isLoaded || level == null) {
        return;
      }
      /// draw the focus
      Vector3 focalWorldPoint = level.focus.vec3 * Chunk.Diameter;
      Gizmos.color = Color.yellow;
      Gizmos.DrawWireSphere(focalWorldPoint, Chunk.Diameter / 2);
      /// draw the meshed chunk area
      float loadedChunkHeight = level.chunkBounds.y * Chunk.Diameter * 0.66f;
      float meshedChunkDiameter = level.meshedChunkBounds[1].x * Chunk.Diameter - level.meshedChunkBounds[0].x * Chunk.Diameter;
      Gizmos.color = Color.blue;
      Gizmos.DrawWireCube(focalWorldPoint, new Vector3(meshedChunkDiameter, loadedChunkHeight, meshedChunkDiameter));
      /// draw the active chunk area
      Gizmos.color = Color.green;
      Gizmos.DrawWireCube(focalWorldPoint, new Vector3(meshedChunkDiameter - 1 , loadedChunkHeight - 1, meshedChunkDiameter - 1));
      /// draw the loaded chunk area
      float loadedChunkDiameter = level.loadedChunkBounds[1].x * Chunk.Diameter - level.loadedChunkBounds[0].x * Chunk.Diameter;
      Gizmos.color = Color.yellow;
      Gizmos.DrawWireCube(focalWorldPoint, new Vector3(loadedChunkDiameter, loadedChunkHeight, loadedChunkDiameter));
    }
#endif

    ///// PUBLIC FUNCTIONS

    /// <summary>
    /// Initilize this chunk controller for it's provided level.
    /// </summary>
    public void initialize() {
      if (chunkObjectPrefab == null) {
        World.Debugger.logError("UnityLevelController Missing chunk prefab, can't work");
      } else if (level == null) {
        World.Debugger.logError("No level provided by world. Did you hook this level controller up to the world controller?");
      } else {
        chunkControllersAssignedChunks = new ConcurrentBag<ChunkController>();
        chunkControllerActivationWaitQueue = new List<ChunkController>();
        //loadedChunkLocations = new ConcurrentBag<Vector3>();
        chunkControllerPool = new ChunkController[level.meshedChunkDiameter * level.meshedChunkDiameter * level.chunkBounds.y];
        isLoaded = true;
        for (int index = 0; index < chunkControllerPool.Length; index++) {
          // for each chunk we want to be able to render at once, create a new pooled gameobject for it with the prefab that has a unitu chunk controller on it
          GameObject chunkObject = Instantiate(chunkObjectPrefab);
          chunkObject.transform.parent = gameObject.transform;
          ChunkController chunkController = chunkObject.GetComponent<ChunkController>();
          if (chunkController == null) {
            //@TODO: make a queue for these maybe, just in case?
            World.Debugger.logError($"No chunk controller on {chunkObject.name}");
          } else {
            chunkControllerPool[index] = chunkController;
            chunkController.levelController = this;
            chunkObject.SetActive(false);
          }
        }
      }
    }

    /// <summary>
    /// Clear all rendered and stored level data that we have.
    /// </summary>
    public void clearAll() {
      level = null;
      isLoaded = false;
      chunkControllerActivationWaitQueue = null;
      foreach (ChunkController chunkController in chunkControllerPool) {
        if (chunkController != null) {
          Destroy(chunkController.gameObject);
        }
      }
    }

    /// <summary>
    /// Get notifications from other observers, EX:
    ///   block breaking and placing
    ///   player chunk location changes
    /// </summary>
    /// <param name="event">The event to notify this observer of</param>
    /// <param name="origin">(optional) the source of the event</param>
    public void notifyOf(IEvent @event, IObserver origin = null) {
      // ignore events if we have no level to control
      if (!isLoaded || level == null) {
        return;
      }

      switch (@event) {
        // when a player spawns in the level
        case Player.SpawnEvent pse:
          level.initializeAround(pse.spawnLocation.worldToChunkLocation());
          break;
        // When the player moves to a new chunk, adjust the loaded level focus
        case Player.ChangeChunkLocationEvent pccle:
          level.adjustFocusTo(pccle.newChunkLocation);
          break;
        // when the level finishes loading a chunk's mesh. Render it in world
        case ChunkManager.ChunkMeshGenerationFinishedEvent lcmgfe:
           if (getUnusedChunkController(lcmgfe.chunkLocation.vec3, out ChunkController unusedChunkController) 
             && unusedChunkController != null
           ) {
             IVoxelChunk chunk = level.getChunk(lcmgfe.chunkLocation, true);
             if (unusedChunkController.setChunkToRender(chunk, lcmgfe.chunkLocation.vec3)) {
               chunkControllersAssignedChunks.Add(unusedChunkController);
             }
           }
          break;
        default:
          return;
      }
    }

    ///// SUB FUNCTIONS

    /// <summary>
    /// Get an unused chunk controller from the pool we made, while also making sure the chunk isn't already part of said pool.
    /// </summary>
    /// <returns></returns>
    bool getUnusedChunkController(Vector3 chunkLocationToSet, out ChunkController unusedChunkController) {
      unusedChunkController = null;
      bool foundUnusedController = false;
      foreach (ChunkController chunkController in chunkControllerPool) {
        if (chunkController != null) {
          // if the chunk is active and already has the location we're looking for, we return false
          if (chunkController.isActive) { // these ifs are seprate because of the else if below.
            if (chunkController.chunkLocation == chunkLocationToSet) {
              if (unusedChunkController != null) {
                unusedChunkController.isActive = false;
                unusedChunkController = null;
                return false;
              }
            }
          // if we found an inactive controller, and we're still looking for that, lets snag it and stop looking.
          } else if (!foundUnusedController) {
            chunkController.isActive = true;
            unusedChunkController = chunkController;
            foundUnusedController = true;
          }
        }
      }

      // return true if we never found a chunkController already set to this new chunk location.
      return true;
    }

    /// <summary>
    /// Attempt to queue newly assigned chunk controllers from the bag
    /// </summary>
    void queueNewlyAssignedChunkControllers() {
      // get the # of assigned controllers at this moment in the bag.
      int newlyAssignedChunkControllerCount = chunkControllersAssignedChunks.Count;
      // we'll try to take that many items out this loop around.
      while ( 0 < newlyAssignedChunkControllerCount--) {
        // @TODO:? Does this need a lock on chunkControllersAssignedChunks for the whole loop?
        if (chunkControllersAssignedChunks.TryTake(out ChunkController newlyAssignedChunkController)) {
          chunkControllerActivationWaitQueue.Add(newlyAssignedChunkController);
        }
      }
     }

    /// <summary>
    /// Check if a chunk should be turned active or nah
    /// </summary>
    /// <returns></returns>
    bool chunkIsWithinActiveBounds(Vector3 chunkLocation) {
      return chunkLocation.toCoordinate().isWithin(level.meshedChunkBounds[0] + 1, level.meshedChunkBounds[1] - 1);
    }

    /// <summary>
    /// On destroy, try to stop the jobs
    /// </summary>
    void OnDestroy() {
      if (level != null) {
        level.stopAllManagers();
      }
    }

#if DEBUG
    /// <summary>
    /// Debug all the level manager info
    /// </summary>
    public void debugLevelManagers() {
      if(level != null) {
        World.Debugger.log(level.getManagerStats());
      }
    }
  }
#endif
}
