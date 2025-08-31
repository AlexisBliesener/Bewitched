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
    /// Tests that LoadNextLevel loads a random level from the stage and moves to the next stage if current stage is -1
    /// </summary>
    [Test]
    public void LoadNextLevel_LoadsRandomLevelAndMovesStageInital()
    {
        StageData stage1 = new StageData{
            stageName = "Stage1",
            levels = new List<string> { "Level1", "Level2" }
        };
        StageData stage2 = new StageData{
            stageName = "Stage2",
            levels = new List<string> { "Level3", "Level4" }
        };
        levelManager.levelData.stages.Add(stage1);
        levelManager.levelData.stages.Add(stage2);
        levelManager.SetCurrentStageIndex(-1);

        levelManager.LoadNextLevel();

        Assert.IsTrue(stage1.levels.Contains(levelManager.lastLoadedLevel));
        Assert.AreEqual(1, levelManager.sceneLoadCallCount);
        Assert.AreEqual(0, levelManager.GetCurrentStageIndex()); // Should move to next stage
    }
    /// <summary>
    /// Tests that LoadNextLevel loads a random level from the stage and moves to the next stage
    /// </summary>
    [Test]
    public void LoadNextLevel_LoadsRandomLevelAndMovesStage()
    {
        StageData stage1 = new StageData{
            stageName = "Stage1",
            levels = new List<string> { "Level1", "Level2" }
        };
        StageData stage2 = new StageData{
            stageName = "Stage2",
            levels = new List<string> { "Level3", "Level4" }
        };
        levelManager.levelData.stages.Add(stage1);
        levelManager.levelData.stages.Add(stage2);
        levelManager.SetCurrentStageIndex(0);

        levelManager.LoadNextLevel();

        Assert.IsTrue(stage2.levels.Contains(levelManager.lastLoadedLevel));
        Assert.AreEqual(1, levelManager.sceneLoadCallCount);
        Assert.AreEqual(1, levelManager.GetCurrentStageIndex()); // Should move to next stage
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
            levels = new List<string> { "Level1" }
        };
        levelManager.levelData.stages.Add(stage);
        levelManager.SetCurrentStageIndex(1); // Beyond available stages

        levelManager.LoadNextLevel();

        Assert.IsNull(levelManager.lastLoadedLevel);
        Assert.AreEqual(0, levelManager.sceneLoadCallCount);
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
            levels = new List<string> { "Level1" }
        };
        levelManager.levelData.stages.Add(stage);
        levelManager.SetCurrentStageIndex(-1);

        string eventLevelName = null;
        levelManager.OnLevelLoaded += (levelName) => eventLevelName = levelName;


        levelManager.LoadNextLevel();


        Assert.AreEqual("Level1", eventLevelName);
    }

    /// <summary>
    /// Tests that OnStageChanged event is triggered during stage change
    /// </summary>
    [Test]
    public void LoadNextLevel_TriggersOnStageChangedEvent()
    {

        StageData stage1 = new StageData
        {
            stageName = "Stage1",
            levels = new List<string> { "Level1" }
        };
        StageData stage2 = new StageData
        {
            stageName = "Stage2",
            levels = new List<string> { "Level2" }
        };
        levelManager.levelData.stages.Add(stage1);
        levelManager.levelData.stages.Add(stage2);

        string eventStageName = null;
        levelManager.OnStageChanged += (stageName) => eventStageName = stageName;
        
        levelManager.SetCurrentStageIndex(0);
        levelManager.LoadNextLevel();

        Assert.AreEqual("Stage2", eventStageName);
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
            levels = new List<string> { "JsonLevel1", "JsonLevel2" }
        });
        
        string json = JsonUtility.ToJson(testData, true);
        File.WriteAllText(testJsonPath, json);


        levelManager.LoadFromJson();

        Assert.AreEqual(1, levelManager.levelData.stages.Count);
        Assert.AreEqual("JsonTestStage", levelManager.levelData.stages[0].stageName);
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
            levels = new List<string> { "SaveLevel1" }
        });


        levelManager.SaveToJson();


        Assert.IsTrue(File.Exists(testJsonPath));
        string savedJson = File.ReadAllText(testJsonPath);
        Assert.IsTrue(savedJson.Contains("SaveTestStage"));
        Assert.IsTrue(savedJson.Contains("SaveLevel1"));
    }
}
