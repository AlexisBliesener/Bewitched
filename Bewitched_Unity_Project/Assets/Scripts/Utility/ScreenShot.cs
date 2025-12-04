using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.IO;
using System.IO;

public class ScreenShot : MonoBehaviour
{
    public KeyCode screenshotKey = KeyCode.T; 
    public string folderName = "Screenshots"; 

    void Update()
    {
        if (Input.GetKeyDown(screenshotKey))
        {
            TakeScreenshot();
        }
    }

    void TakeScreenshot()
    {
        string path = Path.Combine(Application.dataPath, folderName);
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        string filename = "Screenshot_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
        string fullPath = Path.Combine(path, filename);

        ScreenCapture.CaptureScreenshot(fullPath, 3);
        Debug.Log("Screenshot saved to: " + fullPath);
    }
}
