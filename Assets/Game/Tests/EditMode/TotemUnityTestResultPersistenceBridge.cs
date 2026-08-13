#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

[InitializeOnLoad]
internal static class TotemUnityTestResultPersistenceBridge
{
    private static readonly ResultCallbacks Callbacks = new ResultCallbacks();

    static TotemUnityTestResultPersistenceBridge()
    {
        TestRunnerApi.RegisterTestCallback(Callbacks);
    }

    private sealed class ResultCallbacks : ICallbacks
    {
        public void RunStarted(ITestAdaptor testsToRun)
        {
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            string path = Path.Combine(Application.persistentDataPath, "TestResults.xml");
            TestRunnerApi.SaveResultToFile(result, path);
        }

        public void TestStarted(ITestAdaptor test)
        {
        }

        public void TestFinished(ITestResultAdaptor result)
        {
        }
    }
}
#endif
