using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Empress.UITK.Demo {

    public class UIBuildDemo : MonoBehaviour {

        [SerializeField] private Button m_PrevDemo;
        [SerializeField] private Button m_NextDemo;

        private int m_SceneCount;
        private int m_CurrentIndex;

        void OnValidate() {
            gameObject.hideFlags = HideFlags.HideInHierarchy;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () => {
                if (this)
                    gameObject.SetActive(Application.isPlaying);
            };
#endif
        }

        void Awake() {
            if (Application.isEditor) {
                Destroy(gameObject);
                return;
            }

            if (Application.isMobilePlatform)
                Application.targetFrameRate = 60;

            DontDestroyOnLoad(gameObject);
            gameObject.SetActive(true);

            m_SceneCount = SceneManager.sceneCountInBuildSettings;
            m_CurrentIndex = SceneManager.GetActiveScene().buildIndex;

            m_PrevDemo.onClick.AddListener(() => {
                m_CurrentIndex = (m_CurrentIndex - 1 + m_SceneCount) % m_SceneCount;
                SceneManager.LoadSceneAsync(m_CurrentIndex, LoadSceneMode.Single);
            });

            m_NextDemo.onClick.AddListener(() => {
                m_CurrentIndex = (m_CurrentIndex + 1) % m_SceneCount;
                SceneManager.LoadSceneAsync(m_CurrentIndex, LoadSceneMode.Single);
            });
        }
    }
}
