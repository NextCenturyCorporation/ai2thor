// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'
// Upgrade NOTE: updated the depth shader to use a TurboColorMap to display a spectrum
//               of colors to represent distance from the camera.

Shader "Hidden/Depth" {
     Properties
     {
     	 // Depth power of 1 sets it so the spectrum of colors is linear from the camera's near to far clipping
     	 // plane. The closer the value is to 0 sets allocates more room in the spectrum to closer objects.
     	 _DepthPower ("Depth Power", Range(0, 1)) = 0.8
     }
     SubShader
     {
         Pass
         {
             CGPROGRAM
			// Upgrade NOTE: excluded shader from DX11, OpenGL ES 2.0 because it uses unsized arrays
			#pragma exclude_renderers d3d11 gles

             #pragma vertex vert
             #pragma fragment frag
             #include "UnityCG.cginc"

             uniform sampler2D _CameraDepthTexture;
             uniform float _DepthPower;
             
             struct input
             {
                 float4 pos : POSITION;
                 half2 uv : TEXCOORD0;
             };

             struct output
             {
                 float4 pos : SV_POSITION;
                 half2 uv : TEXCOORD0;
             };

             output vert(input i)
             {
                 output o;
                 o.pos = UnityObjectToClipPos(i.pos);
                 o.uv = i.uv;
             	 return o;
             }

             // Copyright 2019 Google LLC.
			 // SPDX-License-Identifier: Apache-2.0
			 // Original LUT: https://gist.github.com/mikhailov-work/ee72ba4191942acecc03fe6da94fc73f
			float3 TurboColormap(float x)
			{
			    const float4 kRedVec4 = float4(0.13572138, 4.61539260, -42.66032258, 132.13108234);
			    const float4 kGreenVec4 = float4(0.09140261, 2.19418839, 4.84296658, -14.18503333);
			    const float4 kBlueVec4 = float4(0.10667330, 12.64194608, -60.58204836, 110.36276771);
			    const float2 kRedVec2 = float2(-152.94239396, 59.28637943);
			    const float2 kGreenVec2 = float2(4.27729857, 2.82956604);
			    const float2 kBlueVec2 = float2(-89.90310912, 27.34824973);
			    x = saturate(x);
			    float4 v4 = float4(1.0, x, x * x, x * x * x);
			    float2 v2 = v4.zw * v4.z;
			    return float3(
			        dot(v4, kRedVec4)   + dot(v2, kRedVec2),
			        dot(v4, kGreenVec4) + dot(v2, kGreenVec2),
			        dot(v4, kBlueVec4)  + dot(v2, kBlueVec2)
			    );
			}
             
             float4 frag(output o) : COLOR
             {
				float depth = tex2D(_CameraDepthTexture, o.uv).r;
				depth = Linear01Depth(depth);
             	depth = pow(depth, _DepthPower);
             	return float4(TurboColormap(depth), 1);
			 }

             ENDCG
         }
     }
 }
