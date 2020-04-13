using System.Collections.Generic;
using UnityEngine.Serialization;
#if ENVIRO_LWRP && ENVIRO_HD
namespace UnityEngine.Rendering.LWRP
{
    public class EnviroVolumeCloudsLWRP : UnityEngine.Rendering.Universal.ScriptableRendererFeature
    {

        EnviroBlitPass blitPass;
        private Camera myCam;

        #region Volume Clouds Var
        ////////////////////// Clouds //////////////////////
        private EnviroHaltonSequence sequence = new EnviroHaltonSequence() { radix = 3 };
        private Material cloudsMat;
        private Material blitMat;
        private Material compose;
        private Material downsample;
        private RenderTexture subFrameTex;
        private RenderTexture prevFrameTex;
        private Matrix4x4 projection;
        private Matrix4x4 projectionSPVR;
        private Matrix4x4 inverseRotation;
        private Matrix4x4 inverseRotationSPVR;
        private Matrix4x4 rotation;
        private Matrix4x4 rotationSPVR;
        private Matrix4x4 previousRotation;
        private Matrix4x4 previousRotationSPVR;
        [HideInInspector]
        public EnviroVolumeCloudsQualitySettings.ReprojectionPixelSize currentReprojectionPixelSize;
        private int reprojectionPixelSize;
        private bool isFirstFrame;
        private int subFrameNumber;
        private int[] frameList;
        private int renderingCounter;
        private int subFrameWidth;
        private int subFrameHeight;
        private int frameWidth;
        private int frameHeight;
        private bool textureDimensionChanged;
        #endregion

        #region Volume Clouds Functions
        ////////// Clouds Functions ///////////////
        private void SetCloudProperties()
        {
            if (EnviroSky.instance.cloudsSettings.cloudsQualitySettings.baseQuality == EnviroVolumeCloudsQualitySettings.CloudDetailQuality.Low)
                cloudsMat.SetTexture("_Noise", EnviroSky.instance.ressources.noiseTexture);
            else
                cloudsMat.SetTexture("_Noise", EnviroSky.instance.ressources.noiseTextureHigh);

            if (EnviroSky.instance.cloudsSettings.cloudsQualitySettings.detailQuality == EnviroVolumeCloudsQualitySettings.CloudDetailQuality.Low)
                cloudsMat.SetTexture("_DetailNoise", EnviroSky.instance.ressources.detailNoiseTexture);
            else
                cloudsMat.SetTexture("_DetailNoise", EnviroSky.instance.ressources.detailNoiseTextureHigh);

            switch (myCam.stereoActiveEye)
            {
                case Camera.MonoOrStereoscopicEye.Mono:
                    projection = myCam.projectionMatrix;
                    Matrix4x4 inverseProjection = projection.inverse;
                    cloudsMat.SetMatrix("_InverseProjection", inverseProjection);
                    inverseRotation = myCam.cameraToWorldMatrix;
                    cloudsMat.SetMatrix("_InverseRotation", inverseRotation);
                    break;

                case Camera.MonoOrStereoscopicEye.Left:
                    projection = myCam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);
                    Matrix4x4 inverseProjectionLeft = projection.inverse;
                    cloudsMat.SetMatrix("_InverseProjection", inverseProjectionLeft);
                    inverseRotation = myCam.GetStereoViewMatrix(Camera.StereoscopicEye.Left).inverse;
                    cloudsMat.SetMatrix("_InverseRotation", inverseRotation);

                    if (myCam.stereoEnabled && EnviroSky.instance.singlePassVR)
                    {
                        Matrix4x4 inverseProjectionRightSP = myCam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right).inverse;
                        cloudsMat.SetMatrix("_InverseProjection_SP", inverseProjectionRightSP);

                        inverseRotationSPVR = myCam.GetStereoViewMatrix(Camera.StereoscopicEye.Right).inverse;
                        cloudsMat.SetMatrix("_InverseRotation_SP", inverseRotationSPVR);
                    }
                    break;

