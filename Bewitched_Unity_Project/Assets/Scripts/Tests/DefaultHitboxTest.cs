using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// This class tests the default hitbox to make sure everything is working properly
/// </summary>
public class DefaultHitboxTest
{
    /// <summary>
    /// Testing class for the hitbox that has getters and setters
    /// </summary>
    private class TestHitbox : DefaultHitbox
    {
        /// <summary>
        /// Gets the count of enemies in hitChar
        /// </summary>
        /// <returns> Number of enemies in hitChar </returns>
        public int GetHitCharLength()
        {
            return hitChars.Count;
        }

        /// <summary>
        /// Returns if the hitbox is active or not
        /// </summary>
        /// <returns> True if it is, false otherwise </returns>
        public bool GetActiveStatus()
        {
            return active;
        }

        /// <summary>
        /// Sets the hit-wall value
        /// </summary>
        public void SetHitWall()
        {
            hitWall = true;
        }

        /// <summary>
        /// Gets the damage
        /// </summary>
        /// <returns> Damage </returns>
        public float GetDmg()
        {
            return damage;
        }

        /// <summary>
        /// Gets the slam damage
        /// </summary>
        /// <returns> Slam damage </returns>
        public float GetSlamDmg()
        {
            return slamDamage;
        }

        /// <summary>
        /// Gets the thrust speed
        /// </summary>
        /// <returns> Thrust speed </returns>
        public float GetForwardVelocity()
        {
            return thrustSpeed;
        }

        /// <summary>
        /// Gets the attack status effects
        /// </summary>
        /// <returns> Attack status effects </returns>
        public AttackStatusEffects GetStatus()
        {
            return statusEffects;
        }

        /// <summary>
        /// Gets the parent hitbox
        /// </summary>
        /// <returns> Parent hitbox </returns>
        public DefaultHitbox GetParent()
        {
            return parent;
        }

        /// <summary>
        /// Checks if the hitbox is a child of this
        /// </summary>
        /// <param name="hitbox"> Hitbox to check </param>
        /// <returns> True if it is a child </returns>
        public bool IsChild(DefaultHitbox hitbox)
        {
            if (children.Contains(hitbox))
            {
                return true;
            }
            return false;
        }
    }

    GameObject userObj = new GameObject("user");
    GameObject enemyObj = new GameObject("enemy");

    Ogre user;
    Goblin enemy;

    TestHitbox hitbox;
    TestHitbox second;

    [SetUp]
    public void SetUp()
    {
        user = userObj.AddComponent<Ogre>();
        enemy = enemyObj.AddComponent<Goblin>();

        user.transform.position = new Vector3(0, 0, 0);
        enemy.transform.position = new Vector3(0, 0, 1);

        user.SetTeamID(1);

        hitbox = new GameObject("hitbox").AddComponent<TestHitbox>();
        second = new GameObject("second").AddComponent<TestHitbox>();

        hitbox.Init(user, dmg: 10, status: null);
    }

    /// <summary>
    /// Cleans up the created GameObject after each test.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        Object.Destroy(user);
        Object.Destroy(enemy);
    }

    /// <summary>
    /// Checks if the function adds to hits properly
    /// </summary>
    [Test]
    public void AddToHit_AddsProperly()
    {
        Assert.AreEqual(0, hitbox.GetHitCharLength()); // Make sure it starts at 0

        hitbox.AddToHit(enemy);
        Assert.AreEqual(1, hitbox.GetHitCharLength()); // Adds an enemy - should go to 1

        hitbox.AddToHit(enemy);
        Assert.AreEqual(1, hitbox.GetHitCharLength()); // Enemy in list - should remain 1
    }

    /// <summary>
    /// Tests to see if enemies themselves are registered as being hit
    /// </summary>
    [Test]
    public void HasBeenHit_EnemyHasBeenHit()
    {
        Assert.IsFalse(hitbox.HasBeenHit(enemy));

        hitbox.AddToHit(enemy);
        Assert.IsTrue(hitbox.HasBeenHit(enemy));
    }

    /// <summary>
    /// Tests to see if the hitbox is active
    /// </summary>
    [Test]
    public void SetActive_IsActiveAndInactive()
    {
        Assert.IsTrue(hitbox.GetActiveStatus());

        hitbox.SetActive(false);
        Assert.IsFalse(hitbox.GetActiveStatus());
    }

    /// <summary>
    /// Tests to see if the hitbox is has hit a wall
    /// </summary>
    [Test]
    public void HitWall_HasHitAWall()
    {
        Assert.IsFalse(hitbox.HasHitWall());

        hitbox.SetHitWall();
        Assert.IsTrue(hitbox.HasHitWall());
    }

    /// <summary>
    /// Tests the basic initialize function
    /// </summary>
    [Test]
    public void TestInit_TestDefaultValues()
    {
        hitbox.Init(user);
        Assert.AreEqual(0, hitbox.GetDmg());
        Assert.AreEqual(0, hitbox.GetSlamDmg());
        Assert.AreEqual(0, hitbox.GetForwardVelocity());
        Assert.IsNull(hitbox.GetStatus());
    }

    /// <summary>
    /// Tests a more complex initialize function
    /// </summary>
    [Test]
    public void TestInit_TestValues()
    {
        hitbox.Init(user, dmg: 10, slamDMG: 15, forwardVelocity: 20);
        Assert.AreEqual(10, hitbox.GetDmg());
        Assert.AreEqual(15, hitbox.GetSlamDmg());
        Assert.AreEqual(20, hitbox.GetForwardVelocity());
        Assert.IsNull(hitbox.GetStatus());
    }

    /// <summary>
    /// Test to ensure adding a child links the two
    /// </summary>
    [Test]
    public void TestAttachChild_Basic()
    {
        second.Init(user, dmg: 10, status: null);
        Assert.IsNull(second.GetParent());
        Assert.IsFalse(hitbox.IsChild(second));

        hitbox.AttachHitbox(second);

        Assert.AreEqual(hitbox, second.GetParent());
        Assert.IsTrue(hitbox.IsChild(second));
    }
}
