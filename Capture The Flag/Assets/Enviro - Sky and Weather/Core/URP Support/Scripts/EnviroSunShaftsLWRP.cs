using System.Collections.Generic;
using UnityEngine.Serialization;
#if ENVIRO_LWRP
namespace UnityEngine.Rendering.LWRP
{
    public class EnviroSunShaftsLWRP : UnityEngine.Rendering.Universal.ScriptableRendererFeature
    {
        EnviroBlitPassIdentifiers getSouceTexture;
        EnviroBlitPass blitPassFinal;
        private Camera myCam;


        #region Shafts Var
        /// LightShafts

        public enum ShaftsScreenBlendMode
        {
            Screen = 0,
            Add = 1,
        }

        [HideInInspector]
        public int radialBlurIterations = 2;
        private Material sunShaftsMaterial;
        private Material moonShaftsMaterial;
        private Material simpleSunClearMaterial;
        private Material simpleMoonClearMaterial;
#endregion

        void CreateMaterialsAndTextures()
        {
            sunShaftsMaterial = new Material(Shader.Find("Enviro/Effects/LightShafts"));
            simpleSunClearMaterial = new Material(Shader.Find("Enviro/Effects/ClearLightShafts"));
        }

        void CleanupMaterials()
        {
            if (sunShaftsMaterial != null)
                DestroyImmediate(sunShaftsMaterial);
            if (simpleSunClearMaterial != null)
                DestroyImmediate(simpleSunClearMaterial);
        }

        void RenderLightShaft(UnityEngine.Rendering.Universal.ScriptableRenderer renderer, RenderTexture source, RenderTargetIdentifier src, UnityEngine.Rendering.Universal.RenderTargetHandle destination, Material mat, Material clearMat, Transform lightSource, Color treshold, Color clr)
        {
            int divider = 4;
            if (EnviroSkyMgr.instance.LightShaftsSettings.resolution == EnviroPostProcessing.SunShaftsResolution.Normal)
                divider = 2;
            else if (EnviroSkyMgr.instance.LightShaftsSettings.resolution == EnviroPostProcessing.SunShaftsResolution.High)
                divider = 1;

            Vector3 v = Vector3.one * 0.5f;

            if (lightSource)
                v = myCam.WorldToViewportPoint(lightSource.position);
            else
                v = new Vector3(0.5f, 0.5f, 0.0f);

            int rtW = source.width / divider;
            int rtH = source.height / divider;
      
            RenderTextureDescriptor textureDescriptor = source.descriptor;
            textureDescriptor.width = rtW;
            textureDescriptor.height = rtH;

            RenderTexture lrColorB;
            RenderTexture lrDepthBuffer;
            // VR Usage
            lrDepthBuffer = RenderTexture.GetTemporary(textureDescriptor);

            // mask out everything except the skybox
            // we have 2 methods, one of which requires depth buffer support, the other one is just comparing images

            mat.SetVector("_BlurRadius4", new Vector4(1.0f, 1.0f, 0.0f, 0.0f) * EnviroSkyMgr.instance.LightShaftsSettings.blurRadius);
            mat.SetVector("_SunPosition", new Vector4(v.x, v.y, v.z, EnviroSkyMgr.instance.LightShaftsSettings.maxRadius));
            mat.SetVector("_SunThreshold", treshold);
 
            Graphics.Blit(source, lrDepthBuffer, mat, 2);

            // paint a small black small border to get rid of clamping problems
            if (myCam.stereoActiveEye == Camera.MonoOrStereoscopicEye.Mono)
                DrawBorder(lrDepthBuffer, clearMat);

            // radial blur:

            radialBlurIterations = Mathf.Clamp(radialBlurIterations, 1, 4);

            float ofs = EnviroSkyMgr.instance.LightShaftsSettings.blurRadius * (1.0f / 768.0f);

            mat.SetVector("_BlurRadius4", new Vector4(ofs, ofs, 0.0f, 0.0f));
            mat.SetVector("_SunPosition", new Vector4(v.x, v.y, v.z, EnviroSkyMgr.instance.LightShaftsSettings.maxRadius));

            for (int it2 = 0; it2 < radialBlurIterations; it2++)
            {
                // each iteration takes 2 * 6 samples
                // we update _BlurRadius each time to cheaply get a very smooth look

                // VR USAGE
                lrColorB = RenderTexture.GetTemporary(textureDescriptor);


                Graphics.Blit(lrDepthBuffer, lrColorB, mat, 1);
                RenderTexture.ReleaseTemporary(lrDepthBuffer);
                ofs = EnviroSkyMgr.instance.LightShaftsSettings.blurRadius * (((it2 * 2.0f + 1.0f) * 6.0f)) / 768.0f;
                mat.SetVector("_BlurRadius4", new Vector4(ofs, ofs, 0.0f, 0.0f));
                // VR USAGE
                lrDepthBuffer = RenderTexture.GetTemporary(textureDescriptor);


                Graphics.Blit(lrColorB, lrDepthBuffer, mat, 1);
                RenderTexture.ReleaseTemporary(lrColorB);
                ofs = EnviroSkyMgr.instance.LightShaftsSettings.blurRadius * (((it2 * 2.0f + 2.0f) * 6.0f)) / 768.0f;
                mat.SetVector("_BlurRadius4", new Vector4(ofs, ofs, 0.0f, 0.0f));
            }

            // put together:

            if (v.z >= 0.0f)
                mat.SetVector("_SunColor", new Vector4(clr.r, clr.g, clr.b, clr.a) * EnviroSkyMgr.instance.LightShaftsSettings.intensity);
            else
                mat.SetVector("_SunColor", Vector4.zero); // no backprojection !

            mat.SetTexture("_ColorBuffer", lrDepthBuffer);

            //FINAL
           // Graphics.Blit(source, destination, mat, (EnviroSkyMgr.instance.LightShaftsSettings.screenBlendMode == EnviroPostProcessing.ShaftsScreenBlendMode.Screen) ? 0 : 4);

            blitPassFinal.Setup(src, destination);
            renderer.EnqueuePass(blitPassFinal);

            RenderTexture.ReleaseTemporary(lrDepthBuffer);
        }


