using System;
using System.IO;
using UnityEngine;

public static class WavUtility
{
    public static byte[] FromAudioClip(AudioClip clip) {
        using (MemoryStream stream = new MemoryStream()) {
            // Reserve space for header
            int headerSize = 44;
            stream.Position = headerSize;

            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            short[] intData = new short[samples.Length];
            byte[] bytesData = new byte[samples.Length * 2];
            float rescaleFactor = 32767; // float [-1,1] -> short

            for (int i = 0; i < samples.Length; i++) {
                intData[i] = (short)(samples[i] * rescaleFactor);
                byte[] byteArr = BitConverter.GetBytes(intData[i]);
                byteArr.CopyTo(bytesData, i * 2);
            }

            stream.Write(bytesData, 0, bytesData.Length);

            // Write WAV header
            stream.Position = 0;
            byte[] header = WriteWavHeader(clip, bytesData.Length);
            stream.Write(header, 0, header.Length);

            return stream.ToArray();
        }
    }

    private static byte[] WriteWavHeader(AudioClip clip, int dataLength) {
        MemoryStream header = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(header);

        int hz = clip.frequency;
        short channels = (short)clip.channels;

        writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + dataLength);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1); // PCM
        writer.Write(channels);
        writer.Write(hz);
        writer.Write(hz * channels * 2); // byte rate
        writer.Write((short)(channels * 2)); // block align
        writer.Write((short)16); // bits per sample
        writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        writer.Write(dataLength);

        writer.Flush();
        return header.ToArray();
    }
}
