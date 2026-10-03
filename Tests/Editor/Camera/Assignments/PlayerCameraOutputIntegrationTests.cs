using Immersive.Framework.Camera;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.Camera.Tests
{
    public sealed class PlayerCameraOutputIntegrationTests
    {
        private GameObject _playerObject;
        private GameObject _cameraObject;
        private GameObject _replacementCameraObject;

        [TearDown]
        public void TearDown()
        {
            if (_playerObject != null) Object.DestroyImmediate(_playerObject);
            if (_cameraObject != null) Object.DestroyImmediate(_cameraObject);
            if (_replacementCameraObject != null) Object.DestroyImmediate(_replacementCameraObject);
        }

        [Test]
        public void AssignmentCameraIsAppliedToPlayerInputAndShutdownClearsOwnedAssociation()
        {
            _playerObject = new GameObject("Player Input");
            _cameraObject = new GameObject("Assigned Output Camera");
            PlayerInput playerInput = _playerObject.AddComponent<PlayerInput>();
            UnityEngine.Camera assignedCamera = _cameraObject.AddComponent<UnityEngine.Camera>();

            PlayerCameraOutputIntegrationRuntime.SetCamera(playerInput, assignedCamera);

            Assert.That(playerInput.camera, Is.SameAs(assignedCamera));
            PlayerCameraOutputIntegrationRuntime.ClearCamera(playerInput, assignedCamera);
            Assert.That(playerInput.camera, Is.Null);
        }

        [Test]
        public void ShutdownDoesNotClearCameraAssociationOwnedByANewerAssignment()
        {
            _playerObject = new GameObject("Player Input");
            _cameraObject = new GameObject("Previous Output Camera");
            _replacementCameraObject = new GameObject("Replacement Output Camera");
            PlayerInput playerInput = _playerObject.AddComponent<PlayerInput>();
            UnityEngine.Camera previousCamera = _cameraObject.AddComponent<UnityEngine.Camera>();
            UnityEngine.Camera replacementCamera = _replacementCameraObject.AddComponent<UnityEngine.Camera>();
            PlayerCameraOutputIntegrationRuntime.SetCamera(playerInput, replacementCamera);

            PlayerCameraOutputIntegrationRuntime.ClearCamera(playerInput, previousCamera);

            Assert.That(playerInput.camera, Is.SameAs(replacementCamera));
        }
    }
}
