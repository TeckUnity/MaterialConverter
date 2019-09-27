using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEditor.AnimatedValues;
using UnityEngine.Events;

public class MaterialConverter : Editor
{
    [MenuItem("Assets/Convert Material(s)")]
    static void ConvertMaterials()
    {
        var shaders = Selection.objects.Where(o => o as Shader != null).Select(o => o as Shader);
        var materials = Selection.objects.Where(o => o as Material != null).Select(o => o as Material);
        foreach (var m in materials)
        {
            if (!shaders.Contains(m.shader))
            {
                shaders = shaders.Append(m.shader);
            }
        }
        MaterialConverterWindow.Open(shaders);
    }

    [MenuItem("Assets/Convert Material(s)", true)]
    static bool ValidateConvertMaterials()
    {
        return Selection.objects.Where(o => o as Shader != null || o as Material != null).Count() > 0;
    }
}

public class MaterialConverterWindow : EditorWindow
{
    private List<ShaderMapping> shaderMappings;
    private Vector2 scrollPos;

    public class ShaderProp
    {
        public string name;
        public ShaderUtil.ShaderPropertyType type;
        public object value;

        public ShaderProp(string _name, ShaderUtil.ShaderPropertyType _type)
        {
            name = _name;
            type = _type;
            value = null;
        }
    }

    public class ShaderMapping
    {
        public AnimBool show;
        public Shader sourceShader;
        public Shader targetShader;
        public ShaderProp[] sourceShaderProps;
        public Dictionary<ShaderUtil.ShaderPropertyType, List<ShaderProp>> targetShaderProps;
        public Dictionary<ShaderProp, ShaderProp> propertyMapping;

        public ShaderMapping(Shader _shader, EditorWindow window)
        {
            sourceShader = _shader;
            int propCount = ShaderUtil.GetPropertyCount(_shader);
            sourceShaderProps = new ShaderProp[propCount];
            for (int i = 0; i < propCount; i++)
            {
                string propName = ShaderUtil.GetPropertyName(_shader, i);
                ShaderUtil.ShaderPropertyType propType = ShaderUtil.GetPropertyType(_shader, i);
                sourceShaderProps[i] = new ShaderProp(ShaderUtil.GetPropertyName(_shader, i), ShaderUtil.GetPropertyType(_shader, i));
            }
            targetShader = null;
            targetShaderProps = new Dictionary<ShaderUtil.ShaderPropertyType, List<ShaderProp>>();
            propertyMapping = new Dictionary<ShaderProp, ShaderProp>();
            show = new AnimBool(true);
            show.valueChanged.AddListener(window.Repaint);
        }

        public void AssignTarget(Shader _shader)
        {
            targetShader = _shader;
            int propCount = ShaderUtil.GetPropertyCount(_shader);
            targetShaderProps.Clear();
            propertyMapping.Clear();
            for (int i = 0; i < propCount; i++)
            {
                string propName = ShaderUtil.GetPropertyName(_shader, i);
                var propType = ShaderUtil.GetPropertyType(_shader, i);
                var shaderProp = new ShaderProp(ShaderUtil.GetPropertyName(_shader, i), ShaderUtil.GetPropertyType(_shader, i));
                if (!targetShaderProps.ContainsKey(shaderProp.type))
                {
                    targetShaderProps.Add(shaderProp.type, new List<ShaderProp>());
                }
                targetShaderProps[shaderProp.type].Add(shaderProp);
            }
            foreach (var prop in sourceShaderProps)
            {
                if (!targetShaderProps.ContainsKey(prop.type))
                {
                    continue;
                }
                int index = targetShaderProps[prop.type].FindIndex(o => o.name == prop.name && o.type == prop.type);
                if (index >= 0)
                {
                    propertyMapping[prop] = targetShaderProps[prop.type][index];
                }
            }
        }
    }

    public static void Open(IEnumerable<Shader> shaders)
    {
        var window = GetWindow<MaterialConverterWindow>();
        window.shaderMappings = new List<ShaderMapping>();
        foreach (var shader in shaders)
        {
            var shaderProps = new ShaderMapping(shader, window);
            window.shaderMappings.Add(shaderProps);
        }
        window.Show();
    }

