using UnityEngine;

public class DuelParticipantData
{
    public GameObject GameObject;
    public Category? Category;
    public DuelAction? Action;
    public DuelCommand? Command;
    public Secret Secret;

    public bool IsComplete =>
        GameObject != null &&
        Category.HasValue &&
        Action.HasValue &&
        Command.HasValue;
}
