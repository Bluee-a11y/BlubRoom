using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ClubhousePC
{
    public sealed class LoadingScreen : MonoBehaviour
    {
        private static LoadingScreen instance;
        private AudioSource music;
        private bool visible;
        private float progress;
        private GUIStyle logo;
        private GUIStyle message;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Create()
        {
            if (instance != null) return;
            var go = new GameObject("BlubRoom Loading Screen");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<LoadingScreen>();
            instance.Setup();
            instance.ShowStartup();
        }

        private void Setup()
        {
            music = gameObject.AddComponent<AudioSource>();
            music.clip = Resources.Load<AudioClip>("Audio/LoadingMusic");
            music.loop = true;
            music.playOnAwake = false;
            music.volume = 0.28f;
        }

        private async void ShowStartup()
        {
            Show();
            progress = 1f;
            await Task.Delay(2500);
            Hide();
        }

        public static async Task LoadScene(string sceneName)
        {
            if (instance == null) Create();
            instance.Show();
            instance.progress = 0f;
            var shownAt = Time.realtimeSinceStartup;
            await Task.Yield();

            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            while (!operation.isDone)
            {
                instance.progress = Mathf.Clamp01(operation.progress / 0.9f);
                await Task.Yield();
            }

            while (Time.realtimeSinceStartup - shownAt < 1.25f) await Task.Yield();
            instance.progress = 1f;
            await Task.Delay(180);
            instance.Hide();
        }

        private void Show()
        {
            visible = true;
            if (music.clip != null && !music.isPlaying) music.Play();
        }

        private void Hide()
        {
            visible = false;
            if (music.isPlaying) music.Stop();
        }

        private void OnGUI()
        {
            if (!visible) return;
            GUI.depth = -10000;
            var previous = GUI.color;
            GUI.color = new Color(0.02f, 0.08f, 0.22f, 1f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previous;

            logo ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Clamp(Screen.height / 12, 38, 82),
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            message ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Clamp(Screen.height / 35, 18, 30),
                normal = { textColor = new Color(0.25f, 0.85f, 1f) }
            };

            GUI.Label(new Rect(0, Screen.height * 0.32f, Screen.width, 100), "BLUBROOM", logo);
            GUI.Label(new Rect(0, Screen.height * 0.48f, Screen.width, 50), "Loading your next adventure…", message);

            var width = Mathf.Min(620, Screen.width * 0.68f);
            var bar = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.62f, width, 18);
            GUI.Box(bar, "");
            GUI.color = new Color(0.05f, 0.7f, 0.95f);
            GUI.DrawTexture(new Rect(bar.x + 2, bar.y + 2, (bar.width - 4) * progress, bar.height - 4), Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
