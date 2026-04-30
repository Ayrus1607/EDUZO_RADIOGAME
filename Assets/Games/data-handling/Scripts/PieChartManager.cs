using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace Eduzo.Games.DataHandling
{
    public class PieChartManager : MonoBehaviour
    {
        [Header("Setup")]
        public GameObject slicePrefab;
        public Transform pieContainer;
        public float labelDistance = 120f;

        [Header("Legend Setup")]
        public GameObject legendItemPrefab;
        public Transform legendContainer;

        [Header("Colors")]
        public List<Color> sliceColors;

        // --- UPDATED: Added targetIndex to know exactly which slice gets the '?' ---
        public void GeneratePieChart(List<int> data, List<string> categoryNames, int targetIndex)
        {
            // 1. Clean up old slices and legends
            foreach (Transform child in pieContainer) Destroy(child.gameObject);
            if (legendContainer != null)
            {
                foreach (Transform child in legendContainer) Destroy(child.gameObject);
            }

            // 2. Sum the data
            float total = 0;
            foreach (int amount in data) total += amount;
            if (total == 0) return;

            // 3. Draw Slices, Labels, and Legends
            float zRotation = 0f;
            for (int i = 0; i < data.Count; i++)
            {
                // --- SLICE DRAWING ---
                GameObject newSlice = Instantiate(slicePrefab, pieContainer);
                Image sliceImage = newSlice.GetComponent<Image>();

                if (sliceColors != null && sliceColors.Count > 0)
                    sliceImage.color = sliceColors[i % sliceColors.Count];

                float fillAmount = (float)data[i] / total;
                sliceImage.fillAmount = fillAmount;
                newSlice.transform.localRotation = Quaternion.Euler(0, 0, -zRotation);

                // --- THE LABEL MATH ---
                TextMeshProUGUI labelText = newSlice.transform.GetComponentInChildren<TextMeshProUGUI>();
                if (labelText != null)
                {
                    // Calculate exact percentage
                    float percentage = fillAmount * 100f;

                    // --- NEW MAGIC: Only hide the text if it matches the GameManager's target! ---
                    if (i == targetIndex)
                    {
                        labelText.text = "?";
                    }
                    else
                    {
                        labelText.text = Mathf.RoundToInt(percentage) + "%";
                    }

                    // Push the text out from the center
                    float halfAngleRad = (fillAmount * 360f / 2f) * Mathf.Deg2Rad;
                    float localX = Mathf.Sin(halfAngleRad) * labelDistance;
                    float localY = Mathf.Cos(halfAngleRad) * labelDistance;

                    labelText.rectTransform.anchoredPosition = new Vector2(localX, localY);
                    labelText.rectTransform.localRotation = Quaternion.Euler(0, 0, zRotation);
                }

                // --- LEGEND DRAWING ---
                if (legendItemPrefab != null && legendContainer != null)
                {
                    GameObject newLegend = Instantiate(legendItemPrefab, legendContainer);
                    Image colorBox = newLegend.transform.Find("Color_Box")?.GetComponent<Image>();
                    TextMeshProUGUI categoryText = newLegend.transform.Find("Category_Name")?.GetComponent<TextMeshProUGUI>();

                    if (colorBox != null) colorBox.color = sliceImage.color;
                    if (categoryText != null)
                    {
                        // Use the custom names passed in from the Form!
                        string catName = (categoryNames != null && i < categoryNames.Count) ? categoryNames[i] : "Item " + (i + 1);
                        categoryText.text = catName;
                    }
                }

                // Advance the rotation for the next slice
                zRotation += fillAmount * 360f;
            }
        }
    }
}