using System.IO;
using NUnit.Framework;
using UnityEngine;

public class GrandFinaleTests
{
    private GameObject go;
    private GrandFinale grandFinale;

    [SetUp]
    public void SetUp()
    {
        go = new GameObject("GrandFinaleTest");
        grandFinale = go.AddComponent<GrandFinale>();
        grandFinale.stackNum = 0;

        // Attach dummy RectTransform for Pulse
        var rectGO = new GameObject("EnemyHealthBar", typeof(RectTransform));
        typeof(GrandFinale)
            .GetField("enemyHealthBar", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(grandFinale, rectGO.GetComponent<RectTransform>());
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(go);
    }

    [Test]
    public void Awake_SetsSingletonInstance()
    {
       
        Assert.AreEqual(grandFinale, GrandFinale.instance);
    }

    [Test]
    public void GetActive_DefaultsToFalse()
    {
        Assert.IsFalse(grandFinale.GetActive());
    }

    [Test]
    public void Activate_SetsActiveTrue()
    {
        grandFinale.Activate();
        Assert.IsTrue(grandFinale.GetActive());
    }

    [Test]
    public void Pulse_ChangesEnemyHealthBarScale()
    {
        var rt = (RectTransform)typeof(GrandFinale)
            .GetField("enemyHealthBar", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(grandFinale);

        float before = rt.localScale.x;

        var method = typeof(GrandFinale).GetMethod("Pulse",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Invoke(grandFinale, new object[] { 1.5f });

        float after = rt.localScale.x;
        Assert.AreNotEqual(before, after);
    }

    [Test]
    public void CheckCharacterBehindEnvironment_ReturnsTrue_WithoutObstacles()
    {
        var target = new GameObject("Target").transform;
        target.position = go.transform.position + Vector3.forward * 5f;

        bool visible = grandFinale.CheckCharacterBehindEnvironment(target);
        Assert.IsTrue(visible);

        Object.DestroyImmediate(target.gameObject);
    }

}
