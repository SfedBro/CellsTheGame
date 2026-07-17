using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(IModuleEffect), true)]
[CustomPropertyDrawer(typeof(IAbilityBehavior), true)]
public class ModuleEffectDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Get base type of field or element type if it is a list/array
        Type baseType = fieldInfo.FieldType;
        if (baseType.IsGenericType)
        {
            baseType = baseType.GetGenericArguments()[0];
        }
        else if (baseType.IsArray)
        {
            baseType = baseType.GetElementType();
        }

        // 1. Calculate heights and layout
        Rect headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        
        // Get type name of managed reference
        object value = property.managedReferenceValue;
        string typeName = value != null ? value.GetType().Name : "None (Select Effect)";

        // Draw foldout arrow if we have a value
        if (value != null)
        {
            property.isExpanded = EditorGUI.Foldout(new Rect(headerRect.x, headerRect.y, 15, headerRect.height), property.isExpanded, "");
        }
        
        // Draw type selector popup button
        Rect popupRect = new Rect(headerRect.x + 15, headerRect.y, headerRect.width - 15, headerRect.height);
        if (GUI.Button(popupRect, $"{label.text}: {typeName}", EditorStyles.popup))
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("None"), value == null, () =>
            {
                property.managedReferenceValue = null;
                property.serializedObject.ApplyModifiedProperties();
            });

            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(t => baseType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var type in types)
            {
                menu.AddItem(new GUIContent(type.Name), value?.GetType() == type, () =>
                {
                    property.managedReferenceValue = Activator.CreateInstance(type);
                    property.serializedObject.ApplyModifiedProperties();
                });
            }
            menu.ShowAsContext();
        }

        // 2. Draw child fields if expanded and value is not null
        if (property.isExpanded && value != null)
        {
            EditorGUI.indentLevel++;
            SerializedProperty endProperty = property.GetEndProperty();
            SerializedProperty child = property.Copy();
            
            float currentY = headerRect.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            bool enterChildren = true;
            
            while (child.NextVisible(enterChildren))
            {
                if (SerializedProperty.EqualContents(child, endProperty)) break;

                float childHeight = EditorGUI.GetPropertyHeight(child, true);
                Rect childRect = new Rect(position.x, currentY, position.width, childHeight);
                EditorGUI.PropertyField(childRect, child, true);
                currentY += childHeight + EditorGUIUtility.standardVerticalSpacing;
                
                enterChildren = false; // Only enter children on first call
            }
            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight;
        object value = property.managedReferenceValue;

        if (property.isExpanded && value != null)
        {
            SerializedProperty endProperty = property.GetEndProperty();
            SerializedProperty child = property.Copy();
            bool enterChildren = true;
            
            while (child.NextVisible(enterChildren))
            {
                if (SerializedProperty.EqualContents(child, endProperty)) break;
                height += EditorGUI.GetPropertyHeight(child, true) + EditorGUIUtility.standardVerticalSpacing;
                enterChildren = false;
            }
        }

        return height;
    }
}
