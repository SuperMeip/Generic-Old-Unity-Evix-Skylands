using MeepTech.Voxel;
using System;
using System.Collections.Generic;

namespace MeepTech.Collections {

  public class Octree<StoredType> {

    /// <summary>
    /// Possible value diameters given octree depths
    /// </summary>
    public enum DepthsByDiameter {
      _1,
      _2,
      _4,
      _8,
      _16,
      _32,
      _64,
      _128,
      _256,
      _512,
      _1024,
      _2048,
      _4069,
      _8192,
      _16384,
      _32768,
      _65536
    };

    /// <summary>
    /// A node in the octree
    /// </summary>
    /// <typeparam name="StoredType"></typeparam>
    class OctreeNode<StoredType> {

      /// <summary>
      /// The x,y,z of this node within the overall octree
      /// </summary>
      public Coordinate position { get; private set; }

      /// <summary>
      /// The diameter of this node
      /// </summary>
      public int size { get; private set; }

      /// <summary>
      /// The depth of this node reaches to
      /// </summary>
      public DepthsByDiameter depth {
        get {
          return (DepthsByDiameter)(int)Math.Log(size, 2);
        }
      }

      /// <summary>
      /// The octant of the parrent this node is in
      /// </summary>
      public Octants.Octant octant {
        get {
          if (isRoot) {
            return Octants.WestBottomSouth;
          }
          return parent.getChildOctantFor(position);
        }
      }

      /// <summary>
      /// if this node is a solid block with no children or subdivisions
      /// </summary>
      public bool isSolid {
        get {
          return children == null;
        }
      }

      /// <summary>
      /// If this is the root node of the tree
      /// </summary>
      public bool isRoot {
        get {
          return parent == null;
        }
      }

      /// <summary>
      /// The center point of this octant.
      /// </summary>
      public Coordinate center {
        get {
          return new Coordinate(
            position.x + size / 2,
            position.y + size / 2,
            position.z + size / 2
          );
        }
      }

      /// <summary>
      /// the sub nodes of this node
      /// </summary>
      OctreeNode<StoredType>[] children;

      /// <summary>
      /// The item at this node location.
      /// </summary>
      StoredType value;

      /// <summary>
      /// The parent of this node
      /// </summary>
      OctreeNode<StoredType> parent;

      /// <summary>
      /// Create a new node for an octree
      /// </summary>
      /// <param name="position"></param>
      /// <param name="size"></param>
      /// <param name="value"></param>
      internal OctreeNode(Coordinate position, int size, StoredType value, OctreeNode<StoredType> parent = null) {
        this.position = position;
        this.size = size;
        this.value = value;
        this.parent = parent;
        children = null;
      }

      /// <summary>
      /// Get the child by octant
      /// </summary>
      /// <param name="octant"></param>
      /// <returns></returns>
      OctreeNode<StoredType> getChild(Octants.Octant octant) {
        return isSolid ? null : children[octant.Value];
      }

      /// <summary>
      /// Get the value at the given position
      /// </summary>
      /// <param name="nodePosition">the position (x,y,z) of the value to grab</param>
      /// <param name="minNodeSize">the minimum node size to seek for the value for, can be thought of as sharpness, larger numbers are more vauge</param>
      /// <returns></returns>
      public StoredType getValueAt(Coordinate nodePosition, int minNodeSize = 1) {
        OctreeNode<StoredType> node = getNodeAt(nodePosition, minNodeSize);
        if (node != null) {
          return node.value;
        }
        return value;
      }

