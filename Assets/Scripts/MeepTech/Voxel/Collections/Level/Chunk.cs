using MeepTech.Voxel.Collections.Storage;
using MeepTech.Voxel.Generation.Mesh;
using UnityEngine;

namespace MeepTech.Voxel.Collections.Level {

  /// <summary
  /// A type of block storage that allows neighbors and a mesh to link together
  /// </summary>
  public class Chunk : IVoxelChunk {

    /// <summary>
    /// The voxel diameter, x y and z, of a chunk in this level
    /// </summary>
    public const int Diameter = 25;

    /// <summary>
    /// if this storage set is empty of voxels
    /// </summary>
    public bool isLoaded {
      get => voxels.isLoaded;
      set { voxels.isLoaded = value; }
    }

    /// <summary>
    /// Check if this chunk has all of it's neighbors loaded.
    /// </summary>
    public bool neighborsAreLoaded {
      get {
        foreach(IVoxelChunk neighbor in neighbors) {
          if (neighbor != null && neighbor.isLoaded) {
            continue;
          } else {
            return false;
          }
        }

        return true;
      }
    }

    /// <summary>
    /// Check if this chunk has all of it's neighbors loaded.
    /// </summary>
    public bool neighborsNeighborsAreLoaded {
      get {
        foreach(Chunk neighbor in neighbors) {
          if (neighbor != null && neighbor.isLoaded) {
           foreach(Chunk neighborOfNeighbor in neighbor.neighbors) {
              if (neighborOfNeighbor != null && neighborOfNeighbor.isLoaded) {
                continue;
              } else {
                return false;
              }
            }
            continue;
          } else {
            return false;
          }
        }

        return true;
      }
    }

    /// <summary>
    /// The voxels in this chunk
    /// </summary>
    public IVoxelStorage voxels {
      get;
      private set;
    }

    /// <summary>
    /// The voxels in this chunk
    /// </summary>
    public IMesh mesh {
      get;
      private set;
    }

    /// <summary>
    /// The bounds of the chunks voxels
    /// </summary>
    public Coordinate bounds
      => voxels.bounds;

    /// <summary>
    /// if this chunk is empty
    /// </summary>
    public bool isEmpty
      => voxels == null || voxels.isEmpty;

    /// <summary>
    /// The neighbors of this chunk, indexed by the direction they're in
    /// </summary>
    IVoxelChunk[] neighbors;

    /// <summary>
    /// Make a chunk out of voxel data
    /// </summary>
    /// <param name="voxels">the voxel data of this chunk</param>
    /// <param name="neighbors">the other chunks to link to</param>
    public Chunk(IVoxelStorage voxels, IVoxelChunk[] neighbors = null, IMesh mesh = null) : this(voxels, neighbors) {
      this.mesh = mesh;
    }

    /// <summary>
    /// Private chunk constructor, capable of making an empty, unloaded chunk.
    /// </summary>
    Chunk(IVoxelStorage voxels = null, IVoxelChunk[] neighbors = null) {
      this.voxels = voxels ?? new VoxelDictionary(Coordinate.Zero);
      if (neighbors != null) {
        this.neighbors = neighbors;
        howdyToAllNeighbors();
      } else {
        this.neighbors = new IVoxelChunk[Directions.All.Length];
      }
    }

    /// <summary>
    /// Get an empty loaded chunk, used to signify space beyond the level.
    /// </summary>
    /// <returns></returns>
    public static Chunk getEmptyChunk(bool withEmptyNeighbors = false) {
      IVoxelChunk[] emptyNeighbors = null;
      if (withEmptyNeighbors) {
        emptyNeighbors = new IVoxelChunk[Directions.All.Length];
        foreach(Directions.Direction direction in Directions.All) {
          emptyNeighbors[direction.Value] = getEmptyChunk();
        }
      }
      Chunk emptyChunk = new Chunk(null, emptyNeighbors);
      emptyChunk.voxels.isLoaded = true;

      return emptyChunk;
    }

