using NUnit.Framework;
using System;
using System.Collections;
using Unity.PerformanceTesting;
using UnityEngine;

namespace PerformanceTests
{
    public abstract class PerformanceTester
    {
        const string k_SceneName = "MCS";
        protected Vector2Int m_Resolution;
        bool m_CaptureRgb;
        bool m_CaptureDepthMaps;
        bool m_CaptureObjectMasks;
        
        public PerformanceTester()
        {
        }
        
        public PerformanceTester(int resX, int resY, bool captureRgb, bool captureDepthMaps, bool captureObjectMasks)
        {
            this.m_Resolution = new Vector2Int(resX, resY);
            this.m_CaptureRgb = captureRgb;
            this.m_CaptureDepthMaps = captureDepthMaps;
            this.m_CaptureObjectMasks = captureObjectMasks;

            SetScreenResolution(this.m_Resolution.x, this.m_Resolution.y);
        }

        [SetUp]
        public void SetUp()
        {
            //Screen.SetResolution(this.m_Resolution.x, this.m_Resolution.y, true);  
            SetScreenResolution(this.m_Resolution.x, this.m_Resolution.y);
        }

        [TearDown]
        public void TearDown()
        {
        }

        [PerformanceUnityTest]
        public IEnumerator ExecuteTest()
        {
            var asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(k_SceneName);

            yield return new WaitUntil(() => asyncLoad.isDone);
            
            var physicsSceneManager = GameObject.Find("PhysicsSceneManager");

            // This only works on standalone, I'm still trying to find out if there is a way to do this
            // in editor
            //SetScreenResolution(1024, 768, false);
            yield return null;
            yield return null;
            yield return null;

            var mcs = physicsSceneManager.GetComponent<MCSPerformerManager>();
            
            mcs.SetRenderImageOverride(m_CaptureRgb);
            mcs.SetRenderDepthImageOverride(m_CaptureDepthMaps);
            mcs.SetRenderObjectImageOverride(m_CaptureObjectMasks);
            
            var holder = new GameObject("MockHolder");
            var moq = holder.AddComponent<EventMocker>();
            
            yield return Measure.Frames().WarmupCount(10).MeasurementCount(100).Run();
            moq.Stop();
            yield return new WaitForSeconds(5);
        }

        public void SetScreenResolution(int width, int height, bool fullscreen = true)
        {
#if UNITY_EDITOR
            GameWindow.SetResolution(width, height);
#else
            Screen.SetResolution(width, height, fullscreen);
#endif
        }
    }

    [TestFixture(320, 240)]
    [TestFixture(640, 480)]
    [TestFixture(1024, 768)]
    [TestFixture(1920, 1080)]
    [Category("Performance")]
    public class PerformanceTestAllOutputs : PerformanceTester
    {
        [SetUp]
        public void SetUp()
        {
            //Screen.SetResolution(this.m_Resolution.x, this.m_Resolution.y, true);           
        }

        public PerformanceTestAllOutputs(int resx, int resy) : base(resx, resy, true, true, true) { }
    }
    
    [TestFixture(320, 240)]
    [TestFixture(640, 480)]
    [TestFixture(1024, 768)]
    [TestFixture(1920, 1080)]
    [Category("Performance")]
    public class PerformanceTestRgb : PerformanceTester
    {
        public void SetUp()
        {
            //Screen.SetResolution(this.m_Resolution.x, this.m_Resolution.y, true);
        }

        public PerformanceTestRgb(int resx, int resy) : base(resx, resy, true, false, false) { }
    }
    
    [TestFixture(320, 240)]
    [TestFixture(640, 480)]
    [TestFixture(1024, 768)]
    [TestFixture(1920, 1080)]
    [Category("Performance")]
    public class PerformanceTestDepth : PerformanceTester
    {
        public void SetUp()
        {
            //Screen.SetResolution(this.m_Resolution.x, this.m_Resolution.y, true);
        }

        public PerformanceTestDepth(int resx, int resy) : base(resx, resy, false, true, false) { }
    }
    
    [TestFixture(320, 240)]
    [TestFixture(640, 480)]
    [TestFixture(1024, 768)]
    [TestFixture(1920, 1080)]
    [Category("Performance")]
    public class PerformanceTestObjectMask : PerformanceTester
    {
        public void SetUp()
        {
            //Screen.SetResolution(this.m_Resolution.x, this.m_Resolution.y, true);
        }

        public PerformanceTestObjectMask(int resx, int resy) : base(resx, resy, false, false, true) { }
    }
}
