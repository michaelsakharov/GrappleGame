using Unity.VisualScripting;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomPropertyDrawer(typeof(Tile))]
public class TileDrawer : PropertyDrawer
{
    float spacing => EditorGUIUtility.singleLineHeight;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        int totalLines = 1;

        if (property.isExpanded)
        {
            totalLines += 9;
        }

        return EditorGUIUtility.singleLineHeight * totalLines + EditorGUIUtility.standardVerticalSpacing * (totalLines - 1);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        var x = position.min.x;
        var y = position.min.y;
        var height = EditorGUIUtility.singleLineHeight;
        Rect rectFoldout = new Rect(x, y, position.size.x, height);
        property.isExpanded = EditorGUI.Foldout(rectFoldout, property.isExpanded, label);
        int lines = 1;
        if (property.isExpanded)
        {
            var name = property.FindPropertyRelative("Name");

            float half = position.size.x / 2;

            var tileset = property.FindPropertyRelative("tileset").objectReferenceValue as Tileset;
            var tile = tileset.GetTile(name.stringValue);
            Rect texRect = tile.TextureRect();
            float aspect = texRect.width / texRect.height;
            float width = Mathf.Min(position.width - half, height * 5 * aspect);
            EditorGUI.DrawRect(new Rect(x + half, (y + height), width + 9999, height * 5), Color.yellow * 0.5f);
            EditorGUI.DrawRect(new Rect(x + half, (y + height), width, height * 5), Color.black);
            GUI.DrawTextureWithTexCoords(new Rect(x + half, (y + height), width, height * 5), tileset.Texture, texRect);

            EditorGUI.indentLevel++;

            Rect rect = new Rect(x, (y + lines++ * height), half - 10, height);
            EditorGUI.PropertyField(rect, name, GUIContent.none);

            rect.y = y + lines++ * height;
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("Type"), GUIContent.none);

            rect.y = y + lines++ * height;
            DrawLabel(ref rect, "Has Collider", 35);
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("HasCollider"), GUIContent.none);

            rect.y = y + lines++ * height;
            DrawLabel(ref rect, "Parent GO For Colliders", 65);
            rect.width = 60;
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("ColliderTemplate"), GUIContent.none);

            rect.y = y + lines++ * height;
            rect.width = 150;
            DrawLabel(ref rect, "Physics Material", 65);
            rect.width = 60;
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("PhysicsMaterial"), GUIContent.none);

            rect.width = half;

            rect.y = y + lines++ * height;
            DrawLabel(ref rect, "Variation Count", half);
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("VariationsCount"), GUIContent.none);

            rect.y = y + lines++ * height;
            DrawLabel(ref rect, "Texture Position", half);
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("Position"), GUIContent.none);

            rect.y = y + lines++ * height;
            DrawLabel(ref rect, "Blend Overlap", 35);
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("BlendOverlap"), GUIContent.none);

            rect.y = y + lines++ * height;
            DrawLabel(ref rect, "Material", half);
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("Material"), GUIContent.none);

            EditorGUI.indentLevel--;
        }
        EditorGUI.EndProperty();
    }

    public void DrawLabel(ref Rect rect, string label, float offset)
    {
        rect.x += offset; GUI.Label(rect, label); rect.x -= offset;
    }

}