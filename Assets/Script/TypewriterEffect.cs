using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TypewriterEffect : MonoBehaviour
{
    public float typingSpeed = 0.05f; // 타이핑 속도 (낮을수록 빠름)
    private Text dialogueText;
    private string currentText = "";
    private bool isTyping = false; // 현재 타이핑 중인지 여부를 확인하는 변수

    void Awake()
    {
        dialogueText = GetComponent<Text>();
    }

    // 텍스트를 한 글자씩 출력하는 코루틴
    public IEnumerator TypeText(string text)
    {
        currentText = text;
        dialogueText.text = "";
        isTyping = true; // 타이핑 시작
        foreach (char letter in currentText.ToCharArray())
        {
            if (!isTyping) // 타이핑이 취소되었으면 바로 전체 텍스트 출력
            {
                dialogueText.text = currentText;
                yield break;
            }
            
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
        isTyping = false; // 타이핑 완료
    }

    void Update()
    {
        // 타이핑 중에 클릭이나 엔터키 입력이 감지되면 전체 텍스트 출력
        if (isTyping && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Return)))
        {
            isTyping = false; // 타이핑 중단
        }
    }
}