        void DrawBorder(RenderTexture dest, Material material)
        {
            float x1;
            float x2;
            float y1;
            float y2;

            RenderTexture.active = dest;
            bool invertY = true; // source.texelSize.y < 0.0ff;
                                 // Set up the simple Matrix
            GL.PushMatrix();
            GL.LoadOrtho();

            for (int i = 0; i < material.passCount; i++)
            {
                material.SetPass(i);

                float y1_; float y2_;
                if (invertY)
                {
                    y1_ = 1.0f; y2_ = 0.0f;
                }
                else
                {
                    y1_ = 0.0f; y2_ = 1.0f;
                }

                // left
                x1 = 0.0f;
                x2 = 0.0f + 1.0f / (dest.width * 1.0f);
                y1 = 0.0f;
                y2 = 1.0f;
                GL.Begin(GL.QUADS);

                GL.TexCoord2(0.0f, y1_); GL.Vertex3(x1, y1, 0.1f);
                GL.TexCoord2(1.0f, y1_); GL.Vertex3(x2, y1, 0.1f);
                GL.TexCoord2(1.0f, y2_); GL.Vertex3(x2, y2, 0.1f);
                GL.TexCoord2(0.0f, y2_); GL.Vertex3(x1, y2, 0.1f);

                // right
                x1 = 1.0f - 1.0f / (dest.width * 1.0f);
                x2 = 1.0f;
                y1 = 0.0f;
                y2 = 1.0f;

                GL.TexCoord2(0.0f, y1_); GL.Vertex3(x1, y1, 0.1f);
                GL.TexCoord2(1.0f, y1_); GL.Vertex3(x2, y1, 0.1f);
                GL.TexCoord2(1.0f, y2_); GL.Vertex3(x2, y2, 0.1f);
                GL.TexCoord2(0.0f, y2_); GL.Vertex3(x1, y2, 0.1f);

                // top
                x1 = 0.0f;
                x2 = 1.0f;
                y1 = 0.0f;
                y2 = 0.0f + 1.0f / (dest.height * 1.0f);

                GL.TexCoord2(0.0f, y1_); GL.Vertex3(x1, y1, 0.1f);
                GL.TexCoord2(1.0f, y1_); GL.Vertex3(x2, y1, 0.1f);
                GL.TexCoord2(1.0f, y2_); GL.Vertex3(x2, y2, 0.1f);
                GL.TexCoord2(0.0f, y2_); GL.Vertex3(x1, y2, 0.1f);

                // bottom
                x1 = 0.0f;
                x2 = 1.0f;
                y1 = 1.0f - 1.0f / (dest.height * 1.0f);
                y2 = 1.0f;

                GL.TexCoord2(0.0f, y1_); GL.Vertex3(x1, y1, 0.1f);
                GL.TexCoord2(1.0f, y1_); GL.Vertex3(x2, y1, 0.1f);
                GL.TexCoord2(1.0f, y2_); GL.Vertex3(x2, y2, 0.1f);
                GL.TexCoord2(0.0f, y2_); GL.Vertex3(x1, y2, 0.1f);

                GL.End();
            }

            GL.PopMatrix();
        }

    
        public override void Create()
        {
           if (EnviroSkyMgr.instance == null)
                return;

            if (EnviroSkyMgr.instance.Camera != null)
                myCam = EnviroSkyMgr.instance.Camera;
            else
                myCam = Camera.main;

            CreateMaterialsAndTextures();

            getSouceTexture = new EnviroBlitPassIdentifiers(UnityEngine.Rendering.Universal.RenderPassEvent.AfterRenderingTransparents, null, 0, "Get Source");
            blitPassFinal = new EnviroBlitPass(UnityEngine.Rendering.Universal.RenderPassEvent.AfterRenderingTransparents, sunShaftsMaterial, 4, "SunShafts Final");
        }

