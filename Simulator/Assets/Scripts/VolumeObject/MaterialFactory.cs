﻿using UnityEngine;

namespace UnityVolumeRendering
{
    public class MaterialFactory
    {
        [System.Obsolete("This method is deprecated, and will be removed. Use VolumeObjectFactory, or custom re-implementation.", false)]
        public static Material CreateMaterialDVR(VolumeDataset dataset)
        {
            Shader shader = Shader.Find("VolumeRendering/DirectVolumeRenderingShader");
            Material material = new Material(shader);

            const int noiseDimX = 512;
            const int noiseDimY = 512;
            Texture2D noiseTexture = NoiseTextureGenerator.GenerateNoiseTexture(noiseDimX, noiseDimY);
            material.SetTexture("_NoiseTex", noiseTexture);
            material.SetTexture("_DataTex", dataset.GetDataTexture());

            return material;
        }
    }
}