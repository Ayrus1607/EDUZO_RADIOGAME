using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Eduzo.Games.DataHandling
{
    public class BarGraphManager : MonoBehaviour
    {
        [Header("References")]
        public GameObject barPrefab;
        public Transform barsAreaParent;

        [Header("Settings")]
        public float maxBarHeight = 400f; // Max height a bar can reach in pixels
        public Color[] barColors; // We will set these in the Inspector

        // Temporary test function just to see it work!
        private void Start()
        {
            // Simulating a question: "Apples = 5, Oranges = 12, Bananas = 8"
            List<int> testData = new List<int> { 5, 12, 8 };
            GenerateGraph(testData);
        }

        public void GenerateGraph(List<int> dataValues)
        {
            // 1. Clear out any old bars
            foreach (Transform child in barsAreaParent)
            {
                Destroy(child.gameObject);
            }

            if (dataValues == null || dataValues.Count == 0) return;

            // 2. Find highest number
            int maxValue = 0;
            foreach (int val in dataValues)
            {
                if (val > maxValue) maxValue = val;
            }

            // 3. Spawn the bars!
            for (int i = 0; i < dataValues.Count; i++)
            {
                GameObject newBar = Instantiate(barPrefab, barsAreaParent);

                // --- THE UNITY GLITCH FIX ---
                // Force the scale to 1 and Z-position to 0 so it doesn't spawn behind the board!
                newBar.transform.localScale = Vector3.one;
                newBar.transform.localPosition = new Vector3(newBar.transform.localPosition.x, newBar.transform.localPosition.y, 0f);
                // ----------------------------

                Image barImage = newBar.GetComponent<Image>();
                if (barColors.Length > 0)
                {
                    barImage.color = barColors[i % barColors.Length];
                }

                float heightPercentage = (float)dataValues[i] / maxValue;
                float targetHeight = heightPercentage * maxBarHeight;

                RectTransform rt = newBar.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(100f, targetHeight);
            }
        }
    }
}