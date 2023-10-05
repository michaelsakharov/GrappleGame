using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(UnityDecimal))]
public class SerializableDecimalDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var daDecBooiizz = (UnityDecimal)this.fieldInfo.GetValue(property.serializedObject.targetObject);
        string text = GUI.TextField(EditorGUI.PrefixLabel(position, label), daDecBooiizz.value.ToString());
        if (GUI.changed)
        {
            if (decimal.TryParse(text, out decimal val))
            {
                daDecBooiizz.value = val;
                property.serializedObject.ApplyModifiedProperties();
            }
        }
        this.fieldInfo.SetValue(property.serializedObject.targetObject, daDecBooiizz);
    }
}

[CustomPropertyDrawer(typeof(dVector2))]
public class DVector2Drawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var daDecBooiizz = (dVector2)this.fieldInfo.GetValue(property.serializedObject.targetObject);
        string text = GUI.TextField(EditorGUI.PrefixLabel(position, label), daDecBooiizz.X.value.ToString() + ":" + daDecBooiizz.Y.value.ToString() );
        if (GUI.changed)
        {
            var parts = text.Split(':');
            if (decimal.TryParse(parts[0], out decimal X))
            {
                daDecBooiizz.X.value = X;
                property.serializedObject.ApplyModifiedProperties();
            }
            if (decimal.TryParse(parts[1], out decimal Y))
            {
                daDecBooiizz.Y.value = Y;
                property.serializedObject.ApplyModifiedProperties();
            }
        }
        this.fieldInfo.SetValue(property.serializedObject.targetObject, daDecBooiizz);
    }
}