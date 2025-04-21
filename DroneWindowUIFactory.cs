using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PhotomodeMultiview
{
    public class DroneWindowUIFactory
    {
        public static GameObject CreateDroneWindowUI(Transform parent)
        {
            // Root object
            GameObject root = new GameObject("DroneWindow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.SetParent(parent, false);
            rootRect.anchorMin = new Vector2(0, 1);
            rootRect.anchorMax = new Vector2(0, 1);
            rootRect.pivot = new Vector2(0, 1);
            rootRect.anchoredPosition = new Vector2(50, -50);
            rootRect.sizeDelta = new Vector2(300, 200);

            Image bgImage = root.GetComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.75f);

            DraggableWindowUI drag = root.AddComponent<DraggableWindowUI>();
            RightClickResizer resize = root.AddComponent<RightClickResizer>();

            // Camera Feed (RawImage)
            GameObject feed = new GameObject("FeedImage", typeof(RectTransform), typeof(RawImage));
            RectTransform feedRect = feed.GetComponent<RectTransform>();
            feedRect.SetParent(root.transform, false);
            feedRect.anchorMin = new Vector2(0, 0);
            feedRect.anchorMax = new Vector2(1, 1);
            feedRect.offsetMin = new Vector2(0, 0);
            feedRect.offsetMax = new Vector2(0, 0);
            RawImage feedImage = feed.GetComponent<RawImage>();

            // Drone UI container under the feed image
            GameObject overlayUI = new GameObject("OverlayUI", typeof(RectTransform));
            RectTransform overlayRect = overlayUI.GetComponent<RectTransform>();
            overlayRect.SetParent(feed.transform, false);
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            // Player dropdown button
            GameObject playerBtn = CreateButton("PlayerButton", "Player", new Vector2(0, 0), new Vector2(100, 20), root.transform);

            // Mode dropdown button
            GameObject modeBtn = CreateButton("ModeButton", "Mode", new Vector2(100, 0), new Vector2(100, 20), root.transform);

            // Logger button ("L")
            GameObject lockBtn = CreateButton("LockButton", "/", new Vector2(-50, 0), new Vector2(25, 20), root.transform, fromRight: true);

            // Logger button ("L")
            GameObject logBtn = CreateButton("LogButton", "L", new Vector2(-25, 0), new Vector2(25, 20), root.transform, fromRight: true);

            // Close button ("X")
            GameObject closeBtn = CreateButton("CloseButton", "X", new Vector2(0, 0), new Vector2(25, 20), root.transform, fromRight: true);

            // Attach reference script
            DroneWindowUI ui = root.AddComponent<DroneWindowUI>();
            ui.feedImage = feedImage;
            ui.playerButton = playerBtn.GetComponent<Button>();
            ui.modeButton = modeBtn.GetComponent<Button>();
            ui.logButton = logBtn.GetComponent<Button>();
            ui.closeButton = closeBtn.GetComponent<Button>();
            ui.lockButton = lockBtn.GetComponent<Button>();

            drag.window = ui;
            resize.window = ui;

            // Try to get and reuse the original Target/VelocityPanel UI
            SpectatorCameraUI spectatorUI = GameObject.FindObjectOfType<SpectatorCameraUI>(true);
            if (spectatorUI != null)
            {
                try
                {
                    Transform guiHolder = spectatorUI.transform.GetChild(0);
                    Transform flyingCameraGUI = guiHolder.GetChild(1);
                    Transform dslrRect = flyingCameraGUI.GetChild(0);

                    GameObject uiCopy = GameObject.Instantiate(dslrRect, overlayRect).gameObject;
                    uiCopy.SetActive(true);

                    List<Transform> toDelete = new List<Transform>();
                    foreach (Transform child in uiCopy.transform)
                    {
                        if (child.name != "Target" && child.name != "VelocityPanel")
                        {
                            toDelete.Add(child);
                        }
                        else if (child.name == "Target")
                        {
                            RectTransform rt = child.GetComponent<RectTransform>();
                            rt.anchorMin = new Vector2(0.5f, 0.94f);
                            rt.anchorMax = new Vector2(1f, 0.99f);

                            TMPro.TextMeshProUGUI tmp = child.GetComponent<TMPro.TextMeshProUGUI>();
                            if (tmp != null)
                            {
                                ui.nameUI = tmp;
                            }
                        }
                        else if (child.name == "VelocityPanel")
                        {
                            RectTransform rt = child.GetComponent<RectTransform>();
                            rt.anchorMin = new Vector2(0f, 0.02f);
                            rt.anchorMax = new Vector2(0.98f, 0.2f);

                            SpeedDisplay display = child.GetComponentInChildren<SpeedDisplay>(true);
                            if (display != null)
                            {
                                display.supplyCustomValues = true;
                                ui.speedDisplay = display;
                            }

                            // Look for the "Velocity" label
                            TMPro.TextMeshProUGUI[] texts = child.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
                            foreach (var tm in texts)
                            {
                                if (tm.gameObject.name == "Velocity")
                                {
                                    ui.velocityUI = tm;
                                    break;
                                }
                            }
                        }
                    }

                    foreach (Transform t in toDelete)
                    {
                        GameObject.DestroyImmediate(t.gameObject);
                    }
                }
                catch
                {
                    Debug.LogWarning("Drone UI copy failed.");
                }
            }

            ui.Initialize();

            return root;
        }

        private static GameObject CreateButton(string name, string text, Vector2 anchoredPos, Vector2 size, Transform parent, bool fromRight = false)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Button), typeof(Image));
            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = size;

            if (fromRight)
            {
                rect.anchorMin = new Vector2(1, 1);
                rect.anchorMax = new Vector2(1, 1);
                rect.pivot = new Vector2(1, 1);
                rect.anchoredPosition = anchoredPos;
            }
            else
            {
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0, 1);
                rect.anchoredPosition = anchoredPos;
            }

            Image img = btnObj.GetComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            GameObject label = new GameObject("Text", typeof(RectTransform), typeof(Text));
            RectTransform textRect = label.GetComponent<RectTransform>();
            textRect.SetParent(btnObj.transform, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text txt = label.GetComponent<Text>();
            txt.text = text;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.fontSize = 14;

            return btnObj;
        }
    }
}