      /// <summary>
      /// Set the value of the node given the position.
      /// </summary>
      /// <param name="nodePosition">the position of the node to edit, the target</param>
      /// <param name="newValue">the new value to set at the positon</param>
      /// <param name="nodeSize">if you want to set an area of the tree at the node level, for edge cases. must be a valid node size for this octeee to work</param>
      public void setValueAt(Coordinate nodePosition, StoredType newValue, int nodeSize = 1) {
        // if we're at the correct size, set this and all below.
        if (size == nodeSize) {
          value = newValue;
          clearChildren();
          return;
        }
        OctreeNode<StoredType> childNode;
        // if this node is a solid chunk of the wrong size split it, create the first node below, and recurse the set on it
        if (isSolid) {
          // if this is solid, and the position is somewhere within, we can stop if the whole this is already the right value
          if (value.Equals(newValue)) {
            return;
          }
          childNode = splitAndAddChildNode(
            getChildOctantFor(nodePosition),
            value
          );
          // if this node is a broken chunk of the wrong size, grab/add the child node , then recurse it.
        } else {
          Octants.Octant childOctant = getChildOctantFor(nodePosition);
          childNode = getChild(childOctant);
          if (childNode == null) {
            childNode = addChild(childOctant, value);
          }
        }
        childNode.setValueAt(nodePosition, newValue, nodeSize);
        // cleanup after recursion, when we come up a level, check to see if this node's children are all the same, and solidify it if they are.
        tryToSolidify();
      }

      /// <summary>
      /// Recursively preform an action on the value of this and each child node.
      /// </summary>
      /// <param name="action">the function to perform, takes the value, position, size, and bordering values</param>
      /// <param name="ignore">a function to ignore certain values to skip calculations</param>
      public void forEach(Action<StoredType, Coordinate, int, StoredType[][]> action, Func<StoredType, bool> ignore = null) {
        // if this is solid, just run it once on this value
        if (isSolid) {
          if (!ignore(value)) {
            action(value, position, size, getBorderingValues(this));
          }
        } else {
          // if it's not solid, go through each child and run it on the child 
          // (or this' value if the child is null)
          foreach (Octants.Octant octant in Octants.All) {
            OctreeNode<StoredType> child = getChild(octant);
            if (child != null) {
              child.forEach(action, ignore);
            } else if (!ignore(value)) {
              // we have to set up a temp child to find the bordering values for it
              // but we don't want to save it to save space in the tree
              child = new OctreeNode<StoredType>(
                getChildOctantCoordinate(octant),
                size / 2,
                value,
                this
              );
              action(
                child.value,
                child.position,
                child.size,
                getBorderingValues(child)
              );
            }
          }
        }
      }

      /// <summary>
      /// Get the node at the given position
      /// </summary>
      /// <param name="nodePosition">the position (x,y,z) of the value to grab</param>
      /// <param name="minNodeSize">the minimum node size to seek for the value for, can be thought of as sharpness, larger numbers are more vauge</param>
      /// <returns></returns>
      public OctreeNode<StoredType> getNodeAt(Coordinate nodePosition, int minNodeSize = 1) {
        // if the current node
        //// is solid
        //// we're at the max depth for searching
        //// is the one we're looking for
        // return the current node value
        if (isSolid
          || size == minNodeSize
        ) {
          return this;
        }
        OctreeNode<StoredType> child = getChild(getChildOctantFor(nodePosition));
        // if the child isn't set it's the same as it's parent
        if (child == null) {
          return this;
          // if there's a child, recurse until max depth is reached
        } else {
          return child.getNodeAt(nodePosition, minNodeSize);
        }
      }

      /// <summary>
      /// Update this node to be a root node of the given sized tree.
      /// </summary>
      /// <param name="size"></param>
      public void makeNodeRoot(int treeDiameter = 1) {
        // if it's solid it may have been grabbed at a size too large.
        if (isSolid) {
          size = treeDiameter;
        }
        parent = null;
      }

      /// <summary>
      /// Get all of the bordering values of the given node
      /// </summary>
      /// <returns></returns>
      StoredType[][] getBorderingValues(OctreeNode<StoredType> node) {
        StoredType[][] borderingValues = new StoredType[Directions.All.Length][];
        foreach (Directions.Direction direction in Directions.All) {
          borderingValues[direction.Value] = node.getValuesToThe(direction);
        }
        return borderingValues;
      }