    /// <summary>
    /// get the voxel, making sure we're in the right chunk first
    /// </summary>
    /// <param name="location"></param>
    /// <returns></returns>
    public Voxel.Type get(Coordinate location) {
      /// empty chunks have only air
      if (isEmpty) {
        return Terrain.Types.Air;
      }

      // if this is out of bounds, try to get the value from the loaded neighbor
      if (!location.isWithin(Coordinate.Zero, bounds)) {
        if (location.x >= bounds.x) {
          return getFromNeighbor(Directions.East, location);
        }
        if (location.x < 0) {
          return getFromNeighbor(Directions.West, location);
        }
        if (location.y >= bounds.y) {
          return getFromNeighbor(Directions.Above, location);
        }
        if (location.y < 0) {
          return getFromNeighbor(Directions.Below, location);
        }
        if (location.z >= bounds.z) {
          return getFromNeighbor(Directions.North, location);
        }
        if (location.z < 0) {
          return getFromNeighbor(Directions.South, location);
        }
      }

      return voxels.get(location);
    }

    /// <summary>
    /// set the voxel, making sure we're in the right chunk first
    /// </summary>
    /// <param name="location"></param>
    /// <param name="newVoxelType"></param>
    public void set(Coordinate location, Voxel.Type newVoxelType) {
      // Can't set voxels in unloaded chunks
      if (!isLoaded) {
        return;
      }

      // if this is out of bounds, try to set the value from on the loaded neighbor
      if (!location.isWithin(Coordinate.Zero, bounds)) {
        if (location.x >= bounds.x) {
          setToNeighbor(Directions.East, location, newVoxelType);
        }
        if (location.x < 0) {
          setToNeighbor(Directions.West, location, newVoxelType);
        }
        if (location.y >= bounds.y) {
          setToNeighbor(Directions.Above, location, newVoxelType);
        }
        if (location.y < 0) {
          setToNeighbor(Directions.Below, location, newVoxelType);
        }
        if (location.z >= bounds.z) {
          setToNeighbor(Directions.North, location, newVoxelType);
        }
        if (location.z < 0) {
          setToNeighbor(Directions.South, location, newVoxelType);
        }
        // can't set if the neighbor isn't loaded
      }

      voxels.set(location, newVoxelType);
    }

    /// <summary>
    /// Get a voxel from a neighbor in the given direction
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="localLocation"></param>
    /// <returns></returns>
    Voxel.Type getFromNeighbor(Directions.Direction direction, Coordinate localLocation) {
      IVoxelChunk neighbor = getNeighbor(direction);
      if (neighbor != null && !neighbor.isEmpty) {
        Coordinate neighborLocalLocation = localLocation - (direction.Offset * Diameter);
        Voxel.Type neighboringBlockType = neighbor.get(neighborLocalLocation);
        return neighboringBlockType;
      }

      return Terrain.Types.Air;
    }

    /// <summary>
    /// Tell all neighbors that this is their neighbor
    /// </summary>
    void howdyToAllNeighbors() {
      foreach(Directions.Direction direction in Directions.All) {
        if (getNeighbor(direction) is Chunk neighbor) {
          neighbor.neighbors[direction.Reverse.Value] = this;
        }
      }
    }

    /// <summary>
    /// Set a voxel oon a neighbor in the given direction
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="localLocation"></param>
    /// <returns></returns>
    void setToNeighbor(Directions.Direction direction, Coordinate localLocation, Voxel.Type newVoxelType) {
      IVoxelChunk neighbor = getNeighbor(direction);
      if (neighbor.isLoaded) {
        Coordinate neighborLocalLocation = localLocation - (direction.Offset * Diameter);
        neighbor.set(neighborLocalLocation, newVoxelType);
      }
    }

    /// <summary>
    /// Get the neigboring chunk
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    IVoxelChunk getNeighbor(Directions.Direction direction) {
      return neighbors[direction.Value] ?? default;
    }
  }
}