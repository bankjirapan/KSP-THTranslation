using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[KSPAddon(KSPAddon.Startup.Instantly, true)]
public class THMod : MonoBehaviour
{
    private static bool applied = false;
    private static Font thaiFont = null;
    private static int lastAppliedCount = -1;

    void Start()
    {
        if (applied) return;
        DontDestroyOnLoad(this);

        try
        {
            string[] candidates = {
                "Noto Sans Thai", "Noto Sans Thai Looped",
                "Sukhumvit Set", "Thonburi", "Ayuthaya", "Krungthep"
            };

            foreach (var name in candidates)
            {
                try
                {
                    var f = Font.CreateDynamicFontFromOSFont(name, 40);
                    if (f != null)
                    {
                        thaiFont = f;
                        Debug.Log("[THMod] Using system font: " + name);
                        break;
                    }
                }
                catch {}
            }

            if (thaiFont == null)
            {
                Debug.LogError("[THMod] No Thai system font found.");
                return;
            }
            Font.textureRebuilt += OnFontTextureRebuilt;

            SceneManager.sceneLoaded += (s, m) => ApplyAll("sceneLoaded:" + s.name);
            ApplyAll("startup");
            StartCoroutine(PeriodicApply(0.5f));

            applied = true;
        }
        catch (Exception ex)
        {
            Debug.LogError("[THMod] Exception: " + ex);
        }
    }

    void OnDestroy()
    {
        Font.textureRebuilt -= OnFontTextureRebuilt;
    }

    private void OnFontTextureRebuilt(Font f)
    {
        if (thaiFont != null && f == thaiFont)
            ApplyAll("textureRebuilt");
    }

    void OnGUI()
    {
        try
        {
            if (thaiFont != null && GUI.skin != null && GUI.skin.font != thaiFont)
            {
                GUI.skin.font = thaiFont;
                Debug.Log("[THMod] Applied to GUI.skin.font (IMGUI)");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[THMod] OnGUI: " + ex);
        }
    }

    private IEnumerator PeriodicApply(float interval)
    {
        var wait = new WaitForSeconds(interval);
        while (true)
        {
            ApplyAll("periodic");
            yield return wait;
        }
    }

    private void ApplyAll(string reason)
    {
        if (thaiFont == null) return;

        int count = 0;

        // ---- UGUI: UnityEngine.UI.Text ----
        var uiTexts = Resources.FindObjectsOfTypeAll<Text>();
        foreach (var t in uiTexts)
        {
            try
            {
                if (t == null) continue;
                if (t.font != thaiFont) t.font = thaiFont;
                if (t.material != null && t.material != thaiFont.material)
                    t.material = thaiFont.material;
                string s = t.text ?? string.Empty;
                thaiFont.RequestCharactersInTexture(s, t.resizeTextForBestFit ? t.cachedTextGenerator.fontSizeUsedForBestFit : t.fontSize, t.fontStyle);
                t.cachedTextGenerator.Invalidate();
                t.SetAllDirty();

                count++;
            }
            catch {}
        }

        var meshes = Resources.FindObjectsOfTypeAll<TextMesh>();
        foreach (var m in meshes)
        {
            try
            {
                if (m == null) continue;

                if (m.font != thaiFont) m.font = thaiFont;
                var r = m.GetComponent<Renderer>();
                if (r != null && r.sharedMaterial != null && r.sharedMaterial != thaiFont.material)
                    r.sharedMaterial = thaiFont.material;

                string s = m.text ?? string.Empty;
                thaiFont.RequestCharactersInTexture(s, m.fontSize, m.fontStyle);

                count++;
            }
            catch {}
        }

        if (count != lastAppliedCount)
        {
            Debug.Log("[THMod] Applied to " + count.ToString() + " text components (" + reason + ")");
            lastAppliedCount = count;
        }
    }
}