      /// <summary>
      /// Check if the neigboring node(s) in the given direction is(are) solid against this node.
      /// </summary>
      /// <param name="direction"></param>
      /// <returns></returns>
      StoredType[] getValuesToThe(Directions.Direction direction) {
        if (isRoot) {
          return new StoredType[1];
        }
        StoredType defaultValue;
        Octants.Octant currentOctant = parent.getChildOctantFor(position);
        Octants.Octant brotherOctant = currentOctant.toThe(direction);
        // if the octant shares a parent, move downwards into the found brother
        OctreeNode<StoredType> neighborNode;
        if (brotherOctant != null) {
          neighborNode = parent.getChild(brotherOctant);
          defaultValue = parent.value;
          // else we have to get the cousin
        } else {
          neighborNode = getCousinToThe(direction, out defaultValue);
        }
        // if we found a node at all, collect the values of that node.
        if (neighborNode != null) {
          return collectBorderValues(neighborNode, direction.Reverse);
          // if there is no neighboring node, return the value we generated for the position instead
        } else {
          return new StoredType[1] { defaultValue };
        }
      }

      /// <summary>
      /// Get the uncle octant to the given direction of the current octant 
      /// (uncle being an octant bordeing the parent sharing a parent.)
      /// </summary>
      /// <param name="direction"></param>
      /// <param name="uncleValue">Get the value of the uncle, can be used in case no node is returned.</param>
      /// <returns></returns>
      OctreeNode<StoredType> getUncleToThe(Directions.Direction direction, out StoredType uncleValue) {
        // we want to make sure we aren't too far up for uncles
        if (!isRoot && !parent.isRoot) {
          Octants.Octant currentParentOctant = parent.parent.getChildOctantFor(parent.position);
          Octants.Octant uncleOctant = currentParentOctant.toThe(direction);
          // if we have an uncle at this level, return the node and or value from the grandparent
          if (uncleOctant != null) {
            OctreeNode<StoredType> uncleNode = parent.parent.getChild((Octants.Octant)uncleOctant);
            uncleValue = uncleNode == null ? parent.parent.value : uncleNode.value;
            return uncleNode;
            // if we don't, we want to see if this node's parent has the uncle we need.
          } else {
            return parent.getUncleToThe(direction, out uncleValue);
          }
        }
        // there's no uncles, only brothers and parents.
        uncleValue = default;
        return null;
      }

      /// <summary>
      /// Get the 'cousin' of the current node in the given dirrection
      /// (cousin being a node of the same size with different parents)
      /// </summary>
      /// <param name="direction"></param>
      /// <param name="uncleValue">Get the value of the cousin, can be used in case no node is returned.</param>
      /// <returns></returns>
      OctreeNode<StoredType> getCousinToThe(Directions.Direction direction, out StoredType cousinValue) {
        OctreeNode<StoredType> uncleNode = getUncleToThe(direction, out cousinValue);
        // if there's an uncle, we need to find the child element at the location
        // 1 to the direction of this one inside of that uncle with a min resolution
        // of this one's size.
        if (uncleNode != null) {
          OctreeNode<StoredType> cousinNode = uncleNode.getNodeAt(
            position.go(direction, size),
            size
          );
          cousinValue = cousinNode.value;
          return cousinNode;
        }
        // if we don't find an uncle, it's outside the level
        return null;
      }

      /// <summary>
      /// Collect the border values for the direction within the given octant node 
      /// (so if north, it will collect all values on(within) the north face of the current node)
      /// </summary>
      /// <param name="currentNode"></param>
      /// <param name="direction"></param>
      /// <param name="values">Used for recursion</param>
      /// <returns></returns>
      StoredType[] collectBorderValues(OctreeNode<StoredType> currentNode, Directions.Direction direction, List<StoredType> values = null) {
        values = values == null ? new List<StoredType>() : values;
        // if the current node is solid, so is the border in the direction we're searching, just send back the current value
        if (currentNode.isSolid) {
          values.Add(currentNode.value);
        } else {
          // if the current node isn't solid, we'll need to check all blocks within it in the bordering direction.
          foreach (Octants.Octant octant in direction.getOctants()) {
            OctreeNode<StoredType> childNode = currentNode.getChild(octant);
            // if there's no child we just use the current node value for that part of the face,
            // if there is a child we need to grab it's values to that facial direction.
            if (childNode != null) {
              collectBorderValues(childNode, direction, values);
            } else {
              values.Add(currentNode.parent.value);
            }
          }
        }
        return values.ToArray();
      }

