Shader "PointCloud"
{
    Properties
    {
		_MainTex("Texture", 2D) = "white" {}
		_DepthTex("Depth Texture", 2D) = "black" {}
		_PointCloudTemplateTex("Point Cloud Template Texture", 2D) = "black" {}
		_CubeScale("Cube Scale", Range(0.00001, 0.003)) = 0.0005
		_MinDepth("Min Depth", Float) = 0.01
		_MaxDepth("Max Dpeth", Float) = 10.0
	}
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
			#pragma geometry geom
            #pragma fragment frag

            #include "UnityCG.cginc"

			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2g {
				float4 position : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			struct g2f {
				float4 position : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

            sampler2D _MainTex;
            float4 _MainTex_ST;
			sampler2D _DepthTex;
			float4 _DepthTex_ST;
			float4 _DepthTex_TexelSize;
			sampler2D _PointCloudTemplateTex;
			float4 _PointCloudTemplateTex_ST;
			float _CubeScale;
			float4x4 _PointTransform;

			float _MinDepth;
			float _MaxDepth;

            v2g vert (appdata v)
            {
				v2g o;
				o.position = mul(unity_ObjectToWorld, v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
            }

			g2f VertexOutput(float3 pos, float2 uv)
			{
				g2f o;
				o.position = UnityWorldToClipPos(pos);
				o.uv = uv;
				return o;
			}

			void CreateCube(inout TriangleStream<g2f> tristream, float3 pos, float2 uv)
			{
				// Cube points
				float scale = _CubeScale;
				float3 c_p0 = pos + float3(-1, -1, -1) * scale;
				float3 c_p1 = pos + float3(+1, -1, -1) * scale;
				float3 c_p2 = pos + float3(-1, +1, -1) * scale;
				float3 c_p3 = pos + float3(+1, +1, -1) * scale;
				float3 c_p4 = pos + float3(-1, -1, +1) * scale;
				float3 c_p5 = pos + float3(+1, -1, +1) * scale;
				float3 c_p6 = pos + float3(-1, +1, +1) * scale;
				float3 c_p7 = pos + float3(+1, +1, +1) * scale;

				// Vertex outputs
				float3 c_n = float3(-1, 0, 0);
				tristream.Append(VertexOutput(c_p2, uv));
				tristream.Append(VertexOutput(c_p0, uv));
				tristream.Append(VertexOutput(c_p6, uv));
				tristream.Append(VertexOutput(c_p4, uv));
				tristream.RestartStrip();

				c_n = float3(1, 0, 0);
				tristream.Append(VertexOutput(c_p1, uv));
				tristream.Append(VertexOutput(c_p3, uv));
				tristream.Append(VertexOutput(c_p5, uv));
				tristream.Append(VertexOutput(c_p7, uv));
				tristream.RestartStrip();

				c_n = float3(0, -1, 0);
				tristream.Append(VertexOutput(c_p0, uv));
				tristream.Append(VertexOutput(c_p1, uv));
				tristream.Append(VertexOutput(c_p4, uv));
				tristream.Append(VertexOutput(c_p5, uv));
				tristream.RestartStrip();

				c_n = float3(0, 1, 0);
				tristream.Append(VertexOutput(c_p3, uv));
				tristream.Append(VertexOutput(c_p2, uv));
				tristream.Append(VertexOutput(c_p7, uv));
				tristream.Append(VertexOutput(c_p6, uv));
				tristream.RestartStrip();

				c_n = float3(0, 0, -1);
				tristream.Append(VertexOutput(c_p1, uv));
				tristream.Append(VertexOutput(c_p0, uv));
				tristream.Append(VertexOutput(c_p3, uv));
				tristream.Append(VertexOutput(c_p2, uv));
				tristream.RestartStrip();

				c_n = float3(0, 0, 1);
				tristream.Append(VertexOutput(c_p4, uv));
				tristream.Append(VertexOutput(c_p5, uv));
				tristream.Append(VertexOutput(c_p6, uv));
				tristream.Append(VertexOutput(c_p7, uv));
				tristream.RestartStrip();
			}

			[maxvertexcount(24)]
			void geom(triangle v2g input[3], uint pid : SV_PrimitiveID,
				inout TriangleStream<g2f> tristream) {

				for (int x = -2; x < 2; x++)
				{
					for (int y = -2; y < 2; y++)
					{
						float2 uv = input[0].uv + float2(x * _DepthTex_TexelSize.x, y * _DepthTex_TexelSize.y);
						if (uv.x > 0 && uv.y > 0 && uv.x < 1 && uv.y < 1)
						{
							// Convert from normalized float back to depth in meters
							float depth = (65535.0f * tex2Dlod(_DepthTex, float4(input[0].uv, 0, 0)).r) / 1000.0f;
							if (depth < _MinDepth || depth > _MaxDepth)
							{
								continue;
							}

							float3 pointCloudTemplate = tex2Dlod(_PointCloudTemplateTex, float4(input[0].uv, 0, 0)).xyz;
							pointCloudTemplate.y *= -1.0f;
							pointCloudTemplate *= 0.001f;
							pointCloudTemplate *= depth;
							float4 pointCloud = mul(_PointTransform, float4(pointCloudTemplate, 1.0f));
							CreateCube(tristream, pointCloud.xyz, uv);
						}
					}
				}
			}

            fixed4 frag (v2g i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
				return col;
			}
            ENDCG
        }
    }
}
