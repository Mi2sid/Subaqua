// Trouvé sur : http://answers.unity.com/answers/1487948/view.html puis modifié pour fonctionner ici

using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(RenameAttribute))]
public class RenameEditor : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        label.text = (attribute as RenameAttribute).NewName;
        EditorGUI.PropertyField(position, property, label, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property);
    }
}