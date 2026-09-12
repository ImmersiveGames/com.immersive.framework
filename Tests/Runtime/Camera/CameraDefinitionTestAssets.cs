using System;
using System.Collections.Generic;
using Immersive.Framework.CameraAuthoring;
using UnityEngine;

namespace Immersive.Framework.Camera.Tests
{
    internal sealed class CameraDefinitionTestAssets : IDisposable
    {
        internal const string MainId = "10000000000000000000000000000001";
        internal const string ViewId = "20000000000000000000000000000001";
        private readonly List<ScriptableObject> _assets = new List<ScriptableObject>();
        private CameraOutputDefinition _main;
        internal CameraOutputDefinition Main => _main != null ? _main : (_main = Output(MainId));

        internal CameraViewDefinition View(string id = ViewId) => Create<CameraViewDefinition>(id);
        internal CameraOutputDefinition Output(string id) => Create<CameraOutputDefinition>(id);
        internal T Behavior<T>() where T : CameraRigBehaviorDefinition => Create<T>();

        private T Create<T>(string id) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            _assets.Add(asset);
            JsonUtility.FromJsonOverwrite("{\"stableId\":\"" + id + "\"}", asset);
            return asset;
        }

        private T Create<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            _assets.Add(asset);
            return asset;
        }

        public void Dispose()
        {
            foreach (var asset in _assets) UnityEngine.Object.DestroyImmediate(asset);
            _assets.Clear();
            _main = null;
        }
    }
}
