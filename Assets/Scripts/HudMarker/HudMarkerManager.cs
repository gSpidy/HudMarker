using System.Collections;
using System.Collections.Generic;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

public class HudMarkerManager : MonoBehaviourSingleton<HudMarkerManager>
{
    [SerializeField] private RectTransform m_markersParent;
    [SerializeField] private int m_initialPoolCapacity = 0;
    
    private Pool<HudMarker> _pool;

    protected override void InitSingleton() =>
        _pool = new Pool<HudMarker>(
            () => Instantiate(HudMarkerSettings.Instance.markerPrefab, m_markersParent),
            m_initialPoolCapacity,
            onTake: marker =>
            {
                if(marker)
                    marker.gameObject.SetActive(true);
            },
            onRecycle: marker =>
            {
                if(marker)
                    marker.gameObject.SetActive(false);
            });

    /// <summary>
    /// Добавить маркер для цели
    /// </summary>
    /// <param name="target">трансформ цели</param>
    /// <param name="sprite">спрайт</param>
    /// <param name="arrowSprite">опциональный спрайт для стрелки</param>
    /// <param name="overrideColor">опциональный цвет для стрелки</param>
    public static void AddMarker(Transform target, Sprite sprite, Sprite arrowSprite = null, Color? overrideColor = null)
    {
        var marker = Instance._pool.Take();
        
        marker.OverrideColor = overrideColor;
        
        marker.Sprite = sprite;
        marker.Target = target;

        if (arrowSprite) marker.ArrowSprite = arrowSprite;

        target.OnDestroyAsObservable()
            .Take(1)
            .Subscribe(_ => Instance._pool.Recycle(marker))
            .AddTo(Instance);
    }
}
