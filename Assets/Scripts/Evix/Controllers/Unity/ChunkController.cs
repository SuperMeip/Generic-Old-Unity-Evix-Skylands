﻿using MeepTech.Voxel.Collections.Level;
using Unity.Jobs;
using UnityEngine;

namespace Evix.Controllers.Unity {

  [RequireComponent(typeof(MeshRenderer))]
  [RequireComponent(typeof(MeshFilter))]
  [RequireComponent(typeof(MeshCollider))]
  public class ChunkController : MonoBehaviour {

    /// <summary>
    /// The controller for the active level.
    /// </summary>
    [HideInInspector] public LevelController levelController;

    /// <summary>
    /// The current chunk location of the chunk this gameobject is representing.
    /// </summary>
    [ReadOnly]
    public Vector3 chunkLocation;

    /// <summary>
    /// If this controller is being used.
    /// </summary>
    [HideInInspector] public bool isActive = false;

    /// <summary>
    /// If this chunk has been meshed with chunk data.
    /// </summary>
    [HideInInspector] public bool isMeshed = false;

    /// <summary>
    /// The current mesh to use for this chunk.
    /// </summary>
    UnityEngine.Mesh currentChunkMesh;

    /// <summary>
    /// The current mesh to use for this chunk.
    /// </summary>
    IVoxelChunk currentChunk;

    /// <summary>
    /// the attached mesh renderer
    /// </summary>
    MeshFilter meshFilter;

    /// <summary>
    /// The attached mesh collider
    /// </summary>
    MeshCollider meshCollider;

    /// <summary>
    /// The job handler for the collider mesh baking job
    /// </summary>
    JobHandle colliderBakerHandler;

    ///// UNITY FUNCTIONS

    /// <summary>
    /// init
    /// </summary>
    void Awake() {
      meshFilter = GetComponent<MeshFilter>();
      meshCollider = GetComponent<MeshCollider>();
    }

    /// <summary>
    /// Free memory
    /// </summary>
    private void OnDestroy() {
      Destroy(currentChunkMesh);
      Destroy(meshFilter.mesh);
      Destroy(meshCollider.sharedMesh);
    }

    ///// PUBLIC FUNCTIONS

    /// <summary>
    /// Set the chunk to render. Returns true if the data was set up
    /// </summary>
    /// <param name="chunk"></param>
    /// <param name="chunkLevelLocation"></param>
    public void setChunkToRender(IVoxelChunk chunk) {
      currentChunk = chunk;
      chunkLocation = chunk.location.vec3;
      isMeshed = false;
    }

    /// <summary>
    /// Can only be called from the main thread. Set this gameobject active.
    /// </summary>
    public void enableObjectVisible() {
      gameObject.SetActive(true);
    }

    /// <summary>
    /// Can only be called from the main thread. Set this gameobject inactive.
    /// </summary>
    public void disableObjectVisible() {
      gameObject.SetActive(false);
    }

    /// <summary>
    /// Update the mesh for it's assigned chunk
    /// </summary>
    public void updateMeshWithChunkData() {
      currentChunkMesh = new UnityEngine.Mesh();
      currentChunkMesh.Clear();

      currentChunkMesh.vertices = currentChunk.mesh.getVertices();
      currentChunkMesh.colors = currentChunk.mesh.getColors();
      currentChunkMesh.SetTriangles(currentChunk.mesh.triangles, 0);
      currentChunkMesh.RecalculateNormals();

      transform.position = chunkLocation * Chunk.Diameter;
      meshFilter.mesh = currentChunkMesh;
      meshCollider.sharedMesh = currentChunkMesh;
      isMeshed = true;

      /// schedule a job to bake the mesh collider asyncly so it doesn't lag.
      colliderBakerHandler = (new ColliderMeshBakingJob(currentChunkMesh.GetInstanceID())).Schedule();
    }

    /// <summary>
    /// deactivate and free up this object for use again by the level controller
    /// </summary>
    public void deactivateAndClear() {
      gameObject.SetActive(false);
      currentChunkMesh = new UnityEngine.Mesh();
      currentChunkMesh.Clear();

      currentChunk = null;
      chunkLocation = default;
      colliderBakerHandler = default;
      isMeshed = false;
      isActive = false;
    }

    /// <summary>
    /// Check if the collider was baked by a job for this chunk
    /// </summary>
    /// <returns></returns>
    public bool checkColliderIsBaked() {
      return colliderBakerHandler.IsCompleted;
    }

    /// <summary>
    /// A unity job to bake the collider mesh
    /// </summary>
    struct ColliderMeshBakingJob : IJob {

      /// <summary>
      /// The id of the mesh to bake
      /// </summary>
      readonly int meshID;

      /// <summary>
      /// Create a new mesh baking job for this controller
      /// </summary>
      /// <param name="meshID"></param>
      /// <param name="chunkController"></param>
      public ColliderMeshBakingJob(int meshID) {
        this.meshID = meshID;
      }

      /// <summary>
      /// Execute the job and bake the mesh
      /// </summary>
      public void Execute() {
        Physics.BakeMesh(meshID, false);
      }
    }
  }
}