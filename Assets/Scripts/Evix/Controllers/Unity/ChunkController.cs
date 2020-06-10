using MeepTech.Voxel.Collections;
using MeepTech.Voxel.Collections.Level;
using MeepTech.Voxel.Generation.Mesh;
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

    void Awake() {
      meshFilter = GetComponent<MeshFilter>();
      meshCollider = GetComponent<MeshCollider>();
    }

    /// <summary>
    /// Set the chunk to render. Returns true if the data was set up
    /// </summary>
    /// <param name="chunk"></param>
    /// <param name="chunkLevelLocation"></param>
    public bool setChunkToRender(IVoxelChunk chunk, Vector3 chunkLevelLocation) {
      if (chunk.isLoaded && chunk.mesh != null && !chunk.isEmpty) {
        currentChunk = chunk;
        chunkLocation = chunkLevelLocation;
        isMeshed = false;

        return true;
      }

      return false;
    }

    /// <summary>
    /// Can only be called from the main thread. Set this gameobject active.
    /// </summary>
    public void setObjectActive() {
      gameObject.SetActive(true);
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
      isMeshed = false;
      isActive = false;
    }

    /// <summary>
    /// Free memory
    /// </summary>
    private void OnDestroy() {
      Destroy(currentChunkMesh);
      Destroy(meshFilter.mesh);
      Destroy(meshCollider.sharedMesh);
    }
  }
}