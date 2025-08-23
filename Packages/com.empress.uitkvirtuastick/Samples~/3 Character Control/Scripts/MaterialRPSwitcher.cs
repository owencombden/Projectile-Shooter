#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Empress.UITK.Demo {

    [ExecuteInEditMode]
    [RequireComponent(typeof(Renderer))]
    class MaterialRPSwitcher : MonoBehaviour {

        Renderer m_Renderer;
        RenderPipelineAsset m_RPAsset;
        Shader m_RPTargetShader;

        void Update() {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (!m_Renderer)
                m_Renderer = GetComponent<Renderer>();

            if (!m_RPTargetShader || m_RPAsset != GraphicsSettings.currentRenderPipeline)
                m_RPTargetShader = Shader.Find(GetBaseShaderForCurrentPipeline());

            if (!m_RPTargetShader || !m_Renderer || m_Renderer.sharedMaterials.Length == 0)
                return;

            UpdateMaterials();
        }

        void UpdateMaterials() {
            var materials = m_Renderer.sharedMaterials;

            // Update material shaders based on the current render pipeline
            foreach (var mat in materials) {
                if (!mat) continue;

                if (mat.shader != m_RPTargetShader) {
                    Undo.RecordObject(mat, "Change Material Shader");
                    mat.shader = m_RPTargetShader;
                    EditorUtility.SetDirty(mat);
                }
            }
        }

        string GetBaseShaderForCurrentPipeline() {
            m_RPAsset = GraphicsSettings.currentRenderPipeline;

            // Built-in RP
            if (!m_RPAsset)
                return "Standard";

            var pipelineType = m_RPAsset.GetType().Name;

            // URP
            if (pipelineType.Contains("UniversalRenderPipelineAsset"))
                return "Universal Render Pipeline/Lit";

            // HDRP
            else if (pipelineType.Contains("HDRenderPipelineAsset"))
                return "HDRP/Lit";

            return "Standard"; // Fallback
        }
    }
}
#endif