      /// <summary>
      /// Split this node up and set just one octant to a new value
      /// </summary>
      /// <param name="octant"></param>
      /// <param name="newValue"></param>
      OctreeNode<StoredType> splitAndAddChildNode(Octants.Octant updatedOctant, StoredType newValue) {
        children = new OctreeNode<StoredType>[8];
        return addChild(updatedOctant, newValue);
      }

      /// <summary>
      /// Add a new child to this node at the given
      /// </summary>
      /// <param name="octant">the octant position to add the child node to</param>
      /// <param name="newValue">the value of the new child node</param>
      /// <returns></returns>
      OctreeNode<StoredType> addChild(Octants.Octant octant, StoredType newValue) {
        children[octant.Value] = new OctreeNode<StoredType>(
          getChildOctantCoordinate(octant),
          size / 2,
          newValue,
          this
        );
        return children[octant.Value];
      }

      /// <summary>
      /// Get the octant the given point is in in relation to the center of this octant.
      /// </summary>
      /// <param name="position"></param>
      /// <returns></returns>
      Octants.Octant getChildOctantFor(Coordinate position) {
        // lil optomization, if it's the same position the child will always be the first octant.
        if (this.position.Equals(position)) {
          return Octants.WestBottomSouth;
        }
        // if it =s the center it will be the last octant.
        if (position.Equals(center)) {
          return Octants.EastTopNorth;
        }
        return center.octantToDirectionOf(position);
      }

      /// <summary>
      /// Get the position of the 0,0 of the requested child octant, given this octants size and position
      /// </summary>
      /// <param name="childOctant"></param>
      /// <returns></returns>
      Coordinate getChildOctantCoordinate(Octants.Octant childOctant) {
        int childOctantSize = size / 2;
        Coordinate childPosition = position;
        if (childOctant.IsEastern) {
          childPosition.x += childOctantSize;
        }
        if (childOctant.IsNorthern) {
          childPosition.z += childOctantSize;
        }
        if (childOctant.IsUpper) {
          childPosition.y += childOctantSize;
        }
        return childPosition;
      }

      /// <summary>
      /// check if this node and it's children can be solidified into one node.
      /// </summary>
      void tryToSolidify() {
        OctreeNode<StoredType> firstChild = getChild(Octants.All[0]);
        // if we can solidify, it must be to this type:
        // if the first value is different from the current one, in order to solidify all other 
        // children must also be this same new value.
        // If the first child is empty or the same as the current one, we need to have all existing children 
        // be either null, or the same as the current value in order to solidify
        StoredType newSolidValue = firstChild == null ? value : firstChild.value;
        foreach (OctreeNode<StoredType> child in children) {
          StoredType childValue = child == null ? value : child.value;
          // we also cannot solidify if one of the children is not solid. (null are solid too)
          if ((child != null && !child.isSolid) || !childValue.Equals(newSolidValue)) {
            return;
          }
        }
        // if we make it through, solidify to the new value.
        value = newSolidValue;
        clearChildren();
      }

      /// <summary>
      /// Remove all children for this node
      /// </summary>
      void clearChildren() {
        children = null;
      }

      /// <summary>
      /// To string
      /// </summary>
      /// <returns></returns>
      public override string ToString() {
        return "=>" + octant + " [" + size + "] " + position;
      }
    }

    /// <summary>
    /// The root node of the octree
    /// </summary>
    private readonly OctreeNode<StoredType> root;

    /// <summary>
    /// The depth(max) of the octree
    /// </summary>
    public DepthsByDiameter depth { get; private set; }

    /// <summary>
    /// The maximum diameter in objects of this octree.
    /// </summary>
    public int diameter {
      get {
        return root.size;
      }
    }

