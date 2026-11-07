using System;
using System.Linq;
using Game;
using Player;
using UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GameplayHudInstaller
{
    public const string HudPrefabPath = "Assets/UI/Prefabs/HUDCanvas.prefab";

    public static void EnsurePrefabHud()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameplayRigBuilder.PrefabPath);
        var current = prefab.GetComponent<GameplayRig>();
        if (current.Hud != null && current.Hud.PlayerHealth == current.Player.GetComponent<PlayerHealth>() &&
            current.Hud.HealthBar != null && current.Hud.PlayerHealth.healthBar == current.Hud.HealthBar)
            return;
        var contents = PrefabUtility.LoadPrefabContents(GameplayRigBuilder.PrefabPath);
        try
        {
            var rig = contents.GetComponent<GameplayRig>();
            var hud = contents.GetComponentInChildren<HUDManager>(true);
            if (hud == null)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
                if (asset == null)
                    throw new InvalidOperationException("Missing HUDCanvas prefab.");
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, contents.scene);
                instance.transform.SetParent(contents.transform, false);
                instance.name = "HUDCanvas";
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localScale = Vector3.one;
                Record(instance.transform);
                hud = instance.GetComponent<HUDManager>();
            }
            SetReference(rig, "hud", hud);
            Connect(hud, rig.Player.GetComponent<PlayerHealth>(), null);
            PrefabUtility.SaveAsPrefabAsset(contents, GameplayRigBuilder.PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    public static void ConfigureScene(Scene scene, GameplayRig rig)
    {
        var hud = rig.Hud;
        if (hud == null)
            throw new InvalidOperationException("The shared gameplay rig has no HUD.");
        var health = rig.Player.GetComponent<PlayerHealth>();
        var slider = hud.GetComponentInChildren<Slider>(true);
        foreach (var previous in GameplayRigBuilder.Components<Slider>(scene).Where(previous => previous != slider &&
            (previous == health.healthBar || previous.name.StartsWith("PlayerHealthBar", StringComparison.Ordinal))))
        {
            if (health.deathScreen != null && health.deathScreen.transform.IsChildOf(previous.transform))
            {
                previous.enabled = false;
                Record(previous);
                foreach (var graphic in previous.GetComponentsInChildren<Graphic>(true)
                    .Where(graphic => !graphic.transform.IsChildOf(health.deathScreen.transform)))
                {
                    graphic.enabled = false;
                    Record(graphic);
                }
            }
            else
            {
                previous.gameObject.SetActive(false);
                Record(previous.gameObject);
            }
        }
        foreach (var other in GameplayRigBuilder.Components<HUDManager>(scene).Where(other => other != hud))
        {
            if (health.deathScreen != null && health.deathScreen.transform.IsChildOf(other.transform))
            {
                other.enabled = false;
                if (other.HealthBar != null)
                    other.HealthBar.gameObject.SetActive(false);
                if (other.CoinsText != null)
                    other.CoinsText.gameObject.SetActive(false);
                Record(other);
            }
            else
            {
                other.gameObject.SetActive(false);
                Record(other.gameObject);
            }
        }
        var session = GameplayRigBuilder.Components<GameSession>(scene).FirstOrDefault(item => item.gameObject.activeInHierarchy);
        if (session == null)
        {
            var owner = new GameObject("GameSession");
            SceneManager.MoveGameObjectToScene(owner, scene);
            session = owner.AddComponent<GameSession>();
        }
        Connect(hud, health, session);
    }

    private static void Connect(HUDManager hud, PlayerHealth health, GameSession session)
    {
        var slider = hud.GetComponentInChildren<Slider>(true);
        if (slider == null || hud.CoinsText == null)
            throw new InvalidOperationException("HUDCanvas must contain a health slider and coin text.");
        SetReference(hud, "_playerHealth", health);
        SetReference(hud, "_healthBar", slider);
        SetReference(hud, "_session", session);
        health.healthBar = slider;
        slider.minValue = 0;
        slider.maxValue = health.maxHealth;
        slider.value = health.maxHealth;
        slider.interactable = false;
        Record(health);
        Record(slider);
    }

    private static void SetReference(UnityEngine.Object owner, string field, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(owner);
        serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        Record(owner);
    }

    private static void Record(UnityEngine.Object target)
    {
        if (PrefabUtility.IsPartOfPrefabInstance(target))
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }
}
