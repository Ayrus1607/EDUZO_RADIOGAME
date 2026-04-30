using UnityEngine;
using UnityEngine.UI;
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

        public void GenerateTallyMarks(List<int> data, List<string> categoryNames)
        {
            // 1. Clear out any old rows
            foreach (Transform child in tallyContainer) Destroy(child.gameObject);

            if (data == null || data.Count == 0) return;

            // --- THE FIX: DYNAMIC HEIGHT CALCULATION ---
            // Measure the container's height
            RectTransform containerRect = tallyContainer.GetComponent<RectTransform>();
            float totalHeight = containerRect != null ? containerRect.rect.height : 400f; // Fallback
            if (totalHeight <= 0) totalHeight = 400f;

            // Subtract any spacing Unity is adding automatically
            VerticalLayoutGroup layoutGroup = tallyContainer.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup != null)
            {
                float padding = layoutGroup.padding.top + layoutGroup.padding.bottom;
                float spacing = layoutGroup.spacing * (data.Count - 1);
                totalHeight = totalHeight - padding - spacing;
            }

            // Divide the usable height by the number of items
            float rowHeight = totalHeight / data.Count;

            // Put a cap so if there are only 2 items, the row doesn't become comically massive
            if (rowHeight > 100f) rowHeight = 100f;

            // 2. Loop through the numbers and spawn the rows
            for (int i = 0; i < data.Count; i++)
            {
                // Spawn the Row 
                GameObject newRow = Instantiate(tallyRowPrefab, tallyContainer);

                // --- APPLY THE HEIGHT MATERIALLY ---
                RectTransform rowRt = newRow.GetComponent<RectTransform>();
                if (rowRt != null)
                {
                    rowRt.sizeDelta = new Vector2(rowRt.sizeDelta.x, rowHeight);
                }

                // If your prefab uses a LayoutElement to dictate height, update that too
                LayoutElement layoutElement = newRow.GetComponent<LayoutElement>();
                if (layoutElement != null)
                {
                    layoutElement.preferredHeight = rowHeight;
                }

                // Set the Category Text and turn on Auto-Size!
                TextMeshProUGUI rowText = newRow.transform.GetComponentInChildren<TextMeshProUGUI>();
                if (rowText != null)
                {
                    rowText.text = (categoryNames != null && i < categoryNames.Count) ? categoryNames[i] : "Item " + (i + 1);
                    rowText.enableAutoSizing = true;
                    rowText.fontSizeMin = 10;
                    rowText.fontSizeMax = 36;
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