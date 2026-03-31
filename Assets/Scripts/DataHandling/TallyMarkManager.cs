using UnityEngine;
using TMPro;
using System.Collections.Generic;

namespace Eduzo.Games.DataHandling
{
    public class TallyMarkManager : MonoBehaviour
    {
        [Header("Prefabs")]
        public GameObject tallyRowPrefab;
        public GameObject tallyBundlePrefab;
        public GameObject tallySinglePrefab;

        [Header("Container")]
        public Transform tallyContainer; // The chalkboard area that holds all the rows

        // --- UPDATED: Now it strictly uses the custom names from the GameManager ---
        public void GenerateTallyMarks(List<int> data, List<string> categoryNames)
        {
            // 1. Clear out any old rows
            foreach (Transform child in tallyContainer) Destroy(child.gameObject);

            // 2. Loop through the numbers and spawn the rows
            for (int i = 0; i < data.Count; i++)
            {
                // Spawn the Row 
                GameObject newRow = Instantiate(tallyRowPrefab, tallyContainer);

                // Set the Category Text using your custom names!
                TextMeshProUGUI rowText = newRow.transform.GetComponentInChildren<TextMeshProUGUI>();
                if (rowText != null)
                {
                    rowText.text = (categoryNames != null && i < categoryNames.Count) ? categoryNames[i] : "Item " + (i + 1);
                }

                // Find the spawn area on the right side of the row
                Transform spawnArea = newRow.transform.Find("Canvas/Tally_Spawn_Area");
                if (spawnArea != null)
                {
                    // --- THE MATH! ---
                    int bundles = data[i] / 5; // How many groups of 5?
                    int singles = data[i] % 5; // What is the remainder?

                    // Spawn the bundles first
                    for (int b = 0; b < bundles; b++)
                    {
                        Instantiate(tallyBundlePrefab, spawnArea);
                    }

                    // Spawn the single lines next
                    for (int s = 0; s < singles; s++)
                    {
                        Instantiate(tallySinglePrefab, spawnArea);
                    }
                }
            }
        }
    }
}