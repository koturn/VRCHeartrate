Shader "koturn/VRCHeartrate/SimpleCounter"
{
    Properties
    {
        // --------------------------------------------------------------------
        [Header(Font and Color)]
        [Space(8)]
        [NoScaleOffset]
        _SpriteTex ("Sprite Sheet", 2D) = "white" {}

        _Columns ("Columns", Int) = 5

        _Rows ("Rows", Int) = 2

        [HDR]
        _Color ("Color", Color) = (1.0, 1.0, 1.0, 0.0)

        [MaterialToggle]
        _LerpColoring ("Enable value dependent coloring", Int) = 0

        _MinValue ("Minimum value for coloring", Int) = 60

        _MinColor ("Color for minimum value", Color) = (0.4, 0.4, 1.0, 1.0)

        _MaxValue ("Maximum value for coloring", Int) = 100

        _MaxColor ("Color for maximum value", Color) = (1.0, 0.4, 0.4, 1.0)


        // --------------------------------------------------------------------
        [Header(Value and Alignment)]
        [Space(8)]
        _Value ("Value", Int) = 0

        _ValueDiscardThreashold ("Discard threashold", Int) = 5

        [IntRange]
        _DisplayLength ("Display Length", Range(1, 3)) = 3

        [Enum(Show Zeros, 0, Right, 1, Left, 2)]
        _Align ("Align", Int) = 0


        // --------------------------------------------------------------------
        [Header(Reverse settings)]
        [Space(8)]
        [MaterialToggle]
        _ReversedBackface ("Reverse backface", Int) = 1

        [MaterialToggle]
        _ReverseInMirror ("Reverse in mirror", Int) = 0

        [Toggle(_USE_VRCHAT_MIRROR_MODE_ON)]
        _UseVRChatMirrorMode ("Use _VRChatMirrorMode", Int) = 0

        [KeywordEnum(None, All Axises, Y Axis)]
        _BillboardMode ("Billboard mode", Int) = 0


        // --------------------------------------------------------------------
        [Header(Rendering Options)]
        [Space(8)]
        [Enum(UnityEngine.Rendering.CullMode)]
        _Cull ("Culling", Int) = 0  // Default: Off
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "IgnoreProjector" = "True"
            "VRCFallback" = "Hidden"
        }

        Cull [_Cull]
        AlphaToMask On


        CGINCLUDE
        #pragma shader_feature_local_vertex _ _USE_VRCHAT_MIRROR_MODE_ON
        #pragma shader_feature_local_vertex _BILLBOARDMODE_NONE _BILLBOARDMODE_ALL_AXISES _BILLBOARDMODE_Y_AXISES

        #include "UnityCG.cginc"

        #define TRUST_UNIFORM_VAR
        #define STRICT_MIRROR_CHECK
        // #define USE_UINT_CALC

        #if defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES) || defined(SHADER_API_D3D9)
        typedef fixed face_t;
        #    define FACE_SEMANTICS VFACE
        #else
        typedef bool face_t;
        #    define FACE_SEMANTICS SV_IsFrontFace
        #endif  // defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES) || defined(SHADER_API_D3D9)

        //! Constant value for _BillboardMode which represents no billboard shader.
        static const int kBillboardNone = 0;
        //! Constant value for _BillboardMode which represents all axis billboard shader.
        static const int kBillboardAll = 1;
        //! Constant value for _BillboardMode which represents Y-axis billboard shader.
        static const int kBillboardYAxis = 2;
        //! Quiet NaN.
        static const float qNaN = asfloat(0x7fc00000);

        //! Sprite texture.
        UNITY_DECLARE_TEX2D(_SpriteTex);
        //! Tint color of Sprite texture.
        uniform float4 _Color;
        //! A flag whether use tint color depends on _MinValue and _MaxValue.
        uniform bool _LerpColoring;
        //! Lower bound value to adopt _MinColor for tint color.
        uniform float _MinValue;
        //! Tint color when _Value <= _MinValue.
        uniform float4 _MinColor;
        //! Upper bound value to adopt _MinColor for tint color.
        uniform float _MaxValue;
        //! Tint color when _Value >= _MaxValue.
        uniform float4 _MaxColor;
        //! Number of columns of _SpriteTex.
        uniform float _Columns;
        //! Number of rows of _SpriteTex.
        uniform float _Rows;
        //! Number of characters of displayed values.
        uniform float _DisplayLength;
        //! Value to display.
        uniform float _Value;
        //! Threshold value, when _Value is less than this value, _Value is not displayed.
        uniform float _ValueDiscardThreashold;
        //! Alignment of displayed value.
        uniform float _Align;
        //! A flag whether to reverse the x-coordinate of the UV shown in mirrors.
        uniform bool _ReversedBackface;
        //! A flag whether to reverse the x-coordinate of the backface.
        uniform bool _ReverseInMirror;
        //! A flag whether to show as billboard.
        uniform int _BillboardMode;
        #if defined(_USE_VRCHAT_MIRROR_MODE_ON)
        /*!
         * @brief An enum value to identify shown in mirror of VRChat.
         *
         * 0: Rendering normally, not in a mirror
         * 1: Rendering in a mirror viewed in VR
         * 2: Rendering in a mirror viewed in desktop mode
         */
        uniform float _VRChatMirrorMode;
        #endif  // defined(_USE_VRCHAT_MIRROR_MODE_ON)


        float4 objectToClipPos(float4 vertex);
        half4 sampleSpriteTex(float2 uv);
        float fmodglsl(float x, float y);
        uint calcDigitUInt(uint val, uint digitNum);
        float calcDigit(float val, float digitNum);
        float remap(float x, float a, float b);
        float remap01Sat(float x, float a, float b);
        float3 remapSat(float x, float a, float b, float3 s, float3 t);
        bool isInMirror();
        bool isFacing(face_t facing);


        /*!
         * @brief Calculate clip space position with vertex position in object space.
         * @param [in] vertex  Vertex position in object space.
         * @return Clip space position.
         * @see https://gam0022.net/blog/2019/07/23/unity-y-axis-billboard-shader/
         */
        float4 objectToClipPos(float4 vertex)
        {
        #if defined(_BILLBOARDMODE_ALL_AXISES)
            float4 viewOffset = float4(mul((float3x3)unity_ObjectToWorld, vertex), 0.0);
            viewOffset.z = -viewOffset.z;
            const float4 viewPos = mul(UNITY_MATRIX_V, unity_ObjectToWorld._m03_m13_m23_m33) + viewOffset;
            return mul(UNITY_MATRIX_P, viewPos);
        #elif defined(_BILLBOARDMODE_Y_AXISES)
            const float3 srWorldPos = mul((float3x3)unity_ObjectToWorld, vertex);
            const float4 viewPos = mul(UNITY_MATRIX_V, unity_ObjectToWorld._m03_m13_m23_m33) + float4(
                dot(float3(1.0, UNITY_MATRIX_V._m01, 0.0), srWorldPos),
                dot(float3(0.0, UNITY_MATRIX_V._m11, 0.0), srWorldPos),
                dot(float3(0.0, UNITY_MATRIX_V._m21, -1.0), srWorldPos),
                0.0);
            return mul(UNITY_MATRIX_P, viewPos);
        #else  // Assume defined(_BILLBOARDMODE_NONE)
            return UnityObjectToClipPos(vertex);
        #endif  // defined(_BILLBOARDMODE_ALL_AXISES)
        }

        /*!
         * @brief Sample sprite texture.
         * @param [in] uv  Sample UV coordinate.
         * @return Color of texel at (uv.x, uv.y).
         */
        half4 sampleSpriteTex(float2 uv)
        {
        #if defined(TRUST_UNIFORM_VAR)
            // _DisplayLength is interger value and the value is between 1 and 6.
            const float displayLength = _DisplayLength;
        #else
            const float displayLength = max(round(_DisplayLength), 1.0);
        #endif  // defined(TRUST_UNIFORM_VAR)

            const float2 uv2 = uv * float2(displayLength, 1.0);

        #if defined(TRUST_UNIFORM_VAR)
            const float2 colrow = float2(_Columns, _Rows);
        #else
            const float2 colrow = round(float2(_Columns, _Rows));
        #endif  // defined(TRUST_UNIFORM_VAR)

            const float val = round(abs(_Value));
            const float digitsCnt = ceil(log10((max(val, 1.0) + 0.5)));
            const float digitNumTmp = displayLength * (uv - 1.0) + saturate(_Align - 1.0) * (displayLength - digitsCnt);
            const float digitNum = ceil(-digitNumTmp);
        #if defined(USE_UINT_CALC)
            const float digit = (float)calcDigitUInt((uint)val, (uint)round(pow(10.0, digitNum)));
        #else
            const float digit = calcDigit(val, pow(10.0, digitNum));
        #endif  // defined(USE_UINT_CALC)
            const float cr = colrow.x * colrow.y;
            const float digitPos = frac((digit + 1e-06) / cr);

            const float4 tex = UNITY_SAMPLE_TEX2D(
                _SpriteTex,
                frac(uv2) / colrow + floor(float2(cr, colrow.y) * float2(digitPos, 1.0 - digitPos)) / colrow);

            const float alpha = 2.0 - 2.0 * tex.a;
            const float mask = saturate(digitNum) * (_Align == 0.0 ? 1.0 : saturate(ceil(digitNumTmp + digitsCnt)));
            const float colAlpha = saturate((1.0 - alpha) / fwidth(alpha)) * mask;

            return half4(tex.rgb, colAlpha);
        }

        /*!
         * @brief Returns the remainder of x divided by y with the same sign as y.
         * @param [in] x  Scalar numerator.
         * @param [in] y  Scalar denominator.
         * @return Remainder of x / y with the same sign as y.
         */
        float fmodglsl(float x, float y)
        {
            return x - y * floor(x / y);
        }

        /*
         * @brief Obtains N-th digit of value.
         *
         * This function is calculate with uint value.
         *
         * @param [in] val  Source value.
         * @param [in] digitNum  Number of digits.
         * @return N-th digit of val.
         */
        uint calcDigitUInt(uint val, uint digitNum)
        {
            return (val % digitNum) * 10 / digitNum;
        }

        /*
         * @brief Obtains N-th digit of value.
         *
         * This function is calculate with float value.
         *
         * @param [in] val  Source value.
         * @param [in] digitNum  Number of digits.
         * @return N-th digit of val.
         */
        float calcDigit(float val, float digitNum)
        {
            return floor(fmodglsl(val, digitNum) * 10 / digitNum);
        }

        /*
         * @brief Apply linear interpolation [a, b] => [0, 1].
         * @param [in] x  Target value.
         * @param [in] a  The lower bound of the input range.
         * @param [in] b  The upper bound of the input range.
         * @return The remapped value.
         */
        float remap01(float x, float a, float b)
        {
            return (x - a) / (b - a);
        }

        /*
         * @brief Apply linear interpolation [a, b] => [0, 1] and saturate the value.
         * @param [in] x  Target value.
         * @param [in] a  The lower bound of the input range.
         * @param [in] b  The upper bound of the input range.
         * @return The remapped and saturated value.
         */
        float remap01Sat(float x, float a, float b)
        {
            return saturate(remap01(x, a, b));
        }

        /*
         * @brief Apply linear interpolation [a, b] => [s, t] and saturate the value.
         * @param [in] x  Target value.
         * @param [in] a  The lower bound of the input range.
         * @param [in] b  The upper bound of the input range.
         * @param [in] s  The lower bound of the output range.
         * @param [in] t  The upper bound of the output range.
         * @return The remapped and saturated value.
         */
        float3 remapSat(float x, float a, float b, float3 s, float3 t)
        {
            return lerp(s, t, remap01Sat(x, a, b));
        }

        /*!
         * @brief Determine whether shown in mirror or not.
         * @return true if shown in mirror, otherwise false.
         */
        bool isInMirror()
        {
        #if defined(_USE_VRCHAT_MIRROR_MODE_ON)
            return _VRChatMirrorMode != 0.0;
        #elif defined(STRICT_MIRROR_CHECK)
            return dot(cross(UNITY_MATRIX_V[0].xyz, UNITY_MATRIX_V[1].xyz), UNITY_MATRIX_V[2].xyz) > 0;
        #else
            return unity_CameraProjection._m20 != 0.0 || unity_CameraProjection._m21 != 0.0;
        #endif  // defined(STRICT_MIRROR_CHECK)
        }

        /*!
         * @brief Identify whether surface is facing the camera or facing away from the camera.
         * @param [in] facing  Facing variable (fixed or bool).
         * @return True if surface facing the camera, otherwise false.
         */
        bool isFacing(face_t facing)
        {
        #if defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES) || defined(SHADER_API_D3D9)
            return facing >= 0.0;
        #else
            return facing;
        #endif  // defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES) || defined(SHADER_API_D3D9)
        }
        ENDCG


        Pass
        {
            Name "UNLIT"

            CGPROGRAM
            #pragma target 3.0

            #pragma vertex vert
            #pragma fragment frag

            /*!
             * @brief Input data type for vertex shader function, vert().
             * @see vert
             */
            struct appdata
            {
                //! Object space position of the vertex.
                float4 vertex : POSITION;
                //! UV coordinate of the vertex.
                float2 texcoord : TEXCOORD0;
                //! instanceID for single pass instanced rendering.
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            /*!
            * @brief Input data type for fragment shader function
            */
            struct v2f
            {
                //! Clip space position of the vertex.
                float4 pos : SV_POSITION;
                //! UV coordinate of the vertex/fragment.
                float2 uv : TEXCOORD0;
                //! Instance ID for single pass instanced rendering, instanceID.
                UNITY_VERTEX_INPUT_INSTANCE_ID
                //! Stereo target eye index for single pass instanced rendering, stereoTargetEyeIndex.
                UNITY_VERTEX_OUTPUT_STEREO
            };


            /*!
             * @brief Vertex shader function.
             * @param [in] v  Input data.
             * @return Interpolation source data for fragment shader function, frag().
             */
            v2f vert(appdata v)
            {
                v2f o;

                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                if (_Value < _ValueDiscardThreashold) {
                    o.pos = qNaN.xxxx;
                    return o;
                }
                o.pos = objectToClipPos(v.vertex);
                o.uv = v.texcoord;
                UNITY_FLATTEN
                if (_ReverseInMirror && isInMirror()) {
                    o.uv.x = 1.0 - o.uv.x;
                }

                return o;
            }

            /*!
            * @brief Fragment shader function.
            * @param [in] fi  Input data from vertex shader.
            * @param [in] facing  Facing parameter.
            * @return color of texel at (fi.uv.x, fi.uv.y).
            */
            half4 frag(v2f fi, face_t facing : FACE_SEMANTICS) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(fi);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(fi);

                float2 uv = fi.uv;
                uv.x = _ReversedBackface && isFacing(facing) ? fi.uv.x : (1.0 - fi.uv.x);

                const half4 tex = sampleSpriteTex(uv);

                float3 col = _Color;
                UNITY_BRANCH
                if (_LerpColoring) {
                    col = remapSat(_Value, _MinValue, _MaxValue, _MinColor.rgb, _MaxColor.rgb);
                }

                return half4(tex.rgb * col, tex.a);
            }
            ENDCG
        }

        Pass
        {
            Name "SHADOW_CASTER"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            ZWrite On
			AlphaToMask Off

            CGPROGRAM
            #pragma target 3.0

            #pragma vertex vertShadowCaster
            #pragma fragment fragShadowCaster

            #pragma multi_compile_shadowcaster

            #include "HLSLSupport.cginc"
            #if SHADER_API_D3D11 || SHADER_API_GLCORE || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN
            #    define CAN_SKIP_VPOS
            #endif  // SHADER_API_D3D11 || SHADER_API_GLCORE || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN
            #include "Lighting.cginc"
            #include "UnityPBSLighting.cginc"


            //! Dither mask texture.
            UNITY_DECLARE_TEX3D(_DitherMaskLOD);


            /*!
             * @brief Input data type for vertex shader function, vertShadowCaster().
             * @see vertShadowCaster
             */
            struct appdata_shadowcaster
            {
                //! Object space position of the vertex.
                float4 vertex : POSITION;
                //! UV coordinate of the vertex.
                float2 texcoord : TEXCOORD0;
                //! Object space normal of the vertex.
                float3 normal : NORMAL;
                //! instanceID for single pass instanced rendering.
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            /*!
             * @brief Output of the vertex shader, vertShadowCaster()
             * and input of fragment shader, fragShadowCaster().
             * @see vertShadowCaster
             * @see fragShadowCaster
             */
            struct v2f_shadowcaster
            {
                /*!
                 * @brief Declare pos and vec.
                 *
                 * pos: Clip space position of the vertex.
                 * vec: Vector from the light source to the vertex, which is declared
                 * when define SHADOWS_CUBE and not defined SHADOWS_CUBE_IN_DEPTH_TEX.
                 */
                V2F_SHADOW_CASTER;
                //! UV coordinate of the vertex/fragment.
                float2 uv : TEXCOORD1;
            #if !defined(CAN_SKIP_VPOS)
                UNITY_VPOS_TYPE vpos : VPOS;
            #endif  // !defined(CAN_SKIP_VPOS)
                //! Instance ID for single pass instanced rendering, instanceID.
                UNITY_VERTEX_INPUT_INSTANCE_ID
                //! Stereo target eye index for single pass instanced rendering, stereoTargetEyeIndex.
                UNITY_VERTEX_OUTPUT_STEREO
            };

            /*!
             * @brief Vertex shader function for ShadowCaster pass.
             * @param [in] v  Input data.
             * @return Interpolation source data for fragment shader function, fragShadowCaster().
             * @see fragShadowCaster
             * @see https://forum.unity.com/threads/shadow-caster-billboard-to-camera.1214808/
             */
            v2f_shadowcaster vertShadowCaster(appdata_shadowcaster v)
            {
                v2f_shadowcaster o;

                UNITY_INITIALIZE_OUTPUT(v2f_shadowcaster, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                if (_Value < _ValueDiscardThreashold) {
                    o.pos = qNaN.xxxx;
                    return o;
                }

                //
                // TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                //
            #if defined(SHADOWS_CUBE) && !defined(SHADOWS_CUBE_IN_DEPTH_TEX)
                o.vec = mul(unity_ObjectToWorld, v.vertex).xyz - _LightPositionRange.xyz;
                o.pos = objectToClipPos(v.vertex);
            #else
            #    if defined(_BILLBOARDMODE_ALL_AXISES) || defined(_BILLBOARDMODE_Y_AXISES)
                float3 viewPos = mul((float3x3)unity_ObjectToWorld, v.vertex.xyz);
            #        if defined(_BILLBOARDMODE_ALL_AXISES)
                viewPos.z = -viewPos.z;
            #        else
                viewPos = float3(
                    dot(float3(1.0, unity_WorldToCamera._m01, 0.0), viewPos),
                    dot(float3(0.0, unity_WorldToCamera._m11, 0.0), viewPos),
                    dot(float3(0.0, unity_WorldToCamera._m21, -1.0), viewPos));
            #        endif  // defined(_BILLBOARDMODE_ALL_AXISES)
                const float4 worldOffset = float4(mul((float3x3)unity_CameraToWorld, viewPos), 0.0);
                float4 wPos = unity_ObjectToWorld._m03_m13_m23_m33 + worldOffset;
                if (unity_LightShadowBias.z != 0.0) {
                    const float3 wNormal = UnityObjectToWorldNormal(v.normal);
                    const float3 wLight = normalize(UnityWorldSpaceLightDir(wPos.xyz));
                    const float shadowCos = dot(wNormal, wLight);
                    const float shadowSine = sqrt(1.0 - shadowCos * shadowCos);
                    const float normalBias = unity_LightShadowBias.z * shadowSine;
                    wPos.xyz -= wNormal * normalBias;
                }
                o.pos = mul(UNITY_MATRIX_VP, wPos);
            #    else  // Assume defined(_BILLBOARDMODE_NONE)
                o.pos = UnityClipSpaceShadowCasterPos(v.vertex, v.normal);
            #    endif  // defined(_BILLBOARDMODE_ALL_AXISES) || defined(_BILLBOARDMODE_Y_AXISES)
                o.pos = UnityApplyLinearShadowBias(o.pos);
            #endif  // defined(SHADOWS_CUBE) && !defined(SHADOWS_CUBE_IN_DEPTH_TEX)
                o.uv = v.texcoord;
                UNITY_FLATTEN
                if (_ReverseInMirror && isInMirror()) {
                    o.uv.x = 1.0 - o.uv.x;
                }

                return o;
            }

            /*!
             * @brief Fragment shader function for ShadowCaster pass.
             * @param [in] fi  Input data from vertex shader.
             * @param [in] facing  Facing parameter.
             * @return Depth of fragment.
             */
            fixed fragShadowCaster(v2f_shadowcaster fi, face_t facing : FACE_SEMANTICS) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(fi);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(fi);

            #if defined(CAN_SKIP_VPOS)
                const float2 vpos = fi.pos;
            #else
                const float2 vpos = fi.vpos;
            #endif  // defined(CAN_SKIP_VPOS)

                float2 uv = fi.uv;
                if (_BillboardMode == 0) {
                    uv.x = _ReversedBackface && isFacing(facing) ? fi.uv.x : (1.0 - fi.uv.x);
                }

                const half4 tex = sampleSpriteTex(uv);
                const half alpha = tex.a;
                const half alphaRef = UNITY_SAMPLE_TEX3D(_DitherMaskLOD, float3(vpos.xy * 0.25, alpha * 0.9375)).a;
                clip(alphaRef - 0.01);

                SHADOW_CASTER_FRAGMENT(fi)
            }
            ENDCG
        }
    }

    CustomEditor "Koturn.VRCHeartrate.Inspectors.SimpleCounterGUI"
}
