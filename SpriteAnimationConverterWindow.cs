using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PMK95.Editor
{
    public class SpriteAnimationConverterWindow : EditorWindow
    {
        private AnimationClip animationClip;
        private readonly string[] postfixes = new string[] { "_ImageAnimation", "_SpriteAnimation" };
        private EditorCurveBinding[] bindings;
        private enum ConvertType
        {
            None,
            SpriteRenderer,
            Image
        }
        private ConvertType inputType;
        private ConvertType outputType;
        private void OnEnable()
        {
            titleContent = new GUIContent("Sprite To Image Animation");
        }

        [MenuItem("Tools/Sprite To Image Animation")]
        public static void ShowWindow()
        {
            GetWindow<SpriteAnimationConverterWindow>("Sprite To Image Animation");
        }
        private void OnGUI()
        {
            string animationSummary = "Animation Clip Summary";
            MessageType messageType = MessageType.Info;
            bool guiEnabled = true;
        
            var clip =  (AnimationClip)EditorGUILayout.ObjectField("Animation Clip", animationClip, typeof(AnimationClip), false);
        
            //onChanged animation clip reset bindings & DetermineInputType
            if (clip != animationClip)
            {
                animationClip = clip;
                if (animationClip != null)
                {
                    bindings = AnimationUtility.GetObjectReferenceCurveBindings(animationClip);
                    inputType = DetermineInputType(bindings);
                }
            }

            inputType = (ConvertType)EditorGUILayout.EnumPopup("Input Type", inputType);
        
            //check valid animation clip
            #region Validation
            if (animationClip == null)
            {
                animationSummary = "No Animation Clip selected";
                messageType = MessageType.Warning;
                guiEnabled = false;
            }
            else
            {
                int keyFrameSpriteLength = 0;
                outputType = ConvertType.None;

                if (bindings != null && bindings.Length > 0)
                {
                    foreach (var binding in bindings)
                    {
                        var keyframes = AnimationUtility.GetObjectReferenceCurve(animationClip, binding);
                        keyFrameSpriteLength += keyframes.Count(keyframe => keyframe.value is Sprite);

                        // Determine the output type based on the first matching binding type
                        if (inputType == ConvertType.SpriteRenderer && binding.type == typeof(SpriteRenderer))
                        {
                            outputType = ConvertType.Image;
                            break;
                        }

                        if (inputType == ConvertType.Image && binding.type == typeof(Image))
                        {
                            outputType = ConvertType.SpriteRenderer;
                            break;
                        }
                    }

                    if (outputType == ConvertType.None)
                    {
                        animationSummary = $"No valid keyframes found for {inputType} Type";
                        messageType = MessageType.Error;
                        guiEnabled = false;
                    }
                    else
                    {
                        animationSummary = $"Animation Clip has \n {bindings.Length} bindings \n {keyFrameSpriteLength} keyframes";
                    }
                }
                else
                {
                    animationSummary = "No valid input type selected";
                    messageType = MessageType.Error;
                    guiEnabled = false;
                }
            }
        
            EditorGUILayout.HelpBox(animationSummary, messageType);
            GUI.enabled = guiEnabled;
            #endregion
        
            // Display Convert Info
            EditorGUILayout.Space(10);
            GUIStyle centeredStyle = new GUIStyle(EditorStyles.boldLabel);
            centeredStyle.alignment = TextAnchor.MiddleCenter;
            EditorGUILayout.LabelField(inputType.ToString(), centeredStyle);
            EditorGUILayout.LabelField("↓", centeredStyle);
            EditorGUILayout.LabelField(outputType.ToString(), centeredStyle);
            EditorGUILayout.Space(10);
            
            
            if (GUILayout.Button("Convert"))
            {
                switch (outputType)
                {
                    case ConvertType.SpriteRenderer:
                        ConvertSpriteAnimation<SpriteRenderer>(animationClip, postfixes[0]);
                        break;
                    case ConvertType.Image:
                        ConvertSpriteAnimation<Image>(animationClip, postfixes[1]);
                        break;
                    case ConvertType.None:
                        break;
                    default:
                        break;
                }
            }
        }
    
        private ConvertType DetermineInputType(EditorCurveBinding[] bindings)
        {
            if (bindings != null && bindings.Length != 0)
            {
                foreach (var binding in bindings)
                {
                    if (binding.type == typeof(SpriteRenderer))
                    {
                        return ConvertType.SpriteRenderer;
                    }
                    else if (binding.type == typeof(Image))
                    {
                        return ConvertType.Image;
                    }
                }
            }
            return ConvertType.None;
        }
    
        /// <summary>
        /// ConvertSpriteAnimation
        /// </summary>
        /// <typeparam name="T"> SpriteRenderer or Image</typeparam>
        private void ConvertSpriteAnimation<T>(AnimationClip clip, string postfix) where T : Object
        {
            if (clip == null)
            {
                Debug.LogError("No Animation Clip selected for conversion.");
                return;
            }

            AnimationClip newClip = new AnimationClip();
            var newClipName = clip.name;
            //postfix array contains remove 
            foreach (var p in postfixes)
            {
                newClipName = newClipName.Replace(p, "");
            }
            newClip.name = newClipName + postfix;
        
            // Copy all the properties of the original clip
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            {
                if (binding.propertyName.Contains("m_Sprite"))
                {
                    ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(clip, binding);

                    EditorCurveBinding newBinding = new EditorCurveBinding
                    {
                        path = binding.path,
                        type = typeof(T),
                        propertyName = "m_Sprite"
                    };
                
                    AnimationUtility.SetObjectReferenceCurve(newClip, newBinding, keyframes);
                }
            }

            string path = AssetDatabase.GetAssetPath(clip);
            string newClipPath = System.IO.Path.GetDirectoryName(path) + "/" + newClip.name + ".anim";
            AssetDatabase.CreateAsset(newClip, newClipPath);
            AssetDatabase.SaveAssets();
 
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<AnimationClip>(newClipPath));

            Debug.Log($"Animation Clip converted and saved at: {newClipPath}");
        }
    }
}