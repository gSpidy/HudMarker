using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TestAttachHudMarker : MonoBehaviour
{
    [SerializeField] private bool m_randomColor = false;
    
    [SerializeField] private Sprite[] m_sprites;
    
    // Start is called before the first frame update
    void Start()
    {
        var rndColor = Random.ColorHSV();
        rndColor.a = 1;

        HudMarkerManager.AddMarker(transform, m_sprites.OrderBy(_ => Random.value).First(),
            overrideColor: m_randomColor ? (Color?) rndColor : null);
    }
}
