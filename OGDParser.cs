using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

using UnityEditor;

using System;

using System.Threading;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI; // Required for UI elements like Text


using UnityEditor;

using System.Collections.Generic;
using System.IO; // Required for Directory operations

public class ProgressReport
{
    System.Diagnostics.Stopwatch stopwatch;
    int progressId;
    public int timeout = 100;



    public ProgressReport()
    {

    }

    public ProgressReport(string name)
    {
        progressId = Progress.Start(name);
        stopwatch = System.Diagnostics.Stopwatch.StartNew();
        //this.timeout = timeout;
       
    }


    public bool Report(float pos, string desc = "")
    {
        if (stopwatch.ElapsedMilliseconds > timeout)
        {
            Progress.Report(progressId, pos, desc);
            stopwatch.Restart();
            return true;
        }
        else
        {
            return false;
        }
    }

    public void End()
    {
        Progress.Remove(progressId);
    }


}

[Serializable]
public struct StateChangeFrame
{
    public float time;
    public bool state;
};

[Serializable]
public struct TrackedObjectFrame
{
   public float time;
   public Vector3 pos;
   public Quaternion rot;
};



[Serializable]
public class GameState
{
    //TODO add other stuff?
    public float seconds_from_launch;
};




[Serializable]
public class DataPacket
{
    //public float time;
    //public Vector3 pos;
    //public Quaternion rot;
     public float[] pos = new float[3];
     public float[] rot = new float[4];
   // public List<Vector3> pos = new List<Vector3>();
    //public List<Quaternion> rot = new List<Quaternion>();
};

[Serializable]
public class DataPackage
{
    public float time;
    public List<DataPacket> data;
}


[Serializable]
public class EventData
{
    public string gaze_data_package;
    public string left_hand_data_package;
    public string right_hand_data_package;
    public string http_user_agent;
    public string server_time;
    public int random_seed;
    public string hand;
};





    

[Serializable]
public class EventCount
{
    //add the stuff we care about
    public string event_type;
    public int count;

    public EventCount(string event_type, int count)
    {
        this.event_type = event_type;
        this.count = count;
    }
};

[Serializable]
public class TrackedObject
{
    public string name;
    public List<TrackedObjectFrame> frames = new List<TrackedObjectFrame>();
}

[Serializable]
public class StateObject
{
    public string name;
    public List<StateChangeFrame> frames = new List<StateChangeFrame>();
}

[Serializable]
public class Session
{
    public string name;
    public string start_server_time;
    public float duration;
    public int random_seed;
    


    public Dictionary<string, int> eventCountIndex = new Dictionary<string, int>();
    public List<EventCount> eventCount = new List<EventCount>();
    //public List<Frame> gazeObjectSequence = new List<Frame>();
    //private List<DataPackage> gazeDataPackages = new List<DataPackage>();

    public List<TrackedObject> trackedObjects = new List<TrackedObject>();
    public List<StateObject> stateObjects = new List<StateObject>();


    public Session(string name)
    {
        this.name = name.Replace("\"", "");
    }

    public void WriteToJSON(string prefix)
    {
        string filename = prefix + "/" +  name + ".json";
        string json = JsonUtility.ToJson(this);
        System.IO.File.WriteAllText(filename, json);
    }

    public void AddStateObjectData(StateChangeFrame stateChangeFrame, string stateObjectName)
    {
        //assume that packages come in order. Lets update the duration
        this.duration = stateChangeFrame.time;

        StateObject stateObject = null;
        foreach (StateObject s in stateObjects)
        {
            if (s.name == stateObjectName)
            {
                stateObject = s;
                break;
            }
        }

        if (stateObject == null)
        {
            stateObject = new StateObject();
            stateObject.name = stateObjectName;
            stateObjects.Add(stateObject);
        }

        stateObject.frames.Add(stateChangeFrame);
    }

