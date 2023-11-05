Shader "MyEffects/CG_WaterEffect"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _WaterHeightMap("Normal map", 2D) = "black" {}
        _BlueNoise("Blue noise", 2D) = "black" {}
    }
        SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0


            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _WaterHeightMap;
            sampler2D _BlueNoise;
            sampler2D _CameraDepthTexture;

            uniform float3 _CamPosition;
            uniform float _WaterHeight;
            uniform float3 _WaterColor;
            uniform float _Density;
            uniform float4x4 _CamToWorld;
            uniform float3 _CamFrustum[4];
            uniform float3 _SunColor;
            uniform float3 _SunDirection;
            uniform float2 _BlueNoiseToSceen;
            uniform int _Steps;
            uniform float _StepSize;
            uniform float _Anisotropy;
            uniform float2 _HeightMapTileSize;
            uniform float2 _HeightMapTileOffset;
            uniform float _LightShaftIntensity;
            uniform float _AmbientLightIntensity;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 ray : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;

                bool t = v.vertex.y > .5;
                bool r = v.vertex.x > .5;
                o.ray = _CamFrustum[t ? (r ? 2 : 1) : (r ? 3 : 0 )];
                o.ray = mul(_CamToWorld, o.ray);
                v.vertex.z = 0;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            
            float fresnel(float3 dir, float3 normal, float eta)
            {
                if(eta <= 0.0) return 1.0;
                float cosi = -dot(dir, normal);
                if (cosi < 0.0)
                {
                    eta = 1.0 / eta;
                    cosi = -cosi;
                }
                float sint = eta * sqrt(max(0.0, 1.0 - cosi * cosi));
                float cos2t = 1.0 - sint * sint;
                if (cos2t < 0.0) return 1.0;
                float cost = sqrt(cos2t);
                float sqRs = (cosi - eta * cost) / (cosi + eta * cost);
                float sqRp = (eta * cosi - cost) / (eta * cosi + cost);
                return (sqRs * sqRs + sqRp * sqRp) * 0.5;
            }

            float henyeyGreenstein(float3 dirI, float3 dirO)
            {
                return (1-_Anisotropy*_Anisotropy) / pow(1+_Anisotropy*(_Anisotropy-2*dot(dirI, dirO)), 1.5);
            }

            float heightMap(float2 pos)
            {
                float2 uv = frac(pos*.15) * _HeightMapTileSize + _HeightMapTileOffset;
                return tex2Dlod(_WaterHeightMap, float4(uv, 0, 0)).r;
            }

            float3 directLight(float3 pos)
            {
                float d = (pos.y - _WaterHeight) / _SunDirection.y;
                float2 uv = (pos - d * _SunDirection).xz;
                return _SunColor * clamp((heightMap(uv)-.3)*3., 0., 1.) * exp(- d * _Density);
            }

            float3 ambientLight(float3 pos)
            {
                float att = exp((pos.y - _WaterHeight) * _Density);
                return _AmbientLightIntensity * unity_AmbientSky * att;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                
                float depth = Linear01Depth(tex2D(_CameraDepthTexture, i.uv).r) * _ProjectionParams.z;
                float flatDepth = 1.0 / length(i.ray); // to convert a spherical euclidian depth to a z buffer distance

                float3 rayDir = i.ray * flatDepth; // normalized i.ray, used for shading
                float3 rayPos = _CamPosition;

                float planeDepth = (_WaterHeight - rayPos.y) / i.ray.y;

                float delta = max(.05, planeDepth*.05);
                float2 projectedUv = (rayPos + planeDepth * i.ray).xz;

                // gradient of the height map to estimate the normal
                float heightCenter = heightMap(projectedUv);
                float heightDx = (heightCenter-heightMap(projectedUv + float2(delta, 0))) / delta;
                float heightDy = (heightCenter-heightMap(projectedUv + float2(0, delta))) / delta;

                float3 planeNorm = normalize(float3(heightDx, 3., heightDy));


                float planeFresnel = fresnel(-rayDir, planeNorm, 1.33);

                float3 bouncedSkyboxRay = rayPos.y > _WaterHeight ? reflect(rayDir, planeNorm) : refract(rayDir, -planeNorm, 1.33);
                bouncedSkyboxRay.y = abs(bouncedSkyboxRay.y);
                half3 bouncedSkyboxColor = DecodeHDR(UNITY_SAMPLE_TEXCUBE(unity_SpecCube0, bouncedSkyboxRay), unity_SpecCube0_HDR);
                
                float volumeBegin = rayPos.y > _WaterHeight && planeDepth > 0.0 ? min(planeDepth, depth) : 0.0;
                float volumeEnd = (rayPos.y - _WaterHeight) * planeDepth > 0.0 ? depth : planeDepth > 0.0 ? min(depth, planeDepth) : 0.0;

                // volume marching loop
                float volAtt = 1.0;
                float3 volCol = 0.0;
                float t = volumeBegin + _StepSize * tex2D(_BlueNoise, i.uv * _BlueNoiseToSceen).r;
                float hg = henyeyGreenstein(rayDir, -_SunDirection);
                float3 dl = 0.0;

                float stepAbsorbance = exp(-_StepSize / flatDepth * _Density);
                float3 stepTransmittance = _WaterColor * (1.0 - stepAbsorbance);

                [unroll(64)]
                for(int j = 0; j < _Steps && t < volumeEnd; j++)
                {
                    float3 pos = rayPos + t * i.ray;
                    dl = hg * _LightShaftIntensity * directLight(pos);
                    volCol += stepTransmittance * volAtt * (dl + ambientLight(pos));
                    volAtt *= stepAbsorbance;
                    t += _StepSize;
                }
                
                if(t > volumeEnd) {
                    float lastStepSize = volumeEnd-(t-_StepSize);
                    float transmittance = hg * _WaterColor * (1.0 - exp(-lastStepSize / flatDepth * _Density));
                    volCol += transmittance * volAtt * dl;
                }
                // compute the total volume absorption (to attenuate what's behind the volume)
                volAtt = exp((volumeBegin - volumeEnd) * _Density);

                // merging all light interactions
                
                float3 att = 1.0;
                float3 col = 0.0;
                
                if (rayPos.y > _WaterHeight)
                {
                    // water surface from above
                    if (planeDepth > 0.0 & planeDepth < depth)
                    {
                        col += att * planeFresnel * bouncedSkyboxColor;
                        att *= 1.0 - planeFresnel;
                    }
                    // volume after
                    col += volCol;
                    att *= volAtt;
                }
                else
                {
                    // volume before
                    col += volCol;
                    att *= volAtt;
                    // water surface from below
                    if (planeDepth > 0.0 & planeDepth < depth)
                    {
                        col += att * (1.0 - planeFresnel) * bouncedSkyboxColor;
                        att *= 0.0;
                    }
                }
                
                // add what's behind (the background is added but considered far enough not to be an issue)
                col += tex2D(_MainTex, i.uv).rgb * att;

                col = (col * (2.51 * col + .03)) / (col * (2.43 * col + .59) + .14); // quick filmic curve
                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }
}
 
