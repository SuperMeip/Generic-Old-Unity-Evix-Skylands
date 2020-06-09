using UnityEngine;
using System.Collections.Generic;

namespace MeepTech.Voxel.Generation.Mesh {

  /// <summary>
  /// A mesh of tris and verts
  /// </summary>
  public class Mesh : IMesh {

    /// <summary>
    /// the vertices
    /// </summary>
    List<Vector3> vertices;

    /// <summary>
    /// the vertices
    /// </summary>
    List<Color> vertexColors;

    /// <summary>
    /// if this mesh is empty
    /// </summary>
    public bool isEmpty
      => triangles.Count == 0 && vertices.Count == 0;

    /// <summary>
    ///  the triangles
    /// </summary>
    public List<int> triangles {
      get;
    }

    /// <summary>
    /// Get the # of triangles in this mesh
    /// </summary>
    public int triangleCount 
      => triangles.Count / 3;

    /// <summary>
    /// Make a mesh
    /// </summary>
    public Mesh() {
      triangles    = new List<int>();
      vertices     = new List<Vector3>();
      vertexColors = new List<Color>();
    }

    /// <summary>
    /// add a vertex
    /// </summary>
    /// <param name="vertex"></param>
    public void addVertex(Vector3 vertex) {
      vertices.Add(vertex);
      vertexColors.Add(default);
    }

    /// <summary>
    /// add a vertex
    /// </summary>
    /// <param name="vertex"></param>
    public void addVertexWithColor(Vector3 vertex, Color color) {
      vertices.Add(vertex);
      vertexColors.Add(color);
    }

    /// <summary>
    /// Get all of the verticies
    /// </summary>
    /// <returns></returns>
    public Vector3[] getVertices() {
      return vertices.ToArray();
    }

    /// <summary>
    /// Get all of the verticies
    /// </summary>
    /// <returns></returns>
    public Color[] getColors() {
      return vertexColors.ToArray();
    }
  }
}