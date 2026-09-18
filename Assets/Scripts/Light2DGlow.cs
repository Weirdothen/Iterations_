using UnityEngine;
using UnityEngine.Rendering.Universal;
using DG.Tweening;

public class Light2DGlow : MonoBehaviour
{
    private Light2D myLight;
    public float minIntensity = 13.5f;
    public float maxIntensity = 15.5f;
    public float glowDuration = 1.5f;

    void Start()
    {
        myLight = GetComponent<Light2D>();
        myLight.intensity = minIntensity;
        DOTween.To(() => myLight.intensity, x => myLight.intensity = x, maxIntensity, glowDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }
}