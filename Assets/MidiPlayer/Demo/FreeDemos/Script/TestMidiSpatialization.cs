using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MidiPlayerTK;

public class TestMidiSpatialization : MonoBehaviour
{
    public GameObject sphere;
    public Material material;
    public Vector3 CenterPosition;
    public float Radius;
    public float smoothTime = 0.3F;
    public Vector3 RotateAmount;  // degrees per second to rotate in each axis. Set in inspector.

    private Vector3 velocity = Vector3.zero;
    private Vector3 Target;
    private void Start()
    {
        Random.InitState(System.DateTime.Now.Millisecond);
        SetTarget();
    }
    public void AddSphere()
    {
        GameObject goCreated = Instantiate(sphere);
        MidiFilePlayer mfp = goCreated.GetComponentInChildren<MidiFilePlayer>();
        mfp.MPTK_MidiIndex = Random.Range(0, MidiPlayerGlobal.MPTK_ListMidi.Count);
        mfp.MPTK_Play();

        Renderer rend = goCreated.GetComponent<Renderer>();
        rend.material = material;
        rend.material.color = new Color(Random.value, Random.value, Random.value) ;
    }

    private void Update()
    {
        if ((transform.position - Target).sqrMagnitude < 0.01f)
            SetTarget();
        transform.position = Vector3.SmoothDamp(transform.position, Target, ref velocity, smoothTime);
        transform.Rotate(RotateAmount * Time.unscaledDeltaTime);
    }

    private void SetTarget()
    {
        Target = Random.insideUnitSphere * Radius + CenterPosition;
    }
}
