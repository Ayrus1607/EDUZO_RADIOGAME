using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Eduzo.Games.DataHandling
{
    public class TallyMarkManager : MonoBehaviour
    {
        [Header("Prefabs & Containers")]
        public GameObject tallyRowPrefab;
        public GameObject tallyBundlePrefab;
        public Transform mainTallyContainer;

        [Header("Colors")]
        public Color[] rowColors;

        public void GenerateTallyMarks(List<int> data)
        {
            // 1. Clear old data from the board
            foreach (Transform child in mainTallyContainer) Destroy(child.gameObject);

            // 2. Loop through each math value (Apples: 5, Oranges: 12, Bananas: 8)
            for (int i = 0; i < data.Count; i++)
            {
                int amount = data[i];
                Color rowColor = (i < rowColors.Length) ? rowColors[i] : Color.white;

                // 3. Spawn a Row for this specific category
                GameObject newRow = Instantiate(tallyRowPrefab, mainTallyContainer);

                // 4. Calculate full bundles of 5, and the remainder (e.g. 12 = two bundles of 5, one bundle of 2)
                int fullBundles = amount / 5;
                int remainder = amount % 5;

                // 5. Spawn the full bundles
                for (int b = 0; b < fullBundles; b++)
                {
                    SpawnBundle(5, newRow.transform, rowColor);
                }

                // 6. Spawn the remainder
                if (remainder > 0)
                {
                    SpawnBundle(remainder, newRow.transform, rowColor);
                }
            }
        }

        private void SpawnBundle(int linesToShow, Transform parentRow, Color color)
        {
            GameObject bundle = Instantiate(tallyBundlePrefab, parentRow);

            // Turn on the exact number of lines we need, and color them!
            for (int i = 0; i < bundle.transform.childCount; i++)
            {
                Transform lineTransform = bundle.transform.GetChild(i);
                lineTransform.gameObject.SetActive(i < linesToShow);

                Image lineImg = lineTransform.GetComponent<Image>();
                if (lineImg != null) lineImg.color = color;
            }
        }
    }
}