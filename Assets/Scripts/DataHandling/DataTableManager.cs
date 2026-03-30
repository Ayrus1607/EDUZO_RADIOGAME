using UnityEngine;
using TMPro;
using System.Collections.Generic;

namespace Eduzo.Games.DataHandling
{
    public class DataTableManager : MonoBehaviour
    {
        [Header("Setup")]
        public GameObject tableRowPrefab;
        public Transform tableContainer;

        // Hardcoded names to match our test data (Apples: 5, Oranges: 12, Bananas: 8)
        private string[] categories = { "Apples", "Oranges", "Bananas" };

        public void GenerateTable(List<int> data)
        {
            // 1. Clear old data from the board
            foreach (Transform child in tableContainer) Destroy(child.gameObject);

            // 2. Spawn the Header Row (Yellow and Bold!)
            SpawnRow("FRUITS", "AMOUNT", true);

            // 3. Loop through the math data and spawn the text rows
            for (int i = 0; i < data.Count; i++)
            {
                string catName = (i < categories.Length) ? categories[i] : "Item";
                SpawnRow(catName, data[i].ToString(), false);
            }
        }

        private void SpawnRow(string col1, string col2, bool isHeader)
        {
            GameObject newRow = Instantiate(tableRowPrefab, tableContainer);

            // Grab the two Text components inside the row
            TextMeshProUGUI[] texts = newRow.GetComponentsInChildren<TextMeshProUGUI>();

            if (texts.Length >= 2)
            {
                texts[0].text = col1;
                texts[1].text = col2;

                if (isHeader)
                {
                    texts[0].color = Color.yellow;
                    texts[1].color = Color.yellow;
                    texts[0].fontStyle = FontStyles.Bold;
                    texts[1].fontStyle = FontStyles.Bold;
                }
            }
        }
    }
}