using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

[ExecuteInEditMode]
public class ShearTransform : MonoBehaviour
{
    public Vector2 shear;
    private Matrix4x4 shearMatrix;

    private void OnValidate()
    {
        shearMatrix = new Matrix4x4(
            new Vector4(1f, 0f, 0f, 0f),
            new Vector4(shear.x, 1f, shear.y, 0f),
            new Vector4(0f, 0f, 1f, 0f),
            new Vector4(0f, 0f, 0f, 1f)
        );
    }

    private void Awake()
    {
        OnValidate();
    }

    void Update()
    {
        Matrix4x4 shearInjection = transform.localToWorldMatrix * shearMatrix * transform.worldToLocalMatrix;
        
        foreach (var childRenderer in transform.GetComponentsInChildren<Renderer>())
        {
            if(!childRenderer.sharedMaterial)
                continue;
            
            if (childRenderer.sharedMaterial.shader.name == "Mixed Reality Toolkit/Shear")
            {
                childRenderer.sharedMaterial.SetMatrix("_ShearInjection", shearInjection);
            }

            
            MeshFilter meshFilter = childRenderer.GetComponent<MeshFilter>();
            if (meshFilter)
            {
                Mesh mesh = meshFilter.sharedMesh;
                Bounds meshBounds = mesh.bounds;
                meshBounds.extents = new Vector3(10000, 10000, 10000);
                mesh.bounds = meshBounds;
            }
        }
    }
}