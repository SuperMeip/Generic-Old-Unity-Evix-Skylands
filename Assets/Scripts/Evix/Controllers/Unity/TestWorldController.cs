﻿using MeepTech.GamingBasics;
using MeepTech.Voxel.Generation.Sources;
using UnityEngine;

namespace Evix.Controllers.Unity {

  public class TestWorldController : MonoBehaviour {

    /// <summary>
    /// The current object to focus on.
    /// </summary>
    public FocusController currentFocus;

    /// <summary>
    /// The controller for the active level.
    /// </summary>
    public LevelController levelController;

    public float XWaveFrequency = 0.1f;
    public float ZWaveFrequency = 0.1f;
    public float Smoothness = -1f;
    public float value4 = 1f;
    public float value5 = 0f;
    public float value6 = 1f;
    public float value7 = 10f;
    public float value8 = 20f;
    public float SeaLevel = 30.0f;

    IVoxelSource voxelSource;

    // Start is called before the first frame update
    void Awake() {
      voxelSource = getConfiguredPlainSource();
      World.InitializeTestWorld(levelController, voxelSource, currentFocus);
    }

    WaveSource getConfiguredWaveSource() {
      WaveSource newSource = new WaveSource();
      newSource.xWaveFrequency = XWaveFrequency;
      newSource.zWaveFrequency = ZWaveFrequency;
      newSource.smoothness = Smoothness;
      newSource.value4 = value4;
      newSource.value5 = value5;
      newSource.value6 = value6;
      newSource.value7 = value7;
      newSource.value8 = value8;

      return newSource;
    }

    FlatPlainsSource getConfiguredPlainSource() {
      FlatPlainsSource plainsSource = new FlatPlainsSource();
      plainsSource.seaLevel = SeaLevel;

      return plainsSource;
    }
  }
}