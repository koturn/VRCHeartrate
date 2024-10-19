using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace Koturn.VRCHeartrate.Inspectors
{
    /// <summary>
    /// Custom editor of UnlitVF.shader
    /// </summary>
    public sealed class SimpleCounterGUI : ShaderGUI
    {
        /// <summary>
        /// Editor UI mode names.
        /// </summary>
        private static readonly string[] _editorModeNames;
        /// <summary>
        /// Current editor UI mode.
        /// </summary>
        private static EditorMode _editorMode;
        /// <summary>
        /// Key list of cache of MaterialPropertyHandlers.
        /// </summary>
        private static List<string> _propStringList;

        /// <summary>
        /// Property name of "_SpriteTex".
        /// </summary>
        private const string PropNameSpriteTex = "_SpriteTex";
        /// <summary>
        /// Property name of "_Color".
        /// </summary>
        private const string PropNameColor = "_Color";
        /// <summary>
        /// Property name of "_LerpColoring".
        /// </summary>
        private const string PropNameLerpColoring = "_LerpColoring";
        /// <summary>
        /// Property name of "_MinValue".
        /// </summary>
        private const string PropNameMinValue = "_MinValue";
        /// <summary>
        /// Property name of "_MinColor".
        /// </summary>
        private const string PropNameMinColor = "_MinColor";
        /// <summary>
        /// Property name of "_MaxValue".
        /// </summary>
        private const string PropNameMaxValue = "_MaxValue";
        /// <summary>
        /// Property name of "_MaxColor".
        /// </summary>
        private const string PropNameMaxColor = "_MaxColor";
        /// <summary>
        /// Property name of "_Columns".
        /// </summary>
        private const string PropNameColumns = "_Columns";
        /// <summary>
        /// Property name of "_Rows".
        /// </summary>
        private const string PropNameRows = "_Rows";
        /// <summary>
        /// Property name of "_Value".
        /// </summary>
        private const string PropNameValue = "_Value";
        /// <summary>
        /// Property name of "_ValueDiscardThreashold".
        /// </summary>
        private const string PropNameValueDiscardThreashold = "_ValueDiscardThreashold";
        /// <summary>
        /// Property name of "_DisplayLength".
        /// </summary>
        private const string PropNameDisplayLength = "_DisplayLength";
        /// <summary>
        /// Property name of "_Align".
        /// </summary>
        private const string PropNameAlign = "_Align";
        /// <summary>
        /// Property name of "_ReversedBackface".
        /// </summary>
        private const string PropNameReversedBackface = "_ReversedBackface";
        /// <summary>
        /// Property name of "_ReverseInMirror".
        /// </summary>
        private const string PropNameReverseInMirror = "_ReverseInMirror";
        /// <summary>
        /// Property name of "_UseVRChatMirrorMode".
        /// </summary>
        private const string PropNameUseVRChatMirrorMode = "_UseVRChatMirrorMode";
        /// <summary>
        /// Property name of "_BillboardMode".
        /// </summary>
        private const string PropNameBillboardMode = "_BillboardMode";
        /// <summary>
        /// Property name of "_Cull".
        /// </summary>
        private const string PropNameCull = "_Cull";


        /// <summary>
        /// Initialize <see cref="_editorMode"/>, <see cref="_editorModeNames"/>.
        /// </summary>
        static SimpleCounterGUI()
        {
            _editorMode = (EditorMode)(-1);
            _editorModeNames = Enum.GetNames(typeof(EditorMode));
        }


        /// <summary>
        /// Draw property items.
        /// </summary>
        /// <param name="me">The <see cref="MaterialEditor"/> that are calling this <see cref="OnGUI(MaterialEditor, MaterialProperty[])"/> (the 'owner')</param>
        /// <param name="mps">Material properties of the current selected shader</param>
        public override void OnGUI(MaterialEditor me, MaterialProperty[] mps)
        {
            if (!Enum.IsDefined(typeof(EditorMode), _editorMode))
            {
                MaterialPropertyUtil.ClearDecoratorDrawers(((Material)me.target).shader, mps);
                _editorMode = EditorMode.Custom;
            }
            using (var ccScope = new EditorGUI.ChangeCheckScope())
            {
                _editorMode = (EditorMode)GUILayout.Toolbar((int)_editorMode, _editorModeNames);
                if (ccScope.changed)
                {
                    if (_propStringList != null)
                    {
                        MaterialPropertyUtil.ClearPropertyHandlerCache(_propStringList);
                    }

                    if (_editorMode == EditorMode.Custom)
                    {
                        _propStringList = MaterialPropertyUtil.ClearDecoratorDrawers(((Material)me.target).shader, mps);
                    }
                    else
                    {
                        _propStringList = MaterialPropertyUtil.ClearCustomDrawers(((Material)me.target).shader, mps);
                    }
                }
            }
            if (_editorMode == EditorMode.Default)
            {
                base.OnGUI(me, mps);
                return;
            }

            EditorGUILayout.LabelField("Font & Color", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                TexturePropertySingleLine(me, mps, PropNameSpriteTex, PropNameColor);

                var mpLerpColoring = FindProperty(PropNameLerpColoring, mps);
                ShaderProperty(me, mpLerpColoring);
                using (new EditorGUI.IndentLevelScope())
                using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                using (new EditorGUI.DisabledScope(mpLerpColoring.floatValue < 0.5))
                {
                    ShaderProperty(me, mps, PropNameMinValue);
                    ShaderProperty(me, mps, PropNameMinColor);
                    ShaderProperty(me, mps, PropNameMaxValue);
                    ShaderProperty(me, mps, PropNameMaxColor);
                }

                EditorGUILayout.LabelField("Sprite settings", EditorStyles.boldLabel);
                using (new EditorGUI.IndentLevelScope())
                using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                {
                    ShaderProperty(me, mps, PropNameColumns);
                    ShaderProperty(me, mps, PropNameRows);
                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Value & Alignment", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                ShaderProperty(me, mps, PropNameValue);
                ShaderProperty(me, mps, PropNameValueDiscardThreashold);
				ShaderProperty(me, mps, PropNameDisplayLength);
                ShaderProperty(me, mps, PropNameAlign);
			}

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Reverse settings", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
				ShaderProperty(me, mps, PropNameReversedBackface);
				ShaderProperty(me, mps, PropNameReverseInMirror);
				ShaderProperty(me, mps, PropNameUseVRChatMirrorMode);
				ShaderProperty(me, mps, PropNameBillboardMode);
			}

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Rendering Options", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                ShaderProperty(me, mps, PropNameCull);

                EditorGUILayout.Space();

                DrawAdvancedOptions(me, mps);
            }
        }

        /// <summary>
        /// Draw inspector items of advanced options.
        /// </summary>
        /// <param name="me">A <see cref="MaterialEditor"/></param>
        /// <param name="mps"><see cref="MaterialProperty"/> array</param>
        private static void DrawAdvancedOptions(MaterialEditor me, MaterialProperty[] mps)
        {
            GUILayout.Label("Advanced Options", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                me.RenderQueueField();
#if UNITY_5_6_OR_NEWER
                me.EnableInstancingField();
                me.DoubleSidedGIField();
#endif  // UNITY_5_6_OR_NEWER
            }
        }

        /// <summary>
        /// Draw default item of specified shader property.
        /// </summary>
        /// <param name="me">A <see cref="MaterialEditor"/></param>
        /// <param name="mps"><see cref="MaterialProperty"/> array</param>
        /// <param name="propName">Name of shader property</param>
        /// <param name="isMandatory">If <c>true</c> then this method will throw an exception
        /// if a property with <paramref name="propName"/> was not found.</param>
        private static void ShaderProperty(MaterialEditor me, MaterialProperty[] mps, string propName, bool isMandatory = true)
        {
            var prop = FindProperty(propName, mps, isMandatory);
            if (prop != null) {
                ShaderProperty(me, prop);
            }
        }

        /// <summary>
        /// Draw default item of specified shader property.
        /// </summary>
        /// <param name="me">A <see cref="MaterialEditor"/></param>
        /// <param name="mp">Target <see cref="MaterialProperty"/></param>
        private static void ShaderProperty(MaterialEditor me, MaterialProperty mp)
        {
            me.ShaderProperty(mp, mp.displayName);
        }

        /// <summary>
        /// Draw default texture and color pair.
        /// </summary>
        /// <param name="me">A <see cref="MaterialEditor"/></param>
        /// <param name="mps"><see cref="MaterialProperty"/> array</param>
        /// <param name="propNameTex">Name of shader property of texture</param>
        /// <param name="propNameColor">Name of shader property of color</param>
        private static void TexturePropertySingleLine(MaterialEditor me, MaterialProperty[] mps, string propNameTex, string propNameColor)
        {
            TexturePropertySingleLine(
                me,
                FindProperty(propNameTex, mps),
                FindProperty(propNameColor, mps));
        }

        /// <summary>
        /// Draw default texture and color pair.
        /// </summary>
        /// <param name="me">A <see cref="MaterialEditor"/></param>
        /// <param name="mpTex">Target <see cref="MaterialProperty"/> of texture</param>
        /// <param name="mpColor">Target <see cref="MaterialProperty"/> of color</param>
        private static void TexturePropertySingleLine(MaterialEditor me, MaterialProperty mpTex, MaterialProperty mpColor)
        {
            me.TexturePropertySingleLine(
                new GUIContent(mpTex.displayName, mpColor.displayName),
                mpTex,
                mpColor);
        }
    }
}
