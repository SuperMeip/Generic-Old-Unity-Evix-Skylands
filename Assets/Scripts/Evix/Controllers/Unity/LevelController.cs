using UnityEngine;
using System.Collections.Concurrent;
using MeepTech.Voxel.Collections.Level;
using MeepTech.Events;
using MeepTech.GamingBasics;
using System.Collections.Generic;
using MeepTech.Voxel.Collections.Level.Management;
using MeepTech.Voxel;

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
    ConcurrentBag<Coordinate> newlyActivatedChunks;

    /// <summary>
    /// Chunk controllers waiting for assignement and activation
    /// </summary>
    List<Coordinate> chunksToActivate;

    /// <summary>
    /// Chunk controllers waiting for assignement and activation
    /// </summary>
    ConcurrentBag<Coordinate> newlyDeactivatedChunks;

    /// <summary>
    /// Chunk controllers waiting for assignement and activation
    /// </summary>
    List<Coordinate> chunksToDeactivate;

    /// <summary>
    /// Chunk controllers waiting for assignement and activation
    /// </summary>
    ConcurrentBag<ChunkController> chunksWithNewlyGeneratedMeshes;

    /// <summary>
    /// Chunk controllers waiting for assignement and activation
    /// </summary>
    List<ChunkController> chunksToMesh;

    /// <summary>
    /// Chunk controllers waiting for assignement and activation
    /// </summary>
    ConcurrentBag<ChunkController> chunksWithOutOfFocusMeshes;

    /// <summary>
    /// Chunk controllers waiting for assignement and activation
    /// </summary>
    List<ChunkController> chunksToDeMesh;

    ///// UNITY FUNCTIONS

    void Update() {
      if (!isLoaded) {
        return;
      }

      // NOTE:: Newly activated chunks goes first so we don't mesh then activate in the same frame
      /// go through the chunk activation queue and activate chunks
      queueNewlyActivatedChunks();
      chunksToActivate.RemoveAll(activatedChunkLocation => {
        ChunkController assignedController = getAssignedChunkController(activatedChunkLocation);
        // if the chunk doesn't have a meshed controller yet, we can't activate it, so wait.
        if (assignedController == null || !(assignedController.isActive && assignedController.isMeshed)) {
          return false;
        }

        assignedController.enableObjectVisible();
        return true;
      });

      /// try to assign newly mehsed chunks that are waiting on controllers, if we run out.
      queueNewChunksWaitingForControllers();
      chunkControllerAssignmentWaitQueue.RemoveAll(chunkLocationWaitingForController => {
        return tryToAssignNewlyMeshedChunkToController(chunkLocationWaitingForController);
      });

      /// try to assign meshes to the chunks with newly generated meshes
      queueChunksWithNewlyGeneratedMeshes();
      chunksToMesh.RemoveAll(chunkToMesh => {
        chunkToMesh.updateMeshWithChunkData();

        return true;
      });

      /// try to remove meshes for the given chunk and reset it's mesh data
      queueChunksWithOutOfFocusMeshes();
      chunksToDeMesh.RemoveAll(chunkToMesh => {
        chunkToMesh.deactivateAndClear();

        return true;
      });

      /// go through the de-activation queue
      queueNewlyDeactivatedChunks();
      chunksToDeactivate.RemoveAll(deactivatedChunkLocation => {
        ChunkController assignedController = getAssignedChunkController(deactivatedChunkLocation);
        if (assignedController != null) {
          assignedController.disableObjectVisible();
        }

        return true;
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
        chunksWithNewlyGeneratedMeshes = new ConcurrentBag<ChunkController>();
        chunksToMesh = new List<ChunkController>();
        newlyActivatedChunks = new ConcurrentBag<Coordinate>();
        chunksToActivate = new List<Coordinate>();
        chunksWithOutOfFocusMeshes = new ConcurrentBag<ChunkController>();
        chunksToDeMesh = new List<ChunkController>();
        newlyDeactivatedChunks = new ConcurrentBag<Coordinate>();
        chunksToDeactivate = new List<Coordinate>();

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
        // when a chunk mesh comes into focus, or loads, set the mesh to a chunkManager
        case LoadedChunkMeshDataResolutionAperture.ChunkMeshLoadingFinishedEvent cmfle:
          if (!tryToAssignNewlyMeshedChunkToController(cmfle.chunkLocation.vec3)) {
            chunksWaitingForAFreeController.Add(cmfle.chunkLocation.vec3);
          }
          break;
        // when the level finishes loading a chunk's mesh. Render it in world
        case ActivateGameobjectResolutionAperture.SetChunkObjectActiveEvent scoae:
          newlyActivatedChunks.Add(scoae.chunkLocation);
          break;
        case ActivateGameobjectResolutionAperture.SetChunkObjectInactiveEvent scoie:
          newlyDeactivatedChunks.Add(scoie.chunkLocation);
          break;
        case LoadedChunkMeshDataResolutionAperture.ChunkMeshMovedOutOfFocusEvent smmoof:
          ChunkController assignedChunkController = getAssignedChunkController(smmoof.chunkLocation);
          if (assignedChunkController != null) {
            chunksWithOutOfFocusMeshes.Add(assignedChunkController);
          }
          break;
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
    bool tryToAssignNewlyMeshedChunkToController(Vector3 chunkLocation) {
      // try to find an unused chunk controller
      if (getUnusedChunkController(chunkLocation, out ChunkController unusedChunkController)
        && unusedChunkController != null
      ) {
        // make sure this chunk can be assigned to a controller, if it can, do so and add the controller to the activation queue.
        IVoxelChunk chunk = level.getChunk(chunkLocation.toCoordinate(), true);
        if (unusedChunkController.setChunkToRender(chunk, chunkLocation)) {
          chunksWithNewlyGeneratedMeshes.Add(unusedChunkController);
          return true;
          // if the chunk isn't meshable, we just drop it from the queue
        } else {
          // World.Debugger.log($"Chunk {chunkLocation} is unmeshable");
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
    /// Get the chunk controller that's already assigned to the given chunk location
    /// </summary>
    /// <param name="chunkLocation"></param>
    /// <returns></returns>
    ChunkController getAssignedChunkController(Coordinate chunkLocation) {
      ChunkController assignedChunkController = null;
      foreach (ChunkController chunkController in chunkControllerPool) {
        if (chunkController != null && chunkController.isActive && chunkController.chunkLocation == chunkLocation.vec3) {
          assignedChunkController = chunkController;
        }
      }

      return assignedChunkController;
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

    /// <summary>
    /// Attempt to queue newly assigned chunk controllers from the bag
    /// </summary>
    void queueChunksWithNewlyGeneratedMeshes() {
      // get the # of assigned controllers at this moment in the bag.
      int newlyGeneratedMeshesCount = chunksWithNewlyGeneratedMeshes.Count;
      // we'll try to take that many items out this loop around.
      while (0 < newlyGeneratedMeshesCount--) {
        if (chunksWithNewlyGeneratedMeshes.TryTake(out ChunkController chunkWithNewMeshLocation)
          && !chunksToMesh.Contains(chunkWithNewMeshLocation)
        ) {
          chunksToMesh.Add(chunkWithNewMeshLocation);
        }
      }
    }

    /// <summary>
    /// Attempt to queue newly assigned chunk controllers from the bag
    /// </summary>
    void queueChunksWithOutOfFocusMeshes() {
      // get the # of assigned controllers at this moment in the bag.
      int outOfFocusMeshes = chunksWithOutOfFocusMeshes.Count;
      // we'll try to take that many items out this loop around.
      while (0 < outOfFocusMeshes--) {
        if (chunksWithOutOfFocusMeshes.TryTake(out ChunkController chunkWithNewMeshLocation)
          && !chunksToDeMesh.Contains(chunkWithNewMeshLocation)
        ) {
          chunksToDeMesh.Add(chunkWithNewMeshLocation);
        }
      }
    }

    /// <summary>
    /// Attempt to queue newly assigned chunk controllers from the bag
    /// </summary>
    void queueNewlyActivatedChunks() {
      // get the # of assigned controllers at this moment in the bag.
      int newlyActivatedChunksCount = newlyActivatedChunks.Count;
      // we'll try to take that many items out this loop around.
      while (0 < newlyActivatedChunksCount--) {
        if (newlyActivatedChunks.TryTake(out Coordinate newlyDeactivatedChunkLocation)
          && !chunksToActivate.Contains(newlyDeactivatedChunkLocation)
        ) {
          chunksToActivate.Add(newlyDeactivatedChunkLocation);
        }
      }
    }

    /// <summary>
    /// Attempt to queue newly assigned chunk controllers from the bag
    /// </summary>
    void queueNewlyDeactivatedChunks() {
      // get the # of assigned controllers at this moment in the bag.
      int newlyDeactivatedChunksCount = newlyDeactivatedChunks.Count;
      // we'll try to take that many items out this loop around.
      while (0 < newlyDeactivatedChunksCount--) {
        if (newlyDeactivatedChunks.TryTake(out Coordinate newlyDeactivatedChunkLocation)
          && !chunksToDeactivate.Contains(newlyDeactivatedChunkLocation)
        ) {
          chunksToDeactivate.Add(newlyDeactivatedChunkLocation);
        }
      }
    }
  }
}
