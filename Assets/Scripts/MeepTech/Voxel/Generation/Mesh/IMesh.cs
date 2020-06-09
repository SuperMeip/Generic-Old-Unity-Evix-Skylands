using System.Collections.Generic;
using UnityEngine;

namespace MeepTech.Voxel.Generation.Mesh {

  /// <summary>
  /// A mesh made of tri and verticie data
  /// </summary>
  public interface IMesh {

    /// <summary>
    /// if this mesh never has any verts added to it
    /// </summary>
    bool isEmpty {
      get;
    }

    /// <summary>
    /// The triangles
    /// </summary>
    List<int> triangles {
      get;
    }

    /// <summary>
    /// The number of triangles
    /// </summary>
    int triangleCount {
      get;
    }

    /// <summary>
    /// Add a vertex to the vertex array
    /// </summary>
    /// <param name="vertex"></param>
    void addVertex(Vector3 vertex);

    /// <summary>
    /// Add a vertex with a color attached to it.
    /// </summary>
    /// <param name="vertex"></param>
    /// <param name="color"></param>
    void addVertexWithColor(Vector3 vertex, Color color);

    /// <summary>
    /// Get all the vers as an array.
    /// </summary>
    /// <returns></returns>
    Vector3[] getVertices();

    /// <summary>
    /// Get all the vers as an array.
    /// </summary>
    /// <returns></returns>
    Color[] getColors();
  }
}