    void OnGUI()
    {
        Event e = Event.current;
        using (var scrollScope = new EditorGUILayout.ScrollViewScope(scrollPos))
        {
            scrollPos = scrollScope.scrollPosition;
            for (int i = 0; i < shaderMappings.Count; i++)
            {
                using (var vScope = new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (var hScope = new EditorGUILayout.HorizontalScope())
                    {
                        string prefix = shaderMappings[i].show.target ? "-" : "+";
                        EditorGUILayout.LabelField($"{prefix} {shaderMappings[i].sourceShader.name}", EditorStyles.boldLabel);
                        if (EditorGUILayout.DropdownButton(new GUIContent(shaderMappings[i].targetShader ? shaderMappings[i].targetShader.name : "Target Shader"), FocusType.Passive))
                        {
                            var menu = new GenericMenu();
                            var infos = ShaderUtil.GetAllShaderInfo();
                            foreach (var info in infos)
                            {
                                var j = i;
                                menu.AddItem(new GUIContent(info.name), shaderMappings[i].targetShader && shaderMappings[i].targetShader.name.Equals(info.name), () =>
                                {
                                    shaderMappings[j].AssignTarget(Shader.Find(info.name));
                                });
                            }
                            menu.ShowAsContext();
                        }
                    }
                    Rect r = GUILayoutUtility.GetLastRect();
                    if (e.type == EventType.MouseDown && e.button == 0 && r.Contains(e.mousePosition))
                    {
                        shaderMappings[i].show.target = !shaderMappings[i].show.target;
                    }
                    using (var fadeScope = new EditorGUILayout.FadeGroupScope(shaderMappings[i].show.faded))
                    {
                        if (fadeScope.visible)
                        {
                            if (shaderMappings[i].targetShader == null)
                            {
                                continue;
                            }
                            if (shaderMappings[i].targetShader == shaderMappings[i].sourceShader)
                            {
                                continue;
                            }
                            EditorGUI.indentLevel++;
                            using (var vScope2 = new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                            {
                                foreach (var prop in shaderMappings[i].sourceShaderProps)
                                {
                                    var j = i;
                                    using (var hScope = new EditorGUILayout.HorizontalScope())
                                    {
                                        EditorGUILayout.LabelField(prop.name);
                                        if (shaderMappings[i].targetShaderProps.ContainsKey(prop.type))
                                        {
                                            if (EditorGUILayout.DropdownButton(new GUIContent(shaderMappings[i].propertyMapping.ContainsKey(prop) ? shaderMappings[i].propertyMapping[prop].name : "None"), FocusType.Passive))
                                            {
                                                var menu = new GenericMenu();
                                                menu.AddItem(new GUIContent("None"), false, () =>
                                                {
                                                    shaderMappings[j].propertyMapping.Remove(prop);
                                                });
                                                foreach (var p in shaderMappings[i].targetShaderProps[prop.type])
                                                {
                                                    menu.AddItem(new GUIContent(p.name), false, () =>
                                                    {
                                                        shaderMappings[j].propertyMapping[prop] = p;
                                                    });
                                                }
                                                menu.ShowAsContext();
                                            }
                                        }
                                        else
                                        {
                                            EditorGUILayout.LabelField($"No {prop.type} properties");
                                        }
                                    }
                                }
                            }
                            EditorGUI.indentLevel--;
                        }
                    }
                    using (var hScope = new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button(new GUIContent("Material List", "List of materials that will be converted")))
                        {
                            var materials = AssetDatabase.FindAssets("t:Material").Select(guid => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid))).Where(m => m.shader == shaderMappings[i].sourceShader);
                            MaterialConverterListWindow.Open(materials.ToArray());
                        }
                        if (GUILayout.Button("Convert Materials"))
                        {
                            var targetShader = shaderMappings[i].targetShader;
                            var materials = AssetDatabase.FindAssets("t:Material").Select(guid => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid))).Where(m => m.shader == shaderMappings[i].sourceShader);
                            foreach (var mat in materials)
                            {
                                foreach (var key in shaderMappings[i].propertyMapping.Keys)
                                {
                                    switch (key.type)
                                    {
                                        case ShaderUtil.ShaderPropertyType.Color:
                                            shaderMappings[i].propertyMapping[key].value = mat.GetColor(key.name);
                                            break;
                                        case ShaderUtil.ShaderPropertyType.Float:
                                        case ShaderUtil.ShaderPropertyType.Range:
                                            shaderMappings[i].propertyMapping[key].value = mat.GetFloat(key.name);
                                            break;
                                        case ShaderUtil.ShaderPropertyType.TexEnv:
                                            shaderMappings[i].propertyMapping[key].value = mat.GetTexture(key.name);
                                            break;
                                        case ShaderUtil.ShaderPropertyType.Vector:
                                            shaderMappings[i].propertyMapping[key].value = mat.GetVector(key.name);
                                            break;
                                    }
                                }
                                mat.shader = shaderMappings[i].targetShader;
                                foreach (var key in shaderMappings[i].propertyMapping.Keys)
                                {
                                    switch (key.type)
                                    {
                                        case ShaderUtil.ShaderPropertyType.Color:
                                            // Debug.Log($"SetColor: {(Color)shaderMappings[i].propertyMapping[key].value}");
                                            mat.SetColor(shaderMappings[i].propertyMapping[key].name, (Color)shaderMappings[i].propertyMapping[key].value);
                                            break;
                                        case ShaderUtil.ShaderPropertyType.Float:
                                        case ShaderUtil.ShaderPropertyType.Range:
                                            // Debug.Log($"SetFloat: {(float)shaderMappings[i].propertyMapping[key].value}");
                                            mat.SetFloat(shaderMappings[i].propertyMapping[key].name, (float)shaderMappings[i].propertyMapping[key].value);
                                            break;
                                        case ShaderUtil.ShaderPropertyType.TexEnv:
                                            // Debug.Log($"SetTexture: {(Texture)shaderMappings[i].propertyMapping[key].value}");
                                            mat.SetTexture(shaderMappings[i].propertyMapping[key].name, (Texture)shaderMappings[i].propertyMapping[key].value);
                                            break;
                                        case ShaderUtil.ShaderPropertyType.Vector:
                                            // Debug.Log($"SetVector: {(Vector4)shaderMappings[i].propertyMapping[key].value}");
                                            mat.SetVector(shaderMappings[i].propertyMapping[key].name, (Vector4)shaderMappings[i].propertyMapping[key].value);
                                            break;
                                    }
                                }
                            }
                            shaderMappings[i] = new ShaderMapping(targetShader, this);
                        }
                    }
                }
            }
        }
    }
}

public class MaterialConverterListWindow : EditorWindow
{
    Material[] materials;
    Vector2 scrollPos;

    public static void Open(Material[] _materials)
    {
        var window = EditorWindow.GetWindow<MaterialConverterListWindow>();
        window.materials = _materials;
        window.Show();
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField($"{materials.Length} Materials", EditorStyles.boldLabel);
        using (var scrollScope = new EditorGUILayout.ScrollViewScope(scrollPos))
        {
            scrollPos = scrollScope.scrollPosition;
            foreach (var mat in materials)
            {
                EditorGUILayout.ObjectField(mat, typeof(Material), false);
            }
        }
    }
}