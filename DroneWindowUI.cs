using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PhotomodeMultiview
{
    public class DroneWindowUI : MonoBehaviour
    {
        public RawImage feedImage;
        public Button playerButton;
        public Button modeButton;
        public Button logButton;
        public Button closeButton;
        public Button lockButton;
        public SpeedDisplay speedDisplay;
        public TMPro.TextMeshProUGUI nameUI;
        public TMPro.TextMeshProUGUI velocityUI;

        public Action OnLogPressed;
        public Action OnClosed;

        public Action<string> OnPlayerSelected;
        public Action<string> OnFollowModeSelected;

        private GameObject playerDropdownPanel;
        private GameObject modeDropdownPanel;

        private List<string> followModes = new List<string>();
        public bool locked = false;

        public void SetVisibility(bool state)
        {
            playerButton.gameObject.SetActive(state);
            modeButton.gameObject.SetActive(state);
            logButton.gameObject.SetActive(state);
            closeButton.gameObject.SetActive(state);
            lockButton.gameObject.SetActive(state);

            if(playerDropdownPanel != null)
            {
                playerDropdownPanel.SetActive(state);
            }

            if(modeDropdownPanel != null)
            {
                modeDropdownPanel.SetActive(state);
            }
        }

        public void SetLocked(bool state)
        {
            locked = state;
            Text txt = lockButton.GetComponentInChildren<Text>();
            txt.text = locked ? "%" : "/";
        }

        public void Initialize()
        {
            logButton?.onClick.AddListener(() => OnLogPressed?.Invoke());

            closeButton?.onClick.AddListener(() =>
            {
                OnClosed?.Invoke();
                Destroy(gameObject);
            });

            playerButton?.onClick.AddListener(TogglePlayerDropdown);
            modeButton?.onClick.AddListener(ToggleModeDropdown);
            lockButton?.onClick.AddListener(ToggleLock);
        }

        public void SetFollowModes(List<string> modes)
        {
            followModes = modes;
        }

        private void ToggleLock()
        {
            locked = !locked;
            Text txt = lockButton.GetComponentInChildren<Text>();
            txt.text = locked ? "%" : "/";
        }

        private void TogglePlayerDropdown()
        {
            if (playerDropdownPanel != null)
            {
                Destroy(playerDropdownPanel);
                return;
            }

            playerDropdownPanel = CreateDropdownPanel(DroneCommand.playerNames, (name) =>
            {
                OnPlayerSelected?.Invoke(name);
                Destroy(playerDropdownPanel);
            }, playerButton.transform);
        }

        private void ToggleModeDropdown()
        {
            if (modeDropdownPanel != null)
            {
                Destroy(modeDropdownPanel);
                return;
            }

            modeDropdownPanel = CreateDropdownPanel(followModes, (mode) =>
            {
                OnFollowModeSelected?.Invoke(mode);
                Destroy(modeDropdownPanel);
            }, modeButton.transform);
        }

        private GameObject CreateDropdownPanel(List<string> options, Action<string> onSelect, Transform anchor)
        {
            GameObject panel = new GameObject("DropdownPanel", typeof(RectTransform), typeof(Image));
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.SetParent(transform, false);
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.sizeDelta = new Vector2(100, options.Count * 20);

            Vector3 anchorPos = anchor.GetComponent<RectTransform>().anchoredPosition;
            rect.anchoredPosition = anchorPos + new Vector3(0, -22, 0);

            Image bg = panel.GetComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

            for (int i = 0; i < options.Count; i++)
            {
                string option = options[i];
                GameObject btn = CreateButton(option, new Vector2(0, -i * 20), rect);
                btn.GetComponent<Button>().onClick.AddListener(() => onSelect(option));
            }

            return panel;
        }

        private GameObject CreateButton(string text, Vector2 offset, RectTransform parent)
        {
            GameObject obj = new GameObject("Option", typeof(RectTransform), typeof(Button), typeof(Image));
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(100, 20);
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = offset;

            Image img = obj.GetComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.2f, 1);

            GameObject label = new GameObject("Text", typeof(RectTransform), typeof(Text));
            RectTransform tRect = label.GetComponent<RectTransform>();
            tRect.SetParent(obj.transform, false);
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;

            Text txt = label.GetComponent<Text>();
            txt.text = text;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.color = Color.white;
            txt.fontSize = 14;

            return obj;
        }
    }
}
