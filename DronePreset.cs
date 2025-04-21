using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PhotomodeMultiview
{
    //structure of data
    //preset seperator: |
    //value seperator: ;
    //element0: Strict/Smooth (FollowMode.Strict, FollowMode.Smooth. Parse the string into the enum, default is FollowMode.Smooth
    //element1: Target string. Name of the target player. Can be empty.
    //element2: Rect behaviour. px for pixels, % for screen percentage based from 0f - 1f.
    //element3: x position (in percentage float, or pixel int | rounded to int)
    //element4: y position (...)
    //element5: width (in percentage of screen float, or pixel in | rounded to int)
    //element6: height (...)


    //strict/smooth;target;px/%;x;y;width;height|strict/smooth;target;px/%;x;y;width;height

    public class DronePreset
    {
        public FollowMode Mode { get; private set; }
        public string Target { get; private set; }
        public bool UsePixels { get; private set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }

        public bool Valid { get; private set; }

        public DronePreset(string data)
        {
            var parts = data.Split(';');
            if (parts.Length != 7)
            {
                //UnityEngine.Debug.LogWarning("DronePreset: Invalid input string");
                Valid = false;
                return;
            }

            // Parse FollowMode (defaults to Smooth)
            if (!System.Enum.TryParse(parts[0], true, out FollowMode parsedMode))
                parsedMode = FollowMode.Smooth;
            Mode = parsedMode;

            Target = parts[1];

            UsePixels = parts[2].ToLower() == "px";

            // Parse positions and dimensions
            if (UsePixels)
            {
                X = Mathf.RoundToInt(ParseFloat(parts[3]));
                Y = Mathf.RoundToInt(ParseFloat(parts[4]));
                Width = Mathf.RoundToInt(ParseFloat(parts[5]));
                Height = Mathf.RoundToInt(ParseFloat(parts[6]));
            }
            else
            {
                X = ParseFloat(parts[3]);
                Y = ParseFloat(parts[4]);
                Width = ParseFloat(parts[5]);
                Height = ParseFloat(parts[6]);
            }

            Valid = true;
        }

        public DronePreset(PhotoDrone drone, bool usePixels = true)
        {
            Mode = drone.followMode;
            Target = drone.targetPlayer != null ? drone.targetPlayer.username : "";
            UsePixels = usePixels;

            RectTransform rect = drone.droneUI.GetComponent<RectTransform>();
            Vector2 anchoredPos = rect.anchoredPosition;
            Vector2 size = rect.sizeDelta;

            if (usePixels)
            {
                X = Mathf.RoundToInt(anchoredPos.x);
                Y = Mathf.RoundToInt(-anchoredPos.y); // Top-left anchor, Y is inverted
                Width = Mathf.RoundToInt(size.x);
                Height = Mathf.RoundToInt(size.y);
            }
            else
            {
                X = anchoredPos.x / Screen.width;
                Y = -anchoredPos.y / Screen.height;
                Width = size.x / Screen.width;
                Height = size.y / Screen.height;
            }

            Valid = true;
        }

        private float ParseFloat(string s)
        {
            if (float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float result))
                return result;
            return 0f;
        }
    }
}
