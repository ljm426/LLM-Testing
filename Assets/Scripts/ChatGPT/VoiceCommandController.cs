using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// We are now using Whisper locally via the whisper.unity package
using Whisper;

namespace ChatGPT
{
    [RequireComponent(typeof(AudioListener))]
    public class VoiceCommandController : MonoBehaviour
    {
        [Header("Whisper")]
        [SerializeField] private WhisperManager whisperManager;

        [Header("Input")]
        [SerializeField] private Key recordKey = Key.V; // Hold to record, release to submit

        [Header("Recording Settings")]
        [SerializeField] private int sampleRate = 16000; // Whisper works well with 16kHz
        [SerializeField] private int maxRecordSeconds = 10;
        [SerializeField] private float minRecordSeconds = 0.25f;
        [SerializeField] private bool autoSelectDefaultMic = true;
        [SerializeField] private string microphoneDevice = null; // null = default

        // Keep a rolling mic buffer alive
        [SerializeField] private int micLoopSeconds = 30;
        // Small pre-roll to avoid chopping first syllable (in seconds)
        private float preRollSeconds = 0.5f;

        [Header("References")]
        [SerializeField] private AgentCommandInterpreter interpreter;

        private AudioClip recordingClip;
        private bool isRecording;
        private double recordStartDspTime;

        // Fields for continuous mic
        private AudioClip micClip;
        private bool micReady;
        private int recordingStartPosition = -1;

        private void Awake()
        {
            if (interpreter == null)
                interpreter = FindFirstObjectByType<AgentCommandInterpreter>();
            if (whisperManager == null)
                whisperManager = FindFirstObjectByType<WhisperManager>();
        }

        private void OnEnable()
        {
            // Start continuous mic
            StartCoroutine(InitMicLoop());
        }

        private void OnDisable()
        {
            // Clean up continous mic
            if (!string.IsNullOrEmpty(microphoneDevice) || Microphone.devices.Length > 0)
            {
                Microphone.End(microphoneDevice);
            }
            micClip = null;
            micReady = false;
        }

        private IEnumerator InitMicLoop()
        {
            var devices = Microphone.devices;
            if (devices == null || devices.Length == 0)
            {
                Debug.LogWarning("No microphone devices found.");
                yield break;
            }

            if (autoSelectDefaultMic) microphoneDevice = null;

            // Start a looped mic clip to act as a rolling buffer
            micClip = Microphone.Start(microphoneDevice, true, micLoopSeconds, sampleRate);

            // Wait until the mic is actually providing samples
            int safety = 0;
            while (Microphone.GetPosition(microphoneDevice) <= 0 && safety++ < 200)
                yield return null;

            if (Microphone.GetPosition(microphoneDevice) <= 0)
            {
                Debug.LogWarning("Microphone did not start in time.");
                yield break;
            }

            micReady = true;
            Debug.Log("Voice: Mic ready (rolling buffer).");

            sampleRate = micClip.frequency; // Actual sample rate
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            // Start recording on press
            if (kb[recordKey].wasPressedThisFrame)
            {
                StartRecording();
            }
            // Stop and submit on release
            if (isRecording && kb[recordKey].wasReleasedThisFrame)
            {
                StopAndSubmit();
            }
        }

        private void StartRecording()
        {
            if (!micReady || micClip == null)
            {
                Debug.LogWarning("Voice: Mic not ready yet.");
                return;
            }

            // Mark the start position at the moment of the press (no pre-roll captured)
            int currentPos = Microphone.GetPosition(microphoneDevice);
            int totalFrames = micClip.samples;
            // Convert pre-roll seconds to frames (per channel frame count)
            int preRollFrames = Mathf.Clamp(Mathf.RoundToInt(preRollSeconds * sampleRate), 0, Mathf.Max(0, totalFrames - 1));
            // Move start backwards slightly (wrap-around safe)
            recordingStartPosition = currentPos - preRollFrames;
            if (recordingStartPosition < 0)
            {
                recordingStartPosition += totalFrames;
            }
            recordStartDspTime = AudioSettings.dspTime;
            isRecording = true;
            Debug.Log("Voice: Recording started ...");
        }