                case Camera.MonoOrStereoscopicEye.Right:
                    projection = myCam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);
                    Matrix4x4 inverseProjectionRight = projection.inverse;
                    cloudsMat.SetMatrix("_InverseProjection", inverseProjectionRight);
                    inverseRotation = myCam.GetStereoViewMatrix(Camera.StereoscopicEye.Right).inverse;
                    cloudsMat.SetMatrix("_InverseRotation", inverseRotation);
                    break;
            }

            //Weather Map
            if (EnviroSky.instance.cloudsSettings.customWeatherMap == null)
                cloudsMat.SetTexture("_WeatherMap", EnviroSky.instance.weatherMap);
            else
                cloudsMat.SetTexture("_WeatherMap", EnviroSky.instance.cloudsSettings.customWeatherMap);

            //Curl Noise
            if (EnviroSky.instance.cloudsSettings.cloudsQualitySettings.useCurlNoise)
            {
                cloudsMat.EnableKeyword("ENVIRO_CURLNOISE");
                cloudsMat.SetTexture("_CurlNoise", EnviroSky.instance.ressources.curlMap);
            }
            else
            {
                cloudsMat.DisableKeyword("ENVIRO_CURLNOISE");
            }

            //Optimizations
            if (EnviroSky.instance.cloudsSettings.useHaltonRaymarchOffset)
            {
                cloudsMat.EnableKeyword("ENVIRO_HALTONOFFSET");
                cloudsMat.SetFloat("_RaymarchOffset", sequence.Get());
                cloudsMat.SetVector("_TexelSize", subFrameTex.texelSize);
            }
            else
            {
                cloudsMat.DisableKeyword("ENVIRO_HALTONOFFSET");
            }

            //RaymarchOffset

            if (!EnviroSky.instance.cloudsSettings.useLessSteps)
                cloudsMat.SetVector("_Steps", new Vector4(EnviroSky.instance.cloudsSettings.cloudsQualitySettings.raymarchSteps * EnviroSky.instance.cloudsConfig.raymarchingScale, EnviroSky.instance.cloudsSettings.cloudsQualitySettings.raymarchSteps * EnviroSky.instance.cloudsConfig.raymarchingScale, 0.0f, 0.0f));
            else
                cloudsMat.SetVector("_Steps", new Vector4((EnviroSky.instance.cloudsSettings.cloudsQualitySettings.raymarchSteps * EnviroSky.instance.cloudsConfig.raymarchingScale) * 0.75f, (EnviroSky.instance.cloudsSettings.cloudsQualitySettings.raymarchSteps * EnviroSky.instance.cloudsConfig.raymarchingScale) * 0.75f, 0.0f, 0.0f));
            cloudsMat.SetFloat("_BaseNoiseUV", EnviroSky.instance.cloudsSettings.cloudsQualitySettings.baseNoiseUV);
            cloudsMat.SetFloat("_DetailNoiseUV", EnviroSky.instance.cloudsSettings.cloudsQualitySettings.detailNoiseUV);
            cloudsMat.SetFloat("_AmbientSkyColorIntensity", EnviroSky.instance.cloudsSettings.ambientLightIntensity.Evaluate(EnviroSky.instance.GameTime.solarTime));
            cloudsMat.SetVector("_CloudsLighting", new Vector4(EnviroSky.instance.cloudsConfig.scatteringCoef, EnviroSky.instance.cloudsSettings.hgPhase, EnviroSky.instance.cloudsSettings.silverLiningIntensity, EnviroSky.instance.cloudsSettings.silverLiningSpread.Evaluate(EnviroSky.instance.GameTime.solarTime)));

            float toneMap = EnviroSky.instance.cloudsSettings.tonemapping ? 0f : 1f;

            //if (!Application.isPlaying && EnviroSky.instance.showVolumeCloudsInEditor)
            //   toneMap =  0f;

            cloudsMat.SetVector("_CloudsLightingExtended", new Vector4(EnviroSky.instance.cloudsConfig.edgeDarkness, EnviroSky.instance.cloudsConfig.ambientSkyColorIntensity, toneMap, EnviroSky.instance.cloudsSettings.cloudsExposure));
            cloudsMat.SetColor("_AmbientLightColor", EnviroSky.instance.cloudsSettings.volumeCloudsAmbientColor.Evaluate(EnviroSky.instance.GameTime.solarTime));


            float bottomH = EnviroSky.instance.cloudsSettings.cloudsQualitySettings.bottomCloudHeight + EnviroSky.instance.cloudsSettings.cloudsHeightMod;
            float topH = EnviroSky.instance.cloudsSettings.cloudsQualitySettings.topCloudHeight + EnviroSky.instance.cloudsSettings.cloudsHeightMod;

            cloudsMat.SetVector("_CloudsParameter", new Vector4(bottomH, topH, topH - bottomH, EnviroSky.instance.cloudsSettings.cloudsWorldScale * 10));

            if (EnviroSky.instance.cloudsSettings.useLessSteps)
                cloudsMat.SetVector("_CloudDensityScale", new Vector4(EnviroSky.instance.cloudsConfig.density * 1.5f, EnviroSky.instance.cloudsConfig.lightStepModifier, EnviroSky.instance.GameTime.dayNightSwitch, EnviroSky.instance.GameTime.solarTime));
            else
                cloudsMat.SetVector("_CloudDensityScale", new Vector4(EnviroSky.instance.cloudsConfig.density, EnviroSky.instance.cloudsConfig.lightStepModifier, EnviroSky.instance.GameTime.dayNightSwitch, EnviroSky.instance.GameTime.solarTime));

            cloudsMat.SetFloat("_CloudsType", EnviroSky.instance.cloudsConfig.cloudType);
            cloudsMat.SetVector("_CloudsCoverageSettings", new Vector4(EnviroSky.instance.cloudsConfig.coverage * EnviroSky.instance.cloudsSettings.globalCloudCoverage, EnviroSky.instance.cloudsConfig.lightAbsorbtion, EnviroSky.instance.cloudsSettings.cloudsQualitySettings.transmissionToExit, 0f));
            cloudsMat.SetVector("_CloudsAnimation", new Vector4(EnviroSky.instance.cloudAnim.x, EnviroSky.instance.cloudAnim.y, EnviroSky.instance.cloudsSettings.cloudsWindDirectionX, EnviroSky.instance.cloudsSettings.cloudsWindDirectionY));
            cloudsMat.SetColor("_LightColor", EnviroSky.instance.cloudsSettings.volumeCloudsColor.Evaluate(EnviroSky.instance.GameTime.solarTime));
            cloudsMat.SetColor("_MoonLightColor", EnviroSky.instance.cloudsSettings.volumeCloudsMoonColor.Evaluate(EnviroSky.instance.GameTime.lunarTime));
            cloudsMat.SetFloat("_stepsInDepth", EnviroSky.instance.cloudsSettings.cloudsQualitySettings.stepsInDepthModificator);
            cloudsMat.SetFloat("_LODDistance", EnviroSky.instance.cloudsSettings.cloudsQualitySettings.lodDistance);


            if (EnviroSky.instance.lightSettings.directionalLightMode == EnviroLightSettings.LightingMode.Dual)
            {
                if (EnviroSky.instance.GameTime.dayNightSwitch < EnviroSky.instance.GameTime.solarTime)
                    cloudsMat.SetVector("_LightDir", -EnviroSky.instance.Components.DirectLight.transform.forward);
                else if (EnviroSky.instance.Components.AdditionalDirectLight != null)
                    cloudsMat.SetVector("_LightDir", -EnviroSky.instance.Components.AdditionalDirectLight.transform.forward);
            }
            else
                cloudsMat.SetVector("_LightDir", -EnviroSky.instance.Components.DirectLight.transform.forward);

            cloudsMat.SetFloat("_LightIntensity", EnviroSky.instance.cloudsSettings.lightIntensity.Evaluate(EnviroSky.instance.GameTime.solarTime));
            cloudsMat.SetVector("_CloudsErosionIntensity", new Vector4(1f - EnviroSky.instance.cloudsConfig.baseErosionIntensity, EnviroSky.instance.cloudsConfig.detailErosionIntensity, EnviroSky.instance.cloudsSettings.attenuationClamp.Evaluate(EnviroSky.instance.GameTime.solarTime), EnviroSky.instance.cloudAnim.z));
            // cloudsMat.SetTexture("_BlueNoise", blueNoise);        
            // cloudsMat.SetVector("_Randomness", new Vector4(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value));
        }

        private void SetBlitmaterialProperties()
        {
            Matrix4x4 inverseProjection = projection.inverse;

            blitMat.SetMatrix("_PreviousRotation", previousRotation);
            blitMat.SetMatrix("_Projection", projection);
            blitMat.SetMatrix("_InverseRotation", inverseRotation);
            blitMat.SetMatrix("_InverseProjection", inverseProjection);

            if (myCam.stereoEnabled && EnviroSky.instance.singlePassVR)
            {
                Matrix4x4 inverseProjectionSPVR = projectionSPVR.inverse;
                blitMat.SetMatrix("_PreviousRotationSPVR", previousRotationSPVR);
                blitMat.SetMatrix("_ProjectionSPVR", projectionSPVR);
                blitMat.SetMatrix("_InverseRotationSPVR", inverseRotationSPVR);
                blitMat.SetMatrix("_InverseProjectionSPVR", inverseProjectionSPVR);
            }

            if (myCam.stereoEnabled && EnviroSky.instance.singlePassInstancedVR)
                blitMat.EnableKeyword("ENVIRO_SINGLEPASSINSTANCED");

            blitMat.SetFloat("_FrameNumber", subFrameNumber);
            blitMat.SetFloat("_ReprojectionPixelSize", reprojectionPixelSize);
            blitMat.SetVector("_SubFrameDimension", new Vector2(subFrameWidth, subFrameHeight));
            blitMat.SetVector("_FrameDimension", new Vector2(frameWidth, frameHeight));
        }
        RenderTexture DownsampleDepth(int X, int Y, Texture src, Material mat, int downsampleFactor)
        {
            Vector2 offset = new Vector2(1.0f / X, 1.0f / X);
            X /= downsampleFactor;
            Y /= downsampleFactor;
            RenderTexture lowDepth = RenderTexture.GetTemporary(X, Y, 0);
            mat.SetVector("_PixelSize", offset);
            Graphics.Blit(src, lowDepth, mat);

            return lowDepth;
        }
        private void RenderClouds(RenderTexture tex)
        {
            if (cloudsMat == null)
                cloudsMat = new Material(Shader.Find("Enviro/Standard/RaymarchClouds"));

           // cloudsMat.SetTexture("_MainTex", source);
            SetCloudProperties();
            //Render Clouds with downsampling tex
            Graphics.Blit(null, tex, cloudsMat);
        }

        private void CreateCloudsRenderTextures(RenderTextureDescriptor d)
        {
            if (subFrameTex != null)
            {
                DestroyImmediate(subFrameTex);
                subFrameTex = null;
            }

            if (prevFrameTex != null)
            {
                DestroyImmediate(prevFrameTex);
                prevFrameTex = null;
            }

            RenderTextureFormat format = myCam.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;

            if (subFrameTex == null)
            {
                RenderTextureDescriptor desc = new RenderTextureDescriptor(subFrameWidth, subFrameHeight, format, 0);
                desc.vrUsage = d.vrUsage;
                subFrameTex = new RenderTexture(desc);
                subFrameTex.filterMode = FilterMode.Bilinear;
                subFrameTex.hideFlags = HideFlags.HideAndDontSave;

                isFirstFrame = true;
            }

            if (prevFrameTex == null)
            {
                RenderTextureDescriptor desc = new RenderTextureDescriptor(frameWidth, frameHeight, format, 0);
                desc.vrUsage = d.vrUsage;
                prevFrameTex = new RenderTexture(desc);
                prevFrameTex.filterMode = FilterMode.Bilinear;
                prevFrameTex.hideFlags = HideFlags.HideAndDontSave;

                isFirstFrame = true;
            }
        }
        private void SetReprojectionPixelSize(EnviroVolumeCloudsQualitySettings.ReprojectionPixelSize pSize)
        {
            switch (pSize)
            {
                case EnviroVolumeCloudsQualitySettings.ReprojectionPixelSize.Off:
                    reprojectionPixelSize = 1;
                    break;

                case EnviroVolumeCloudsQualitySettings.ReprojectionPixelSize.Low:
                    reprojectionPixelSize = 2;
                    break;

                case EnviroVolumeCloudsQualitySettings.ReprojectionPixelSize.Medium:
                    reprojectionPixelSize = 4;
                    break;

                case EnviroVolumeCloudsQualitySettings.ReprojectionPixelSize.High:
                    reprojectionPixelSize = 8;
                    break;
            }

            frameList = CalculateFrames(reprojectionPixelSize);
        }
        private void StartFrame()
        {
            textureDimensionChanged = UpdateFrameDimensions();

            switch (myCam.stereoActiveEye)
            {
                case Camera.MonoOrStereoscopicEye.Mono:
                    projection = myCam.projectionMatrix;
                    rotation = myCam.worldToCameraMatrix;
                    inverseRotation = myCam.cameraToWorldMatrix;
                    break;

                case Camera.MonoOrStereoscopicEye.Left:
                    projection = myCam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);
                    rotation = myCam.GetStereoViewMatrix(Camera.StereoscopicEye.Left);
                    inverseRotation = rotation.inverse;

                    if (EnviroSky.instance.singlePassVR)
                    {
                        projectionSPVR = myCam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);
                        rotationSPVR = myCam.GetStereoViewMatrix(Camera.StereoscopicEye.Right);
                        inverseRotationSPVR = rotationSPVR.inverse;
                    }
                    break;

                case Camera.MonoOrStereoscopicEye.Right:
                    projection = myCam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);
                    rotation = myCam.GetStereoViewMatrix(Camera.StereoscopicEye.Right);
                    inverseRotation = rotation.inverse;
                    break;
            }
        }
        private void FinalizeFrame()
        {
            renderingCounter++;

            previousRotation = rotation;
            if (EnviroSky.instance.singlePassVR)
                previousRotationSPVR = rotationSPVR;

            int reproSize = reprojectionPixelSize * reprojectionPixelSize;
            subFrameNumber = frameList[renderingCounter % reproSize];
        }
        private bool UpdateFrameDimensions()
        {
            //Add downsampling
            int newFrameWidth = myCam.pixelWidth / EnviroSky.instance.cloudsSettings.cloudsQualitySettings.cloudsRenderResolution;
            int newFrameHeight = myCam.pixelHeight / EnviroSky.instance.cloudsSettings.cloudsQualitySettings.cloudsRenderResolution;

            //Reset temporal reprojection size when zero. Needed if SkyManager starts deactivated
            if (EnviroSky.instance != null && reprojectionPixelSize == 0)
                SetReprojectionPixelSize(EnviroSky.instance.cloudsSettings.cloudsQualitySettings.reprojectionPixelSize);

            //Calculate new frame width and height
            while (newFrameWidth % reprojectionPixelSize != 0)
            {
                newFrameWidth++;
            }

            while (newFrameHeight % reprojectionPixelSize != 0)
            {
                newFrameHeight++;
            }

            int newSubFrameWidth = newFrameWidth / reprojectionPixelSize;
            int newSubFrameHeight = newFrameHeight / reprojectionPixelSize;

            //Check if diemensions changed
            if (newFrameWidth != frameWidth || newSubFrameWidth != subFrameWidth || newFrameHeight != frameHeight || newSubFrameHeight != subFrameHeight)
            {
                //Cache new dimensions
                frameWidth = newFrameWidth;
                frameHeight = newFrameHeight;
                subFrameWidth = newSubFrameWidth;
                subFrameHeight = newSubFrameHeight;
                return true;
            }
            else
            {
                //Cache new dimensions
                frameWidth = newFrameWidth;
                frameHeight = newFrameHeight;
                subFrameWidth = newSubFrameWidth;
                subFrameHeight = newSubFrameHeight;
                return false;
            }
        }
        private int[] CalculateFrames(int reproSize)
        {
            subFrameNumber = 0;

            int i = 0;
            int reproCount = reproSize * reproSize;
            int[] frameNumbers = new int[reproCount];

            for (i = 0; i < reproCount; i++)
            {
                frameNumbers[i] = i;
            }

            while (i-- > 0)
            {
                int frame = frameNumbers[i];
                int count = (int)(UnityEngine.Random.Range(0, 1) * 1000.0f) % reproCount;
                frameNumbers[i] = frameNumbers[count];
                frameNumbers[count] = frame;
            }

            return frameNumbers;
        }
