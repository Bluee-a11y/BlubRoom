using Unity.Services.Authentication;
using Unity.Services.Vivox;
using UnityEngine;

namespace ClubhousePC
{
    public sealed class VoiceChatManager : MonoBehaviour
    {
        private string status = "Voice: waiting for multiplayer";
        private bool ready;
        private bool joining;
        private bool muted = true;
        private bool initialized;
        private bool loggedIn;
        private string currentChannel;

        public async void JoinVoice(string channelName)
        {
            if (joining || string.IsNullOrWhiteSpace(channelName)) return;
            channelName = channelName.Trim().ToUpperInvariant();
            joining = true;
            status = "Voice: connecting…";

            try
            {
                if (!initialized)
                {
                    await VivoxService.Instance.InitializeAsync();
                    initialized = true;
                }
                if (!loggedIn)
                {
                    var playerId = AuthenticationService.Instance.PlayerId;
                    var shortId = string.IsNullOrEmpty(playerId) ? Random.Range(1000, 9999).ToString() : playerId.Substring(0, 6);
                    await VivoxService.Instance.LoginAsync(new LoginOptions { DisplayName = "Player-" + shortId });
                    loggedIn = true;
                }

                var requestedChannel = "blubcenter-" + channelName;
                if (ready && currentChannel == requestedChannel)
                {
                    VivoxService.Instance.UnmuteOutputDevice();
                    VivoxService.Instance.SetOutputDeviceVolume(50);
                    VivoxService.Instance.MuteInputDevice();
                    muted = true;
                    status = "Voice ready — hold V to talk (O speaker, I mic)";
                    return;
                }
                if (ready && !string.IsNullOrEmpty(currentChannel))
                    await VivoxService.Instance.LeaveChannelAsync(currentChannel);

                await VivoxService.Instance.JoinGroupChannelAsync(requestedChannel, ChatCapability.AudioOnly);
                currentChannel = requestedChannel;
                // Input is push-to-talk, but incoming voices must always remain audible.
                VivoxService.Instance.UnmuteOutputDevice();
                VivoxService.Instance.SetOutputDeviceVolume(50);
                VivoxService.Instance.MuteInputDevice();
                muted = true;
                ready = true;
                status = "Voice ready — hold V to talk (O speaker, I mic)";
            }
            catch (System.Exception exception)
            {
                status = "Voice failed: " + exception.Message;
                Debug.LogException(exception);
            }
            finally { joining = false; }
        }

        private void Update()
        {
            if (!ready) return;

            if (Input.GetKeyDown(KeyCode.V))
            {
                VivoxService.Instance.UnmuteInputDevice();
                muted = false;
                status = "Voice: TALKING";
            }

            if (Input.GetKeyUp(KeyCode.V))
            {
                VivoxService.Instance.MuteInputDevice();
                muted = true;
                status = "Voice ready — hold V to talk (O speaker, I mic)";
            }

            if (Input.GetKeyDown(KeyCode.O)) CycleOutputDevice();
            if (Input.GetKeyDown(KeyCode.I)) CycleInputDevice();

            if (Input.GetKeyDown(KeyCode.M))
            {
                if (muted) VivoxService.Instance.UnmuteInputDevice();
                else VivoxService.Instance.MuteInputDevice();
                muted = !muted;
                status = muted ? "Voice: muted" : "Voice: open microphone";
            }
        }

        private async void CycleOutputDevice()
        {
            var devices = VivoxService.Instance.AvailableOutputDevices;
            if (devices.Count == 0) { status = "Voice: no speaker devices found"; return; }
            var current = VivoxService.Instance.ActiveOutputDevice.DeviceName;
            var index = 0;
            for (var i = 0; i < devices.Count; i++)
                if (devices[i].DeviceName == current) index = (i + 1) % devices.Count;
            await VivoxService.Instance.SetActiveOutputDeviceAsync(devices[index]);
            VivoxService.Instance.UnmuteOutputDevice();
            status = "Speaker: " + devices[index].DeviceName;
        }

        private async void CycleInputDevice()
        {
            var devices = VivoxService.Instance.AvailableInputDevices;
            if (devices.Count == 0) { status = "Voice: no microphone devices found"; return; }
            var current = VivoxService.Instance.ActiveInputDevice.DeviceName;
            var index = 0;
            for (var i = 0; i < devices.Count; i++)
                if (devices[i].DeviceName == current) index = (i + 1) % devices.Count;
            await VivoxService.Instance.SetActiveInputDeviceAsync(devices[index]);
            status = "Microphone: " + devices[index].DeviceName;
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(18, Screen.height - 58, 300, 38), "");
            GUI.Label(new Rect(30, Screen.height - 49, 280, 24), status);
        }
    }
}
