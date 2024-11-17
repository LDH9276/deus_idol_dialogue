using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Dialogue
{
    [Tooltip("해당 씬의 등장 캐릭터")]
    public string sceneCharacter;

    [Tooltip("대사를 하는 캐릭터")]
    public string characterName;

    [Tooltip("대사 내용")]
    public string contexts;

    [Tooltip("이벤트 번호")]
    public int eventNumber;

    [Tooltip("스킵라인")]
    public string skipNum;

    
}

public class DialogueEvent
{
    public string characterName;
    public string dialogueText;
    public string[] options;

}