        private void StopAndSubmit()
        {
            if (!isRecording) return;
            isRecording = false;

            if (!micReady || micClip == null || recordingStartPosition < 0)
            {
                Debug.LogWarning("Voice: Mic not ready or invalid start position.");
                return;
            }

            // Calculate the length of the recording
            int stopPos = Microphone.GetPosition(microphoneDevice);
            int channels = micClip.channels;
            int totalFrames = micClip.samples;

            // Compute number of frames between start and stop with wrap-around
            int framesToCopy = stopPos >= recordingStartPosition
                ? (stopPos - recordingStartPosition)
                : (totalFrames - recordingStartPosition + stopPos);

            // Cap to maxRecordSeconds
            int maxFrames = Mathf.RoundToInt(maxRecordSeconds * sampleRate);
            framesToCopy = Mathf.Min(framesToCopy, maxFrames);

            float durationSec = (float)framesToCopy / sampleRate;
            if (durationSec < minRecordSeconds)
            {
                Debug.Log("Voice: Recording too short, ignored.");
                return;
            }

            // Extract the samples recorded after the press
            float[] trimmed = new float[framesToCopy * channels];
            int framesFirst = Mathf.Min(framesToCopy, totalFrames - recordingStartPosition);
            if (framesFirst > 0)
            {
                float[] seg1 = new float[framesFirst * channels];
                micClip.GetData(seg1, recordingStartPosition);
                Array.Copy(seg1, 0, trimmed, 0, seg1.Length);
            }

            // Wrap around to the start of the buffer if needed
            int framesSecond = framesToCopy - framesFirst;
            if (framesSecond > 0)
            {
                float[] seg2 = new float[framesSecond * channels];
                micClip.GetData(seg2, 0);
                Array.Copy(seg2, 0, trimmed, framesFirst * channels, seg2.Length);
            }

            // Reset start position for next recording
            recordingStartPosition = -1;

            if (whisperManager == null)
            {
                Debug.LogError("WhisperManager instance not found for transcription.");
                return;
            }

            StartCoroutine(TranscribeAndSendLocal(trimmed, sampleRate, channels));
        }

        private IEnumerator TranscribeAndSendLocal(float[] samples, int sampleRate, int channels)
        {
            string transcribed = null;

            // WhisperManager expects float[] samples, sampleRate, channels
            var task = whisperManager.GetTextAsync(samples, sampleRate, channels);
            while (!task.IsCompleted)
                yield return null;

            if (task.Result == null)
            {
                Debug.LogError("Whisper transcription failed.");
                yield break;
            }

            transcribed = task.Result.Result;

            if (string.IsNullOrWhiteSpace(transcribed))
            {
                Debug.Log("Voice: Empty transcription.");
                yield break;
            }

            Debug.Log($"Voice: Transcribed -> {transcribed}");
            if (interpreter != null)
            {
                interpreter.ProcessCommand(transcribed);
            }
        }

        // Minimal WAV encoder for PCM16 from float samples [-1,1]
        private static class WavUtility
        {
            public static byte[] FromAudioSamples(float[] samples, int channels, int sampleRate)
            {
                // Convert to 16-bit PCM
                short[] intData = new short[samples.Length];
                byte[] bytesData = new byte[intData.Length * 2];
                for (int i = 0; i < samples.Length; i++)
                {
                    float f = Mathf.Clamp(samples[i], -1f, 1f);
                    intData[i] = (short)(f * short.MaxValue);
                }
                Buffer.BlockCopy(intData, 0, bytesData, 0, bytesData.Length);

                // WAV header
                List<byte> wav = new List<byte>(44 + bytesData.Length);
                // RIFF chunk descriptor
                wav.AddRange(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                wav.AddRange(BitConverter.GetBytes(36 + bytesData.Length));
                wav.AddRange(System.Text.Encoding.ASCII.GetBytes("WAVE"));
                // fmt sub-chunk
                wav.AddRange(System.Text.Encoding.ASCII.GetBytes("fmt "));
                wav.AddRange(BitConverter.GetBytes(16)); // Subchunk1Size for PCM
                wav.AddRange(BitConverter.GetBytes((short)1)); // PCM = 1
                wav.AddRange(BitConverter.GetBytes((short)channels));
                wav.AddRange(BitConverter.GetBytes(sampleRate));
                int byteRate = sampleRate * channels * 2;
                wav.AddRange(BitConverter.GetBytes(byteRate));
                short blockAlign = (short)(channels * 2);
                wav.AddRange(BitConverter.GetBytes(blockAlign));
                wav.AddRange(BitConverter.GetBytes((short)16)); // BitsPerSample
                // data sub-chunk
                wav.AddRange(System.Text.Encoding.ASCII.GetBytes("data"));
                wav.AddRange(BitConverter.GetBytes(bytesData.Length));
                wav.AddRange(bytesData);

                return wav.ToArray();
            }
        }
    }
}
