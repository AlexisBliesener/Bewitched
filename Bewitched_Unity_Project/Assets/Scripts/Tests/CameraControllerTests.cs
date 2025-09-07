using Cinemachine;
using NUnit.Framework;
using System.Reflection;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;

public class CameraControllerTests
{
    [Tooltip("The CameraController instance under test.")]
    private CameraController controller;
    [Tooltip("A mock character used for testing.")]
    private Character fakeCharacter;
    [Tooltip("The GameObject hosting the CameraController.")]
    private GameObject host;

    /// <summary>
    /// A simple mock replacement for the Character class used in tests.
    /// Provides only what CameraController depends on.
    /// </summary>
    public class MockCharacter : Character
    {
        public override void Die() { }
        public override void PrimaryAttack() { }
        public override void SecondaryAttack() { }
    }

    [SetUp]
    public void SetUp()
    {
        LogAssert.ignoreFailingMessages = true; // prevents log errors from failing tests (so FMOD doesnt fail the test)

        host = new GameObject("CameraControllerHost");
        host.gameObject.SetActive(false);
        controller = host.AddComponent<CameraController>();

        // Fake Character
        var charObj = new GameObject("FakeCharacter");
        fakeCharacter = charObj.AddComponent<MockCharacter>();

        // Inject character into private field
        typeof(CameraController)
            .GetField("characterToFollow", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(controller, fakeCharacter);

        // Create a  CinemachineVirtualCamera
        var vcam = host.AddComponent<Cinemachine.CinemachineVirtualCamera>();
        vcam.AddCinemachineComponent<Cinemachine3rdPersonFollow>(); // ensures not null
        typeof(CameraController)
            .GetField("virtualCamera", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(controller, vcam);

        // Create a main camera
        var mainCamGO = new GameObject("MainCam");
        var mainCam = mainCamGO.AddComponent<Camera>();
        typeof(CameraController)
            .GetField("mainCam", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(controller, mainCam);

        // Create a StudioListener on its own GO
        var listenerGO = new GameObject("Listener");
        var listener = listenerGO.AddComponent<FMODUnity.StudioListener>();
        typeof(CameraController)
            .GetField("listener", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(controller, listener);

        host.gameObject.SetActive(true);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(host);
        Object.DestroyImmediate(fakeCharacter.gameObject);
    }

    /// <summary>
    /// Tests that the CameraController correctly updates its reference to a new character
    /// when SwitchCharacter is invoked. Verifies that the private characterToFollow field
    /// is updated to the new character.
    /// </summary>
    [Test]
    public void SwitchCharacter_UpdatesCharacterReference()
    {
        // Arrange
        var newChar = new GameObject("NewChar").AddComponent<MockCharacter>();

        var method = typeof(CameraController)
            .GetMethod("SwitchCharacter", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        method.Invoke(controller, new object[] { newChar });

        // Assert
        var charToFollow = typeof(CameraController)
            .GetField("characterToFollow", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(controller);

        Assert.AreSame(newChar, charToFollow);

        Object.DestroyImmediate(newChar.gameObject);
    }

    /// <summary>
    /// Tests that the CameraController defaults the camera side to the right
    /// when SwitchCameraSide is invoked and there are no collisions detected.
    /// Verifies that the private targetCamSide field is set to 1.
    /// </summary>
    [UnityTest]
    public IEnumerator SwitchCameraSide_DefaultsToRight()
    {
        // Make certain no collisions will be detected on the mask used by the raycast.
        typeof(CameraController)
            .GetField("environmentMask", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(controller, (LayerMask)0); // layerMask = 0 hits nothing

        // Let Awake/Start run.
        yield return null;

        // Let at least one Update() tick happen (SwitchCameraSide is called in Update).
        yield return null;

        var targetCamSide = (float)typeof(CameraController)
            .GetField("targetCamSide", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(controller);

        Assert.AreEqual(1f, targetCamSide, 0.05f);
    }
}