#endregion

        private void CreateMaterialsAndTextures()
        {

            if (cloudsMat == null)
                cloudsMat = new Material(Shader.Find("Enviro/Standard/RaymarchClouds"));

            if (blitMat == null)
                blitMat = new Material(Shader.Find("Enviro/Standard/Blit"));

            if (compose == null)
                compose = new Material(Shader.Find("Hidden/Enviro/Upsample"));

            if (downsample == null)
                downsample = new Material(Shader.Find("Hidden/Enviro/DepthDownsample"));
        }

        private void RenderVolumeClouds(RenderTextureDescriptor d)
        {
        #region Clouds

            if (blitMat == null)
                blitMat = new Material(Shader.Find("Enviro/Standard/Blit"));

            StartFrame();

            if (subFrameTex == null || prevFrameTex == null || textureDimensionChanged)
                CreateCloudsRenderTextures(d);
         
            EnviroSky.instance.cloudsRenderTarget = subFrameTex;

            //Rendering Clouds
            RenderClouds(subFrameTex);

            if (isFirstFrame)
            {
                Graphics.Blit(subFrameTex, prevFrameTex);
                isFirstFrame = false;
            }

            int downsampling = EnviroSky.instance.cloudsSettings.bilateralUpsampling ? reprojectionPixelSize * EnviroSky.instance.cloudsSettings.cloudsQualitySettings.cloudsRenderResolution : 1;

            if (downsampling > 1)
            {

                if (compose == null)
                    compose = new Material(Shader.Find("Hidden/Enviro/Upsample"));

                if (downsample == null)
                    downsample = new Material(Shader.Find("Hidden/Enviro/DepthDownsample"));


                RenderTexture lowDepth = DownsampleDepth(Screen.width, Screen.height, null, downsample, downsampling);

                compose.SetTexture("_CameraDepthLowRes", lowDepth);

                RenderTexture upsampledTex = RenderTexture.GetTemporary(myCam.pixelWidth / downsampling * 2, myCam.pixelHeight / downsampling * 2, 0, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Default);
                upsampledTex.filterMode = FilterMode.Bilinear;

                // composite to screen
                Vector2 pixelSize = new Vector2(1.0f / lowDepth.width, 1.0f / lowDepth.height);
                compose.SetVector("_LowResPixelSize", pixelSize);
                compose.SetVector("_LowResTextureSize", new Vector2(lowDepth.width, lowDepth.height));
                compose.SetFloat("_DepthMult", 32.0f);
                compose.SetFloat("_Threshold", 0.0005f);

                compose.SetTexture("_LowResTexture", subFrameTex);
                Graphics.Blit(subFrameTex, upsampledTex, compose);
                RenderTexture.ReleaseTemporary(lowDepth);

                //Blit clouds to final image
               // blitMat.SetTexture("_MainTex", src);
                blitMat.SetTexture("_SubFrame", upsampledTex);
                blitMat.SetTexture("_PrevFrame", prevFrameTex);
                SetBlitmaterialProperties();

                //Graphics.Blit(src, dst, blitMat);

                Graphics.Blit(upsampledTex, prevFrameTex);

                RenderTexture.ReleaseTemporary(upsampledTex);
            }
            else
            {
                //Blit clouds to final image
              //  blitMat.SetTexture("_MainTex", src);
                blitMat.SetTexture("_SubFrame", subFrameTex);
                blitMat.SetTexture("_PrevFrame", prevFrameTex);
                SetBlitmaterialProperties();

               // Graphics.Blit(src, dst, blitMat);
                Graphics.Blit(subFrameTex, prevFrameTex);
            }
            FinalizeFrame();
#endregion
        }

        public override void Create()
        {
          if (EnviroSkyMgr.instance == null || EnviroSky.instance == null)
              return;

            CreateMaterialsAndTextures();

            if (EnviroSky.instance != null)
                SetReprojectionPixelSize(EnviroSky.instance.cloudsSettings.cloudsQualitySettings.reprojectionPixelSize);
            
            blitPass = new EnviroBlitPass(UnityEngine.Rendering.Universal.RenderPassEvent.BeforeRenderingTransparents, blitMat, 0, name);
        }

        public override void AddRenderPasses(UnityEngine.Rendering.Universal.ScriptableRenderer renderer, ref UnityEngine.Rendering.Universal.RenderingData renderingData)
        {
            if (EnviroSkyMgr.instance != null && EnviroSky.instance != null && EnviroSkyMgr.instance.useVolumeClouds && EnviroSky.instance.PlayerCamera != null)
            {
                if (renderingData.cameraData.camera.cameraType == CameraType.Preview)
                    return;

                myCam = renderingData.cameraData.camera;

                var src = renderer.cameraColorTarget;
                var dest = UnityEngine.Rendering.Universal.RenderTargetHandle.CameraTarget;

                if (currentReprojectionPixelSize != EnviroSky.instance.cloudsSettings.cloudsQualitySettings.reprojectionPixelSize)
                {
                    currentReprojectionPixelSize = EnviroSky.instance.cloudsSettings.cloudsQualitySettings.reprojectionPixelSize;
                    SetReprojectionPixelSize(EnviroSky.instance.cloudsSettings.cloudsQualitySettings.reprojectionPixelSize);
                }

                if (renderingData.cameraData.isSceneViewCamera && !EnviroSky.instance.showVolumeCloudsInEditor)
                    return;

                RenderVolumeClouds(renderingData.cameraData.cameraTargetDescriptor);

                if (blitPass == null)
                    Create();

                blitPass.Setup(src, dest);

                renderer.EnqueuePass(blitPass);
            }
        }
    }
}
#endif