#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(SpawnClipContext))]
public class SpawnClipContextDrawer : PropertyDrawer {
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        // 각 필드별 라인 수 계산
        var spawnTypeProp = property.FindPropertyRelative("spawnType");
        var monsterCountProp = property.FindPropertyRelative("monsterCount");
        int monsterCount = monsterCountProp.intValue;
        bool showInterval = monsterCount > 1;
        bool showSpawnType = monsterCount == 1;
        
        int lineCount = 1;    // monsterId
        lineCount += 1;    // monsterCount
        
        if(showSpawnType) 
            lineCount += 1; // spawnType

        if (showInterval)
            lineCount += 1; // interval, intervalType

        float line = EditorGUIUtility.singleLineHeight;
        float space = EditorGUIUtility.standardVerticalSpacing;

        return lineCount * line + (lineCount - 1) * space;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        EditorGUI.BeginProperty(position, label, property);

        // 자식 프로퍼티 찾기
        var spawnTypeProp = property.FindPropertyRelative("spawnType");
        var monsterIdProp = property.FindPropertyRelative("monsterId");
        var monsterCountProp = property.FindPropertyRelative("monsterCount");
        var intervalTypeProp = property.FindPropertyRelative("intervalType");

        int monsterCount = monsterCountProp.intValue;
        bool showInterval = monsterCount > 1;
        bool showSpawnType = monsterCount == 1;

        float line = EditorGUIUtility.singleLineHeight;
        float space = EditorGUIUtility.standardVerticalSpacing;

        Rect r = new Rect(position.x, position.y, position.width, line);

        // 1) Monster Id
        EditorGUI.PropertyField(r, monsterIdProp);

        // 2) Monster Count
        r.y += line + space;
        EditorGUI.PropertyField(r, monsterCountProp);

        // 3) Spawn Type
        if (showSpawnType) {
            r.y += line + space;
            EditorGUI.PropertyField(r, spawnTypeProp);
        }

        // 4) Interval
        if (showInterval == true) {
            r.y += line + space;
            EditorGUI.PropertyField(r, intervalTypeProp);
        }

        EditorGUI.EndProperty();
    }
}
#endif
