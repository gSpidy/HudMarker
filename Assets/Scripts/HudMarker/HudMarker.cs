using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class HudMarker : MonoBehaviour
{
    [SerializeField] private Image m_iconImg;
    [SerializeField] private Image m_arrowImg;
    [SerializeField] private Text m_distanceTxt;
    
    [Header("Initialization (optional)")]
    [SerializeField] private Sprite m_initialSprite;
    [SerializeField] private Sprite m_initialArrowSprite;
    [SerializeField] private Transform m_initialTarget;

    [NonSerialized] private CanvasGroup _cg;
    private CanvasGroup Cg => _cg??(_cg = GetComponent<CanvasGroup>());

    private readonly ReactiveProperty<Transform> _targetRp = new ReactiveProperty<Transform>();
    private readonly ReactiveProperty<Sprite> _spriteRp = new ReactiveProperty<Sprite>();

    public Transform Target{ get => _targetRp.Value; set => _targetRp.Value = value;}
    
    public Sprite Sprite{ get => _spriteRp.Value; set => _spriteRp.Value = value;}
    
    public Sprite ArrowSprite{get => m_arrowImg.sprite;set => m_arrowImg.sprite = value;}

    private Color? _overrideColor = null;
    public Color? OverrideColor
    {
        set
        {
            _overrideColor = value;
            SetColor(value??Color.white);
        }
    }

    void SetColor(Color color)
    {
        m_arrowImg.color = color;
        m_distanceTxt.color = color;
    }

    void Awake()
    {
        var targetPosObs = GetObservableTargetPosition();
        var distObs = GetDistanceObservable(targetPosObs);
        
        InitPositionSubscriptions(targetPosObs);
        InitDistanceSubscriptions(distObs);
        InitFadeSubscriptions(targetPosObs,distObs);
        InitSpriteSubscriptions();
    }

    void Start()
    {
        //set initial values
        if(!_targetRp.Value) _targetRp.Value = m_initialTarget;
        if(!_spriteRp.Value) _spriteRp.Value = m_initialSprite;
        if(!ArrowSprite) ArrowSprite = m_initialArrowSprite;
    }

    [NonSerialized] private Camera _mCam;
    [NonSerialized] private Transform _mCamTr;
    
    private Camera MainCam => _mCam ?? (_mCam = Camera.main);
    private Transform MainCamTr => _mCamTr ?? (_mCamTr = MainCam.transform);

    /// <summary>
    /// обсервабл позиция цели
    /// </summary>
    /// <returns></returns>
    IObservable<Vector3> GetObservableTargetPosition() =>
        _targetRp
            .Where(x => x)
            .Select(tgt => this.UpdateAsObservable() //updateasobservable здесь и далее чтобы вычисления производились только когда этот геймобжект активен
                .StartWith(Unit.Default) //первое событие сразу же, не ждем следующего апдейта
                .TakeUntilDestroy(tgt)
                .Select(_ => tgt.position)
                .DistinctUntilChanged())
            .Switch()
            .Replay(1)
            .RefCount();

    /// <summary>
    /// обсервабл дистанция до цели
    /// </summary>
    /// <param name="targetPosObs">позиция цели</param>
    /// <returns></returns>
    IObservable<float> GetDistanceObservable(IObservable<Vector3> targetPosObs)
    {
        //обсервабл позиция камеры
        var camPosObs = this.UpdateAsObservable()
            .Select(_=>MainCamTr.position)
            .DistinctUntilChanged();

        //обсервабл дистанции между целью и камерой, пересчитывается при изменении одной или другой позиции
        return targetPosObs 
            .CombineLatest(camPosObs, Tuple.Create)
            .BatchFrame() //одно событие за кадр
            .Select(x=>x.Last()) //последнее из буффера
            .Select(x=>(x.Item1-x.Item2).magnitude)
            .DistinctUntilChanged()
            .Publish()
            .RefCount();
    }

    void InitPositionSubscriptions(IObservable<Vector3> targetPosObs)
    {
        var parentRt = (RectTransform) transform.parent;
        var parentRect = parentRt.rect;

        var inBackMultiplier =
            Mathf.Sqrt(parentRect.width * parentRect.width + parentRect.height * parentRect.height) *.51f; //чуть больше половины длины диагонали

        //обсервабл позиции цели на экране (в локальных координатах parentRt)
        var localPosObs = this.UpdateAsObservable()
            .WithLatestFrom(targetPosObs, (_, pos) =>
            {
                var localPos = parentRt.InverseTransformPoint(MainCam.WorldToScreenPoint(pos));

                var inFront = localPos.z > 0;
                localPos.z = 0;

                if (inFront)
                    return Vector3.ClampMagnitude(localPos, inBackMultiplier);

                return -localPos.normalized * inBackMultiplier;
            })
            .DistinctUntilChanged()
            .Publish()
            .RefCount();

        //показывать ли иконку или стрелку
        var showIconObs = localPosObs
            .Select(parentRect.Contains)
            .DistinctUntilChanged()
            .Publish()
            .RefCount();

        //переключаем иконку/стрелку
        showIconObs
            .Subscribe(showIcon =>
            {
                m_iconImg.gameObject.SetActive(showIcon);
                m_arrowImg.gameObject.SetActive(!showIcon);
            })
            .AddTo(this);

        //если показываем стрелку то поворачиваем ее пока не станем показывать иконку
        showIconObs
            .Where(x => !x)
            .SelectMany(_ => this.UpdateAsObservable()
                .TakeUntil(showIconObs
                    .Where(x => x)))
            .Subscribe(_ => m_arrowImg.transform.up = transform.localPosition)
            .AddTo(this);

        //клампуем позицию внутри прямоугольника парента и перемещаем маркер туда
        localPosObs
            .Subscribe(pos =>
            {
                pos.x = Mathf.Clamp(pos.x, parentRect.xMin, parentRect.xMax);
                pos.y = Mathf.Clamp(pos.y, parentRect.yMin, parentRect.yMax);
                
                transform.localPosition = pos;
            })
            .AddTo(this);
    }

    void InitDistanceSubscriptions(IObservable<float> distObs)
    {
        //обновление текста с расстоянием
        distObs
            .Select(dist => string.Format(HudMarkerSettings.Instance.distanceTextFormat, dist))
            .DistinctUntilChanged() //только если строка обновилась
            .SubscribeToText(m_distanceTxt)
            .AddTo(this);
    }

    void InitFadeSubscriptions(IObservable<Vector3> targetPosObs, IObservable<float> distObs)
    {
        //меньше ли расстояние порога фейда
        var fadeCheckerObs = distObs
            .Select(x => x < HudMarkerSettings.Instance.fadeDistance)
            .DistinctUntilChanged()
            .Publish()
            .RefCount();
        
        //иконка пропадает вблизи (если не за стеной)
        fadeCheckerObs
            .SelectMany(isFade =>
            {
                if (!isFade)
                    return Observable.Return(false); //иконка появляется

                //если иконка должна пропасть
                return this.UpdateAsObservable()
                    .ThrottleFirst(TimeSpan.FromSeconds(0.5f)) //чекаем только раз в полсекунды
                    .Where(_ => _targetRp.Value) //где цель еще существует
                    .WithLatestFrom(targetPosObs, (_, tPos) =>
                    {
                        if (Physics.Raycast(MainCamTr.position, tPos - MainCamTr.position, out var hInf,HudMarkerSettings.Instance.fadeDistance))
                            return hInf.transform.IsChildOf(_targetRp.Value); //если попали рейкастом во что-то кроме чайлда цели, то иконка не пропадает
                        
                        return true; //если никуда не попали рейкастом то иконка таки пропадает
                    })
                    .TakeUntil(fadeCheckerObs.Where(x => !x));
            })
            .DistinctUntilChanged()
            .Subscribe(isFade =>
            {
                Cg.DOKill();
                Cg.DOFade(isFade ? 0 : 1, .5f);
            })
            .AddTo(this);
    }
    
    Color CalcAvgColor(IEnumerable<Color> colors)
    {
        var r = 0f;
        var g = 0f;
        var b = 0f;
        var wSum = 0f;
        
        foreach (var c in colors)
        {
            r += c.r * c.a;
            g += c.g * c.a;
            b += c.b * c.a;
            wSum += c.a; //альфа как вес цвета
        }
        
        return new Color(r/wSum,g/wSum,b/wSum);
    }
    
    private void InitSpriteSubscriptions()
    {
        _spriteRp
            .Subscribe(x => m_iconImg.sprite = x) //меняем спрайт на имаге
            .AddTo(this);
        
        _spriteRp
            .Where(x => x)
            .Where(_=>!_overrideColor.HasValue)// если кастомый цвет не задан
            .SelectMany(sprite =>
            {
                var ex = false;

                if (!sprite.texture.isReadable)
                {
                    Debug.LogWarning($"Texture {sprite.texture.name} is not readable");
                    ex = true;
                }
                
                if (sprite.packed && sprite.packingMode != SpritePackingMode.Rectangle)
                {
                    Debug.LogWarning($"Can not extract pixels from tight packed atlas {sprite.texture.name}");
                    ex = true;
                }
                
                if(ex) return Observable.Return(Color.white);
                
                var sRect = sprite.textureRect;
                var pixels = sprite.texture.GetPixels((int)sRect.x, (int)sRect.y, (int)sRect.width, (int)sRect.height);

                return Observable
                    .Start(() => CalcAvgColor(pixels)) //Start запускается в отдельном потоке, вычисляем усредненный цвет пикселей
                    .ObserveOnMainThread(); //когда полутали результат, возвращаемся в мейн поток
            })
            .Where(_=>!_overrideColor.HasValue) //еще раз перепроверяем, вдруг задали кастомный цвет во время асинхронных расчетов
            .Subscribe(SetColor) //применить цвета
            .AddTo(this);
    }
}
