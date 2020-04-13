using System.Collections.Generic;
using UnityEngine.Serialization;
#if ENVIRO_LWRP
namespace UnityEngine.Rendering.LWRP
{
    public class EnviroDistanceBlurLWRP : UnityEngine.Rendering.Universal.ScriptableRendererFeature
    {
        EnviroBlitPass blitPass;
        EnviroBlitPassIdentifiers getSouceTexture;

        private Camera myCam;

        #region Blur Var
        /////////////////// Blur //////////////////////
        private Material postProcessMat;
        private const int kMaxIterations = 16;
        private RenderTexture[] _blurBuffer1 = new RenderTexture[kMaxIterations];
        private RenderTexture[] _blurBuffer2 = new RenderTexture[kMaxIterations];
        private Texture2D distributionTexture;
        // private float _threshold = 0f;
        public float thresholdGamma
        {
            get { return Mathf.Max(0f, 0); }
            // set { _threshold = value; }
        }
        public float thresholdLinear
        {
            get { return Mathf.GammaToLinearSpace(thresholdGamma); }
            //   set { _threshold = Mathf.LinearToGammaSpace(value); }
        }
#endregion

        private void CreateMaterialsAndTextures()
        {
            if (postProcessMat != null)
                postProcessMat = new Material(Shader.Find("Hidden/EnviroDistanceBlur"));    

            if (distributionTexture == null)
                distributionTexture = Resources.Load("tex_enviro_linear", typeof(Texture2D)) as Texture2D;
        }

        private void UpdateMatrix()
        {
            ///////////////////Matrix Information
            if (myCam.stereoEnabled)
            {
                // Both stereo eye inverse view matrices
                Matrix4x4 left_world_from_view = myCam.GetStereoViewMatrix(Camera.StereoscopicEye.Left).inverse;
                Matrix4x4 right_world_from_view = myCam.GetStereoViewMatrix(Camera.StereoscopicEye.Right).inverse;

                // Both stereo eye inverse projection matrices, plumbed through GetGPUProjectionMatrix to compensate for render texture
                Matrix4x4 left_screen_from_view = myCam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);
                Matrix4x4 right_screen_from_view = myCam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);
                Matrix4x4 left_view_from_screen = GL.GetGPUProjectionMatrix(left_screen_from_view, true).inverse;
                Matrix4x4 right_view_from_screen = GL.GetGPUProjectionMatrix(right_screen_from_view, true).inverse;

                // Negate [1,1] to reflect Unity's CBuffer state
                if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.OpenGLCore && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3)
                {
                    left_view_from_screen[1, 1] *= -1;
                    right_view_from_screen[1, 1] *= -1;
                }

