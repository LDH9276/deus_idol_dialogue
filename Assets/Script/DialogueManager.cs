using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

[System.Serializable]
public class Choice
{
    public string text;
    public string nextSceneId;
}

[System.Serializable]
public class DialogueEntry
{
    public string character;
    public string text;
    public List<Choice> choices;
    public string nextSceneId;
}

[System.Serializable]
public class Scene
{
    public string sceneId;
    public List<DialogueEntry> dialogues;
    public string background;
    public List<string> sceneCharacters;
}

[System.Serializable]
public class Character
{
    public string characterId;
    public string name;
    public string description;
    public SpritePath spritePath;
    public Sprite standingSprite;
}

[System.Serializable]
public class SpritePath
{
    public string standing;
}

[System.Serializable]
public class CharacterData
{
    public List<Character> characters;
}

[System.Serializable]
public class DialogueData
{
    public List<Scene> scenes;
}

public class DialogueManager : MonoBehaviour
{
    [SerializeField] private Text nameText;
    [SerializeField] private Text dialogueText;
    [SerializeField] private GameObject endCursor;
    [SerializeField] private GameObject[] choiceButtons;
    [SerializeField] private TypewriterEffect typewriterEffect;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private GameObject[] currentCharacter;
    [SerializeField] private Image leftCharacterImage;
    [SerializeField] private Image rightCharacterImage;



    private DialogueData dialogueData;
    private int currentDialogueIndex;
    private Scene currentScene;
    private string pendingNextSceneId;

    private bool isSceneTransitionPending;
    private CharacterData characterData;
    private Dictionary<string, Character> characterDictionary;
    private string leftCharacterName;
    private string rightCharacterName;

    private void Start()
    {
        typewriterEffect = dialogueText.GetComponent<TypewriterEffect>();
        endCursor.SetActive(false); // Ensure EndCursor is initially inactive
        LoadDialogueData();
        LoadCharacterData();
        StartScene("prologue");
        AdjustCharacterImageSize();
    }

    void LoadDialogueData()
    {
        TextAsset jsonData = Resources.Load<TextAsset>("event_text");
        if (jsonData == null)
        {
            Debug.LogError("리소스 불러오기를 실패했습니다. 올바른 경로에 파일이 있는지 확인하세요.");
            return;
        }

        dialogueData = JsonUtility.FromJson<DialogueData>(jsonData.text);
        if (dialogueData == null)
        {
            Debug.LogError("대사집 변환에 실패했습니다. JSON 파일 형식을 확인하세요.");
            return;
        }
    }

    void LoadCharacterData()
    {
        TextAsset jsonData = Resources.Load<TextAsset>("character");
        if (jsonData == null)
        {
            Debug.LogError("캐릭터 데이터를 불러올 수 없습니다.");
            return;
        }

        characterData = JsonUtility.FromJson<CharacterData>(jsonData.text);
        if (characterData == null)
        {
            Debug.LogError("캐릭터 데이터 변환에 실패했습니다.");
            return;
        }

        characterDictionary = new Dictionary<string, Character>();
        foreach (var character in characterData.characters)
        {
            if (character.spritePath == null)
            {
                Debug.LogError($"캐릭터 '{character.name}'의 spritePath가 null입니다.");
                continue;
            }

            // 스프라이트 로드
            string spritePath = character.spritePath.standing;
            character.standingSprite = Resources.Load<Sprite>(spritePath);
            if (character.standingSprite == null)
            {
                Debug.LogError($"캐릭터 스프라이트를 불러올 수 없습니다: {spritePath}");
            }

            characterDictionary.Add(character.name, character);
        }
    }

    public void StartScene(string sceneId)
    {
        currentScene = dialogueData.scenes.Find(scene => scene.sceneId == sceneId);
        if (currentScene == null)
        {
            Debug.LogError($"Scene '{sceneId}'은 올바른 sceneId가 아닙니다.");
            return;
        }

        currentDialogueIndex = 0;
        isSceneTransitionPending = false;

        UpdateBackgroundImage(currentScene.background);
        DisplaySceneCharacters();

        DisplayNextDialogue();
    }
    private void UpdateBackgroundImage(string backgroundFileName)
    {
        if (!string.IsNullOrEmpty(backgroundFileName))
        {
            string filePath = $"backgroundimages/{Path.GetFileNameWithoutExtension(backgroundFileName)}";
            Sprite newBackground = Resources.Load<Sprite>(filePath);

            if (newBackground != null)
            {
                backgroundImage.sprite = newBackground;
            }
            else
            {
                Debug.LogError($"배경 이미지를 찾을 수 없습니다. 경로를 확인하세요: {filePath}");
            }
        }
    }

    public void DisplayNextDialogue()
    {
        if (isSceneTransitionPending && !string.IsNullOrEmpty(pendingNextSceneId))
        {
            endCursor.SetActive(false); // 씬 전환 중에는 EndCursor를 비활성화
            StartScene(pendingNextSceneId);
            return;
        }

        if (currentDialogueIndex < currentScene.dialogues.Count)
        {
            DialogueEntry currentDialogue = currentScene.dialogues[currentDialogueIndex];
            nameText.text = currentDialogue.character;

            // 캐릭터 밝기 조절
            AdjustCharacterBrightness(currentDialogue.character);

            if (typewriterEffect != null)
            {
                StopAllCoroutines();
                StartCoroutine(ShowDialogueWithChoices(currentDialogue));
            }
            else
            {
                dialogueText.text = currentDialogue.text;
                ShowChoicesIfNeeded(currentDialogue);
                endCursor.SetActive(true); // EndCursor 활성화
            }

            currentDialogueIndex++;
        }
        else
        {
            // 씬의 끝 (다음 이벤트 설정 가능)
        }
    }

