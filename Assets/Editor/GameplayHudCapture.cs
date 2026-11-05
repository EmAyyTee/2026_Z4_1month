using System.IO;
using Player;
using UnityEngine;
using UnityEngine.Rendering;

public static class GameplayHudCapture
{
    public static void Capture(GameplayRig rig, string name)
    {
        var camera = rig.GameplayCamera;
        var canvas = rig.Hud.GetComponent<Canvas>();
        var previousMode = canvas.renderMode;
        var previousCamera = canvas.worldCamera;
        float previousDistance = canvas.planeDistance;
        var previousTarget = camera.targetTexture;
        var previousActive = RenderTexture.active;
        var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
        var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        try
        {
            target.Create();
            camera.targetTexture = target;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;
            Canvas.ForceUpdateCanvases();
            var request = new RenderPipeline.StandardRequest { destination = target };
            RenderPipeline.SubmitRenderRequest(camera, request);
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            texture.Apply();
            Directory.CreateDirectory("Library/MapPreviews");
            File.WriteAllBytes("Library/MapPreviews/HUD-" + name + ".png", texture.EncodeToPNG());
        }
        finally
        {
            canvas.renderMode = previousMode;
            canvas.worldCamera = previousCamera;
            canvas.planeDistance = previousDistance;
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }
}
