﻿using UnityEngine;

namespace UnityVolumeRendering
{
    [ExecuteInEditMode]
    public class SlicingPlane : MonoBehaviour
    {
        public VolumeRenderedObject targetObject;
        private MeshRenderer meshRenderer;

        private void Start()
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }

        private void Update()
        {
            meshRenderer.sharedMaterial.SetMatrix("_parentInverseMat", transform.parent.worldToLocalMatrix);
            meshRenderer.sharedMaterial.SetMatrix("_planeMat", transform.localToWorldMatrix); // TODO: allow changing scale
        }
    }
}

// using UnityEngine;

// namespace UnityVolumeRendering
// {
//     [ExecuteInEditMode]
//     public class SlicingPlane : MonoBehaviour
//     {
//         private MeshRenderer meshRenderer;

//         private void Start()
//         {
//             meshRenderer = GetComponent<MeshRenderer>();
//         }

//         private void Update()
//         {
//             meshRenderer.sharedMaterial.SetMatrix("_parentInverseMat", transform.parent.worldToLocalMatrix);
//             // meshRenderer.sharedMaterial.SetMatrix("_planeMat", Matrix4x4.TRS(transform.position, Quaternion.Inverse(transform.rotation), transform.parent.lossyScale)); // TODO: allow changing scale
//             meshRenderer.sharedMaterial.SetMatrix("_planeMat", Matrix4x4.TRS(transform.position, transform.rotation, transform.parent.lossyScale)); // TODO: allow changing scale
//         }
//     }
// }

