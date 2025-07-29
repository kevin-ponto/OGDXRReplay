using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

using UnityEditor;
using Unity.Burst;


using System;



using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;


using Unity.Collections;


public class OGDReplay : MonoBehaviour
{
    public string replayFile;
    public Session session;

    public float playbackTime;
    bool quitOnCompletion = true;

    public bool isPlaying = true;

    //for recording to video
    public bool recordVideo = true; 
    public ScreenRecorder screenRecorder;

    [Serializable]
    public class ScreenRecorder
    {
        public Vector2Int videoDim = new Vector2Int(1920, 1080);
        
        public CoreEncoderSettings.OutputCodec codec = CoreEncoderSettings.OutputCodec.MP4;
        public CoreEncoderSettings.VideoEncodingQuality quality = CoreEncoderSettings.VideoEncodingQuality.High;
        public float frameRate = 60f;

        public bool recordAudio = true;
        public bool captureAlpha = true;

        

        RecorderController recorderController;

        public void Init(string name, string path="")
        {
            var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            recorderController = new RecorderController(controllerSettings);

            MovieRecorderSettings settings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            settings.name = name;
            settings.Enabled = true;
            
            settings.EncoderSettings = new CoreEncoderSettings
            {
                EncodingQuality = quality,
                Codec = codec
            };
            settings.ImageInputSettings = new GameViewInputSettings
            {
                OutputWidth = videoDim[0],
                OutputHeight = videoDim[1]
            };

            string dir = Path.GetDirectoryName(name);
            string file = Path.GetFileNameWithoutExtension(name);
            string outfile = dir + "/" + file;
            Debug.Log(dir + "/" + file);
            settings.OutputFile = outfile;

            // Setup Recording
            controllerSettings.AddRecorderSettings(settings);
            controllerSettings.SetRecordModeToManual();
            controllerSettings.FrameRate = frameRate;

            RecorderOptions.VerboseMode = false;
            recorderController.PrepareRecording();
            

            Debug.Log($"Started recording for file {settings.OutputFile}");
        }

        public void Start()
        {
            recorderController.StartRecording();
        }

        public void Stop()
        {
            recorderController.StopRecording();
        }

        
    };

    [Serializable]
    public struct OGDFrame
    {
        public float time;
        public Vector3 pos;
        public Quaternion rot;

        public OGDFrame(OGDFrame frame)
        {
            this.time = frame.time;
            this.pos = frame.pos;
            this.rot = frame.rot;
        }
    };

    //make an optimized frame struct
    [BurstCompile]
    public struct OPFrame
    {
        public float time;
        public Vector3 pos;
        public Quaternion rot;

        public OPFrame(OGDFrame frame)
        {
            this.time = frame.time;
            this.pos = frame.pos;
            this.rot = frame.rot;
        }
    };




    //public class FrameSequence
    //{
    //    public NativeArray<Frame> frameArray = new NativeArray<Frame>(;
    //}

    //public class PlaybackManager
    //{
    //    public List<FrameSequence> frameSequences = new List<FrameSequence>();
    //    public int maxFramesPerSequence = 1024;
    //    public void AddFrames(List<Frame> frames)
    //    {
    //        FrameSequence sequence = new FrameSequence();
    //        for (int )

    //    }
    //}


    [Serializable]
    public class TrackedObject
    {
        public string name;
        public GameObject gameObject = null;
        public List<OGDFrame> frames = new List<OGDFrame>();

        int frameBatchSize = 1024;
        public List<NativeArray<OPFrame>> frameBatchArray = new List<NativeArray<OPFrame>>();
        //we will keep track of our previous frame
        OPFrame currFrame;
        OPFrame nextFrame;
        int frameIndex = 0;
        int frameCount = 0;
        float frameDeltaTime = 1;

        [BurstCompile]
        OPFrame getNextFrame(int index)
        {
            index = Math.Min(frameCount - 1, index);

            int batchIndex = index / frameBatchSize;
            int frameIndex = index % frameBatchSize;

//            Debug.Log(batchIndex + " " + frameIndex);

            return frameBatchArray[batchIndex][frameIndex];
        }



        public void Set(float time)
        {

//            Debug.Log($"{time} > {nextFrame.time}");

            if (time > nextFrame.time)
            {
                frameIndex++;
                currFrame = nextFrame; // frameArray[frameIndex];
                nextFrame = getNextFrame(frameIndex);
                frameDeltaTime = nextFrame.time - currFrame.time;
            }

            ////find the right frame
            //for (int i = prevFrame; i < frames.Count; i++)
            //{
            //    if (time <= frames[i].time)
            //    {
            //        break;
            //    }
            //    else
            //    {
            //        prevFrame = i;
            //    }
            //}

           // int nextFrame = Math.Min(prevFrame + 1, frames.Count - 1);


            float lerp = (float)(time - currFrame.time) / frameDeltaTime;
            gameObject.transform.position = Vector3.Lerp(currFrame.pos, nextFrame.pos, lerp);
            gameObject.transform.rotation = Quaternion.Lerp(currFrame.rot, nextFrame.rot, lerp);

        }

