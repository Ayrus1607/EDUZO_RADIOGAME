using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Networking; // Needed for downloading custom images!
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
        public GameObject uploadPanel; // Hides/Shows the upload UI
        public TMP_InputField imagePathInput;
        public Button loadImageButton;
        public Image imagePreview;

        private Sprite customLoadedSprite; // Temporarily holds the uploaded picture

        void Start()
        {
            modeDropdown.onValueChanged.AddListener(delegate { UpdateFormUI(); });
            foreach (var input in valueInputs)
            {
                input.onValueChanged.AddListener(delegate { UpdateFormUI(); });
            }

            // Hook up the new Load Image button!
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

            // 1. Show Upload Panel ONLY if Look & Count (Mode 3) is selected
            if (uploadPanel != null) uploadPanel.SetActive(mode == 3);

            // 2. Pie Chart 100% Validation Rule
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

            // Lock the Add button if the math is wrong!
            if (addButton != null) addButton.interactable = isValid;
        }

        // --- UPDATED: THE BULLETPROOF IMAGE DOWNLOADER ---
        private void OnLoadImageClicked()
        {
            // Get the text and remove any accidental spaces
            string path = imagePathInput.text.Trim();

            // Windows "Copy as path" adds quote marks, let's strip them out!
            path = path.Replace("\"", "");
            path = path.Replace("'", "");

            if (string.IsNullOrEmpty(path)) return;

            // If it's a local computer file, we need to add "file:///" to the front of it
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
                    // Convert the downloaded texture into a Unity Sprite
                    Texture2D texture = DownloadHandlerTexture.GetContent(uwr);
                    customLoadedSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

                    // Show it in the preview window!
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

            // Read the 3 rows of custom names and numbers
            for (int i = 0; i < 3; i++)
            {
                string itemName = string.IsNullOrEmpty(nameInputs[i].text) ? "Item " + (i + 1) : nameInputs[i].text;
                newQ.categoryNames.Add(itemName);

                int val = 0;
                int.TryParse(valueInputs[i].text, out val);
                newQ.dataValues.Add(val);
            }

            // Auto-generate the question based on the custom names!
            int randomTarget = Random.Range(0, 3);
            newQ.questionText = $"How many {newQ.categoryNames[randomTarget]} are there?";
            newQ.correctAnswer = newQ.dataValues[randomTarget].ToString();

            // --- NEW: Save the row number so the game knows exactly what to hide! ---
            newQ.targetIndex = randomTarget;

            // Attach the custom uploaded picture if it's Look & Count
            if (newQ.preferredMode == 3 && customLoadedSprite != null)
            {
                newQ.questionImage = customLoadedSprite;
            }

            // Send it to the main game!
            gameManager.questionBank.Add(newQ);

            Debug.Log($"Added Question: {newQ.questionText} | Answer: {newQ.correctAnswer}");

            // Clean up the preview for the next question
            customLoadedSprite = null;
            if (imagePreview != null) imagePreview.sprite = null;
            if (imagePathInput != null) imagePathInput.text = "";
        }

        // --- NEW: THE PLAY BUTTON LOGIC ---
        public void PlayGame()
        {
            // Make sure they actually added a question first!
            if (gameManager.questionBank.Count == 0)
            {
                Debug.LogWarning("You need to add a question first!");
                return;
            }

            // 1. Hide the Form Screen
            gameManager.formScreen.SetActive(false);

            // 2. Show the Mode Selection Screen instead of jumping into the game!
            if (gameManager.modeSelectionScreen != null)
            {
                gameManager.modeSelectionScreen.SetActive(true);
            }
        }
    }
}