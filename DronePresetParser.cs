using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PhotomodeMultiview
{
    public static class DronePresetParser
    {
        public static List<DronePresetGroup> ParseAllGroups(string input)
        {
            var groups = new List<DronePresetGroup>();
            if (string.IsNullOrWhiteSpace(input))
                return groups;

            // You can adjust this to dynamically fetch actual screen size if needed
            int screenWidth = Mathf.RoundToInt(Screen.width);
            int screenHeight = Mathf.RoundToInt(Screen.height);

            var rawGroups = input.Split(new[] { "||" }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (var rawGroup in rawGroups)
            {
                var parts = rawGroup.Split('|');
                if (parts.Length < 2)
                    continue;

                var groupName = parts[0].Trim();
                if (string.IsNullOrEmpty(groupName))
                {
                    groupName = "GroupName";
                }

                var group = new DronePresetGroup(groupName);

                for (int i = 1; i < parts.Length; i++)
                {
                    var trimmed = parts[i].Trim();
                    if (string.IsNullOrEmpty(trimmed))
                        continue;

                    try
                    {
                        var dp = new DronePreset(trimmed);

                        if (dp.UsePixels)
                        {
                            // Validate pixel size is positive and not larger than screen
                            if (dp.Width <= 0 || dp.Width > screenWidth || dp.Height <= 0 || dp.Height > screenHeight)
                            {
                                Debug.LogWarning($"DronePresetParser: Skipped invalid px preset '{trimmed}' in group '{groupName}'. Window size exceeds screen dimensions.");
                                continue;
                            }
                        }
                        else
                        {
                            // Validate percentage values for size only, position can be outside [0–1]
                            if (dp.Width <= 0 || dp.Width > 1 || dp.Height <= 0 || dp.Height > 1)
                            {
                                Debug.LogWarning($"DronePresetParser: Skipped invalid % preset '{trimmed}' in group '{groupName}'. Window size must be between 0–1.");
                                continue;
                            }
                        }

                        group.Presets.Add(dp);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"DronePresetParser: Skipped invalid preset '{trimmed}' in group '{groupName}'. Reason: {e.Message}");
                    }
                }

                groups.Add(group);
            }

            return groups;
        }

    }

}
