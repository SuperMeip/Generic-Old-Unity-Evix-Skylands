using UnityEngine;

namespace MeepTech {
  public class UnityDebugger {
    public bool isEnabled = true;

    public UnityDebugger(bool isEnabled = true) {
      this.isEnabled = isEnabled;
    }

    public void log(string debugMessage) {
      if (isEnabled) {
        Debug.Log(debugMessage);
      }
    }
  }
}
