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

        public void GenerateGraph(List<int> data, List<string> categoryNames)
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

            // --- FIXED MATH: Calculate Ceiling and Steps ---
            int graphCeiling = Mathf.CeilToInt(maxValue / 10f) * 10;
            if (graphCeiling < 10) graphCeiling = 10;

            int stepSize = graphCeiling / 10;
            if (maxValue <= 10) { graphCeiling = 10; stepSize = 1; }

            // 3. Draw the Y-Axis Ticks & Numbers (Only spawns up to 10 ticks now!)
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

            // 4. Draw the Bars
            for (int i = 0; i < data.Count; i++)
            {
                GameObject newColumn = Instantiate(columnPrefab, graphContainer);

                Image barImage = newColumn.transform.Find("Bar_Chalk")?.GetComponent<Image>();
                TextMeshProUGUI categoryText = newColumn.transform.Find("Category_Text")?.GetComponent<TextMeshProUGUI>();

                if (barImage == null || categoryText == null) continue;

                if (chalkColors != null && chalkColors.Count > 0) barImage.color = chalkColors[i % chalkColors.Count];

                // Height based on ceiling
                float heightPercentage = (float)data[i] / graphCeiling;
                barImage.rectTransform.sizeDelta = new Vector2(barImage.rectTransform.sizeDelta.x, maxHeight * heightPercentage);

                if (categoryNames != null && categoryNames.Count > 0)
                {
                    string catName = (i < categoryNames.Count) ? categoryNames[i] : "Item";
                    categoryText.text = catName;
                }
            }
        }
    }
}