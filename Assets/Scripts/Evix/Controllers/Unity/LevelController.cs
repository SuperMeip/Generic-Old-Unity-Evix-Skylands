using UnityEngine;
using System.Collections.Concurrent;
using MeepTech.Voxel.Collections.Level;
using MeepTech.Events;
using MeepTech.GamingBasics;
using System.Collections.Generic;
using MeepTech.Voxel.Collections.Level.Management;

namespace Evix.Controllers.Unity {

  /// <summary>
  /// Used to control a level in the game world
  /// </summary>
  public class LevelController : MonoBehaviour, ILevelController {

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
    public ILevel level {
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
    ConcurrentBag<Vector3> chunksWaitingForAFreeController;

    /// <summary>
    /// Chunk controllers waiting for assignement and activation
    /// </summary>
    List<Vector3> chunkControllerAssignmentWaitQueue;

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
      if (!isLoaded) {
        return;
      }

      /// add any chunks that were assigned controllers asycly.
      queueNewlyAssignedChunkControllers();
      // while this is loaded, go through the chunk activation queue and activate or attach meshes to chunks, doing both in the same frame
      // can cause lag in unity.
      chunkControllerActivationWaitQueue.RemoveAll(assignedChunkController => {
        if (assignedChunkController != null) {
          // if the chunk is not meshed yet, mesh it with the assigned chunk data
          if (!assignedChunkController.isMeshed) {
            assignedChunkController.updateMeshWithChunkData();
            // if the chunk is meshed, but not yet active:
          } else if (!assignedChunkController.gameObject.activeSelf) {
            assignedChunkController.setObjectActive();
            return true;
            // make sure to remove still hidden items from the queue when we move away, and to de-mesh them.
          }
        }

        return false;
      });

      /// try to assign new chunks that are waiting on controllers, if we run out.
      queueNewChunksWaitingForControllers();
      chunkControllerAssignmentWaitQueue.RemoveAll(chunkLocationWaitingForController => {
        return tryToAssignChunkToController(chunkLocationWaitingForController);
      });
    }

    ///// PUBLIC FUNCTIONS
    
    /// <summary>
    /// Initilize this chunk controller for it's provided level.
    /// </summary>
    public void initializeFor(ILevel level) {
      if (chunkObjectPrefab == null) {
        World.Debugger.logError("UnityLevelController Missing chunk prefab, can't work");
      } else if (level == null) {
        World.Debugger.logError("No level provided to level controller");
      } else {
        /// init
        this.level = level;
        chunksWaitingForAFreeController = new ConcurrentBag<Vector3>();
        chunkControllerAssignmentWaitQueue = new List<Vector3>();
        chunkControllersAssignedChunks = new ConcurrentBag<ChunkController>();
        chunkControllerActivationWaitQueue = new List<ChunkController>();

        // build the controller pool based on the maxed meshed chunk area we should ever have:
        IChunkResolutionAperture meshResolutionAperture = level.getApetureForResolutionLayer(Level.FocusResolutionLayers.Meshed);
        chunkControllerPool = new ChunkController[meshResolutionAperture.managedChunkRadius * meshResolutionAperture.managedChunkRadius * level.chunkBounds.y * 2];
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

        /// this controller is now loaded
        isLoaded = true;
      }
    }

    /// <summary>
    /// Clear all rendered and stored level data that we have.
    /// </summary>
    public void clearAll() {
      level = null;
      isLoaded = false;
      chunkControllerActivationWaitQueue = null;
      chunkControllerAssignmentWaitQueue = null;
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
        // when the level finishes loading a chunk's mesh. Render it in world
        case ActivateGameobjectResolutionAperture.SetChunkObjectActiveEvent scoae:
          if (!tryToAssignChunkToController(scoae.chunkLocation.vec3)) {
            chunksWaitingForAFreeController.Add(scoae.chunkLocation.vec3);
          }
          break;
        /*case ActivateGameobjectResolutionAperture.SetChunkObjectInactiveEvent scoie:
          
          break;*/
        default:
          return;
      }
    }

    ///// SUB FUNCTIONS

    /// <summary>
    /// Try to assign a chunk to an unused controller.
    /// </summary>
    /// <param name="chunkLocation"></param>
    /// <returns>A bool for being used in Removeall, if the chunk should be removed from the wait queue.</returns>
    bool tryToAssignChunkToController(Vector3 chunkLocation) {
      // try to find an unused chunk controller
      if (getUnusedChunkController(chunkLocation, out ChunkController unusedChunkController)
        && unusedChunkController != null
      ) {
        // make sure this chunk can be assigned to a controller, if it can, do so and add the controller to the activation queue.
        IVoxelChunk chunk = level.getChunk(chunkLocation.toCoordinate(), true);
        if (unusedChunkController.setChunkToRender(chunk, chunkLocation)) {
          chunkControllersAssignedChunks.Add(unusedChunkController);
          return true;
          // if the chunk isn't meshable, we just drop it from the queue
        } else {
          return true;
        }
        // don't drop it yet, we didn't find a chunk controller.
      } else {
        return false;
      }
    }

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
              }

              return false;
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
      while (0 < newlyAssignedChunkControllerCount--) {
        if (chunkControllersAssignedChunks.TryTake(out ChunkController newlyAssignedChunkController)
          && !chunkControllerActivationWaitQueue.Contains(newlyAssignedChunkController)
        ) {
          chunkControllerActivationWaitQueue.Add(newlyAssignedChunkController);
        }
      }
    }

    /// <summary>
    /// Attempt to queue newly assigned chunk controllers from the bag
    /// </summary>
    void queueNewChunksWaitingForControllers() {
      // get the # of assigned controllers at this moment in the bag.
      int newChunkCount = chunksWaitingForAFreeController.Count;
      // we'll try to take that many items out this loop around.
      while (0 < newChunkCount--) {
        if (chunksWaitingForAFreeController.TryTake(out Vector3 newChunkLocation)
          && !chunkControllerAssignmentWaitQueue.Contains(newChunkLocation)
        ) {
          chunkControllerAssignmentWaitQueue.Add(newChunkLocation);
        }
      }
    }
  }
}
