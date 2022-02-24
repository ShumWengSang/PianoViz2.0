using UnityEditor;
using UnityEngine;

public class FocusReplacer
{
    static float defaultExtent = 1f; // if no renderer or collider is found, zoom to an object of this size
    static float maxExtent = 20f; // controls the maximum "zoom-out" when focusing large objects
    static float zoomFactor = 0.7f; // zoom-in more than Unity would by default (1f)

    [MenuItem("Edit/Focus Selected &f")]
    static void Focus()
    {
        if (Selection.activeObject == null)
            return;

        //Detect object is in project, so just ping
        if (AssetDatabase.Contains(Selection.activeObject))
        {
            EditorGUIUtility.PingObject(Selection.activeInstanceID);
            return;
        }

        //Detect hierarchy window is focused, so just ping
        if (EditorWindow.mouseOverWindow != SceneView.lastActiveSceneView || SceneView.lastActiveSceneView == null)
        {
            EditorGUIUtility.PingObject(Selection.activeInstanceID);
            return;
        }

        Bounds bounds;
        Bounds allBounds = new Bounds();
        foreach (GameObject obj in Selection.gameObjects)
        {
            Renderer renderer = obj.GetComponentInChildren<Renderer>();
            Collider collider = obj.GetComponentInChildren<Collider>();
            if (renderer && !(renderer is ParticleSystemRenderer))
            {
                bounds = renderer.bounds;
            }
            else if (collider)
            {
                bounds = collider.bounds;
            }
            else
            {
                bounds = new Bounds(obj.transform.position, Vector3.one * defaultExtent);
            }
            if (allBounds.extents == Vector3.zero)
                allBounds = bounds;
            else
                allBounds.Encapsulate(bounds);
        }

        allBounds.extents *= zoomFactor;
        if (allBounds.extents.magnitude > maxExtent)
            allBounds.extents = allBounds.extents.normalized * maxExtent;
        SceneView.lastActiveSceneView.Frame(allBounds, false);
    }
}
