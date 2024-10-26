using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class GeneSheepManager : MonoBehaviour
{

    public int initWidth;
    public int initHeight;
    private int width;
    private int height;
    public int numSpecies = 4;
    public int timeStepsPerFrame;
    public int iterations = 0;
    public ComputeShader geneSheep;
    public ComputeShader geneSheepRandomizer;
    public ComputeShader geneSheepSetConstant;
    public ComputeShader setBufferConstant;
    public ComputeShader orAndSet;
    private int setConstantKernel;
    private int bufferConstantKernel;
    private int kernel;
    private int randomizerKernel;
    private int orAndSetKernel;
    public RenderTexture prevRenderTex;
    public RenderTexture nextRenderTex;
    public ComputeBuffer sleeping;
    private ComputeBuffer willWake;
    
    public RenderTexture colorTexture;
    private Vector4 curColor;
    public int numChanges;
    public int numSleeping;
    private ComputeBuffer numChangesBuffer;
    private ComputeBuffer numSleepingBuffer;
    public Camera secondaryCamera;
    public bool isDone = false;
    public bool autoRestart = true;
    public bool queuedSave = false;
    public bool queuedRestart = false;
    public bool randomStartColor = false;
    public float pixelChangeScale = 0.0001f;
    public int updateRule = 0;
    public Color startColor = Color.black;
    bool skip = true;

    private void OnRenderImage(RenderTexture source, RenderTexture destination) 
    {
        // replace the camera drawing with the color texture
        Graphics.Blit(colorTexture, destination);
    }

    private RenderTexture MakeBlankRenderTexture(bool extraData = false) {
        // create a blank render texture with all the properties we need
        RenderTexture output;
        if (extraData) {
            output = new RenderTexture(width, height, 24, RenderTextureFormat.ARGBFloat);
        } else {
            output = new RenderTexture(width, height, 24);
        }
        output.wrapMode = TextureWrapMode.Repeat;
        output.filterMode = FilterMode.Point;
        output.enableRandomWrite = true;
        output.useMipMap = false;
        output.Create();
        return output;
    }

    void Start () {

        // get a random color for the color texture
        numChangesBuffer = new ComputeBuffer(1, sizeof(int));
        numSleepingBuffer = new ComputeBuffer(1, sizeof(int));
        bufferConstantKernel = setBufferConstant.FindKernel("SetBufferConstant");
        kernel = geneSheep.FindKernel("GeneSheep");

        orAndSetKernel = orAndSet.FindKernel("OrAndSet");
        orAndSet.SetInt("Val", 0);
        Restart();

        
    }
	
	// Update is called once per frame
	void Update ()
	{   
        if (skip) {
            skip = false;
            return;
        }

        if (!isDone) {
            // each update comes with some overhead, so it's faster to do a bunch of time steps at once
            for (int i = 0; i < timeStepsPerFrame - 1; i++) {
                DoTimeStep();
            }
            DoTimeStep(true);
            if (numChanges == 0) {
                isDone = true;
                SaveToFile();
            } else if (queuedSave) {
                queuedSave = false;
                SaveToFile();
            }

            
        }

        if (queuedRestart) {
            queuedRestart = false;
            Restart();
        }

        if (isDone && autoRestart) {
            Restart();
        }

	}

    void Restart() {
        isDone = false;
        if (randomStartColor) {
            curColor = new Vector4( UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), 1);
        } else {
            curColor = startColor;
        }

        width = initWidth;
        height = initHeight;
        sleeping = new ComputeBuffer(width * height, sizeof(uint));
        willWake = new ComputeBuffer(width * height, sizeof(uint));
        setBufferConstant.SetInt("Val", 1);
        setBufferConstant.SetInt("Width", width);
        setBufferConstant.SetBuffer(bufferConstantKernel, "Result", sleeping);
        setBufferConstant.Dispatch(bufferConstantKernel, width / 8, height / 8, 1);
        setBufferConstant.SetBuffer(bufferConstantKernel, "Result", willWake);
        setBufferConstant.Dispatch(bufferConstantKernel, width / 8, height / 8, 1);
        orAndSet.SetInt("Width", width);
        orAndSet.SetBuffer(orAndSetKernel, "Source", willWake);
        orAndSet.SetBuffer(orAndSetKernel, "Target", sleeping);



        // initialize prevRenderTex with random species
        prevRenderTex = MakeBlankRenderTexture();
        randomizerKernel = geneSheepRandomizer.FindKernel("GeneSheepRandomizer");
        geneSheepRandomizer.SetFloat("RngOffset", UnityEngine.Random.Range(0.0f, 1.0f));
        geneSheepRandomizer.SetInt("NumSpecies", numSpecies);
        geneSheepRandomizer.SetTexture(randomizerKernel, "Result", prevRenderTex);
        geneSheepRandomizer.SetBuffer(randomizerKernel, "WasModified", willWake);
        geneSheepRandomizer.Dispatch(randomizerKernel, width / 8, height / 8, 1);

        // initialize nextRenderTex
        nextRenderTex = MakeBlankRenderTexture();

        // initialize colorTexture (the visible output) with a random or custom color
        colorTexture = MakeBlankRenderTexture(true);
        geneSheepSetConstant.SetVector("Color", curColor);
        // geneSheepSetConstant.SetVector("Color", curColor);
        setConstantKernel = geneSheepSetConstant.FindKernel("GeneSheepSetConstant");
        geneSheepSetConstant.SetTexture(kernel, "Result", colorTexture);
        geneSheepSetConstant.Dispatch(setConstantKernel, width / 8, height / 8, 1);

        // set some constant parameters that we won't need to change
        geneSheep.SetInt("UpdateRule", updateRule);
        geneSheep.SetFloat("Width", width);
        geneSheep.SetFloat("Height", height);
        geneSheep.SetFloat("PixelChangeScale", pixelChangeScale);
        geneSheep.SetVector("CurColor", curColor);
        geneSheep.SetTexture(kernel, "Colors", colorTexture);
        geneSheep.SetBuffer(kernel, "NumChanges", numChangesBuffer);
        geneSheep.SetBuffer(kernel, "NumSleeping", numSleepingBuffer);
        geneSheep.SetBuffer(kernel, "Sleeping", sleeping);
        geneSheep.SetBuffer(kernel, "WillWake", willWake);

        secondaryCamera.GetComponent<DrawTexture>().reInit = true;
    }

    void SaveToFile() {
        RenderTexture.active = colorTexture;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        byte[] pngBytes = tex.EncodeToPNG();
        var dirPath = Application.dataPath + "/../SavedImages/";
        if(!System.IO.Directory.Exists(dirPath)) {
            System.IO.Directory.CreateDirectory(dirPath);
        }
        string dateTimeStr = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        System.IO.File.WriteAllBytes(dirPath + "Image" + dateTimeStr + ".png", pngBytes);
    }

    void DoTimeStep(bool getNumChanges=false) {
        randomStepCurrentColors();

        if (getNumChanges) {
            // getting the number of changes requires reading gpu data back to the cpu, which is slow
            // thus, we want to do this at most once per frame
            numChangesBuffer.SetData( new int[] {0} );
            numSleepingBuffer.SetData( new int[] {0} );
        }
        

    
        // set the rng, color, and texture parameters, then dispatch to run a timestep
        geneSheep.SetTexture(kernel, "Input", prevRenderTex);
        geneSheep.SetTexture(kernel, "Result", nextRenderTex);
        geneSheep.SetFloat("RngOffset", UnityEngine.Random.Range(0.0f, 1.0f));
        geneSheep.SetVector("curColor", curColor);
        geneSheep.Dispatch(kernel, width / 8, height / 8, 1);

        // updates alive cells by waking all cells adjecent to an updated cell
        // (and then resetting the `updated` flag to 0)
        orAndSet.Dispatch(orAndSetKernel, width / 8, height / 8, 1);


        if (getNumChanges) {
            int[] numChangesStorage = {0};
            int[] numSleepingStorage = {0};
            numChangesBuffer.GetData(numChangesStorage);
            numSleepingBuffer.GetData(numSleepingStorage);
            numChanges = numChangesStorage[0];
            numSleeping = numSleepingStorage[0];
        }
        
        
        // switch the previous and next textures
        // this prevents needing to initialize a blank texture each time, which is expensive
        (prevRenderTex, nextRenderTex) = (nextRenderTex, prevRenderTex);

        
        iterations++;
    }

    void randomStepCurrentColors() {
        // randomly tweak the current color
        float colorChangeRange = pixelChangeScale * 10;
        curColor[0] = Mathf.Clamp(curColor[0] + UnityEngine.Random.Range(-colorChangeRange, colorChangeRange), 0f, 1f);
        curColor[1] = Mathf.Clamp(curColor[1] + UnityEngine.Random.Range(-colorChangeRange, colorChangeRange), 0f, 1f);
        curColor[2] = Mathf.Clamp(curColor[2] + UnityEngine.Random.Range(-colorChangeRange, colorChangeRange), 0f, 1f);
    }
}
