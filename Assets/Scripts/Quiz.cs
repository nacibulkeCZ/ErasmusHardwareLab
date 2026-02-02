using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class Quiz : MonoBehaviour
{
    [SerializeField] private TextAsset questionFile;

    [SerializeField] private TextMeshProUGUI questionText;
    [SerializeField] private Button[] answerButtons;
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private TextMeshProUGUI scoreText;

    private List<QuizQuestion> questions = new List<QuizQuestion>();
    private int currentQuestionIndex = 0;
    private int correctAnswersCount = 0;

    void Start()
    {
        LoadQuestions();
        SetupButtons();
        UpdateScoreDisplay();
        if (feedbackText != null)
        {
            feedbackText.text = "";
        }
        DisplayCurrentQuestion();
    }

    void LoadQuestions()
    {
        if (questionFile == null)
        {
            return;
        }

        string[] lines = questionFile.text.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            if (string.IsNullOrEmpty(line))
                continue;

            if (line.StartsWith("Q:"))
            {
                QuizQuestion question = new QuizQuestion();
                question.question = line.Substring(2).Trim();

                for (int j = 1; j <= 3; j++)
                {
                    if (i + j < lines.Length)
                    {
                        string answerLine = lines[i + j].Trim();

                        if (answerLine.StartsWith("*"))
                        {
                            question.correctAnswer = answerLine.Substring(1).Trim();
                            question.answers.Add(question.correctAnswer);
                        }
                        else
                        {
                            question.answers.Add(answerLine);
                        }
                    }
                }

                questions.Add(question);
                i += 3;
            }
        }

    }

    void SetupButtons()
    {
        for (int i = 0; i < answerButtons.Length; i++)
        {
            int index = i;
            answerButtons[i].onClick.AddListener(() => OnAnswerSelected(index));
        }
    }

    void DisplayCurrentQuestion()
    {
        if (questions.Count == 0)
        {
            return;
        }

        if (currentQuestionIndex >= questions.Count)
        {
            questionText.text = "Quiz Completed!";
            foreach (var button in answerButtons)
            {
                button.gameObject.SetActive(false);
            }
            return;
        }

        QuizQuestion currentQuestion = questions[currentQuestionIndex];
        questionText.text = currentQuestion.question;

        // Update button texts
        for (int i = 0; i < answerButtons.Length && i < currentQuestion.answers.Count; i++)
        {
            TextMeshProUGUI buttonText = answerButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = currentQuestion.answers[i];
            }
            answerButtons[i].gameObject.SetActive(true);
        }

        // Hide extra buttons if there are fewer answers
        for (int i = currentQuestion.answers.Count; i < answerButtons.Length; i++)
        {
            answerButtons[i].gameObject.SetActive(false);
        }
    }

    void OnAnswerSelected(int answerIndex)
    {
        QuizQuestion currentQuestion = questions[currentQuestionIndex];
        string selectedAnswer = currentQuestion.answers[answerIndex];
        bool isCorrect = selectedAnswer == currentQuestion.correctAnswer;

        // Disable all buttons to prevent multiple clicks
        foreach (var button in answerButtons)
        {
            button.interactable = false;
        }

        // Update score if correct
        if (isCorrect)
        {
            correctAnswersCount++;
            UpdateScoreDisplay();
        }

        // Always highlight the correct answer in green
        for (int i = 0; i < currentQuestion.answers.Count; i++)
        {
            if (currentQuestion.answers[i] == currentQuestion.correctAnswer)
            {
                ColorBlock correctColors = answerButtons[i].colors;
                correctColors.disabledColor = Color.green;
                answerButtons[i].colors = correctColors;
                break;
            }
        }

        // If answer was wrong, highlight the selected wrong answer in red
        if (!isCorrect)
        {
            ColorBlock colors = answerButtons[answerIndex].colors;
            colors.disabledColor = Color.red;
            answerButtons[answerIndex].colors = colors;
        }

        // Display feedback message
        if (feedbackText != null)
        {
            if (isCorrect)
            {
                feedbackText.text = $"<color=green>Correct!</color>\n\nThe answer is: {currentQuestion.correctAnswer}";
            }
            else
            {
                feedbackText.text = $"<color=red>Wrong!</color>\n\nThe correct answer is: <color=green>{currentQuestion.correctAnswer}</color>";
            }
        }

        // Wait before moving to next question
        StartCoroutine(MoveToNextQuestionAfterDelay(2.5f));
    }

    System.Collections.IEnumerator MoveToNextQuestionAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Reset button colors
        foreach (var button in answerButtons)
        {
            ColorBlock colors = button.colors;
            colors.disabledColor = Color.gray;
            button.colors = colors;
            button.interactable = true;
        }

        // Clear feedback text
        if (feedbackText != null)
        {
            feedbackText.text = "";
        }

        // Move to next question
        currentQuestionIndex++;
        DisplayCurrentQuestion();
    }

    void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {correctAnswersCount}/{questions.Count}";
        }
    }
}

[System.Serializable]
public class QuizQuestion
{
    public string question;
    public List<string> answers = new List<string>();
    public string correctAnswer;
}
