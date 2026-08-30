using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MathGameScript : MonoBehaviour
{
    private void Start()
    {
        gc.ActivateLearningGame();
        CursedThinkPadInstaller.ApplyTo(this);
        EnableKeyboardInput();
        if (gc.notebooks == 1)
        {
            QueueAudio(bal_intro);
            QueueAudio(bal_howto);
        }
        NewProblem();
        if (gc.spoopMode)
        {
            baldiFeedTransform.position = new Vector3(-1000f, -1000f, 0f);
        }
    }
    private void Update()
    {
        if (!baldiAudio.isPlaying)
        {
            if (audioInQueue > 0 & !gc.spoopMode)
            {
                PlayQueue();
            }
            baldiFeed.SetBool("talking", false);
        }
        else
        {
            baldiFeed.SetBool("talking", true);
        }
        if ((Input.GetKeyDown("return") || Input.GetKeyDown("enter")) & questionInProgress)
        {
            questionInProgress = false;
            CheckAnswer();
        }
        if (problem > 3)
        {
            endDelay -= 1f * Time.unscaledDeltaTime;
            if (endDelay <= 0f)
            {
                GC.Collect();
                ExitGame();
            }
        }
    }
    private void NewProblem()
    {
        playerAnswer.text = string.Empty;
        problem++;
        EnableKeyboardInput();
        if (problem <= 3)
        {
            QueueAudio(bal_problems[problem - 1]);
            if (problem <= 2 || gc.notebooks <= 1)
            {
                num1 = (float)Mathf.RoundToInt(UnityEngine.Random.Range(0f, 9f));
                num2 = (float)Mathf.RoundToInt(UnityEngine.Random.Range(0f, 9f));
                // 0 = addition, 1 = subtraction, 2 = multiplication.
                // The integer overload keeps all three operations equally likely.
                sign = UnityEngine.Random.Range(0, 3);
                QueueAudio(bal_numbers[Mathf.RoundToInt(num1)]);
                if (sign == 0)
                {
                    solution = num1 + num2;
                    questionText.text = string.Concat(new object[]
                    {
                        "SOLVE MATH Q",
                        problem,
                        ": \n \n",
                        num1,
                        "+",
                        num2,
                        "="
                    });
                    QueueAudio(bal_plus);
                }
                else if (sign == 1)
                {
                    solution = num1 - num2;
                    questionText.text = string.Concat(new object[]
                    {
                        "SOLVE MATH Q",
                        problem,
                        ": \n \n",
                        num1,
                        "-",
                        num2,
                        "="
                    });
                    QueueAudio(bal_minus);
                }
                else
                {
                    solution = num1 * num2;
                    questionText.text = string.Concat(new object[]
                    {
                        "SOLVE MATH Q",
                        problem,
                        ": \n \n",
                        num1,
                        "X",
                        num2,
                        "="
                    });
                    QueueAudio(bal_times);
                }
                QueueAudio(bal_numbers[Mathf.RoundToInt(num2)]);
                QueueAudio(bal_equals);
            }
            else
            {
                impossibleMode = true;
                num1 = UnityEngine.Random.Range(1f, 9999f);
                num2 = UnityEngine.Random.Range(1f, 9999f);
                num3 = UnityEngine.Random.Range(1f, 9999f);
                sign = Mathf.RoundToInt((float)UnityEngine.Random.Range(0, 1));
                QueueAudio(bal_screech);
                if (sign == 0)
                {
                    questionText.text = string.Concat(new object[]
                    {
                        "SOLVE MATH Q",
                        problem,
                        ": \n",
                        num1,
                        "+(",
                        num2,
                        "X",
                        num3,
                        "="
                    });
                    QueueAudio(bal_plus);
                    QueueAudio(bal_screech);
                    QueueAudio(bal_times);
                    QueueAudio(bal_screech);
                }
                else if (sign == 1)
                {
                    questionText.text = string.Concat(new object[]
                    {
                        "SOLVE MATH Q",
                        problem,
                        ": \n (",
                        num1,
                        "/",
                        num2,
                        ")+",
                        num3,
                        "="
                    });
                    QueueAudio(bal_divided);
                    QueueAudio(bal_screech);
                    QueueAudio(bal_plus);
                    QueueAudio(bal_screech);
                }
                num1 = UnityEngine.Random.Range(1f, 9999f);
                num2 = UnityEngine.Random.Range(1f, 9999f);
                num3 = UnityEngine.Random.Range(1f, 9999f);
                sign = Mathf.RoundToInt((float)UnityEngine.Random.Range(0, 1));
                if (sign == 0)
                {
                    questionText2.text = string.Concat(new object[]
                    {
                        "SOLVE MATH Q",
                        problem,
                        ": \n",
                        num1,
                        "+(",
                        num2,
                        "X",
                        num3,
                        "="
                    });
                }
                else if (sign == 1)
                {
                    questionText2.text = string.Concat(new object[]
                    {
                        "SOLVE MATH Q",
                        problem,
                        ": \n (",
                        num1,
                        "/",
                        num2,
                        ")+",
                        num3,
                        "="
                    });
                }
                num1 = UnityEngine.Random.Range(1f, 9999f);
                num2 = UnityEngine.Random.Range(1f, 9999f);
                num3 = UnityEngine.Random.Range(1f, 9999f);
                sign = Mathf.RoundToInt((float)UnityEngine.Random.Range(0, 1));
                if (sign == 0)
                {
                    questionText3.text = string.Concat(new object[]
                    {
                        "SOLVE MATH Q",
                        problem,
                        ": \n",
                        num1,
                        "+(",
                        num2,
                        "X",
                        num3,
                        "="
                    });
                }
                else if (sign == 1)
                {
                    questionText3.text = string.Concat(new object[]
                    {
                        "SOLVE MATH Q",
                        problem,
                        ": \n (",
                        num1,
                        "/",
                        num2,
                        ")+",
                        num3,
                        "="
                    });
                }
                QueueAudio(bal_equals);
            }
            questionInProgress = true;
        }
        else
        {
            endDelay = 5f;
            if (CursedPhaseManager.IsPhase2 && gc.notebooks == 2)
            {
                ShowPhase2FinalNotebookMessage();
            }
            else if (!gc.spoopMode)
            {
                questionText.text = "WOW! YOU EXIST!";
            }
            else if (problemsWrong >= 3)
            {
                questionText.text = "HE IS ALREADY BEHIND YOU";
                questionText2.text = string.Empty;
                questionText3.text = string.Empty;
                if (baldiScript.isActiveAndEnabled) baldiScript.Hear(playerPosition, 7f);
                gc.failedNotebooks++;
            }
            else
            {
                int num2 = Mathf.RoundToInt(UnityEngine.Random.Range(0f, 1f));
                questionText.text = hintText[num2];
                questionText2.text = string.Empty;
                questionText3.text = string.Empty;
            }
        }
    }
    private void ShowPhase2FinalNotebookMessage()
    {
        questionText.text = string.Empty;
        questionText2.text = string.Empty;
        questionText3.text = string.Empty;

        GameObject root = mathGame != null ? mathGame : gameObject;
        if (root.transform.Find("Phase 2 Final Notebook Message") != null) return;

        GameObject messageObject = new GameObject("Phase 2 Final Notebook Message", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        messageObject.transform.SetParent(root.transform, false);
        messageObject.transform.SetAsLastSibling();

        RectTransform rect = messageObject.GetComponent<RectTransform>();
        // Position the message inside the cursed Think Pad's upper LCD panel.
        rect.anchorMin = new Vector2(0.20f, 0.44f);
        rect.anchorMax = new Vector2(0.71f, 0.86f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI message = messageObject.GetComponent<TextMeshProUGUI>();
        message.font = questionText.font;
        message.fontSize = 22f;
        message.enableAutoSizing = true;
        message.fontSizeMin = 12f;
        message.fontSizeMax = 22f;
        message.enableWordWrapping = true;
        message.alignment = TextAlignmentOptions.Center;
        message.color = Color.red;
        message.raycastTarget = false;
        message.text = "If you've found this, then I am truly cursed. I was never meant to be this way! I never wanted to be this... I NEVER WANTED TO BE THIS!";
    }
    public void OKButton()
    {
        CheckAnswer();
    }
    public void CheckAnswer()
    {
        // Phase 1 shows the fake piracy warning and exits. On the next launch,
        // Phase 2 uses the same answer submission to begin the horror phase.
        if (problem == 3 && gc.notebooks == 2)
        {
            if (CursedPhaseManager.HandleSecondNotebookFinalAnswer()) return;
        }
        bool testRoomAnswer = CursedPhaseManager.IsTestRoomEnabled && playerAnswer.text == "31718";
        bool cheatAnswer = testRoomAnswer;
        bool correctAnswer = playerAnswer.text == solution.ToString() && !impossibleMode;
        if (gc.notebooks == 1 && problem <= 3 && !cheatAnswer && !correctAnswer)
        {
            if (CursedPhaseManager.HandleFirstNotebookWrongAnswer()) return;
        }
        if (testRoomAnswer)
        {
            StartCoroutine(CheatText("THIS IS WHERE IT ALL BEGAN"));
            SceneManager.LoadSceneAsync("TestRoom");
        }
        if (problem <= 3)
        {
            if (playerAnswer.text == solution.ToString() & !impossibleMode)
            {
                results[problem - 1].texture = correct;
                baldiAudio.Stop();
                ClearAudioQueue();
                int num = Mathf.RoundToInt(UnityEngine.Random.Range(0f, 4f));
                QueueAudio(bal_praises[num]);
                NewProblem();
            }
            else
            {
                problemsWrong++;
                results[problem - 1].texture = incorrect;
                if (!gc.spoopMode)
                {
                    baldiFeed.SetTrigger("angry");
                    if (CursedPhaseManager.IsPhase2)
                    {
                        CursedBaldiPortraitTransition.Play(baldiFeed);
                    }
                    gc.ActivateSpoopMode();
                }
                if (problem == 3)
                {
                    baldiScript.GetAngry(1f);
                }
                else
                {
                    baldiScript.GetTempAngry(0.25f);
                }
                ClearAudioQueue();
                baldiAudio.Stop();
                NewProblem();
            }
        }
    }
    private void QueueAudio(AudioClip sound)
    {
        audioQueue[audioInQueue] = sound;
        audioInQueue++;
    }
    private void PlayQueue()
    {
        baldiAudio.PlayOneShot(audioQueue[0]);
        UnqueueAudio();
    }
    private void UnqueueAudio()
    {
        for (int i = 1; i < audioInQueue; i++)
        {
            audioQueue[i - 1] = audioQueue[i];
        }
        audioInQueue--;
    }
    private void ClearAudioQueue()
    {
        audioInQueue = 0;
    }
    private void ExitGame()
    {
        gc.DeactivateLearningGame(gameObject);
    }
    public void ButtonPress(int value)
    {
        if (value >= 0 & value <= 9)
        {
            playerAnswer.text = playerAnswer.text + value;
        }
        else if (value == -1)
        {
            playerAnswer.text = playerAnswer.text + "-";
        }
        else
        {
            playerAnswer.text = string.Empty;
        }
    }
    private void EnableKeyboardInput()
    {
        if (playerAnswer == null) return;
        playerAnswer.interactable = true;
        playerAnswer.readOnly = false;
        playerAnswer.contentType = TMP_InputField.ContentType.IntegerNumber;
        playerAnswer.characterLimit = 6;
        playerAnswer.Select();
        playerAnswer.ActivateInputField();
    }
    private IEnumerator CheatText(string text)
    {
        for (; ; )
        {
            questionText.text = text;
            questionText2.text = string.Empty;
            questionText3.text = string.Empty;
            yield return new WaitForEndOfFrame();
        }
    }
    public GameControllerScript gc;
    public BaldiScript baldiScript;
    public Vector3 playerPosition;
    public GameObject mathGame;
    public RawImage[] results = new RawImage[3];
    public Texture correct;
    public Texture incorrect;
    public TMP_InputField playerAnswer;
    public TMP_Text questionText;
    public TMP_Text questionText2;
    public TMP_Text questionText3;
    public Animator baldiFeed;
    public Transform baldiFeedTransform;
    public AudioClip bal_plus;
    public AudioClip bal_minus;
    public AudioClip bal_times;
    public AudioClip bal_divided;
    public AudioClip bal_equals;
    public AudioClip bal_howto;
    public AudioClip bal_intro;
    public AudioClip bal_screech;
    public AudioClip[] bal_numbers = new AudioClip[10];
    public AudioClip[] bal_praises = new AudioClip[5];
    public AudioClip[] bal_problems = new AudioClip[3];
    public Button firstButton;
    private float endDelay;
    private int problem;
    private int audioInQueue;
    private float num1;
    private float num2;
    private float num3;
    private int sign;
    private float solution;
    private string[] hintText = new string[]
    {
        "THE LIGHTS WILL NOT SAVE YOU",
        "HE HEARS EVERY DOOR YOU OPEN"
    };
    private bool questionInProgress;
    private bool impossibleMode;
    private int problemsWrong;
    private AudioClip[] audioQueue = new AudioClip[20];
    public AudioSource baldiAudio;
}
