#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ClubhousePC
{
    public sealed class DiscordRichPresence : MonoBehaviour
    {
        private const string ApplicationId = "1543648423088230600";
        private CancellationTokenSource cancellation;
        private NamedPipeClientStream pipe;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindObjectOfType<DiscordRichPresence>() != null) return;
            var presence = new GameObject("Discord Rich Presence").AddComponent<DiscordRichPresence>();
            DontDestroyOnLoad(presence.gameObject);
        }

        private void Start()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            cancellation = new CancellationTokenSource();
            ConnectLoop(cancellation.Token);
#endif
        }

        private async void ConnectLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (pipe == null || !pipe.IsConnected)
                        await ConnectAndSetPresence(token);
                }
                catch (Exception exception) when (!(exception is OperationCanceledException))
                {
                    UnityEngine.Debug.Log("Discord Rich Presence waiting for Discord: " + exception.Message);
                    DisposePipe();
                }

                try { await Task.Delay(15000, token); }
                catch (OperationCanceledException) { break; }
            }
        }

        private async Task ConnectAndSetPresence(CancellationToken token)
        {
            for (var index = 0; index < 10 && !token.IsCancellationRequested; index++)
            {
                var candidate = new NamedPipeClientStream(".", "discord-ipc-" + index,
                    PipeDirection.InOut, PipeOptions.Asynchronous);
                try
                {
                    await Task.Run(() => candidate.Connect(250), token);
                    pipe = candidate;
                    break;
                }
                catch
                {
                    candidate.Dispose();
                }
            }

            if (pipe == null || !pipe.IsConnected) throw new IOException("Discord desktop is not running");

            await SendFrame(0, "{\"v\":1,\"client_id\":\"" + ApplicationId + "\"}", token);
            await ReadFrame(token);

            var nonce = Guid.NewGuid().ToString("N");
            var pid = Process.GetCurrentProcess().Id;
            var started = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var activity = "{\"cmd\":\"SET_ACTIVITY\",\"args\":{\"pid\":" + pid +
                ",\"activity\":{\"details\":\"Bouncing Balls\"," +
                "\"timestamps\":{\"start\":" + started + "}}},\"nonce\":\"" + nonce + "\"}";
            await SendFrame(1, activity, token);
            await ReadFrame(token);
            UnityEngine.Debug.Log("Discord Rich Presence connected: Playing Blub Room — Bouncing Balls");
        }

        private async Task SendFrame(int operation, string json, CancellationToken token)
        {
            var payload = Encoding.UTF8.GetBytes(json);
            var header = new byte[8];
            Buffer.BlockCopy(BitConverter.GetBytes(operation), 0, header, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(payload.Length), 0, header, 4, 4);
            await pipe.WriteAsync(header, 0, header.Length, token);
            await pipe.WriteAsync(payload, 0, payload.Length, token);
            await pipe.FlushAsync(token);
        }

        private async Task ReadFrame(CancellationToken token)
        {
            var header = new byte[8];
            await ReadExact(header, token);
            var length = BitConverter.ToInt32(header, 4);
            if (length < 0 || length > 1024 * 1024) throw new IOException("Invalid Discord response");
            await ReadExact(new byte[length], token);
        }

        private async Task ReadExact(byte[] buffer, CancellationToken token)
        {
            var offset = 0;
            while (offset < buffer.Length)
            {
                var count = await pipe.ReadAsync(buffer, offset, buffer.Length - offset, token);
                if (count == 0) throw new EndOfStreamException();
                offset += count;
            }
        }

        private void OnDestroy()
        {
            cancellation?.Cancel();
            DisposePipe();
            cancellation?.Dispose();
        }

        private void DisposePipe()
        {
            pipe?.Dispose();
            pipe = null;
        }
    }
}
#endif
