using Cinemachine;
using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.UI;

public class PossessionAbilityTests
{
    /// <summary>
    /// Mock version of Hag that skips Awake logic.
    /// </summary>
    public class MockHag : Hag
    {
        protected new void Awake() { }

        protected new void OnDestroy() { }
    }

    public class MockPossssionAbility : PossessionAbility
    {
        protected override void UpdateUI() { }
    }

    public class MockEnemy : Enemy
    {

    }

    GameObject possessionObj;
    MockPossssionAbility possessionAbility;
    MockHag hag;
    CinemachineVirtualCamera cam;

    [UnitySetUp]
    public IEnumerator UnitySetUp()
    {
        possessionObj = new GameObject("Possession");
        possessionAbility = possessionObj.AddComponent<MockPossssionAbility>();

        hag = new GameObject("Hag").AddComponent<MockHag>();
        SetPrivate(possessionAbility, "oldHag", hag);
        SetPrivate(possessionAbility, "currentCharacter", hag);

        var camObj = new GameObject("VCam");
        cam = camObj.AddComponent<CinemachineVirtualCamera>();
        SetPrivate(possessionAbility, "virtualCam", cam);

        yield return null; // let Unity finish Awake
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(possessionObj);
        Object.DestroyImmediate(hag.gameObject);
        Object.DestroyImmediate(cam.gameObject);
    }

    [Test]
    public void Awake_SetsCurrentCharacterToHag()
    {
        var current = GetPrivate<Character>(possessionAbility, "currentCharacter");
        Assert.AreEqual(hag, current);
    }

    [UnityTest]
    public IEnumerator Update_MovesHagToEnemy_WhenEnemyControlled()
    {
        var enemy = new GameObject("Enemy").AddComponent<MockEnemy>();
        enemy.transform.position = Vector3.one;
        SetPrivate(possessionAbility, "currentCharacter", enemy);

        var oldPos = hag.transform.position;
        possessionAbility.SendMessage("Update");
        yield return null;

        Assert.AreNotEqual(oldPos, hag.transform.position);
    }

    [Test]
    public void SwitchCharacter_ToEnemy_SetsTeamAndHealth()
    {
        var enemy = new GameObject("Enemy").AddComponent<MockEnemy>();
        enemy.gameObject.AddComponent<EnemyHealth>();

        possessionAbility.SwitchCharacter(enemy);

        Assert.AreEqual(enemy, GetPrivate<Character>(possessionAbility, "currentCharacter"));
        Assert.AreEqual(1, enemy.teamID);
    }

    // ----- Helpers -----
    private T GetPrivate<T>(object obj, string fieldName)
    {
        return (T)obj.GetType()
            .GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(obj);
    }
    private void SetPrivate<T>(object obj, string fieldName, T value)
    {
        var field = obj.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance);

        if (field == null)
            throw new System.Exception($"Field '{fieldName}' not found on {obj.GetType().Name}");

        field.SetValue(obj, value);
    }
}