                Shader.SetGlobalMatrix("_LeftWorldFromView", left_world_from_view);
                Shader.SetGlobalMatrix("_RightWorldFromView", right_world_from_view);
                Shader.SetGlobalMatrix("_LeftViewFromScreen", left_view_from_screen);
                Shader.SetGlobalMatrix("_RightViewFromScreen", right_view_from_screen);
            }
            else
            {
                // Main eye inverse view matrix
                Matrix4x4 left_world_from_view = myCam.cameraToWorldMatrix;

                // Inverse projection matrices, plumbed through GetGPUProjectionMatrix to compensate for render texture
                Matrix4x4 screen_from_view = myCam.projectionMatrix;
                Matrix4x4 left_view_from_screen = GL.GetGPUProjectionMatrix(screen_from_view, true).inverse;

                // Negate [1,1] to reflect Unity's CBuffer state
                if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.OpenGLCore && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3)
                    left_view_from_screen[1, 1] *= -1;

                // Store matrices
                Shader.SetGlobalMatrix("_LeftWorldFromView", left_world_from_view);
                Shader.SetGlobalMatrix("_LeftViewFromScreen", left_view_from_screen);
            }
            //////////////////////////////


        }


        private void RenderDistanceBlur(RenderTexture source, UnityEngine.Rendering.Universal.RenderTargetHandle destination, UnityEngine.Rendering.Universal.ScriptableRenderer renderer, RenderTargetIdentifier src,RenderTextureDescriptor d)
        {
            var useRGBM = myCam.allowHDR;

            // source texture size
            var tw = source.width;
            var th = source.height;

            // halve the texture size for the low quality mode
            if (!EnviroSky.instance.distanceBlurSettings.highQuality)
            {
                tw /= 2;
                th /= 2;
            }

            if (postProcessMat == null)
                postProcessMat = new Material(Shader.Find("Hidden/EnviroDistanceBlur"));

            postProcessMat.SetTexture("_DistTex", distributionTexture);
            postProcessMat.SetFloat("_Distance", EnviroSky.instance.blurDistance);
            postProcessMat.SetFloat("_Radius", EnviroSky.instance.distanceBlurSettings.radius);

            // blur buffer format
            var rtFormat = useRGBM ?
                RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;

            // determine the iteration count
            var logh = Mathf.Log(th, 2) + EnviroSky.instance.distanceBlurSettings.radius - 8;
            var logh_i = (int)logh;
            var iterations = Mathf.Clamp(logh_i, 1, kMaxIterations);

            // update the shader properties
            var lthresh = thresholdLinear;
            postProcessMat.SetFloat("_Threshold", lthresh);

            var knee = lthresh * 0.5f + 1e-5f;
            var curve = new Vector3(lthresh - knee, knee * 2, 0.25f / knee);
            postProcessMat.SetVector("_Curve", curve);

            var pfo = !EnviroSky.instance.distanceBlurSettings.highQuality && EnviroSky.instance.distanceBlurSettings.antiFlicker;
            postProcessMat.SetFloat("_PrefilterOffs", pfo ? -0.5f : 0.0f);

            postProcessMat.SetFloat("_SampleScale", 0.5f + logh - logh_i);
            postProcessMat.SetFloat("_Intensity", EnviroSky.instance.blurIntensity);
            postProcessMat.SetFloat("_SkyBlurring", EnviroSky.instance.blurSkyIntensity);

            // prefilter pass
            RenderTextureDescriptor renderDescriptor = d;
            renderDescriptor.width = tw;
            renderDescriptor.height = th;

            if (!EnviroSky.instance.singlePassInstancedVR)
                renderDescriptor.vrUsage = VRTextureUsage.None;

            var prefiltered = RenderTexture.GetTemporary(renderDescriptor);

            var pass = EnviroSky.instance.distanceBlurSettings.antiFlicker ? 1 : 0;
            Graphics.Blit(source, prefiltered, postProcessMat, pass);

            // construct a mip pyramid
            var last = prefiltered;
            for (var level = 0; level < iterations; level++)
            {
                RenderTextureDescriptor lastDescriptor = last.descriptor;
                lastDescriptor.width = lastDescriptor.width / 2;
                lastDescriptor.height = lastDescriptor.height / 2;
                _blurBuffer1[level] = RenderTexture.GetTemporary(lastDescriptor);

                pass = (level == 0) ? (EnviroSky.instance.distanceBlurSettings.antiFlicker ? 3 : 2) : 4;
                Graphics.Blit(last, _blurBuffer1[level], postProcessMat, pass);

                last = _blurBuffer1[level];
            }

            // upsample and combine loop
            for (var level = iterations - 2; level >= 0; level--)
            {
                var basetex = _blurBuffer1[level];
                postProcessMat.SetTexture("_BaseTex", basetex);


                RenderTextureDescriptor baseDescriptor = basetex.descriptor;
                _blurBuffer2[level] = RenderTexture.GetTemporary(baseDescriptor);

                pass = EnviroSky.instance.distanceBlurSettings.highQuality ? 6 : 5;
                Graphics.Blit(last, _blurBuffer2[level], postProcessMat, pass);
                last = _blurBuffer2[level];
            }

            // finish process
            postProcessMat.SetTexture("_BaseTex", source);
            blitPass.Setup(src, destination);
            renderer.EnqueuePass(blitPass);

            // release the temporary buffers
            for (var i = 0; i < kMaxIterations; i++)
            {
                if (_blurBuffer1[i] != null)
                    RenderTexture.ReleaseTemporary(_blurBuffer1[i]);

                if (_blurBuffer2[i] != null)
                    RenderTexture.ReleaseTemporary(_blurBuffer2[i]);

                _blurBuffer1[i] = null;
                _blurBuffer2[i] = null;
            }

            RenderTexture.ReleaseTemporary(prefiltered);
        }


        public override void Create()
        {
           if (EnviroSkyMgr.instance == null || EnviroSky.instance == null)
               return;

            CreateMaterialsAndTextures();

            getSouceTexture = new EnviroBlitPassIdentifiers(UnityEngine.Rendering.Universal.RenderPassEvent.BeforeRenderingTransparents, null, 0, "Get Source");

            var pass = EnviroSky.instance.distanceBlurSettings.highQuality ? 8 : 7;
            blitPass = new EnviroBlitPass(UnityEngine.Rendering.Universal.RenderPassEvent.BeforeRenderingTransparents, postProcessMat, pass, "Final Distance Blur");
        }

        public override void AddRenderPasses(UnityEngine.Rendering.Universal.ScriptableRenderer renderer, ref UnityEngine.Rendering.Universal.RenderingData renderingData)
        {
            if (renderingData.cameraData.camera.cameraType == CameraType.Preview || renderingData.cameraData.camera.cameraType == CameraType.Reflection)
                return;

            myCam = renderingData.cameraData.camera;

            if (EnviroSkyMgr.instance != null && EnviroSky.instance != null && EnviroSkyMgr.instance.useDistanceBlur && EnviroSky.instance.PlayerCamera != null)
            {

                if (renderingData.cameraData.isSceneViewCamera && !EnviroSky.instance.showDistanceBlurInEditor)
                    return;

                var src = renderer.cameraColorTarget;
                var dest = UnityEngine.Rendering.Universal.RenderTargetHandle.CameraTarget;

                RenderTexture newSource = RenderTexture.GetTemporary(renderingData.cameraData.cameraTargetDescriptor);

                RenderTargetIdentifier newSourceID = new RenderTargetIdentifier(newSource);

                if (getSouceTexture == null)
                    Create();

                getSouceTexture.Setup(src, newSourceID);
                renderer.EnqueuePass(getSouceTexture);

                RenderDistanceBlur(newSource, dest, renderer, src, renderingData.cameraData.cameraTargetDescriptor);

                RenderTexture.ReleaseTemporary(newSource);
            }
        }
    }
}
#endif