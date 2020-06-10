using MeepTech.Voxel.Collections.Storage;
using UnityEngine;
using System.Collections.Concurrent;
using MeepTech.Voxel.Collections.Level;
using MeepTech.Events;
using MeepTech;
using MeepTech.GamingBasics;
using static MeepTech.Voxel.Generation.Managers.ChunkMeshGenerationManager;

namespace Evix.Controllers.Unity {

  /// <summary>
  /// Used to control a level in the game world
  /// </summary>
  public class UnityLevelController : MonoBehaviour, IObserver {

    /// <summary>
    /// The prefab used to render a chunk in unity.
    /// </summary>
    public GameObject chunkObjectPrefab;

    /// <summary>
    /// The level this is managing
    /// </summary>
    [HideInInspector] public ILevel level;

    /// <summary>
    /// The level is loaded enough for the manager to begin working
    /// </summary>
    [HideInInspector] public bool isLoaded;

    /// <summary>
    /// The count of rendered chunks atm
    /// </summary>
    [ReadOnly] public int renderedChunksCount;

    /// <summary>
    /// The pool of prefabs
    /// </summary>
    UnityChunkController[] chunkControllerPool;

    /// <summary>
    /// Chunk controllers waiting for assignement and activation
    /// </summary>
    ConcurrentQueue<UnityChunkController> chunkControllerActivationQueue;

    ///// UNITY FUNCTIONS

    void Update() {
      if (isLoaded) {
        // while this is loaded, go through the chunk activation queue and activate or attach meshes to chunks, doing both in the same frame
        // can cause lag in unity.
        chunkControllerActivationQueue = chunkControllerActivationQueue ?? new ConcurrentQueue<UnityChunkController>();
        if (chunkControllerActivationQueue.Count > 0 && chunkControllerActivationQueue.TryPeek(out UnityChunkController chunkController)) {
          if (!chunkController.isMeshed) {
            chunkController.updateMeshWithChunkData();
          } else if (!chunkController.gameObject.activeSelf) {
            chunkController.setObjectActive();
            renderedChunksCount++;
            chunkControllerActivationQueue.TryDequeue(out _);
          // make sure to remove stuck items from the queue
          } else {
            chunkControllerActivationQueue.TryDequeue(out _);
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
      float loadedChunkHeight = level.chunkBounds.y * Chunk.Diameter;
      focalWorldPoint.y = loadedChunkHeight / 2;
      float meshedChunkDiameter = level.meshedChunkBounds[1].x * Chunk.Diameter - level.meshedChunkBounds[0].x * Chunk.Diameter;
      Gizmos.color = Color.blue;
      Gizmos.DrawWireCube(focalWorldPoint, new Vector3(meshedChunkDiameter, loadedChunkHeight, meshedChunkDiameter));
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
        chunkControllerActivationQueue = new ConcurrentQueue<UnityChunkController>();
        //loadedChunkLocations = new ConcurrentBag<Vector3>();
        chunkControllerPool = new UnityChunkController[level.meshedChunkDiameter * level.meshedChunkDiameter * level.chunkBounds.y];
        isLoaded = true;
        for (int index = 0; index < chunkControllerPool.Length; index++) {
          // for each chunk we want to be able to render at once, create a new pooled gameobject for it with the prefab that has a unitu chunk controller on it
          GameObject chunkObject = Instantiate(chunkObjectPrefab);
          chunkObject.transform.parent = gameObject.transform;
          UnityChunkController chunkController = chunkObject.GetComponent<UnityChunkController>();
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
      chunkControllerActivationQueue = null;
      foreach (UnityChunkController chunkController in chunkControllerPool) {
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
        case ChunkMeshGenerationFinishedEvent lcmgfe:
           UnityChunkController unusedChunkController = getUnusedChunkController();
           if (unusedChunkController == null) {
             Debug.LogError($"No free chunk controller found for {lcmgfe.chunkLocation.ToString()}");
           } else {
             IVoxelChunk chunk = level.getChunk(lcmgfe.chunkLocation, true);
             if (unusedChunkController.setChunkToRender(chunk, lcmgfe.chunkLocation.vec3)) {
               chunkControllerActivationQueue.Enqueue(unusedChunkController);
             }
           }
          break;
        default:
          return;
      }
    }

    ///// SUB FUNCTIONS

    /// <summary>
    /// Get an unused chunk controller from the pool we made
    /// </summary>
    /// <returns></returns>
    UnityChunkController getUnusedChunkController() {
      foreach(UnityChunkController chunkController in chunkControllerPool) {
        if (chunkController != null && !chunkController.isActive) {
          chunkController.isActive = true;
          return chunkController;
        }
      }

      return null;
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
