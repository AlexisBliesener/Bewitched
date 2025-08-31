using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.IO;

/// <summary>
/// Unit tests for the LevelManager class.
/// Tests level and stage progression and JSON data functionality
/// </summary>
public class LevelManagerTests
{

    /// <summary>
    /// The LevelManager GameObject for the test
    /// </summary>
    private GameObject levelManagerObj;

    /// <summary>
    /// The LevelManager instance for the tests
    /// </summary>
    private MockLevelManager levelManager;

    /// <summary>
    /// Test JSON file path
    /// </summary>
    private string testJsonPath;


    /// <summary>
    /// Mock LevelManager that only overrides scene loading for testing.
    /// </summary>
    public class MockLevelManager : LevelManager
    {
        [Tooltip("Last level name that was loaded")]
        public string lastLoadedLevel;
        [Tooltip("Number of times scene loading was called")]
        public int sceneLoadCallCount = 0;

        /// <summary>
        /// Override the LoadScene method to prevent actual scene loading in tests
        /// </summary>
        protected override void LoadScene(string levelName)
        {
            lastLoadedLevel = levelName;
            sceneLoadCallCount++;
        }
    }

    /// <summary>
    /// Sets up the test environment and initializes LevelManager.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        levelManagerObj = new GameObject("LevelManager");
        levelManager = levelManagerObj.AddComponent<MockLevelManager>();
        
        // Set up test JSON path
        testJsonPath = Path.Combine(Application.dataPath, "TestLevelData.json");
        levelManager.JSON_PATH = "TestLevelData.json";
        
