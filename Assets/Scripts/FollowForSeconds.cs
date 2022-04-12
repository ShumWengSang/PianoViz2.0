using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(FollowCameraScript))]
public class FollowForSeconds : MonoBehaviour
{
    public float waitTime;
    private FollowCameraScript followCameraScript;
    // Start is called before the first frame update
    void Start()
    {
        followCameraScript = GetComponent<FollowCameraScript>();
        followCameraScript.enabled = true;
        StartCoroutine(waitToDisable());
    }

    IEnumerator waitToDisable()
    {
        yield return new WaitForSeconds(waitTime);
        followCameraScript.enabled = false;
    }

}
