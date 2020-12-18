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
        Vector2Int m_Resolution;
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
        }

        [SetUp]
        public void SetUpTest()
        {
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
            
            //Screen.SetResolution(this.m_Resolution.x, this.m_Resolution.y, false);
            
            var physicsSceneManager = GameObject.Find("PhysicsSceneManager");
            var mcs = physicsSceneManager.GetComponent<MCSPerformerManager>();
            mcs.renderImageOverride = m_CaptureRgb;
            mcs.renderDepthImageOverride = m_CaptureDepthMaps;
            mcs.renderObjectImageOverride = m_CaptureObjectMasks;
            
            var holder = new GameObject("MockHolder");
            var moq = holder.AddComponent<EventMocker>();
            
            yield return Measure.Frames().WarmupCount(10).MeasurementCount(50).Run();

            moq.Stop();
            
            yield return new WaitForSeconds(5);
        }
    }

    [TestFixture(320, 240)]
    [TestFixture(640, 480)]
    [TestFixture(1024, 768)]
    [TestFixture(1920, 1080)]
    [Category("Performance")]
    public class PerformanceTestAllOutputs : PerformanceTester
    {
        public PerformanceTestAllOutputs(int resx, int resy) : base(resx, resy, true, true, true) { }
    }
    
    [TestFixture(320, 240)]
    [TestFixture(640, 480)]
    [TestFixture(1024, 768)]
    [TestFixture(1920, 1080)]
    [Category("Performance")]
    public class PerformanceTestRgb : PerformanceTester
    {
        public PerformanceTestRgb(int resx, int resy) : base(resx, resy, true, false, false) { }
    }
    
    [TestFixture(320, 240)]
    [TestFixture(640, 480)]
    [TestFixture(1024, 768)]
    [TestFixture(1920, 1080)]
    [Category("Performance")]
    public class PerformanceTestDepth : PerformanceTester
    {
        public PerformanceTestDepth(int resx, int resy) : base(resx, resy, false, true, false) { }
    }
    
    [TestFixture(320, 240)]
    [TestFixture(640, 480)]
    [TestFixture(1024, 768)]
    [TestFixture(1920, 1080)]
    [Category("Performance")]
    public class PerformanceTestObjectMask : PerformanceTester
    {
        public PerformanceTestObjectMask(int resx, int resy) : base(resx, resy, false, false, true) { }
    }
}