        public override void AddRenderPasses(UnityEngine.Rendering.Universal.ScriptableRenderer renderer, ref UnityEngine.Rendering.Universal.RenderingData renderingData)
        {
            if (renderingData.cameraData.camera.cameraType == CameraType.Preview || renderingData.cameraData.camera.cameraType == CameraType.SceneView || renderingData.cameraData.camera.cameraType == CameraType.Reflection)
                return;

            if (EnviroSkyMgr.instance != null && EnviroSkyMgr.instance.useSunShafts && EnviroSkyMgr.instance.Camera != null)
            {
                var src = renderer.cameraColorTarget;
                var dest = UnityEngine.Rendering.Universal.RenderTargetHandle.CameraTarget;

                if (myCam == null)
                    myCam = EnviroSkyMgr.instance.Camera;

                RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
                descriptor.graphicsFormat = Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat;
                RenderTexture newSource = RenderTexture.GetTemporary(descriptor);

                RenderTargetIdentifier newSourceID = new RenderTargetIdentifier(newSource);
             
                if (getSouceTexture == null || blitPassFinal == null)
                    Create();

                getSouceTexture.Setup(src, newSourceID);
                renderer.EnqueuePass(getSouceTexture);

                RenderLightShaft(renderer, newSource, src, dest, sunShaftsMaterial, simpleSunClearMaterial, EnviroSkyMgr.instance.Components.Sun.transform, EnviroSkyMgr.instance.LightShaftsSettings.thresholdColorSun.Evaluate(EnviroSkyMgr.instance.Time.solarTime), EnviroSkyMgr.instance.LightShaftsSettings.lightShaftsColorSun.Evaluate(EnviroSkyMgr.instance.Time.solarTime));

                RenderTexture.ReleaseTemporary(newSource);
            }
        }
    }
}
#endif