    public void AddTrackedData(DataPackage dataPackage, string trackedObjectName)
    {
        //assume that packages come in order. Lets update the duration
        this.duration = dataPackage.time;

        //find the right object - note that this could be sped up
        TrackedObject trackedObject = null;
        foreach (TrackedObject t in trackedObjects)
        {
            if (t.name == trackedObjectName)
            {
                trackedObject = t;
                break;
            }
        }
        if (trackedObject == null)
        {
            trackedObject = new TrackedObject();
            trackedObject.name = trackedObjectName;
            trackedObjects.Add(trackedObject);
        }

        int numItems = dataPackage.data.Count;
        float prevTime = 0;
        if (trackedObject.frames.Count > 0)
        {
            prevTime = trackedObject.frames[trackedObject.frames.Count - 1].time;
        }

        float deltaTime = (float)(dataPackage.time - prevTime) / numItems;

        //make sure everything is good
        Debug.Assert(numItems > 0);
        Debug.Assert(deltaTime >= 0);

        for (int i = 0; i < numItems; i++)
        {
            TrackedObjectFrame frame = new TrackedObjectFrame();
            frame.time = prevTime + deltaTime * i;
            for (int ii = 0; ii < 3; ii++)
                frame.pos[ii] = dataPackage.data[i].pos[ii];
            for (int ii = 0; ii < 4; ii++)
                frame.rot[ii] = dataPackage.data[i].rot[ii];

            trackedObject.frames.Add(frame);
        }

    }

    //{
    //  //  Debug.Log($"add time: {gazeDataPackage.time}");

    //    float prevTime = 0;
    //    if (gazeDataPackages.Count > 0)
    //    {
    //        prevTime = gazeDataPackages[gazeDataPackages.Count - 1].time;
    //    }

    //    if (gazeDataPackage.time >= prevTime)
    //    {
    //        gazeDataPackages.Add(gazeDataPackage);
    //    }
    //    else
    //    {
    //        Debug.LogError($"out of order {gazeDataPackage.time} < {prevTime}");
    //        //find where the package should go
    //        int index = gazeDataPackages.Count - 2;
    //        for (; index >= 0; index--)
    //        {
    //            if (gazeDataPackage.time >= gazeDataPackages[index].time)
    //                  break;
    //        }
            
    //        gazeDataPackages.Insert(index + 1, gazeDataPackage);

    //        //lets do a quick check of this
    //        Debug.Log($"Inserted {gazeDataPackages[index].time} < " +
    //            $"{gazeDataPackages[index + 1].time}" + "<" + 
    //            $"{gazeDataPackages[index + 2].time}");
    //    }
    //}

    //public void Process()
    //{
    //    for (int i = 0; i < gazeDataPackages.Count; i++)
    //        ProcessGazeData(gazeDataPackages[i]);
    //}

    //public void ProcessGazeData(DataPackage gazeDataPackage)
    //{ 
    //    int numItems = gazeDataPackage.data.Count;
    //    float prevTime = 0;
    //    if (gazeObjectSequence.Count > 0)
    //    {
    //        prevTime = gazeObjectSequence[gazeObjectSequence.Count - 1].time;
    //    }

    //    float deltaTime = (float)(gazeDataPackage.time -prevTime) / numItems;

    //    //make sure everything is good
    //    Debug.Assert(numItems > 0);
    //    Debug.Assert(deltaTime >= 0);

    //    for (int i=0; i < numItems; i++)
    //    {
    //        Frame frame = new Frame();
    //        frame.time = prevTime + deltaTime*i;
    //        for (int ii=0; ii<3; ii++)
    //            frame.pos[ii] = gazeDataPackage.data[i].pos[ii];
    //        for (int ii = 0; ii < 4; ii++)
    //            frame.rot[ii] = gazeDataPackage.data[i].rot[ii];

    //        gazeObjectSequence.Add(frame);
    //    }
    //}
}


public class OGDParser : MonoBehaviour
{
    static string inputFile, outputPath;
    static Dictionary<string, int> dataColumnTypes = new Dictionary<string, int>();
    static Dictionary<string, int> sessionItems = new Dictionary<string, int>();
    static Dictionary<string, Session> sessionEvents = new Dictionary<string, Session>();

    [MenuItem("OGDReplay/Log to Batch")]
    static void ReplaysFromLogMenu()
    {
        OGDReplay replay = Selection.activeTransform.gameObject.GetComponent<OGDReplay>();
        if (replay == null)
        {
            EditorUtility.DisplayDialog("Select OGD Replay Object", "You must select a OGD Replay Object object in your project to use this function", "OK");
            return;
        }

        BatchProcessingWindow window = EditorWindow.GetWindow<BatchProcessingWindow>("Batch Processing:"); ;

        inputFile = EditorUtility.OpenFilePanel("Log file", "", "tsv");
        outputPath = EditorUtility.OpenFolderPanel("OutputFolder", Path.GetDirectoryName(inputFile), "");
        if ((inputFile.Length != null) && (outputPath != null))
        {
            Task.Run(Parse);

        }

        window.LoadFiles(outputPath);
    }