        public void Optimize()
        {

            //frameArray.Dispose();
            frameBatchArray.Clear();
            for (int index=0; index < frames.Count; index+= frameBatchSize)
            {
                int batchSize = Math.Min(frames.Count - index, frameBatchSize);
                NativeArray<OPFrame> frameArray = new NativeArray<OPFrame>(frameBatchSize, Allocator.Persistent);
                for (int i = 0; i < batchSize; i++)
                {
                    frameArray[i] = new OPFrame(frames[index + i]);
                }
                frameBatchArray.Add(frameArray);


            }

            Debug.Log("fr " + frameBatchArray.Count + " = " +(frames.Count / frameBatchSize));

            //if (currFrame == null)
            currFrame = frameBatchArray[0][0];
            // if (nextFrame == null)
            nextFrame = frameBatchArray[0][1];

            //set our max frame count
            frameCount = frames.Count;

            frames.Clear();
        }

        public void Dispose()
        {
            foreach (NativeArray<OPFrame> frameArray in frameBatchArray)
                frameArray.Dispose();
        }
    }

    [Serializable]
    public class Session
    {
        public string name;
        public string start_server_time;
        public float duration;
        public int random_seed;

        public List<EventCount> eventCount = new List<EventCount>();
       
        public List<TrackedObject> trackedObjects = new List<TrackedObject>();

        public void Optimize()
        {
            foreach (TrackedObject trackedObject in trackedObjects)
                trackedObject.Optimize();
        }
        public void Dispose()
        {
            foreach (TrackedObject trackedObject in trackedObjects)
                trackedObject.Dispose();
        }
    }



    [MenuItem("OGDReplay/Single Step/Load Replay File")]
    static void LoadReplayMenu()
    {
        OGDReplay replay = Selection.activeTransform.gameObject.GetComponent<OGDReplay>();
        if (replay == null)
        {
            EditorUtility.DisplayDialog("Select OGD Replay Object", "You must select a OGD Replay Object object in your project to use this function", "OK");
            return;
        }

        replay.replayFile = EditorUtility.OpenFilePanel("Replay file", "", "json");
        if (replay.replayFile.Length != 0)
        {
            replay.LoadReplayFile();
        }
    }

    public void LoadReplayFile()
    {
        
        List<GameObject> prevGameObjs = new List<GameObject>();

        //store the old refs
        foreach (TrackedObject t in session.trackedObjects)
            prevGameObjs.Add(t.gameObject);

        string json = File.ReadAllText(replayFile);
        this.session = JsonUtility.FromJson<Session>(json);

        //now reset the refs
        for (int i = 0; i < session.trackedObjects.Count; i++)
        {
            session.trackedObjects[i].gameObject = prevGameObjs[i];

            //if we don't have nay references, lets check for tags
            if (session.trackedObjects[i].gameObject == null)
            {
                GameObject refObject = GameObject.FindGameObjectWithTag(session.trackedObjects[i].name);
                if (refObject != null)
                    session.trackedObjects[i].gameObject = refObject;
            }
        }



    }


    //void ParseCommandLineArguments()
    //{
    //    string[] args = System.Environment.GetCommandLineArgs();

    //    // The first argument is usually the path to the executable itself
    //    // so we start parsing from the second argument.
    //    for (int i = 1; i < args.Length; i++)
    //    {
    //        if (args[i] == "-i")
    //        {
    //            i++;
    //            replayFile = args[i];
    //        }
    //    }
    //}


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playbackTime = 0;

        //set the random seed
        UnityEngine.Random.InitState(session.random_seed);

        //do we have references for everything?
        for (int i = 0; i < session.trackedObjects.Count; i++)
        {
           //if we don't have nay references, lets check for tags
            if (session.trackedObjects[i].gameObject == null)
            {
                GameObject refObject = GameObject.FindGameObjectWithTag(session.trackedObjects[i].name);
                if (refObject != null)
                    session.trackedObjects[i].gameObject = refObject;
            }
        }

    }

    public float Progress()
    {
        return (playbackTime / session.duration);
    }

    // Update is called once per frame
    void Update()
    {
        if (isPlaying)
        {
            playbackTime += Time.deltaTime;

        }
        foreach(TrackedObject trackedObject in session.trackedObjects)
        {
            if (trackedObject.gameObject != null)
                trackedObject.Set(playbackTime);
        }
        if ((quitOnCompletion) && (playbackTime >= session.duration))
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
        //}
    }

    //for screen recording
    void OnEnable()
    {
        LoadReplayFile();
        session.Optimize();
        if (recordVideo)
        {
            screenRecorder.Init(replayFile);
            screenRecorder.Start();
        }
    }
    void OnDisable()
    {
        if (recordVideo)
        {
            screenRecorder.Stop();
        }
        session.Dispose();
    }
}
