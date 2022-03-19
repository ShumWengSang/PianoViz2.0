using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using DG.Tweening.Core;
using MidiVisualization.Scripts;
using UnityEngine;
using UnityEngine.Assertions;

public static class ColorExtension
{
    public static Vector3 ToHSV(this Color color)
    {
        Vector3 hsv;
        Color.RGBToHSV(color, out hsv.x, out hsv.y, out hsv.z);
        return hsv;
    }

    public static Color HSVToRGB(Vector3 hsv)
    {
        return Color.HSVToRGB(hsv.x, hsv.y, hsv.z);
    }
}

public class keyboardKeyHighlights : MonoBehaviour
{
    public enum Mode
    {
        Off,
        IrridescentPulse,
        Flash,
        RainbowInOut
    }

    [SerializeField] public Mode mode = Mode.Off;
    [SerializeField] private KeyboardHighlightMaterials materials;

    private Sequence currentSequence;
    private Mode currentMode = Mode.Off;
    private Renderer renderer;

    private Sequence IrridescentPulseSequence => DOTween.Sequence()
        .AppendCallback(() =>
        {
            renderer.material = materials.irridescent;
            renderer.material.SetFloat(materials.environmentColorThreasholdID, 0f);
        })

        // tween threashold to 1.16
        .Append(
            DOTween.To(
                    () => renderer.material.GetFloat(materials.environmentColorThreasholdID),
                    x => renderer.material.SetFloat(materials.environmentColorThreasholdID, x),
                    1.16f,
                    1f)
                .SetTarget(renderer.material)
        )

        // tween threashold back to 0
        .Append(
            DOTween.To(
                    () => renderer.material.GetFloat(materials.environmentColorThreasholdID),
                    x => renderer.material.SetFloat(materials.environmentColorThreasholdID, x),
                    0f,
                    1f)
                .SetTarget(renderer.material)
        )
        .SetLoops(-1);

    private Sequence OffSequence => DOTween.Sequence()
        .AppendCallback(() => { renderer.material = materials.black; });


    private Sequence FlashSequence => DOTween.Sequence()
        .AppendCallback(() =>
        {
            renderer.material = materials.flash;
            renderer.material.SetColor(materials.albedoColorID, Color.black);
            renderer.material.SetColor(materials.rimLightColorID, Color.black);
        })

        // quickly flash white
        .Append(DOTween.To(
                () => renderer.material.GetColor(materials.albedoColorID),
                x => renderer.material.SetColor(materials.albedoColorID, x),
                materials.flash.GetColor(materials.albedoColorID),
                .1f)
            .SetTarget(renderer.material))
        .Join(DOTween.To(
                () => renderer.material.GetColor(materials.rimLightColorID),
                x => renderer.material.SetColor(materials.rimLightColorID, x),
                materials.flash.GetColor(materials.rimLightColorID),
                .1f)
            .SetTarget(renderer.material))

        // fade to black
        .Append(DOTween.To(
                () => renderer.material.GetColor(materials.albedoColorID),
                x => renderer.material.SetColor(materials.albedoColorID, x),
                Color.black,
                1f)
            .SetTarget(renderer.material))
        .Join(DOTween.To(
                () => renderer.material.GetColor(materials.rimLightColorID),
                x => renderer.material.SetColor(materials.rimLightColorID, x),
                Color.black,
                1f)
            .SetTarget(renderer.material))
        .AppendCallback(() => { mode = Mode.Off; });

    private Sequence RainbowInOutSequence => DOTween.Sequence()
        .Append(DOTween.Sequence()
            .AppendCallback(() =>
            {
                renderer.material = materials.flash;
                renderer.material.SetColor(materials.albedoColorID, Color.HSVToRGB(0, 1, 0.0001f));
                renderer.material.SetColor(materials.rimLightColorID, Color.HSVToRGB(0, 1, 0.0001f));
            })

            // get brighter and cycle hue
            .Append(DOTween.To(
                    () => renderer.material.GetColor(materials.albedoColorID).ToHSV(),
                    x => renderer.material.SetColor(materials.albedoColorID, ColorExtension.HSVToRGB(x)),
                    new Vector3(.25f, 1f, .7f),
                    .5f)
                .SetTarget(renderer.material))
            .Join(DOTween.To(
                    () => renderer.material.GetColor(materials.rimLightColorID),
                    x => renderer.material.SetColor(materials.rimLightColorID, x),
                    materials.flash.GetColor(materials.rimLightColorID),
                    .5f)
                .SetTarget(renderer.material))

            // get darker and cycle hue
            .Append(DOTween.To(
                    () => renderer.material.GetColor(materials.albedoColorID).ToHSV(),
                    x => renderer.material.SetColor(materials.albedoColorID, ColorExtension.HSVToRGB(x)),
                    new Vector3(1f, 1f, 0f),
                    1.5f)
                .SetTarget(renderer.material))
            .Join(DOTween.To(
                    () => renderer.material.GetColor(materials.rimLightColorID),
                    x => renderer.material.SetColor(materials.rimLightColorID, x),
                    Color.black,
                    1.5f)
                .SetTarget(renderer.material))
            .AppendCallback(() => { mode = Mode.Off; })
        )
        
        // tell siblings to start this sequence .05 seconds after we started it
        // this will cause the effect to propogate outward from this key
        .Join(DOTween.Sequence()
            .AppendInterval(.05f)
            .AppendCallback(() =>
            {
                int right = transform.GetSiblingIndex() + 1;
                if (right < transform.parent.childCount)
                {
                    transform.parent.GetChild(right).GetComponent<keyboardKeyHighlights>().mode =
                        Mode.RainbowInOut;
                }

                int left = transform.GetSiblingIndex() - 1;
                if (left >= 0)
                {
                    transform.parent.GetChild(left).GetComponent<keyboardKeyHighlights>().mode =
                        Mode.RainbowInOut;
                }
            })
        );

    private void Awake()
    {
        renderer = GetComponent<Renderer>();
        if (mode == Mode.Off)
        {
            currentSequence = OffSequence;
        }
    }

    private void OnEnable()
    {
        currentSequence?.Play();
    }

    private void OnDisable()
    {
        currentSequence.Pause();
    }

    // Update is called once per frame
    void Update()
    {
        if (currentMode != mode)
        {
            currentMode = mode;
            currentSequence?.Kill(true);
            currentSequence = null;
            switch (mode)
            {
                case Mode.Off:
                    currentSequence = OffSequence;
                    break;
                case Mode.IrridescentPulse:
                    currentSequence = IrridescentPulseSequence;
                    break;
                case Mode.Flash:
                    currentSequence = FlashSequence;
                    break;
                case Mode.RainbowInOut:
                    currentSequence = RainbowInOutSequence;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public void SetModeOff()
    {
        mode = Mode.Off;
    }

    public void SetModeIrridescentPulse(bool restart = false)
    {
        if (restart)
            currentMode = Mode.Off;
        mode = Mode.IrridescentPulse;
    }

    public void SetModeFlash(bool restart = false)
    {
        if (restart)
            currentMode = Mode.Off;
        mode = Mode.Flash;
    }

    public void SetModeRainbowInOut(bool restart = false)
    {
        if (restart)
            currentMode = Mode.Off;
        mode = Mode.RainbowInOut;
    }
}