    [MenuItem("OGDReplay/Single Step/Parse Log File to JSON")]
    static void ParseMenu()
    {

        inputFile = EditorUtility.OpenFilePanel("Log file", "", "tsv");
        outputPath = EditorUtility.OpenFolderPanel("OutputFolder", Path.GetDirectoryName(inputFile), "");
        if ((inputFile.Length != null) && (outputPath != null))
        {
            Task.Run(Parse);

        }
    }



    static void SetHeaders(string line, bool debug = true)
    {
        char[] seperators = { '\t' };
        string[] items = line.Split(seperators);

        dataColumnTypes.Clear();

        for (int i = 0; i < items.Length; i++)
        {
            dataColumnTypes.Add(items[i], i);
        }

        //for debug
        if (debug)
            foreach (string dt in dataColumnTypes.Keys)
                Debug.Log($"found: {dt} at {dataColumnTypes[dt]}");
    }



    static DataPackage jsonArraytoDataPackage(string eventDataArray)
    {
        DataPackage dataPackage = new DataPackage();
        string jsonString = "{\"data\":" + eventDataArray + "}";
        try
        {
            //sequence = JsonUtility.FromJson<Sequence>(jsonString);
            dataPackage = JsonUtility.FromJson<DataPackage>(jsonString);
            //Debug.Log(gazeDataPackage.data.Count);
            //now we set the time for this package
 


        }
        catch (ArgumentException ex) // JsonUtility often throws ArgumentException for format issues
        {
            Debug.LogError($"JSON Parsing Failed (ArgumentException): {ex.Message}");
            Debug.LogError($"Problematic JSON: {jsonString}");
        }
        catch (Exception ex) // Catch any other unexpected exceptions
        {
            Debug.LogError($"JSON Parsing Failed (General Exception): {ex.Message}");
            Debug.LogError($"Problematic JSON: {jsonString}");
        }
        return dataPackage;
    }

    static void ParseLineForEvents(string line, bool debug = true)
    {
        char[] seperators = { '\t' };
        string[] items = line.Split(seperators);

        //lets see what session this is
        string session_id = items[dataColumnTypes["session_id"]];

        Session session = sessionEvents[session_id];


        //now get the game state information
        GameState gs = JsonUtility.FromJson<GameState>(items[dataColumnTypes["game_state"]]);
        //Debug.Log(gs.seconds_from_launch);

        //now switch out the event
        string event_id = items[dataColumnTypes["event_name"]]; 
        
        //add this event to the counter
        if (session.eventCountIndex.ContainsKey(event_id))
        {
            int index = session.eventCountIndex[event_id];
            session.eventCount[index].count++;
        }
        else
        {
            session.eventCountIndex.Add(event_id, session.eventCount.Count);
            session.eventCount.Add(new EventCount(event_id, 1));
        }
        //lets see what type of event this is
        if (event_id == "\"session_start\"")
        {
            string data = items[dataColumnTypes["event_data"]];
            EventData eventData = JsonUtility.FromJson<EventData>(data);

            //set the session values
            session.start_server_time = eventData.server_time;
            session.random_seed = eventData.random_seed;

        }
        else if (event_id == "\"viewport_data\"")
        {
            string data = items[dataColumnTypes["event_data"]];
            EventData eventData = JsonUtility.FromJson<EventData>(data);

            DataPackage dataPackage = jsonArraytoDataPackage(eventData.gaze_data_package);
            dataPackage.time = gs.seconds_from_launch;
            session.AddTrackedData(dataPackage, "viewport");
        }
        else if (event_id == "\"left_hand_data\"")
        {
            string data = items[dataColumnTypes["event_data"]];
            EventData eventData = JsonUtility.FromJson<EventData>(data);

            DataPackage dataPackage = jsonArraytoDataPackage(eventData.left_hand_data_package);
            dataPackage.time = gs.seconds_from_launch;
            session.AddTrackedData(dataPackage, "left_hand");
        }
        else if (event_id == "\"right_hand_data\"")
        {
            string data = items[dataColumnTypes["event_data"]];

            EventData eventData = JsonUtility.FromJson<EventData>(data);

            DataPackage dataPackage = jsonArraytoDataPackage(eventData.right_hand_data_package);
            dataPackage.time = gs.seconds_from_launch;
            session.AddTrackedData(dataPackage, "right_hand");
        }
        else if (event_id == "\"grab_gesture\"")
        {
            string data = items[dataColumnTypes["event_data"]];
            EventData eventData = JsonUtility.FromJson<EventData>(data);
            StateChangeFrame stateChangeFrame = new StateChangeFrame();
            stateChangeFrame.time = gs.seconds_from_launch;
            stateChangeFrame.state = true;
            string eventName = eventData.hand + "_Grip";
            session.AddStateObjectData(stateChangeFrame, eventName);
                     //  Debug.Log(eventData.hand);
        }
        else if (event_id == "\"grab_release\"")
        {
            string data = items[dataColumnTypes["event_data"]];
            EventData eventData = JsonUtility.FromJson<EventData>(data);
            StateChangeFrame stateChangeFrame = new StateChangeFrame();
            stateChangeFrame.time = gs.seconds_from_launch;
            stateChangeFrame.state = false;
            string eventName = eventData.hand + "_Grip";
            session.AddStateObjectData(stateChangeFrame, eventName);


            // Debug.Log(data);
        }
        //else if (event_id.Contains("release"))
        //{
        //    Debug.Log("release with " + event_id);
        //    //assume we are releasing an object. So undo grip
        //    string data = items[dataColumnTypes["event_data"]];
        //    EventData eventData = JsonUtility.FromJson<EventData>(data);
        //    StateChangeFrame stateChangeFrame = new StateChangeFrame();
        //    stateChangeFrame.time = gs.seconds_from_launch;
        //    stateChangeFrame.state = false;
        //    string eventName = eventData.hand + "_Grip";
        //    session.AddStateObjectData(stateChangeFrame, eventName);
        //}

        //should we close out this session
        sessionItems[session_id]--;
        if (sessionItems[session_id] <= 0)
        {
            Debug.Log($"Close session {session_id}");
            //session.Process();
            session.WriteToJSON(outputPath);
        }

    }



