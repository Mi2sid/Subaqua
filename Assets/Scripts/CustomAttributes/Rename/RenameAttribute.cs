// Trouv� sur : http://answers.unity.com/answers/1487948/view.html puis modifi� pour fonctionner ici

using UnityEngine;

public class RenameAttribute : PropertyAttribute
{
    public string NewName { get; private set; }
    public RenameAttribute(string name)
    {
        NewName = name;
    }
}