        // Initialize with empty data to avoid awake loading
        levelManager.levelData = new LevelData();
    }

    /// <summary>
    /// Clean up test objects and files after each test.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        // remove test JSON file
        if (File.Exists(testJsonPath))
        {
            File.Delete(testJsonPath);
        }
        
        Object.DestroyImmediate(levelManagerObj);
    }


    /// <summary>
    /// Test that LevelManager creates a singleton instance on Awake.
    /// </summary>
    [UnityTest]
    public IEnumerator Awake_CreatesSingletonInstance()
    {
        levelManager.SendMessage("Awake");
        yield return null;

        Assert.AreEqual(levelManager, LevelManager.Instance);
    }

    /// <summary>
    /// Tests the duplicate LevelManager instances should be destroyed
    /// </summary>
    [UnityTest]
    public IEnumerator Awake_DestroysDuplicateInstance()
    {
        levelManager.SendMessage("Awake");
        GameObject duplicateObj = new GameObject("DuplicateLevelManager");
        MockLevelManager duplicate = duplicateObj.AddComponent<MockLevelManager>();

        duplicate.SendMessage("Awake");
        yield return null;

        Assert.IsTrue(duplicateObj == null);
        Assert.AreEqual(levelManager, LevelManager.Instance);
    }


    /// <summary>
    /// Tests that LoadNextLevel loads first level in sequential stage
    /// </summary>
    [Test]
    public void LoadNextLevel_SequentialStageLoadsFirstLevel()
    {
        StageData stage = new StageData{
            stageName = "TestStage",
            isRandomized = false,
            levels = new List<string> { "Level1", "Level2", "Level3" }
        };
        levelManager.levelData.stages.Add(stage);
        levelManager.SetCurrentStageIndex(0);
        levelManager.SetRemainingLevels(null);

        
        levelManager.LoadNextLevel();

        Assert.AreEqual("Level1", levelManager.lastLoadedLevel);
        Assert.AreEqual(1, levelManager.sceneLoadCallCount);
    }

    /// <summary>
    /// Tests that LoadNextLevel loads random level in randomized stage.
    /// </summary>
    [Test]
    public void LoadNextLevel_RandomizedStageLoadsRandomLevel()
    {
        StageData stage = new StageData{
            stageName = "RandomStage",
            isRandomized = true,
            levels = new List<string> { "Level1", "Level2", "Level3" }
        };
        levelManager.levelData.stages.Add(stage);
        levelManager.SetCurrentStageIndex(0);
        levelManager.SetRemainingLevels(new List<string> { "Level1", "Level2", "Level3" });

        levelManager.LoadNextLevel();

        Assert.IsTrue(stage.levels.Contains(levelManager.lastLoadedLevel));
        Assert.AreEqual(1, levelManager.sceneLoadCallCount);
        Assert.AreEqual(2, levelManager.GetRemainingLevels().Count); // Should remove one level
    }

    /// <summary>
    /// Tests that LoadNextLevel handles empty stages correctly.
    /// </summary>
    [Test]
    public void LoadNextLevel_EmptyStagesDoesNotLoad()
    {
        
        levelManager.levelData.stages.Clear();

       
        levelManager.LoadNextLevel();

      
        Assert.IsNull(levelManager.lastLoadedLevel);
        Assert.AreEqual(0, levelManager.sceneLoadCallCount);
    }

    /// <summary>
    /// Tests that LoadNextLevel handles completed stages correctly
    /// </summary>
    [Test]
    public void LoadNextLevel_AllStagesCompletedDoesNotLoad()
    {
        StageData stage = new StageData
        {
            stageName = "TestStage",
            isRandomized = false,
            levels = new List<string> { "Level1" }
        };
        levelManager.levelData.stages.Add(stage);
        levelManager.SetCurrentStageIndex(1); // Beyond available stages

        levelManager.LoadNextLevel();

        Assert.IsNull(levelManager.lastLoadedLevel);
        Assert.AreEqual(0, levelManager.sceneLoadCallCount);
    }

    /// <summary>
    /// Tests that LoadNextLevel progresses through sequential levels correctly.
    /// </summary>
    [Test]
    public void LoadNextLevel_SequentialProgressesThroughLevels()
    {
        StageData stage = new StageData
        {
            stageName = "SequentialStage",
            isRandomized = false,
            levels = new List<string> { "Level1", "Level2" }
        };
        levelManager.levelData.stages.Add(stage);
        levelManager.SetCurrentStageIndex(0);

        //  First level
        levelManager.LoadNextLevel();
        Assert.AreEqual("Level1", levelManager.lastLoadedLevel);

        // Second level (simulating completion of first)
        levelManager.LoadNextLevel();
        Assert.AreEqual("Level2", levelManager.lastLoadedLevel);
        Assert.AreEqual(2, levelManager.sceneLoadCallCount);
    }

    /// <summary>
    /// Tests that LoadNextLevel moves to next stage when current stage is completed
    /// </summary>
    [Test]
    public void LoadNextLevel_StageCompletedMovesToNextStage()
    {
        StageData stage1 = new StageData
        {
            stageName = "Stage1",
            isRandomized = true,
            levels = new List<string> { "Level1" }
        };
        StageData stage2 = new StageData
        {
            stageName = "Stage2",
            isRandomized = false,
            levels = new List<string> { "Level2" }
        };
        levelManager.levelData.stages.Add(stage1);
        levelManager.levelData.stages.Add(stage2);
        levelManager.SetCurrentStageIndex(0);
        levelManager.SetRemainingLevels(new List<string>()); // Empty remaining levels


        levelManager.LoadNextLevel();

        Assert.AreEqual(1, levelManager.GetCurrentStageIndex());
        Assert.AreEqual("Level2", levelManager.lastLoadedLevel);
    }


    /// <summary>
    /// Tests that InitializeStage sets up randomized stage correctly.
    /// </summary>
    [Test]
    public void InitializeStage_RandomizedStageSetsRemainingLevels()
    {
        StageData stage = new StageData
        {
            stageName = "RandomStage",
            isRandomized = true,
            levels = new List<string> { "Level1", "Level2", "Level3" }
        };
        levelManager.levelData.stages.Add(stage);
        levelManager.SetCurrentStageIndex(0);

        List<string> remainingLevels = levelManager.GetRemainingLevels();
        Assert.IsNotNull(remainingLevels);
        Assert.AreEqual(3, remainingLevels.Count);
        Assert.IsTrue(remainingLevels.Contains("Level1"));
        Assert.IsTrue(remainingLevels.Contains("Level2"));
        Assert.IsTrue(remainingLevels.Contains("Level3"));
    }

    /// <summary>
    /// Tests that InitializeStage sets up sequential stage correctly.
    /// </summary>
    [Test]
    public void InitializeStage_SequentialStageSetsRemainingLevelsToNull()
    {

        StageData stage = new StageData
        {
            stageName = "SequentialStage",
            isRandomized = false,
            levels = new List<string> { "Level1", "Level2" }
        };
        levelManager.levelData.stages.Add(stage);
        levelManager.SetCurrentStageIndex(0);


        Assert.IsNull(levelManager.GetRemainingLevels());
    }

    /// <summary>
    /// Tests that InitializeStage handles empty stages list.
    /// </summary>
    [Test]
    public void InitializeStage_EmptyStagesDoesNotThrow()
    {
        levelManager.levelData.stages.Clear();

        Assert.DoesNotThrow(() => levelManager.SetCurrentStageIndex(2));
    }


    /// <summary>
    /// Tests that OnLevelLoaded event is triggered when level loads.
    /// </summary>
    [Test]
    public void LoadNextLevel_TriggersOnLevelLoadedEvent()
    {

        StageData stage = new StageData
        {
            stageName = "TestStage",
            isRandomized = false,
            levels = new List<string> { "Level1" }
        };
        levelManager.levelData.stages.Add(stage);
        levelManager.SetCurrentStageIndex(0);
        levelManager.SetRemainingLevels(null);

        string eventLevelName = null;
        levelManager.OnLevelLoaded += (levelName) => eventLevelName = levelName;


        levelManager.LoadNextLevel();


        Assert.AreEqual("Level1", eventLevelName);
    }

    /// <summary>
    /// Tests that OnStageChanged event is triggered during stage initialization
    /// </summary>
    [Test]
    public void InitializeStage_TriggersOnStageChangedEvent()
    {

        StageData stage = new StageData
        {
            stageName = "EventTestStage",
            isRandomized = false,
            levels = new List<string> { "Level1" }
        };
        levelManager.levelData.stages.Add(stage);
        string eventStageName = null;
        levelManager.OnStageChanged += (stageName) => eventStageName = stageName;
        
        levelManager.SetCurrentStageIndex(0);



        Assert.AreEqual("EventTestStage", eventStageName);
    }


    /// <summary>
    /// Tests that LoadFromJson loads valid JSON data correctly.
    /// </summary>
    [Test]
    public void LoadFromJson_ValidFileLoadsData()
    {
  
        LevelData testData = new LevelData();
        testData.stages.Add(new StageData 
        { 
            stageName = "JsonTestStage", 
            isRandomized = true,
            levels = new List<string> { "JsonLevel1", "JsonLevel2" }
        });
        
        string json = JsonUtility.ToJson(testData, true);
        File.WriteAllText(testJsonPath, json);


        levelManager.LoadFromJson();

        Assert.AreEqual(1, levelManager.levelData.stages.Count);
        Assert.AreEqual("JsonTestStage", levelManager.levelData.stages[0].stageName);
        Assert.IsTrue(levelManager.levelData.stages[0].isRandomized);
        Assert.AreEqual(2, levelManager.levelData.stages[0].levels.Count);
    }

    /// <summary>
    /// Tests that LoadFromJson handles missing file 
    /// </summary>
    [Test]
    public void LoadFromJson_MissingFileCreatesEmptyData()
    {

        levelManager.JSON_PATH = "NonExistentFile.json";


        levelManager.LoadFromJson();


        Assert.IsNotNull(levelManager.levelData);
        Assert.IsNotNull(levelManager.levelData.stages);
        Assert.AreEqual(0, levelManager.levelData.stages.Count);
    }

    /// <summary>
    /// Tests that SaveToJson creates valid JSON file.
    /// </summary>
    [Test]
    public void SaveToJson_CreatesValidFile()
    {

        levelManager.levelData.stages.Add(new StageData 
        { 
            stageName = "SaveTestStage", 
            isRandomized = false,
            levels = new List<string> { "SaveLevel1" }
        });


        levelManager.SaveToJson();


        Assert.IsTrue(File.Exists(testJsonPath));
        string savedJson = File.ReadAllText(testJsonPath);
        Assert.IsTrue(savedJson.Contains("SaveTestStage"));
        Assert.IsTrue(savedJson.Contains("SaveLevel1"));
    }


    /// <summary>
    /// Tests that LoadNextLevel handles null level names.
    /// </summary>
    [Test]
    public void LoadNextLevel_NullLevelNameDoesNotLoadScene()
    {
        StageData stage = new StageData
        {
            stageName = "NullTestStage",
            isRandomized = false,
            levels = new List<string> { null }
        };
        levelManager.levelData.stages.Add(stage);
        levelManager.SetCurrentStageIndex(0);
        levelManager.SetRemainingLevels(null);

    
        levelManager.LoadNextLevel();


        Assert.AreEqual(0, levelManager.sceneLoadCallCount);
    }

    /// <summary>
    /// Tests that LoadNextLevel handles empty level names
    /// </summary>
    [Test]
    public void LoadNextLevel_EmptyLevelNameDoesNotLoadScene()
    {
        StageData stage = new StageData
        {
            stageName = "EmptyTestStage",
            isRandomized = false,
            levels = new List<string> { "" }
        };
        levelManager.levelData.stages.Add(stage);
        levelManager.SetCurrentStageIndex(0);
        levelManager.SetRemainingLevels(null);


        levelManager.LoadNextLevel();


        Assert.AreEqual(0, levelManager.sceneLoadCallCount);
    }
}