    static void ParseLineForSessions(string line, bool debug = true)
    {
        char[] seperators = { '\t' };
        string[] items = line.Split(seperators);


        string session_id = items[dataColumnTypes["session_id"]];

        if (sessionItems.ContainsKey(session_id))
        {
            sessionItems[session_id]++;
        }
        else
        {
            sessionItems.Add(session_id, 1);
        }

       

        // Debug.Log(session);
    }


    public static void Parse()
    {
       
        Debug.Log("Parse: " + inputFile + " to " + outputPath);
        ProgressReport pr = new ProgressReport("Detect sessions");

        //clear out old stuff
        sessionItems.Clear();

        using (StreamReader sr = File.OpenText(inputFile))
        {
            string line;
            int lineNumber = 0;
            while ((line = sr.ReadLine()) != null)
            {

//                Debug.Log((float)(sr.BaseStream.Position) / sr.BaseStream.Length);

                if (lineNumber == 0)
                {
                    SetHeaders(line);

                }
                else
                {
                    ParseLineForSessions(line);
                }


                lineNumber++;

                pr.Report((float) (sr.BaseStream.Position) / sr.BaseStream.Length);

            }
        }
        pr.End();

        sessionEvents.Clear();
        foreach (string s in sessionItems.Keys)
        {
            Debug.Log($"found: {s} with {sessionItems[s]} items");
            sessionEvents.Add(s, new Session(s));
        }
            

     


        pr = new ProgressReport("Parse " + sessionItems.Count + " sessions");

        using (StreamReader sr = File.OpenText(inputFile))
        {
            string line;
            int lineNumber = 0;
            while ((line = sr.ReadLine()) != null)
            {


                if (lineNumber == 0)
                {
                  //  SetHeaders(line);

                }
                else
                {
                    ParseLineForEvents(line);
                }


                lineNumber++;

                pr.Report((float)(sr.BaseStream.Position) / sr.BaseStream.Length);

            }
        }

        pr.End();


        //TODO: move this later?
        //pr = new ProgressReport("Export " + sessionEvents.Count + " sessions");

        //foreach (Session session in sessionEvents.Values)
        //{
        //    session.Process();
        //    session.WriteToJSON("test/");
        //}

        //pr.End();
    }
}
