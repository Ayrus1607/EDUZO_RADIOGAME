using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Collections;

namespace Eduzo.Games.DataHandling
{
    public class DataFormController : MonoBehaviour
    {
        public DataGameManager gameManager;

        [Header("Form UI")]
        public TMP_Dropdown modeDropdown;

        [Header("NEW: Range Restriction")]
        public TMP_Dropdown rangeDropdown; // Options should be text like "10", "20", "30"
        public TextMeshProUGUI rangeWarningText;

        [Header("Input Pairs")]
        public TMP_InputField[] nameInputs;
        public TMP_InputField[] valueInputs;

        [Header("Buttons & Warnings")]
        public Button addButton;
        public TextMeshProUGUI pieChartWarning;

        [Header("Custom Image Upload (Mode 3)")]
        public GameObject uploadPanel;
        public TMP_InputField imagePathInput;
        public Button loadImageButton;
        public Image imagePreview;

        private Sprite customLoadedSprite;

        void Start()
        {
            modeDropdown.onValueChanged.AddListener(delegate { UpdateFormUI(); });

            // Listen for range changes
            if (rangeDropdown != null)
                rangeDropdown.onValueChanged.AddListener(delegate { UpdateFormUI(); });

            foreach (var input in valueInputs)
            {
                input.onValueChanged.AddListener(delegate { UpdateFormUI(); });
            }

            if (loadImageButton != null)
            {
                loadImageButton.onClick.AddListener(OnLoadImageClicked);
            }

            UpdateFormUI();
        }

        public void UpdateFormUI()
        {
            int mode = modeDropdown.value;
            bool isValid = true;
            int validItemCount = 0;
            int sum = 0;
            bool isOverRange = false;

            if (uploadPanel != null) uploadPanel.SetActive(mode == 3);

            // --- NEW FIX: Get the Max Range Allowed from Dropdown ---
            int maxRangeAllowed = 10;
            if (rangeDropdown != null && rangeDropdown.options.Count > 0)
            {
                // Grabs the text (e.g. "20") and parses it to an int
                int.TryParse(rangeDropdown.options[rangeDropdown.value].text, out maxRangeAllowed);
                if (maxRangeAllowed <= 0) maxRangeAllowed = 10; // Fallback
            }

            // Check all input fields
            for (int i = 0; i < valueInputs.Length; i++)
            {
                string textValue = valueInputs[i].text.Trim();

                if (!string.IsNullOrEmpty(textValue))
                {
                    if (int.TryParse(textValue, out int parsedValue))
                    {
                        validItemCount++;
                        sum += parsedValue;

                        // Check if the value breaks the range rule! (Skip checking for Pie Chart)
                        if (mode != 1 && parsedValue > maxRangeAllowed)
                        {
                            isValid = false;
                            isOverRange = true;
                        }
                    }
                    else
                    {
                        isValid = false;
                    }
                }
            }

            if (validItemCount == 0) isValid = false;

            // --- NEW FIX: Show Range Warning ---
            if (rangeWarningText != null)
            {
                if (isOverRange)
                {
                    rangeWarningText.text = $"Values cannot exceed Max Range ({maxRangeAllowed})!";
                    rangeWarningText.gameObject.SetActive(true);
                }
                else
                {
                    rangeWarningText.gameObject.SetActive(false);
                }
            }

            // Pie Chart Specific Logic
            if (mode == 1)
            {
                if (sum != 100)
                {
                    isValid = false;
                    if (pieChartWarning != null)
                    {
                        pieChartWarning.gameObject.SetActive(true);
                        pieChartWarning.text = $"Pie Chart total must be 100! (Current: {sum})";
                    }
                }
                else
                {
                    if (pieChartWarning != null) pieChartWarning.gameObject.SetActive(false);
                }
            }
            else
            {
                if (pieChartWarning != null) pieChartWarning.gameObject.SetActive(false);
            }

            if (addButton != null) addButton.interactable = isValid;
        }

        private void OnLoadImageClicked()
        {
            string path = imagePathInput.text.Trim();
            path = path.Replace("\"", "");
            path = path.Replace("'", "");

            if (string.IsNullOrEmpty(path)) return;

            if (!path.StartsWith("http") && !path.StartsWith("file://"))
            {
                path = "file:///" + path.Replace("\\", "/");
            }

            StartCoroutine(DownloadImage(path));
        }

        private IEnumerator DownloadImage(string url)
        {
            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
            {
                yield return uwr.SendWebRequest();

                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Failed to load image: " + uwr.error);
                }
                else
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(uwr);
                    customLoadedSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

                    if (imagePreview != null) imagePreview.sprite = customLoadedSprite;
                }
            }
        }

        public void SubmitNewQuestion()
        {
            QuestionData newQ = new QuestionData();
            newQ.preferredMode = modeDropdown.value;
            newQ.categoryNames = new List<string>();
            newQ.dataValues = new List<int>();

            for (int i = 0; i < valueInputs.Length; i++)
            {
                string textValue = valueInputs[i].text.Trim();

                if (!string.IsNullOrEmpty(textValue) && int.TryParse(textValue, out int val))
                {
                    string itemName = string.IsNullOrEmpty(nameInputs[i].text) ? "Item " + (i + 1) : nameInputs[i].text;
                    newQ.categoryNames.Add(itemName);
                    newQ.dataValues.Add(val);
                }
            }

            int randomTarget = Random.Range(0, newQ.dataValues.Count);
            newQ.questionText = $"How many {newQ.categoryNames[randomTarget]} are there?";
            newQ.correctAnswer = newQ.dataValues[randomTarget].ToString();
            newQ.targetIndex = randomTarget;

            if (newQ.preferredMode == 3 && customLoadedSprite != null)
            {
                newQ.questionImage = customLoadedSprite;
            }

            gameManager.questionBank.Add(newQ);

            if (gameManager != null && gameManager.correctSound != null)
            {
                gameManager.correctSound.Play();
            }

            Debug.Log($"Added Question with {newQ.dataValues.Count} items!");

            // Wipe the form clean
            if (modeDropdown != null) modeDropdown.value = 0;
            if (rangeDropdown != null) rangeDropdown.value = 0; // Reset range dropdown too

            if (nameInputs != null)
            {
                foreach (var input in nameInputs) { if (input != null) input.text = ""; }
            }

            if (valueInputs != null)
            {
                foreach (var input in valueInputs) { if (input != null) input.text = ""; }
            }

            customLoadedSprite = null;
            if (imagePreview != null) imagePreview.sprite = null;
            if (imagePathInput != null) imagePathInput.text = "";

            UpdateFormUI();
        }

        public void PlayGame()
        {
            if (gameManager.questionBank.Count == 0)
            {
                Debug.LogWarning("You need to add a question first!");
                return;
            }

            if (gameManager != null && gameManager.correctSound != null)
            {
                gameManager.correctSound.Play();
            }

            gameManager.formScreen.SetActive(false);

            if (gameManager.modeSelectionScreen != null)
            {
                gameManager.modeSelectionScreen.SetActive(true);
            }
        }
    }
}