using UnityEngine;
using UnityEngine.UI; // Required for UI elements like Text


using UnityEditor;

using System.Collections.Generic;
using System.IO; // Required for Directory operations

public class BatchProcessingWindow : EditorWindow
{
    private List<string> replayFiles = new List<string>();
    private string newObjectName = "";
    private int selectedIndex = -1;

    // -- New: Add a boolean variable for the toggle switch --
    private bool automaticallyPlayNext = false;

    float progress = 0;
    string progressText = "progress";

    bool timeout = false;
    double timeoutStart;
    double timeoutDur = 3f;

    // Scroll position for the list
    private Vector2 scrollPos;

    [MenuItem("OGDReplay/Single Step/Add Replays to Batch")]
    public static void AddReplaysToBatch()
    {


        BatchProcessingWindow window = GetWindow<BatchProcessingWindow>("Batch Processing:");
        string path = EditorUtility.OpenFolderPanel("Select Folder for JSON Files", "", "");
        window.LoadFiles(path);
    }

    void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.update += Update;
    }

    void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.update -= Update;
    }

    private void Update()
    {
        if ((automaticallyPlayNext) && (timeout))
        {
            progress = (float)((EditorApplication.timeSinceStartup - timeoutStart) / timeoutDur);
            progressText = "Preparing next replay " + progress.ToString("G2");
            Repaint();

            if (progress >= 1)
            {
                OGDReplay replay = Selection.activeTransform.gameObject.GetComponent<OGDReplay>();
                if (replay != null)
                {
                    timeout = false;
                    replay.replayFile = replayFiles[0];
                    replayFiles.RemoveAt(0);
                    EditorApplication.EnterPlaymode();
                }
            }
        }
        else
        {
            if (Selection.activeTransform != null)
            {
                OGDReplay replay = Selection.activeTransform.gameObject.GetComponent<OGDReplay>();
                if (replay != null)
                {
                    progress = replay.Progress();
                    progressText = progress.ToString("G2");
                    Repaint();
                }
            }
        }
    }

    void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        Debug.Log("Play Mode" + state);

        if (automaticallyPlayNext)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {

                if (replayFiles.Count > 0)
                    if (Selection.activeTransform != null)
                    {
                        OGDReplay replay = Selection.activeTransform.gameObject.GetComponent<OGDReplay>();
                        if (replay != null)
                        {

                            //EditorApplication.EnterPlaymode();
                            timeout = true;
                            timeoutStart = EditorApplication.timeSinceStartup;
                            

                        }
                    }

            }
        }
        // Force a repaint whenever play mode state changes, to update the button text and status
    }

    public void LoadFiles(string path)
    {
        //replayFiles.Clear();
        //string path = EditorUtility.OpenFolderPanel("Select Folder for JSON Files", "", "");
        if (!string.IsNullOrEmpty(path))
        {
            string[] allFiles = Directory.GetFiles(path, "*.json", SearchOption.TopDirectoryOnly);
            foreach (string file in allFiles)
            {
                replayFiles.Add(file);
            }
        }
        timeout = false;
    }

    // New: Method to toggle play mode
    void TogglePlayMode()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.ExitPlaymode();
            //lets stop auto playback
            automaticallyPlayNext = false;
            timeout = false; 
        }
        else
        {
            if (replayFiles.Count > 0)
                if (Selection.activeTransform != null)
                {
                    OGDReplay replay = Selection.activeTransform.gameObject.GetComponent<OGDReplay>();
                    if (replay != null)
                    {
                        replay.replayFile = replayFiles[0];
                        replayFiles.RemoveAt(0);
                        EditorApplication.EnterPlaymode();
                    }
                }

        }
    }

    void OnGUI()
    {




        EditorGUILayout.LabelField("Replay Files", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // --- Play Mode Toggle Button ---
        string buttonText = "Start";
        if ((EditorApplication.isPlaying) || timeout)
        {
            buttonText = "Stop";
        }
        string playModeButtonText = buttonText;
        Color originalGUIColor = GUI.color; // Store original color
        GUI.color = EditorApplication.isPlaying ? Color.red : timeout ? Color.yellow : Color.green; // Change color based on state

        if (GUILayout.Button(playModeButtonText, GUILayout.Height(30)))
        {
            TogglePlayMode();
        }
        GUI.color = originalGUIColor; // Restore original color
        EditorGUILayout.Space();

        EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 20), progress, progressText);

        // -- New: Add the toggle switch to the UI --
        automaticallyPlayNext = EditorGUILayout.Toggle("Play next automatically", automaticallyPlayNext);
        EditorGUILayout.Space(); // Add some space below the toggle


        if (GUILayout.Button("Clear List", GUILayout.Height(30)))
        {
            replayFiles.Clear();
        }


        // --- Add New Object Section ---
        //EditorGUILayout.BeginHorizontal();
        //newObjectName = EditorGUILayout.TextField("New Object Name:", newObjectName);
        //if (GUILayout.Button("Add Object", GUILayout.Width(100)))
        //{
        //    if (!string.IsNullOrWhiteSpace(newObjectName))
        //    {
        //        objectNames.Add(newObjectName);
        //        newObjectName = "";
        //        GUI.FocusControl(null);
        //    }
        //}
        //EditorGUILayout.EndHorizontal();
        //EditorGUILayout.Space();

        // --- Object List Section ---
        EditorGUILayout.LabelField($"Replay Files: {replayFiles.Count}", EditorStyles.largeLabel);

        EditorGUILayout.BeginVertical(GUI.skin.box);
        if (replayFiles.Count == 0)
        {
            EditorGUILayout.LabelField("No objects in the list.", EditorStyles.miniLabel);
        }
        else
        {
            // Begin the scroll view. This is where the scrollbar will appear if content overflows.
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200)); // Fixed height for scroll view

            for (int i = 0; i < replayFiles.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                bool isSelected = (selectedIndex == i);
                bool newSelection = EditorGUILayout.Toggle(isSelected, GUILayout.Width(20));

                if (newSelection != isSelected)
                {
                    selectedIndex = newSelection ? i : -1;
                }

                EditorGUILayout.LabelField(replayFiles[i]);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView(); // End the scroll view
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();

        // --- Remove Selected Object Section ---
        GUI.enabled = (selectedIndex != -1 && selectedIndex < replayFiles.Count);
        if (GUILayout.Button("Remove Selected Object"))
        {
            if (selectedIndex != -1 && selectedIndex < replayFiles.Count)
            {
                replayFiles.RemoveAt(selectedIndex);
                selectedIndex = -1;
            }
        }



        GUI.enabled = true;
    }
}