using General;
using UnityEngine;

namespace General.Module.Samples
{
    public class SampleMain : MainBase
    {
        private const string SampleFileName = "general-module-sample";

        protected override void Awake()
        {
            base.Awake();
            InitModule();
        }

        [ContextMenu("General Module Sample/Increment Score")]
        private void IncrementScore()
        {
            var module = GetModule<SampleSaveModule>();
            module?.IncrementScore();
        }

        [ContextMenu("General Module Sample/Save")]
        private async void SaveSample()
        {
            var result = await Save(SavePath, SampleFileName);
            Debug.Log($"Sample save result: {result}");
        }

        [ContextMenu("General Module Sample/Load")]
        private void LoadSample()
        {
            var result = Load(SavePath, SampleFileName);
            Debug.Log($"Sample load result: {result}");
        }
    }
}
