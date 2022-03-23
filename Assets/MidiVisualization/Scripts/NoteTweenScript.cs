using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using MidiPlayerTK;
using MPTK.NAudio.Midi;
using UnityEngine;
using UnityEngine.Assertions;

public class NoteTweenScript : MonoBehaviour
{
    public keyboardKeyHighlights keyboardKeyHighlights = null;
    private float backY; // y of the object's transform that's furthest from destination
    private float frontY; // y of the object's transform that's closest to destination

    void Awake()
    {
        frontY = transform.localPosition.y;
        backY = transform.localPosition.y;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 localPosition = transform.localPosition;
        Vector3 scale = transform.localScale;
        localPosition.y = frontY;
        scale.y = backY - frontY;

        transform.localPosition = localPosition;
        transform.localScale = scale;
    }


    /// <summary>Tweens the notes scale and position accross the play space
    /// Also stores the NoteTweenScript as the tween's target so it can be used for filtered operations</summary>
    /// <param name="endPos">The position you wish the beginning of the note to cross in world space</param>
    /// <param name="duration">The duration of the tween</param>
    /// <param name="backDelay">how long to delay movement of the back of the bar</param>
    public Sequence DOMoveScale(Vector3 endPos, float duration, float backDelay, Ease ease)
    {
        // not sure why i have to devide by 5 but it works...
        Vector3 endLocal = transform.InverseTransformPoint(endPos) / 5;
        
        // make sure that desination only needs to move in the local y direction
        Assert.IsTrue(Mathf.Abs(0.0f - endLocal.x) < 0.001);
        Assert.IsTrue(Mathf.Abs(0.0f - endLocal.z) < 0.001);
        
        float endY = endLocal.y;
        return DOTween.Sequence()
            .Insert(0,

                // tween the front of the object
                DOTween.Sequence()
                    .Append(DOTween.To(() => frontY, x => frontY = x, endY,
                        duration).SetEase(ease))
                    .AppendCallback(() => keyboardKeyHighlights?.SetModeFlash(true))
            )
            .Insert(0,

                // delay for backDelay then tween the back of the object
                DOTween.Sequence()
                    .AppendInterval(backDelay)
                    .Append(DOTween.To(() => backY, x => backY = x, endY,
                        duration).SetEase(ease))
            )
            .SetTarget(this);
    }
}