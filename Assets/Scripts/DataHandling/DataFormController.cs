using UnityEngine;
using TMPro;
using System.Collections.Generic;

namespace Eduzo.Games.DataHandling
{
    public class DataFormController : MonoBehaviour
    {
        [Header("System Reference")]
        public DataGameManager gameManager;

        [Header("UI Inputs")]
        public TMP_InputField questionInput;
        public TMP_InputField dataValuesInput;
        public TMP_InputField correctAnswerInput;
        public TMP_Dropdown chartTypeDropdown;

        [Header("Audio")]
        public AudioSource successSound;
        public AudioSource errorSound;

        public void OnAddQuestionClicked()
        {
            // 1. Safety Check: Did they leave anything blank?
            if (string.IsNullOrEmpty(questionInput.text) ||
                string.IsNullOrEmpty(dataValuesInput.text) ||
                string.IsNullOrEmpty(correctAnswerInput.text))
            {
                if (errorSound != null) errorSound.Play();
                Debug.LogWarning("Bro, fill in all the fields!");
                return;
            }

            // 2. Magic Math: Convert their comma string ("5, 12, 8") into a real integer list!
            List<int> parsedData = new List<int>();
            string[] rawValues = dataValuesInput.text.Split(',');

            foreach (string val in rawValues)
            {
                if (int.TryParse(val.Trim(), out int parsedInt))
                {
                    parsedData.Add(parsedInt);
                }
            }

            if (parsedData.Count == 0)
            {
                if (errorSound != null) errorSound.Play();
                Debug.LogWarning("Invalid data numbers!");
                return;
            }

            // 3. Create the Question Data
            QuestionData newQuestion = new QuestionData();
            newQuestion.questionText = questionInput.text;
            newQuestion.dataValues = parsedData;
            newQuestion.correctAnswer = correctAnswerInput.text.Trim();

            // Dropdown math: Random is 0. Bar is 1. We subtract 1 so Random = -1, Bar = 0, etc.
            newQuestion.preferredMode = chartTypeDropdown.value - 1;

            // 4. Inject it straight into the GameManager's Brain!
            gameManager.questionBank.Add(newQuestion);

            if (successSound != null) successSound.Play();
            Debug.Log("Question Added Successfully!");

            // 5. Clear the form so they can type another one
            questionInput.text = "";
            dataValuesInput.text = "";
            correctAnswerInput.text = "";
            questionInput.Select();
        }

        public void OnPlayClicked()
        {
            // Turn off the form, turn on the Mode Selection menu!
            gameObject.SetActive(false);
            if (gameManager.modeSelectionScreen != null) gameManager.modeSelectionScreen.SetActive(true);
        }
    }
}