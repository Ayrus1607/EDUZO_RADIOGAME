using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace Eduzo.Games.DataHandling
{
    public class BarGraphManager : MonoBehaviour
    {
        [Header("Setup")]
        public GameObject columnPrefab;
        public Transform graphContainer;
        public Transform yAxisLine;
        public TextMeshProUGUI fontStyleReference;

        public float maxHeight = 350f;

        [Header("Chalk Colors")]
        public List<Color> chalkColors;

        public void GenerateGraph(List<int> data, List<string> categoryNames, int dynamicYRange = 0)
        {
            // 1. Clean up old bars and old ticks
            foreach (Transform child in graphContainer) Destroy(child.gameObject);
            if (yAxisLine != null)
            {
                foreach (Transform child in yAxisLine) Destroy(child.gameObject);
            }

            // 2. Find the highest number
            int maxValue = 0;
            foreach (int amount in data)
            {
                if (amount > maxValue) maxValue = amount;
            }
            if (maxValue < 1) maxValue = 1;

            // 3. Smart Y-Axis Ceiling
            int graphCeiling = dynamicYRange > 0 ? dynamicYRange : Mathf.CeilToInt(maxValue / 10f) * 10;
            if (graphCeiling < 10) graphCeiling = 10;

            int stepSize = graphCeiling / 10;
            if (maxValue <= 10) { graphCeiling = 10; stepSize = 1; }

            // 4. Draw the Y-Axis Ticks & Numbers
            if (yAxisLine != null && fontStyleReference != null)
            {
                for (int i = stepSize; i <= graphCeiling; i += stepSize)
                {
                    GameObject tick = new GameObject("Tick_" + i);
                    tick.transform.SetParent(yAxisLine, false);
                    Image img = tick.AddComponent<Image>();
                    img.color = Color.white;
                    RectTransform rt = tick.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 0);
                    rt.anchorMax = new Vector2(0.5f, 0);
                    rt.pivot = new Vector2(0f, 0.5f);
                    rt.sizeDelta = new Vector2(20, 4);

                    // Height based on ceiling
                    float heightPos = maxHeight * ((float)i / graphCeiling);
                    rt.anchoredPosition = new Vector2(0, heightPos);

                    GameObject txtObj = new GameObject("Num_" + i);
                    txtObj.transform.SetParent(tick.transform, false);
                    TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
                    txt.font = fontStyleReference.font;
                    txt.fontSize = fontStyleReference.fontSize;
                    txt.color = fontStyleReference.color;
                    txt.alignment = TextAlignmentOptions.Right | TextAlignmentOptions.Capline;
                    txt.text = i.ToString();

                    RectTransform txtRt = txtObj.GetComponent<RectTransform>();
                    txtRt.pivot = new Vector2(1f, 0.5f);
                    txtRt.sizeDelta = new Vector2(150, 50);
                    txtRt.anchoredPosition = new Vector2(-60, 0);
                }
            }

            // 5. Measure the container width
            RectTransform containerRect = graphContainer.GetComponent<RectTransform>();
            float totalWidth = containerRect != null ? containerRect.rect.width : 600f;
            if (totalWidth <= 0) totalWidth = 600f;

            // Subtract the Unity Spacing so we know exactly how much room the bars actually have
            HorizontalLayoutGroup layoutGroup = graphContainer.GetComponent<HorizontalLayoutGroup>();
            if (layoutGroup != null && data.Count > 0)
            {
                float padding = layoutGroup.padding.left + layoutGroup.padding.right;
                float spacing = layoutGroup.spacing * (data.Count - 1);
                totalWidth = totalWidth - padding - spacing;
            }

            // Divide the usable space by the number of items
            float columnWidth = totalWidth / data.Count;

            // --- THE FIX: Let the bar take up 100% of the column width! ---
            // The only gap between the bars will be the "10" or "15" spacing you set in Unity.
            float barWidth = columnWidth;
            if (barWidth > 150f) barWidth = 150f;

            // 6. Draw the Bars
            for (int i = 0; i < data.Count; i++)
            {
                GameObject newColumn = Instantiate(columnPrefab, graphContainer);

                RectTransform columnRt = newColumn.GetComponent<RectTransform>();
                if (columnRt != null)
                {
                    columnRt.sizeDelta = new Vector2(columnWidth, columnRt.sizeDelta.y);
                }

                Image barImage = newColumn.transform.Find("Bar_Chalk")?.GetComponent<Image>();
                TextMeshProUGUI categoryText = newColumn.transform.Find("Category_Text")?.GetComponent<TextMeshProUGUI>();

                if (barImage != null)
                {
                    if (chalkColors != null && chalkColors.Count > 0) barImage.color = chalkColors[i % chalkColors.Count];

                    float heightPercentage = (float)data[i] / graphCeiling;
                    barImage.rectTransform.sizeDelta = new Vector2(barWidth, maxHeight * heightPercentage);
                }

                if (categoryText != null)
                {
                    string catName = (categoryNames != null && i < categoryNames.Count) ? categoryNames[i] : "Item";
                    categoryText.text = catName;

                    categoryText.rectTransform.sizeDelta = new Vector2(columnWidth, categoryText.rectTransform.sizeDelta.y);

                    categoryText.enableAutoSizing = true;
                    categoryText.fontSizeMin = 10;
                    categoryText.fontSizeMax = 28;
                }
            }
        }
    }
}