using UnityEngine;
using System.Collections.Concurrent;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
using System;
using UnityEngine.Rendering;
#endif

public class H264Decoder : MonoBehaviour
{
    public Texture2D OutputTexture { get; private set; }
    private ConcurrentQueue<byte[]> nalQueue = new();

#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject mediaCodec;
    private byte[] buffer;
#endif

    void Awake()
    {
        // Initialize a 720p texture
        OutputTexture = new Texture2D(1280, 720, TextureFormat.RGBA32, false);

#if UNITY_ANDROID && !UNITY_EDITOR
        InitializeMediaCodec();
#endif
    }

    public void EnqueueNAL(byte[] nal)
    {
        nalQueue.Enqueue(nal);
    }

    void Update()
    {
        while (nalQueue.TryDequeue(out var nal))
        {
            Decode(nal);
        }
    }

    void Decode(byte[] nal)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Send NAL to MediaCodec for decoding
        if (mediaCodec != null)
        {
            // Example: send nal to MediaCodec input buffer
            mediaCodec.Call("queueInputBuffer", nal);
            // output is written into OutputTexture via SurfaceTexture
        }
#else
        // In Editor or non-Android, we can't decode; optionally simulate
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private void InitializeMediaCodec()
    {
        using var codecClass = new AndroidJavaClass("android.media.MediaCodec");
        mediaCodec = codecClass.CallStatic<AndroidJavaObject>("createDecoderByType", "video/avc");

        // Configure MediaCodec with width, height, format, and SurfaceTexture
        // Native plugin needed to write decoded frames to OutputTexture
    }
#endif
}