    private IEnumerator ShowDialogueWithChoices(DialogueEntry dialogueEntry)
    {
        yield return StartCoroutine(typewriterEffect.TypeText(dialogueEntry.text));
        endCursor.SetActive(true); // 타이핑 효과가 끝나면 EndCursor 활성화
        ShowChoicesIfNeeded(dialogueEntry);
    }
    private void ShowChoicesIfNeeded(DialogueEntry dialogueEntry)
    {
        if (dialogueEntry.choices != null && dialogueEntry.choices.Count > 0)
        {
            DisplayChoices(dialogueEntry.choices);
        }
        else if (!string.IsNullOrEmpty(dialogueEntry.nextSceneId))
        {
            pendingNextSceneId = dialogueEntry.nextSceneId;
            isSceneTransitionPending = true;
        }
        else
        {
            HideChoices();
        }
    }

    void DisplayChoices(List<Choice> choices)
    {
        HideChoices();
        for (int i = 0; i < choices.Count; i++)
        {
            if (i < choiceButtons.Length)
            {
                choiceButtons[i].SetActive(true);
                choiceButtons[i].GetComponentInChildren<Text>().text = choices[i].text;

                int choiceIndex = i;
                choiceButtons[i].GetComponent<Button>().onClick.RemoveAllListeners();
                choiceButtons[i].GetComponent<Button>().onClick.AddListener(() => SelectChoice(choiceIndex));
            }
        }
    }

    void HideChoices()
    {
        foreach (GameObject button in choiceButtons)
        {
            button.SetActive(false);
        }
    }

    public void SelectChoice(int choiceIndex)
    {
        endCursor.SetActive(false); // Hide EndCursor when a choice is selected
        DialogueEntry currentDialogue = currentScene.dialogues[currentDialogueIndex - 1];
        if (currentDialogue.choices != null && currentDialogue.choices.Count > choiceIndex)
        {
            HideChoices();
            dialogueText.text = "";

            string nextSceneId = currentDialogue.choices[choiceIndex].nextSceneId;
            StartScene(nextSceneId);
        }
    }
    void DisplaySceneCharacters()
    {
        if (currentScene.sceneCharacters != null)
        {
            for (int i = 0; i < currentScene.sceneCharacters.Count; i++)
            {
                string characterName = currentScene.sceneCharacters[i];
                if (characterDictionary.TryGetValue(characterName, out Character character))
                {
                    if (i == 0)
                    {
                        leftCharacterImage.sprite = character.standingSprite;
                        leftCharacterName = character.name;
                        SetCharacterBrightness(leftCharacterImage, 0.7f);
                    }
                    else if (i == 1)
                    {
                        rightCharacterImage.sprite = character.standingSprite;
                        rightCharacterName = character.name;
                        SetCharacterBrightness(rightCharacterImage, 0.7f);
                    }
                }
                else
                {
                    Debug.LogError($"캐릭터를 찾을 수 없습니다: {characterName}");
                }
            }
        }
    }

    void SetCharacterBrightness(Image image, float brightness)
    {
        if (image != null)
        {
            Color color = image.color;
            color.r = brightness;
            color.g = brightness;
            color.b = brightness;
            image.color = color;
        }
    }    

    void AdjustCharacterBrightness(string speakingCharacter)
    {
        // 처음에 모든 캐릭터의 밝기를 낮춥니다.
        SetCharacterBrightness(leftCharacterImage, 0.7f);
        SetCharacterBrightness(rightCharacterImage, 0.7f);

        if (string.IsNullOrEmpty(speakingCharacter))
            return;

        // 말하는 캐릭터의 밝기를 높입니다.
        if (speakingCharacter == leftCharacterName)
        {
            SetCharacterBrightness(leftCharacterImage, 1f);
        }
        else if (speakingCharacter == rightCharacterName)
        {
            SetCharacterBrightness(rightCharacterImage, 1f);
        }
    }

    void AdjustCharacterImageSize()
    {
        float screenHeight = Screen.height;
        float targetHeight = screenHeight * 1.5f;
        float aspectRatio = leftCharacterImage.sprite.bounds.size.x / leftCharacterImage.sprite.bounds.size.y;
        float targetWidth = targetHeight * aspectRatio;

        float leftCharacterX = targetWidth * 0.5f + 20;
        float rightCharacterX = targetWidth * -0.5f - 20;
        float standingSpriteY = targetHeight * -0.2f;

        // 왼쪽 캐릭터 이미지 크기 설정
        leftCharacterImage.rectTransform.sizeDelta = new Vector2(targetWidth, targetHeight);

        // 오른쪽 캐릭터 이미지 크기 설정
        rightCharacterImage.rectTransform.sizeDelta = new Vector2(targetWidth, targetHeight);

        // 왼쪽 캐릭터 이미지 위치 설정
        leftCharacterImage.rectTransform.anchoredPosition = new Vector2(leftCharacterX, standingSpriteY);

        // 오른쪽 캐릭터 이미지 위치 설정
        rightCharacterImage.rectTransform.anchoredPosition = new Vector2(rightCharacterX, standingSpriteY);
    }    

    void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Return))
        {
            if (!choiceButtons[0].activeSelf && endCursor.activeSelf)
            {
                endCursor.SetActive(false); // Hide EndCursor when advancing the dialogue
                DisplayNextDialogue();
            }
        }
    }
}