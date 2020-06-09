using MeepTech.Voxel.Collections.Storage;
using MeepTech.Voxel.Generation.Sources;
using System;

namespace MeepTech.Voxel.Generation.Biomes {
  public class BiomeMap : VoxelSource {

    /// <summary>
    /// The types of master biome tiles.
    /// </summary>
    public enum MasterBiomeTileType {Sky, Land, Sea, Mountain};

    /// <summary>
    /// The master biome tile map
    /// </summary>
    MasterBiomeTileType[,] masterBiomeTileMap;

    /// <summary>
    /// The size of a master biome tile in chunks, x and z.
    /// </summary>
    int masterBiomeChunkResolution;

    /// <summary>
    /// The xhunk border thicknesses, indexed by direction.
    /// </summary>
    int[] chunkBorderThicknesses;

    /// <summary>
    /// Create a new biome map with the given resolutions.
    /// </summary>
    /// <param name="mapSizeInChunks">the map bounds, in chunks.</param>
    /// <param name="masterBiomeResolution">The amount of master biome tiles we want across the smallest axis of the map size</param>
    /// <param name="minorBiomeVoronoiResolution">The amount of Voronoi dots to use per master biome tile for generating minorbiome tiles</param>
    BiomeMap(int seed, Coordinate mapSizeInChunks, int masterBiomeResolution = 7, int minorBiomeVoronoiResolution = 15, int baseBorderChunks = 2) : base(seed) {
      chunkBorderThicknesses = new int[Directions.Cardinal.Length];
      // the master tile map has a border, this is to keep the tiles square even on an odd count map, and to guarentee coastlines.
      Coordinate masterTileMapSize = mapSizeInChunks - (baseBorderChunks * 2);
      /// we'll use the smaller length of the map to generat the master biome tile resolution settings.
      // the tiles must be square.
      Coordinate masterTileMapResolution = Coordinate.Zero;
      bool xisSmallerThanZ = masterTileMapSize.x < masterTileMapSize.z;
      if (xisSmallerThanZ) {
        masterTileMapResolution.x = masterBiomeResolution;
      } else {
        masterTileMapResolution.z = masterBiomeResolution;
      }
      int masterMapSmallerSideLength = xisSmallerThanZ ? masterTileMapSize.x : masterTileMapSize.z;
      int remainingChunks = masterMapSmallerSideLength % masterBiomeResolution;
      // we set up the resolution in chunks of the master map tiles based on the smaller edge of the overall map.
      masterBiomeChunkResolution = masterMapSmallerSideLength / masterBiomeResolution;
      /// set up the border thicknesses for the master biome tile map.
      // if we have an even number of remaining chunks.
      if (remainingChunks % 2 == 0) {
        chunkBorderThicknesses[xisSmallerThanZ ? Directions.East.Value : Directions.North.Value]
          = chunkBorderThicknesses[xisSmallerThanZ ? Directions.West.Value : Directions.South.Value]
            = baseBorderChunks + remainingChunks / 2;
      // else if it's odd:
      } else {
        chunkBorderThicknesses[xisSmallerThanZ ? Directions.East.Value : Directions.North.Value] = baseBorderChunks + remainingChunks / 2 + 1;
        chunkBorderThicknesses[xisSmallerThanZ ? Directions.West.Value : Directions.South.Value] = baseBorderChunks + remainingChunks / 2 ;
      }
      masterTileMapSize -= remainingChunks;
      masterTileMapSize = masterTileMapSize.replaceY(0);

      /// set the master tile map resolution of the longer/other side based on how big we know a master biome is now.
      int masterMapLongerSideLength = xisSmallerThanZ ? masterTileMapSize.z : masterTileMapSize.x;
      if (xisSmallerThanZ) {
        masterTileMapResolution.z = masterMapLongerSideLength / masterBiomeChunkResolution;
      } else {
        masterTileMapResolution.x = masterMapLongerSideLength / masterBiomeChunkResolution;
      }
      remainingChunks = masterMapLongerSideLength % masterBiomeChunkResolution;

      /// add more border thicknesses for the master biome tile map.
      // if we have an even number of remaining chunks.
      if (remainingChunks % 2 == 0) {
        chunkBorderThicknesses[!xisSmallerThanZ ? Directions.East.Value : Directions.North.Value] += remainingChunks / 2;
        chunkBorderThicknesses[!xisSmallerThanZ ? Directions.West.Value : Directions.South.Value] += remainingChunks / 2;
        // else if it's odd:
      } else {
        chunkBorderThicknesses[!xisSmallerThanZ ? Directions.East.Value : Directions.North.Value] += remainingChunks / 2 + 1;
        chunkBorderThicknesses[!xisSmallerThanZ ? Directions.West.Value : Directions.South.Value] += remainingChunks / 2;
      }

      masterBiomeTileMap = new MasterBiomeTileType[masterTileMapResolution.x, masterTileMapResolution.z];
    }

    protected override float getNoiseValueAt(Coordinate location) {
      throw new NotImplementedException();
    }
  }
}