    /// <summary>
    /// Create an octree
    /// </summary>
    /// <param name="size"></param>
    public Octree(DepthsByDiameter maxDepth, StoredType defaultValue) {
      depth = maxDepth;
      root = new OctreeNode<StoredType>(
        new Coordinate(0, 0, 0),
        (int)Math.Pow(2, (int)depth),
        defaultValue
      );
    }

    /// <summary>
    /// Create an octree around the given root node.
    /// </summary>
    /// <param name="root"></param>
    private Octree(OctreeNode<StoredType> root) {
      depth = root.depth;
      this.root = root;
    }

    /// <summary>
    /// Get the value at the given position in the tree
    /// </summary>
    /// <param name="nodePosition">the position (x,y,z) of the value to grab</param>
    /// <param name="minNodeSize">the minimum node size to seek for the value for, can be thought of as sharpness, larger numbers are more vauge</param>
    /// <returns></returns>
    public StoredType getValueAt(Coordinate position, int minNodeSize = 1) {
      return root.getValueAt(
        position,
        // if there's no maxDepth set, use the tree's max depth
        minNodeSize != 1 && !nodeSizeIsValid(minNodeSize)
          ? 1
          : minNodeSize
      );
    }

    /// <summary>
    /// Set the value at the given position in the tree
    /// </summary>
    /// <param name="nodePosition">the position of the node to edit, the target</param>
    /// <param name="newValue">the new value to set at the positon</param>
    /// <param name="nodeSize">if you want to set an area of the tree at the node level, for edge cases. must be a valid node size for this octeee to work</param>
    public void setValueAt(Coordinate position, StoredType newValue, int nodeSize = 1) {
      root.setValueAt(
        position,
        newValue,
        nodeSize != 1 && !nodeSizeIsValid(nodeSize)
          ? 1
          : nodeSize
      );
    }

    /// <summary>
    /// Get a tree from the given node depth
    /// </summary>
    /// <param name="position">The position of the node to get</param>
    /// <param name="diameter">The diameter/depth of the child tree we are grabbing (how large it is, now how deep it is in the parent)</param>
    /// <returns></returns>
    public Octree<StoredType> getTreeAt(Coordinate position, DepthsByDiameter diameter) {
      int expectedSize = GetSizeOfNodeFromDepth(diameter);
      OctreeNode<StoredType> childTreeRootNode = root.getNodeAt(
        position,
        expectedSize
      );
      // trim the grabbed node to the selected size and push it into a tree as the root.
      childTreeRootNode.makeNodeRoot(expectedSize);
      return new Octree<StoredType>(childTreeRootNode);
    }

    /// <summary>
    /// Preform an action recursively on the value of every node in this tree.
    /// </summary>
    /// <param name="action">An action that takes the node value, node location, node size, and bordering values as it's parameter</param>
    /// <param name="ignore">a function to ignore certain values to skip calculations</param>
    public void forEach(Action<StoredType, Coordinate, int, StoredType[][]> action, Func<StoredType, bool> ignore = null) {
      root.forEach(action, ignore);
    }

    /// <summary>
    /// Get the diameter of a node at the given depth of this tree using maths
    /// </summary>
    /// <param name="depth"></param>
    /// <returns></returns>
    int getNodeSizeAtDepth(DepthsByDiameter depth) {
      return (int)(root.size * (1 / Math.Pow(2, (int)depth)));
    }

    /// <summary>
    /// Get the diameter of a node with the given depth.
    /// </summary>
    /// <param name="depth"></param>
    /// <returns>The size of node with the given max depth</returns>
    public static int GetSizeOfNodeFromDepth(DepthsByDiameter depth) {
      return (int)Math.Pow(2, (int)depth);
    }

    /// <summary>
    /// Check if the given node size is valid for this octree
    /// </summary>
    /// <param name="nodeSize"></param>
    /// <returns></returns>
    bool nodeSizeIsValid(int nodeSize) {
      return root != null
        && (nodeSize != 0)
        && root.size >= nodeSize
        && ((nodeSize & (nodeSize - 1)) == 0);
    }
  }
}