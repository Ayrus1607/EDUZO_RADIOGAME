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

        [Header("Input Pairs (Must be exactly 3 of each)")]
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

            if (uploadPanel != null) uploadPanel.SetActive(mode == 3);

            foreach (var input in valueInputs)
            {
                if (!int.TryParse(input.text, out int parsedValue))
                {
                    isValid = false;
                }
            }

            if (mode == 1)
            {
                int sum = 0;
                foreach (var input in valueInputs)
                {
                    int val = 0;
                    int.TryParse(input.text, out val);
                    sum += val;
                }

                if (sum != 100)
                {
                    isValid = false;
                    if (pieChartWarning != null) { pieChartWarning.gameObject.SetActive(true); pieChartWarning.text = $"Pie Chart total must be 100! (Current: {sum})"; }
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

            for (int i = 0; i < 3; i++)
            {
                string itemName = string.IsNullOrEmpty(nameInputs[i].text) ? "Item " + (i + 1) : nameInputs[i].text;
                newQ.categoryNames.Add(itemName);

                int val = 0;
                int.TryParse(valueInputs[i].text, out val);
                newQ.dataValues.Add(val);
            }

            int randomTarget = Random.Range(0, 3);
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

            Debug.Log($"Added Question: {newQ.questionText} | Answer: {newQ.correctAnswer}");

            // --- NEW: WIPE THE FORM CLEAN ---

            // 1. Reset Dropdown to the top choice
            if (modeDropdown != null) modeDropdown.value = 0;

            // 2. Clear all Item Name boxes
            if (nameInputs != null)
            {
                foreach (var input in nameInputs) { if (input != null) input.text = ""; }
            }

            // 3. Clear all Item Value boxes
            if (valueInputs != null)
            {
                foreach (var input in valueInputs) { if (input != null) input.text = ""; }
            }

            // 4. Clear Image Upload stuff
            customLoadedSprite = null;
            if (imagePreview != null) imagePreview.sprite = null;
            if (imagePathInput != null) imagePathInput.text = "";

            // 5. Force the UI to update so the Add button locks itself again!
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