using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Cinemachine;
using FMODUnity;
using UnityEngine.UI;

/// <summary>
/// Unit tests for CameraController. 
/// Tests initialization, camera priority setup, and FMOD listener linking.
/// </summary>
public class CameraControllerTests
{
    /// <summary>
    /// Mock character class for testing purposes.
    /// Provides Cinemachine cameras and AimCam without requiring full game logic.
    /// </summary>
    public class MockCharacter : Character
    {
        [Tooltip("FreeLook camera used for third-person view.")]
        private CinemachineFreeLook freeLook;

        [Tooltip("Virtual camera used for aiming.")]
        private CinemachineVirtualCamera virtualCam;

        [Tooltip("AimCam component for aiming behavior.")]
        private AimCam aimCam;

        /// <summary>
        /// Initializes the mock character's cameras and AimCam.
        /// </summary>
        public void Init()
        {
            var freeObj = new GameObject("FreeLookCam");
            freeObj.transform.SetParent(this.transform);
            freeLook = freeObj.AddComponent<CinemachineFreeLook>();

            var virtualObj = new GameObject("VirtualCam");
            virtualObj.transform.SetParent(this.transform);
            virtualCam = virtualObj.AddComponent<CinemachineVirtualCamera>();

            aimCam = virtualObj.AddComponent<AimCam>();
        }

        /// <summary>
        /// Overrides Character.Die() but does nothing for tests.
        /// </summary>
        public override void Die() { }

        /// <summary>Returns the FreeLook camera.</summary>
        public new CinemachineFreeLook GetFreeLookCam() => freeLook;

        /// <summary>Returns the virtual camera.</summary>
        public new CinemachineVirtualCamera GetVirtualCam() => virtualCam;

        /// <summary>Returns the AimCam component.</summary>
        public AimCam GetAimCam() => aimCam;
    }

    [Tooltip("GameObject holding the CameraController.")]
    private GameObject camObj;
    [Tooltip("CameraController being tested.")]
    private CameraController controller;
    [Tooltip("FreeLook camera from the mock character.")]
    private CinemachineFreeLook freeLook;
    [Tooltip("Virtual camera from the mock character.")]
    private CinemachineVirtualCamera virtualCam;
    [Tooltip("Mock character used for testing.")]
    private MockCharacter mockCharacter;
    [Tooltip("FMOD Studio listener attached to the camera.")]
    private StudioListener listener;
    [Tooltip("Crosshair image used for aiming.")]
    private Image crossHair;

    /// <summary>
    /// Sets up the CameraController, mock character, cameras, listener, and crosshair before each test.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        camObj = new GameObject("CameraControllerTest");
        camObj.SetActive(false);
        controller = camObj.AddComponent<CameraController>();

        // Mock character setup
        var charObj = new GameObject("Character");
        mockCharacter = charObj.AddComponent<MockCharacter>();
        mockCharacter.Init();

        freeLook = mockCharacter.GetFreeLookCam();
        virtualCam = mockCharacter.GetVirtualCam();

        listener = camObj.AddComponent<StudioListener>();
        crossHair = new GameObject("Crosshair").AddComponent<Image>();

        // Inject private fields into CameraController
        typeof(CameraController).GetField("freeLookCam", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(controller, freeLook);
        typeof(CameraController).GetField("virtualCam", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(controller, virtualCam);
        typeof(CameraController).GetField("currentCharacter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(controller, mockCharacter);
        typeof(CameraController).GetField("crossHair", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(controller, crossHair);
        typeof(CameraController).GetField("listener", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(controller, listener);

        camObj.SetActive(true);

        // Manually run Awake
        var awake = typeof(CameraController).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        awake.Invoke(controller, null);
    }

    /// <summary>
    /// Cleans up GameObjects after each test.
    /// </summary>
    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(camObj);
        Object.DestroyImmediate(mockCharacter.gameObject);
        Object.DestroyImmediate(crossHair.gameObject);
    }

    /// <summary>
    /// Tests that Awake correctly initializes aiming state, camera priorities, and FMOD listener.
    /// </summary>
    [Test]
    public void Awake_InitializesCorrectly()
    {
        Assert.IsFalse(CameraController.GetIsAiming(), "Aiming should be false on Awake.");
        Assert.AreEqual(2, freeLook.Priority, "FreeLook priority should be initialized to 2.");
        Assert.AreEqual(1, virtualCam.Priority, "VirtualCam priority should be initialized to 1.");
        Assert.NotNull(listener.attenuationObject, "FMOD listener should be linked to character.");
    }
}
