using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Eduzo.Games.DataHandling
{
    public class DataTableManager : MonoBehaviour
    {
        [Header("Setup")]
        public GameObject tableRowPrefab;
        public Transform tableContainer;

        // Names to match our data
        private string[] categories = { "Oranges", "Apples", "Grapes" };

        // --- THE FIX: Now this script has TWO hands to catch both pieces of info! ---
        public void GenerateTable(List<int> data, int targetIndex)
        {
            // 1. Clear old data from the board
            foreach (Transform child in tableContainer) Destroy(child.gameObject);

            // 2. Spawn the Header Row (Matching Varun's "ITEMS" mockup)
            SpawnRow("ITEMS", "COUNT", true, false);

            // 3. Loop through the data
            for (int i = 0; i < data.Count; i++)
            {
                string catName = (i < categories.Length) ? categories[i] : "Item";

                // If this is the row the player needs to answer, we show the blank box logic
                bool isTarget = (i == targetIndex);

                SpawnRow(catName, data[i].ToString(), false, isTarget);
            }
        }

        private void SpawnRow(string col1, string col2, bool isHeader, bool isTarget)
        {
            GameObject newRow = Instantiate(tableRowPrefab, tableContainer);

            // Grab the Text components inside the row
            TextMeshProUGUI[] texts = newRow.GetComponentsInChildren<TextMeshProUGUI>();

            if (texts.Length >= 2)
            {
                texts[0].text = col1;

                if (isHeader)
                {
                    texts[0].color = Color.black;
                    texts[1].text = col2;
                    texts[1].color = Color.black;
                }
                else
                {
                    if (isTarget)
                    {
                        // Show a "?" for the one they need to scan
                        texts[1].text = "?";
                        texts[1].color = Color.red; // Make it pop so they know it's the target
                    }
                    else
                    {
                        texts[1].text = col2;
                        texts[1].color = Color.black;
                    }
                }
            }

            // --- OPTIONAL: Visual Box Logic ---
            Image inputBox = newRow.GetComponentInChildren<Image>();
            if (inputBox != null && !isHeader)
            {
                inputBox.enabled = isTarget; // Only show the box outline for the target row
            }
        }
    }
}