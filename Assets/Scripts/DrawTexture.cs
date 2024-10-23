using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class DrawTexture : MonoBehaviour
{
    private RenderTexture cellView;
    private ComputeBuffer aliveCells;
    private RenderTexture aliveTexture;
    public ComputeShader bufferToTexture;
    private GeneSheepManager geneSheepManager;
    private int bufferToTextureKernel;
    private int width;
    private int height;
    public int mode = 0;
    public bool reInit = false;
    void Start() {
        // texture = Camera.main.GetComponent<GeneSheepManager>().prevAwake;
        geneSheepManager = Camera.main.GetComponent<GeneSheepManager>();
    }

    void Init() {
        cellView = geneSheepManager.prevRenderTex;
        aliveCells = geneSheepManager.sleeping;

        width = geneSheepManager.initWidth;
        height = geneSheepManager.initWidth;

        aliveTexture = new RenderTexture(width, height, 24);
        aliveTexture.enableRandomWrite = true;
        aliveTexture.filterMode = FilterMode.Point;

        bufferToTextureKernel = bufferToTexture.FindKernel("BufferToTexture");
        bufferToTexture.SetTexture(bufferToTextureKernel, "Result", aliveTexture);
        bufferToTexture.SetInt("Width", width);
        bufferToTexture.SetBuffer(bufferToTextureKernel, "Alive", aliveCells);
    }
    private void OnRenderImage(RenderTexture source, RenderTexture destination) 
    {
        if (reInit) {
            Init();
            reInit = false;
        }
        // replace the camera drawing with the color texture
        if (mode == 0) {
            Graphics.Blit(cellView, destination);
        } else if (mode == 1) {
            bufferToTexture.Dispatch(bufferToTextureKernel, width / 8, height / 8, 1);
            Graphics.Blit(aliveTexture, destination);
        }
        
    }
}
