/*using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace PhotomodeMultiview
{
    public enum FollowMode
    {
        Smooth,
        Strict,
        Locked,
        Bumper,
        First
    }

    public class PhotoDrone : MonoBehaviour
    {
        private Camera mainCamera;
        private Camera skyboxCamera;

        private RenderTexture camTexture;

        //UI
        private SpeedDisplay speedDisplay;
        private TextMeshProUGUI nameUI;
        private TextMeshProUGUI velocityUI;

        //Target
        private PlayerData targetPlayer;
        private Transform targetTransform;

        //GUI
        private Rect windowRect;
        private bool following = false;
        private bool showGUI = false;
        private FollowMode followMode = FollowMode.Smooth;
        public Vector3 followOffset = new Vector3(0, 2, -3);
        public Vector3 lookOffset = new Vector3(6f, 0, 0);
        public Vector3 firstPersonOffset = new Vector3(0, 0.2f, 2.2f);
        public Vector3 bumperOffset = new Vector3(0, 0.5f, 3.5f);
        public Vector3 lockedOffset = new Vector3(0, 1.7f, -3f);
        private bool modeDropdownExpanded = false;
        private bool dropdownExpanded = false;

        private bool resizing = false;
        private Vector2 lastMousePos;
        int prevWidth = 0;
        int prevHeight = 0;

        //Reconnection
        private Coroutine recoveryRoutine;

        public void Setup(GameObject dronePrefab)
        {
            //Add the drone prefab to the transfrom.
            GameObject cameraRig = Instantiate(dronePrefab, transform);
            cameraRig.name = "DroneCameraRig";
            cameraRig.gameObject.SetActive(true);

            //Get all the references
            camTexture = new RenderTexture(320, 240, 16);
            // Assign cameras
            Camera[] cams = cameraRig.GetComponentsInChildren<Camera>();
            foreach (Camera cam in cams)
            {
                if (cam.clearFlags == CameraClearFlags.Skybox)
                {
                    skyboxCamera = cam;
                }
                else
                {
                    mainCamera = cam;
                }
            }
            mainCamera.targetTexture = camTexture;
            skyboxCamera.targetTexture = camTexture;
            mainCamera.depth = 1;
            skyboxCamera.depth = 0;

            //Get UI
            speedDisplay = GetComponentInChildren<SpeedDisplay>(true);
            TextMeshProUGUI[] tms = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (TextMeshProUGUI tm in tms)
            {
                if (tm.gameObject.name == "Velocity")
                {
                    velocityUI = tm;
                }

                if (tm.gameObject.name == "Target")
                {
                    nameUI = tm;
                }
            }

            //Window
            windowRect = new Rect(10, 10, 340, 260);
        }

        public void SetInitialTarget()
        {
            List<string> names = new List<string>(DroneCommand.playerNames);
            foreach (string n in names)
            {
                PlayerData player = DroneCommand.GetPlayer(n);
                if (player != null)
                {
                    SetTarget(player);
                    return;
                }
            }

            ShutDown(false);
        }

        public void ApplyPreset(DronePreset preset)
        {
            if (preset == null)
            {
                return;
            }

            followMode = preset.Mode;
            if (preset.Target == "")
            {
                SetInitialTarget();
            }
            else
            {
                PlayerData p = DroneCommand.GetPlayer(preset.Target);
                if (p == null)
                {
                    SetInitialTarget();
                }
                else
                {
                    SetTarget(p);
                }
            }

            float x = preset.X;
            float y = preset.Y;
            float width = preset.Width;
            float height = preset.Height;

            if (!preset.UsePixels)
            {
                x *= Screen.width;
                y *= Screen.height;
                width *= Screen.width;
                height *= Screen.height;
            }

            // Round to ensure clean pixels
            windowRect.x = Mathf.RoundToInt(x);
            windowRect.y = Mathf.RoundToInt(y);
            windowRect.width = Mathf.Max(100, Mathf.RoundToInt(width));
            windowRect.height = Mathf.Max(100, Mathf.RoundToInt(height));

            // Sync dragging-related state
            prevWidth = Mathf.RoundToInt(windowRect.width);
            prevHeight = Mathf.RoundToInt(windowRect.height);

            // Resize RenderTexture immediately
            ResizeRenderTexture(prevWidth, prevHeight);
        }

        public void SetTarget(PlayerData player)
        {
            //Debug.Log("Setting target to: " + player.username);
            try
            {
                targetPlayer = player;
                //Debug.LogWarning("✔ targetPlayer assigned");

                if (targetPlayer.isLocalPlayer)
                {
                    var local = GameObject.FindObjectOfType<ReadyToReset>(true);
                    //Debug.LogWarning("✔ Found ReadyToReset: " + (local != null));
                    targetTransform = local.transform;

                    //Debug.LogWarning("✔ targetTransform set for local");

                    speedDisplay.gameObject.SetActive(false);
                    velocityUI.gameObject.SetActive(false);
                    //Debug.LogWarning("✔ Disabled UI for local player");
                }
                else
                {
                    var ghost = targetPlayer.zeepkistNetworkPlayer;
                    //Debug.LogWarning("✔ targetPlayer.zeepkistNetworkPlayer: " + (ghost != null));
                    //Debug.LogWarning("✔ ghost.Zeepkist: " + (ghost.Zeepkist != null));
                    //Debug.LogWarning("✔ ghost.Zeepkist.ghostModel: " + (ghost.Zeepkist.ghostModel != null));

                    targetTransform = ghost.Zeepkist.ghostModel.transform;
                    //Debug.LogWarning("✔ targetTransform set for remote");

                    speedDisplay.gameObject.SetActive(true);
                    velocityUI.gameObject.SetActive(true);
                    //Debug.LogWarning("✔ Enabled UI for remote player");
                }

                nameUI.text = targetPlayer.username;
                //Debug.LogWarning("✔ nameUI set");

                mainCamera.enabled = true;
                skyboxCamera.enabled = true;
                //Debug.LogWarning("✔ Cameras enabled");

                following = true;
                showGUI = true;
                //Debug.LogWarning("✔ Following & GUI enabled");
            }
            catch (Exception e)
            {
                Debug.LogWarning("❌ Exception in SetTarget: " + e.Message);
                ShutDown(false);
            }
        }


        public void ShutDown(bool cooldown = true)
        {
            //Debug.LogWarning("Shutdown");

            mainCamera.enabled = false;
            skyboxCamera.enabled = false;
            following = false;

            if (cooldown)
            {
                Debug.LogWarning("Trying reattempt");
                if (recoveryRoutine != null)
                {
                    StopCoroutine(recoveryRoutine);
                }

                recoveryRoutine = StartCoroutine(AttemptRecovery());
            }
            else
            {
                DroneCommand.DestroyDrone(this);
            }
        }

        private IEnumerator AttemptRecovery()
        {
            float timeout = 3f;
            float elapsed = 0f;
            float interval = 0.25f;

            while (elapsed < timeout)
            {
                yield return new WaitForSeconds(interval);
                elapsed += interval;

                PlayerData retry = DroneCommand.GetPlayer(targetPlayer.username);
                if (retry != null)
                {
                    SetTarget(retry);
                    yield break;
                }
            }

            DroneCommand.DestroyDrone(this);
        }

        void Update()
        {
            if (targetPlayer == null || targetTransform == null)
            {
                if (following)
                {
                    ShutDown();
                    return;
                }
            }

            if (!following)
            {
                return;
            }

            try
            {
                Vector3 lookPoint = targetTransform.position + targetTransform.forward * 5f;
                transform.LookAt(lookPoint);

                switch (followMode)
                {
                    case FollowMode.Smooth:
                        // Smooth follow logic
                        Vector3 desiredPosition = targetTransform.position + targetTransform.rotation * followOffset;
                        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * 5f);
                        Quaternion targetRotation = Quaternion.LookRotation(lookPoint - transform.position);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
                        break;
                    case FollowMode.Strict:
                        transform.position = targetTransform.position + targetTransform.rotation * followOffset;
                        transform.LookAt(lookPoint);
                        break;
                    case FollowMode.Locked:
                        transform.position = targetTransform.TransformPoint(lockedOffset);
                        Quaternion baseRotation = targetTransform.rotation;
                        Quaternion localTilt = Quaternion.Euler(lookOffset);
                        transform.rotation = baseRotation * localTilt;
                        break;
                    case FollowMode.First:
                        transform.position = targetTransform.TransformPoint(firstPersonOffset);
                        transform.rotation = targetTransform.rotation;
                        break;
                    case FollowMode.Bumper:
                        transform.position = targetTransform.TransformPoint(bumperOffset);
                        transform.rotation = targetTransform.rotation;
                        break;


                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(e.Message);
                ShutDown();
            }

            if (targetPlayer.zeepkistNetworkPlayer.Zeepkist != null)
            {
                NetworkedZeepkistGhost ghost = targetPlayer.zeepkistNetworkPlayer.Zeepkist;
                velocityUI.text = ghost.displayVelocity.ToString();
                speedDisplay.DrawControlDisplay(ghost.armsUp, ghost.brake, ghost.isUsingActionKey, ghost.GetInputScalar(), ghost.GetSteeringScalar(), ghost.GetMaxSteerAngle(), ghost.zeepkistPowerUp, ghost.GetPitchScalar());
            }
        }

        void OnGUI()
        {
            if (!showGUI || camTexture == null)
                return;
            try
            {
                windowRect = GUI.Window(GetInstanceID(), windowRect, DrawWindow, "");
            }
            catch
            {
                showGUI = false;
                ShutDown(false);
            }
        }

        void DrawWindow(int windowID)
        {
            Event e = Event.current;
            Vector2 mousePos = e.mousePosition;

            // Start resizing if right-clicked inside the window
            if (e.type == EventType.MouseDown && e.button == 1)
            {
                resizing = true;
                lastMousePos = mousePos;
                e.Use();
            }

            // Perform resizing while right-click dragging
            if (e.type == EventType.MouseDrag && e.button == 1 && resizing)
            {
                Vector2 delta = mousePos - lastMousePos;
                windowRect.width = Mathf.Max(100, windowRect.width + delta.x);
                windowRect.height = Mathf.Max(100, windowRect.height + delta.y);
                lastMousePos = mousePos;
                e.Use();
            }

            // Stop resizing on mouse up
            if (e.type == EventType.MouseUp && e.button == 1)
            {
                resizing = false;
                e.Use();
            }

            // Resize render texture if window dimensions changed
            int texWidth = Mathf.RoundToInt(windowRect.width);
            int texHeight = Mathf.RoundToInt(windowRect.height + 5);

            if (texWidth != prevWidth || texHeight != prevHeight)
            {
                ResizeRenderTexture(texWidth, texHeight);
                prevWidth = texWidth;
                prevHeight = texHeight;
            }

            // Draw the camera feed
            Rect textureRect = new Rect(0, 0, texWidth, texHeight);

            if (targetTransform != null && camTexture != null)
            {
                GUI.DrawTexture(textureRect, camTexture, ScaleMode.ScaleToFit, false);
            }
            else
            {
                // Draw black placeholder if no valid target
                Color prevColor = GUI.color;
                GUI.color = Color.black;
                GUI.DrawTexture(textureRect, Texture2D.whiteTexture); // Draw black because we set color
                GUI.color = prevColor;
            }

            if (Plugin.Instance.showDroneUI.Value)
            {
                // --- Titlebar row with dropdowns and close button ---
                float buttonHeight = 20;
                float dropdownWidth = 100;
                float modeWidth = 100;
                float closeWidth = 25;

                // Dropdown (target select)
                if (GUI.Button(new Rect(0, 0, dropdownWidth, buttonHeight), targetPlayer == null ? "" : targetPlayer.username))
                {
                    dropdownExpanded = !dropdownExpanded;
                }

                // Follow mode dropdown
                if (GUI.Button(new Rect(dropdownWidth + 2, 0, modeWidth, buttonHeight), followMode.ToString()))
                {
                    modeDropdownExpanded = !modeDropdownExpanded;
                }

                // Close button (top right)
                if (GUI.Button(new Rect(windowRect.width - closeWidth, 0, closeWidth, buttonHeight), "X"))
                {
                    showGUI = false;
                    ShutDown(false);
                    return;
                }

                // Log button
                if (GUI.Button(new Rect(windowRect.width - (closeWidth * 2), 0, closeWidth, buttonHeight), "L"))
                {
                    string presetString = $"----------\nScreenSpace: {GetCurrentPreset(false)}\nPixels    : {GetCurrentPreset(true)}";
                    Plugin.Instance.WriteStringToFile(presetString);
                }

                try
                {
                    // Handle dropdowns below the top row
                    if (dropdownExpanded)
                    {
                        for (int i = 0; i < DroneCommand.playerNames.Count; i++)
                        {
                            Rect optionRect = new Rect(0, buttonHeight + i * buttonHeight, dropdownWidth, buttonHeight);
                            if (GUI.Button(optionRect, DroneCommand.playerNames[i]))
                            {
                                SetTarget(DroneCommand.GetPlayer(DroneCommand.playerNames[i]));
                                dropdownExpanded = false;
                            }
                        }
                    }

                    if (modeDropdownExpanded)
                    {
                        string[] modes = Enum.GetNames(typeof(FollowMode));
                        for (int i = 0; i < modes.Length; i++)
                        {
                            Rect optionRect = new Rect(dropdownWidth, buttonHeight + i * buttonHeight, modeWidth, buttonHeight);
                            if (GUI.Button(optionRect, modes[i]))
                            {
                                followMode = (FollowMode)Enum.Parse(typeof(FollowMode), modes[i]);
                                modeDropdownExpanded = false;
                            }
                        }
                    }
                }
                catch
                {
                    Debug.Log("Players probably changed!");
                }
            }

            // Allow normal dragging with left-click in top area
            GUI.DragWindow(new Rect(0, 20, windowRect.width, windowRect.height));
        }

        void ResizeRenderTexture(int width, int height)
        {
            if (camTexture != null)
            {
                camTexture.Release();
                Destroy(camTexture);
            }

            var newTex = new RenderTexture(width, height, 16, RenderTextureFormat.ARGB32);

            if (mainCamera != null)
                mainCamera.targetTexture = newTex;

            if (skyboxCamera != null)
                skyboxCamera.targetTexture = newTex;

            camTexture = newTex;
        }

        public string GetCurrentPreset(bool usePixels = true)
        {
            string mode = followMode.ToString().ToLower(); // "strict" or "smooth"
            string target = targetPlayer != null ? targetPlayer.username : "";
            string unit = usePixels ? "px" : "%";

            float x = usePixels ? Mathf.RoundToInt(windowRect.x) : windowRect.x / Screen.width;
            float y = usePixels ? Mathf.RoundToInt(windowRect.y) : windowRect.y / Screen.height;
            float width = usePixels ? Mathf.RoundToInt(windowRect.width) : windowRect.width / Screen.width;
            float height = usePixels ? Mathf.RoundToInt(windowRect.height) : windowRect.height / Screen.height;

            string presetString = $"{mode};{target};{unit};{x.ToString(System.Globalization.CultureInfo.InvariantCulture)};" +
                                  $"{y.ToString(System.Globalization.CultureInfo.InvariantCulture)};" +
                                  $"{width.ToString(System.Globalization.CultureInfo.InvariantCulture)};" +
                                  $"{height.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

            return presetString;
        }


        void OnDestroy()
        {
            if (camTexture != null)
            {
                camTexture.Release();
            }
        }
    }
}*/