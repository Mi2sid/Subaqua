// Code repris et adapté depuis : https://bgolus.medium.com/normal-mapping-for-a-triplanar-shader-10bf39dca05a#9363

Shader "Triplanar/Surface Shader (RNM)"
{
    Properties
    {
        _MainTex ("Top texture", 2D) = "white" {}
        _SecondaryTex ("Bottom and sides texture", 2D) = "white" {}
        [NoScaleOffset] _MainNM("Normal Map top", 2D) = "bump" {}
        [NoScaleOffset] _SecondaryNM("Normal Map bottom and sides", 2D) = "bump" {}
        _NormalStrength("Normal Strength", Range(0, 5)) = 1.0
        [NoScaleOffset] _OcclusionMapMain("Occlusion top", 2D) = "white" {}
        [NoScaleOffset] _OcclusionMapSecondary("Occlusion bottom and sides", 2D) = "white" {}
        _OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 1.0
        [Gamma] _Metallic("Metallic", Range(0, 1)) = 0
        _Glossiness("Smoothness", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        #include "UnityStandardUtils.cginc"

        // flip UVs horizontally to correct for back side projection
        #define TRIPLANAR_CORRECT_PROJECTED_U

        // offset UVs to prevent obvious mirroring
        #define TRIPLANAR_UV_OFFSET

        // Reoriented Normal Mapping
        // http://blog.selfshadow.com/publications/blending-in-detail/
        // Altered to take normals (-1 to 1 ranges) rather than unsigned normal maps (0 to 1 ranges)
        half3 blend_rnm(half3 n1, half3 n2)
        {
            n1.z += 1;
            n2.xy = -n2.xy;

            return n1 * dot(n1, n2) / n1.z - n2;
        }

        sampler2D _MainTex;
        sampler2D _SecondaryTex;
        float4 _MainTex_ST;

        sampler2D _MainNM;
        sampler2D _SecondaryNM;
        sampler2D _OcclusionMapMain;
        sampler2D _OcclusionMapSecondary;

        half _Glossiness;
        half _Metallic;

        half _OcclusionStrength;
        half _NormalStrength;

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
            INTERNAL_DATA
        };

        float3 WorldToTangentNormalVector(Input IN, float3 normal)
        {
            float3 t2w0 = WorldNormalVector(IN, float3(1,0,0));
            float3 t2w1 = WorldNormalVector(IN, float3(0,1,0));
            float3 t2w2 = WorldNormalVector(IN, float3(0,0,1));
            float3x3 t2w = float3x3(t2w0, t2w1, t2w2);
            return normalize(mul(t2w, normal));
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // work around bug where IN.worldNormal is always (0,0,0)!
            IN.worldNormal = WorldNormalVector(IN, float3(0,0,1));

            // calculate triplanar blend
            half3 triblend = pow(abs(IN.worldNormal), 5);
            triblend /= dot(triblend, 1);

            // calculate triplanar uvs
            // applying texture scale and offset values ala TRANSFORM_TEX macro
            float2 uvX = IN.worldPos.zy * _MainTex_ST.xy + _MainTex_ST.zy;
            float2 uvY = IN.worldPos.xz * _MainTex_ST.xy + _MainTex_ST.zy;
            float2 uvZ = IN.worldPos.xy * _MainTex_ST.xy + _MainTex_ST.zy;

            // offset UVs to prevent obvious mirroring
            #if defined(TRIPLANAR_UV_OFFSET)
            uvY += 0.33;
            uvZ += 0.67;
            #endif

            // minor optimization of sign(). prevents return value of 0
            half3 axisSign = IN.worldNormal < 0 ? -1 : 1;
            const float isBot = step(IN.worldNormal.y, 0);

            // flip UVs horizontally to correct for back side projection
            #if defined(TRIPLANAR_CORRECT_PROJECTED_U)
            uvX.x *= axisSign.x;
            uvY.x *= axisSign.y;
            uvZ.x *= -axisSign.z;
            #endif

            // albedo textures
            fixed4 colX = tex2D(_SecondaryTex, uvX);
            fixed4 colbY = tex2D(_SecondaryTex, uvY);
            fixed4 coltY = tex2D(_MainTex, uvY);
            fixed4 colZ = tex2D(_SecondaryTex, uvZ);

            fixed4 col = colX * triblend.x +
                colbY * triblend.y * isBot +
                coltY * triblend.y * (1 - isBot) +
                colZ * triblend.z;

            // occlusion textures
            half occX = tex2D(_OcclusionMapSecondary, uvX).g;
            half occbY = tex2D(_OcclusionMapSecondary, uvY).g;
            half occtY = tex2D(_OcclusionMapMain, uvY).g;
            half occZ = tex2D(_OcclusionMapSecondary, uvZ).g;
            half occ = LerpOneTo(
                occX * triblend.x +
                occbY * triblend.y * isBot +
                occtY * triblend.y * (1 - isBot) +
                occZ * triblend.z,
                _OcclusionStrength);

            // tangent space normal maps
            half3 tnormalX = UnpackNormal(tex2D(_SecondaryNM, uvX));
            half3 tnormalbY = UnpackNormal(tex2D(_SecondaryNM, uvY));
            half3 tnormaltY = UnpackNormal(tex2D(_MainNM, uvY));
            half3 tnormalZ = UnpackNormal(tex2D(_SecondaryNM, uvZ));

            // flip normal maps' x axis to account for flipped UVs
            #if defined(TRIPLANAR_CORRECT_PROJECTED_U)
            tnormalX.x *= axisSign.x;
            tnormalbY.x *= axisSign.y;
            tnormaltY.x *= axisSign.y;
            tnormalZ.x *= -axisSign.z;
            #endif

            half3 absVertNormal = abs(IN.worldNormal);

            // swizzle world normals to match tangent space and apply reoriented normal mapping blend
            tnormalX = blend_rnm(half3(IN.worldNormal.zy, absVertNormal.x), tnormalX);
            tnormalbY = blend_rnm(half3(IN.worldNormal.xz, absVertNormal.y), tnormalbY);
            tnormaltY = blend_rnm(half3(IN.worldNormal.xz, absVertNormal.y), tnormaltY);
            tnormalZ = blend_rnm(half3(IN.worldNormal.xy, absVertNormal.z), tnormalZ);

            // apply world space sign to tangent space Z
            tnormalX.z *= axisSign.x;
            tnormalbY.z *= axisSign.y;
            tnormaltY.z *= axisSign.y;
            tnormalZ.z *= axisSign.z;

            // sizzle tangent normals to match world normal and blend together
            half3 worldNormal =
                tnormalX.zyx * triblend.x +
                tnormalbY.xzy * triblend.y * isBot +
                tnormaltY.xyz * triblend.y * (1 - isBot) +
                tnormalZ.xyz * triblend.z
            ;

            // set surface ouput properties
            o.Albedo = col.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Occlusion = occ;

            // convert world space normals into tangent normals
            worldNormal = half3(worldNormal.rg * _NormalStrength, lerp(1, worldNormal.b, saturate(_NormalStrength))); // https://docs.unity3d.com/Packages/com.unity.shadergraph@7.1/manual/Normal-Strength-Node.html
            o.Normal = WorldToTangentNormalVector(IN, worldNormal);
        }
        ENDCG
    }
    FallBack "Diffuse"
}


