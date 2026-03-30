using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Eduzo.Games.DataHandling
{
    public class PieChartManager : MonoBehaviour
    {
        [Header("Setup")]
        public GameObject pieSlicePrefab;
        public Transform pieContainer;

        [Header("Slice Colors")]
        public Color[] sliceColors; // We will set Red, Grey, Purple in the Inspector

        public void GeneratePieChart(List<int> data)
        {
            // 1. Clean up old slices
            foreach (Transform child in pieContainer)
            {
                Destroy(child.gameObject);
            }

            // 2. Find the total sum
            float total = 0;
            foreach (int amount in data) total += amount;

            // 3. Draw the slices
            float currentRotation = 0f;

            for (int i = 0; i < data.Count; i++)
            {
                // Spawn the slice
                GameObject newSlice = Instantiate(pieSlicePrefab, pieContainer);
                Image sliceImage = newSlice.GetComponent<Image>();

                // Set Color
                if (i < sliceColors.Length) sliceImage.color = sliceColors[i];

                // Calculate how much of the pie this slice takes
                float fillPercentage = data[i] / total;
                sliceImage.fillAmount = fillPercentage;

                // Rotate it so it starts exactly where the last one ended
                newSlice.GetComponent<RectTransform>().localRotation = Quaternion.Euler(0, 0, -currentRotation);

                // Update rotation for the next slice
                currentRotation += (fillPercentage * 360f);
            }
        }
    }
}