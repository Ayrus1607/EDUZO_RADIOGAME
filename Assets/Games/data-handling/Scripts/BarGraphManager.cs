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

            // 2. Find the highest number to set the ceiling
            int maxValue = 0;
            foreach (int amount in data)
            {
                if (amount > maxValue) maxValue = amount;
            }
            if (maxValue < 1) maxValue = 1;

            // 3. Smart Y-Axis Ceiling: Exact Max Value!
            // We no longer round up to the nearest 10. The highest bar touches the exact top.
            int graphCeiling = maxValue;

            // 4. Extract EXACT unique values for the Y-Axis
            List<int> uniqueValues = new List<int>();
            foreach (int val in data)
            {
                // We grab every unique number the teacher typed (ignoring 0)
                if (val > 0 && !uniqueValues.Contains(val))
                {
                    uniqueValues.Add(val);
                }
            }
            uniqueValues.Sort(); // Sort from lowest to highest

            // 5. Draw the Y-Axis Ticks & Numbers Exactly at Data Heights
            if (yAxisLine != null && fontStyleReference != null)
            {
                foreach (int val in uniqueValues)
                {
                    GameObject tick = new GameObject("Tick_" + val);
                    tick.transform.SetParent(yAxisLine, false);
                    Image img = tick.AddComponent<Image>();
                    img.color = Color.white;
                    RectTransform rt = tick.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 0);
                    rt.anchorMax = new Vector2(0.5f, 0);
                    rt.pivot = new Vector2(0f, 0.5f);
                    rt.sizeDelta = new Vector2(20, 4);

                    // Height perfectly matches the exact value!
                    float heightPos = maxHeight * ((float)val / graphCeiling);
                    rt.anchoredPosition = new Vector2(0, heightPos);

                    GameObject txtObj = new GameObject("Num_" + val);
                    txtObj.transform.SetParent(tick.transform, false);
                    TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
                    txt.font = fontStyleReference.font;
                    txt.fontSize = fontStyleReference.fontSize;
                    txt.color = fontStyleReference.color;
                    txt.alignment = TextAlignmentOptions.Right | TextAlignmentOptions.Capline;
                    txt.text = val.ToString();

                    RectTransform txtRt = txtObj.GetComponent<RectTransform>();
                    txtRt.pivot = new Vector2(1f, 0.5f);
                    txtRt.sizeDelta = new Vector2(150, 50);
                    txtRt.anchoredPosition = new Vector2(-60, 0);
                }
            }

            // 6. Measure the container width
            RectTransform containerRect = graphContainer.GetComponent<RectTransform>();
            float totalWidth = containerRect != null ? containerRect.rect.width : 600f;
            if (totalWidth <= 0) totalWidth = 600f;

            HorizontalLayoutGroup layoutGroup = graphContainer.GetComponent<HorizontalLayoutGroup>();
            if (layoutGroup != null && data.Count > 0)
            {
                float padding = layoutGroup.padding.left + layoutGroup.padding.right;
                float spacing = layoutGroup.spacing * (data.Count - 1);
                totalWidth = totalWidth - padding - spacing;
            }

            float columnWidth = totalWidth / data.Count;
            float barWidth = columnWidth;
            if (barWidth > 150f) barWidth = 150f;

            // 7. Draw the Bars
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