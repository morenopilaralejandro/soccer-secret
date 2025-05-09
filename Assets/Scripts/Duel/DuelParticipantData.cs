using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DuelParticipantData
{
    public GameObject gameObj;
    public Category? category;
    public DuelAction? action;
    public DuelCommand? command;
    public Secret secret;

    public bool IsComplete =>
        gameObj != null && category.HasValue && action.HasValue && command.HasValue;
}