/* Ma version personelle sans normal ni occlusion

Shader "Custom/TriplanarMapping"
{
    Properties
    {
        _Sharpness ("Blend sharpness", float) = 1
        _MainTex ("Top texture", 2D) = "white" {}
        _MainNormal ("Top normal", 2D) = "white" {}
        _MainTiling ("Top texture tiling scale", Range(0.001, 10)) = 0.1
        _SecondaryTex ("Bottom and sides texture", 2D) = "white" {}
        _SecondaryNormal ("Bottom and sides normals", 2D) = "white" {}
        _SecondaryTiling ("Bottom and sides texture tiling scale", Range(0.001, 10)) = 0.1
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _SecondaryTex;
        sampler2D _MainNormal;
        sampler2D _SecondaryNormal;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldNormal;
            float3 worldPos;
            INTERNAL_DATA
        };

        half _Glossiness;
        half _Metallic;
        half _MainTiling;
        half _SecondaryTiling;
        float _Sharpness;
        fixed4 _Color;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
        // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)
        

        float3 WorldToTangentNormalVector(Input IN, float3 normal) {
            float3 t2w0 = WorldNormalVector(IN, float3(1,0,0));
            float3 t2w1 = WorldNormalVector(IN, float3(0,1,0));
            float3 t2w2 = WorldNormalVector(IN, float3(0,0,1));
            float3x3 t2w = float3x3(t2w0, t2w1, t2w2);
            return normalize(mul(t2w, normal));
        }
        
        float3 triplanarNormal(float3 pos, float3 surfaceNormal)
        {
            const float3 tX = UnpackNormal(tex2D(_SecondaryNormal, pos.zy * _SecondaryTiling));
            const float3 tTop = UnpackNormal(tex2D(_MainNormal, pos.xz * _MainTiling));
            const float3 tBot = UnpackNormal(tex2D(_SecondaryNormal, pos.xz * _SecondaryTiling));
            const float3 tZ = UnpackNormal(tex2D(_SecondaryNormal, pos.xy * _SecondaryTiling));

            float3 blend = pow(abs(surfaceNormal), _Sharpness);
            blend /= dot(blend, 1);
            const float isBot = step(surfaceNormal.y, 0);
            return normalize(tTop * blend.y * (1. - isBot) + tBot * blend.y * isBot + tX * blend.x + tZ * blend.z);
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            const float4 tX = tex2D(_SecondaryTex, IN.worldPos.zy * _SecondaryTiling);
            const float4 tTop = tex2D(_MainTex, IN.worldPos.xz * _MainTiling);
            const float4 tBot = tex2D(_SecondaryTex, IN.worldPos.xz * _SecondaryTiling);
            const float4 tZ = tex2D(_SecondaryTex, IN.worldPos.xy * _SecondaryTiling);

            float3 blend = pow(abs(IN.worldNormal), _Sharpness);
            blend /= dot(blend, 1);
            const float isBot = step(IN.worldNormal.y, 0);

            const fixed4 c = tTop * blend.y * (1. - isBot) + tBot * blend.y * isBot + tX * blend.x + tZ * blend.z;

            o.Albedo = c.rgb;
            //o.Normal = triplanarNormal(IN.worldPos, IN.worldNormal);
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
*/