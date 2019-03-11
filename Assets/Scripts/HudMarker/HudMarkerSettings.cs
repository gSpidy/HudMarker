using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = nameof(HudMarkerSettings),menuName = "Game Configs/"+nameof(HudMarkerSettings))]
public class HudMarkerSettings : ScriptableObjectSingleton<HudMarkerSettings>
{
    public HudMarker markerPrefab;
    public string distanceTextFormat = "{0:F1} m";
    public float fadeDistance = 2;
}
