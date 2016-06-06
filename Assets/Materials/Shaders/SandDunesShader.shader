Shader "Custom/Sand Dunes" {
        Properties {
            _EdgeLength ("Edge length", Range(2,500)) = 15
            _MainTex ("Base (RGB)", 2D) = "white" {}
            _SandTex ("Sand", 2D) = "white" {}
            _DispTex ("Disp Texture", 2D) = "gray" {}
            _NormalMap ("Normalmap", 2D) = "bump" {}
          	_NoiseTex ("Noise Texture", 2D) = "gray" {}
            _ChurnedNormal ("Churned Normal", 2D) = "bump" {}
            _Displacement ("Displacement", Range(0, 100.0)) = 0.3
            _DispOffset ("Disp Offset", Range(-10, 10)) = 0.5
            _Color ("Color", color) = (1,1,1,0)
            _SpecPow ("Metallic", Range(0, 1)) = 0.5
            _GlossPow ("Smoothness", Range(0, 1)) = 0.5
            _DetTex("Detail Texture", 2D) = "grey" {}
			_DetailNormalMap("Normal Map", 2D) = "bump" {}
			_Tess ("Tessellation", float) = 4
			_Frequency ("Frequency", float) = 10
			_Lacunarity ("Lacunarity", float) = 10
			_Gain ("Gain", float) = 10
			_TimeMultiplier ("Time", Range (0, 1000)) = 100
			_TimeOffset ("Time Offset", Range (0, 1000)) = 0
			_ScaleMultiplier ("Scale", Range (0, 10)) = 1
			_PosOffset ("Pos Offset", Vector) = (0,0,0)
			_MaskTime ("Mask Time", Range (0, 1)) = 0
        }
        SubShader {
            Tags { "RenderType"="Opaque" }
            LOD 300
            
            CGPROGRAM
            #pragma surface surf Standard addshadow fullforwardshadows vertex:disp tessellate:tessEdge
            #pragma target 5.0
            #include "Tessellation.cginc"
            #include "ImprovedPerlinNoise4D.cginc"
           	#define OCTAVES 6

           	float _Tess;
        	float _EdgeLength;
        	sampler2D _DispTex;
        	sampler2D _NoiseTex;
        	sampler2D _MainTex;
            sampler2D _DetTex;
            sampler2D _NormalMap;
            sampler2D _SandTex;
            sampler2D _ChurnedNormal;
        	uniform float4 NoiseTex_ST;
            uniform float4 _DispTex_ST;
            float _Displacement;
            float _DispOffset;
            float _TimeMultiplier;
            float _TimeOffset;
            float _ScaleMultiplier;
            float3 _PosOffset;
            float _MaskTime;
            fixed4 _Color;
            float _SpecPow;
            float _GlossPow;

            struct appdata {
                float4 vertex : POSITION;
                float4 tangent : TANGENT;
                float3 normal : NORMAL;
                float2 texcoord : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                float2 texcoord2 : TEXCOORD2;
            };

            float4 tessEdge (appdata v0, appdata v1, appdata v2)
            {
               //float minDist = 10.0;
               //float maxDist = 25.0;
               //return UnityDistanceBasedTess(v0.vertex, v1.vertex, v2.vertex, minDist, maxDist, _Tess);
               return UnityEdgeLengthBasedTessCull (v0.vertex, v1.vertex, v2.vertex, _EdgeLength, _Tess);// * _Tess;
            }

			void disp (inout appdata v)
			{
				half4 n = tex2Dlod(_NoiseTex, float4 (v.texcoord.xy * NoiseTex_ST.xy + NoiseTex_ST.zw,0,0)); 
				half4 m = tex2Dlod(_DispTex, float4 (v.texcoord.xy * _DispTex_ST.xy + _DispTex_ST.zw,0,0));
                //the churned sand mask is revealed
				float mask = n.r;
				if ((m.b + n.r) > _MaskTime) {
					mask = m.r - (n.r * 0.25);
					//make the edges bleed with the noise texture
				}
				mask = pow (mask, 5);
				float t = turbulence (float4((v.vertex.xyz + _PosOffset) * _ScaleMultiplier, (_Time.x * _TimeMultiplier) + _TimeOffset), OCTAVES) * _Displacement * mask;

				v.vertex.xyz += v.normal * t;
				v.vertex.y += _DispOffset;
			}

            struct Input {
                float2 uv_MainTex;
                float2 uv_DetTex;
                float2 uv_SandTex;
                float2 uv_ChurnedNormal;
                float2 uv_NoiseTex;
                float3 worldPos;
            };

            void surf (Input IN, inout SurfaceOutputStandard o) {
                half4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
                half4 m = tex2D (_DispTex, IN.uv_MainTex);
                half4 s = tex2D (_SandTex, IN.uv_SandTex);
                half4 d = tex2D (_DetTex, IN.uv_DetTex);
                half4 n = tex2D (_NoiseTex, IN.uv_NoiseTex);

                //the churned sand mask is revealed
				float mask = 0;
				if ((m.b + n.r) > _MaskTime - 0.035) {
					mask = m.r;
				}
				//Must use object pos not world pos for noise uv's
        		float3 objectPos = mul(unity_WorldToObject,float4(IN.worldPos,1)).xyz;
        		float t = turbulence (float4((objectPos + _PosOffset) * _ScaleMultiplier, (_Time.x * _TimeMultiplier) + _TimeOffset), OCTAVES) * _Displacement * mask;
                //harden turbulence edges
                t = max (t * 2, 1);

                o.Albedo = c.rgb * (lerp (d.rgb, s.rgb, clamp (mask * t, 0, 1)));
                o.Metallic = _SpecPow;
                o.Smoothness = _GlossPow * d.rgb * (1 - (mask * 0.5));
                o.Normal = lerp (UnpackNormal(tex2D(_NormalMap, IN.uv_MainTex)), UnpackNormal(tex2D(_ChurnedNormal, IN.uv_ChurnedNormal)), mask);
            }
            ENDCG
        }
        FallBack "Diffuse"
    }