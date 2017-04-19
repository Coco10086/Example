// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/TestShader" {
	Properties {
         _MainTex ("Base (RGB)", 2D) = "white" {}
		 [TestMatPropDrawer(_TESTPROP_ON)]
		 _testProp ("TestProp", int) = 1
		 [Toggle(_TESTPROP2_ON)]
		 _testProp2 ("TestProp2", int) = 1
     }
     SubShader
    {
        pass 
        {
            Tags{ "LightMode"="ForwardBase"}
            CGPROGRAM
            #pragma shader_feature _TESTPROP2_ON
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
 
            struct vertOut{
                float4 pos:SV_POSITION;
                float4 color:COLOR;
            };
            vertOut vert(appdata_base v)
            {
                vertOut o = (vertOut)0;
                o.pos=UnityObjectToClipPos(v.vertex);
                
                o.color=float4(1,0,0,1);//红
                
                #if _TESTPROP_ON
                   // o.color=float4(0,1,0,1);//绿
              	#elif _TESTPROP2_ON
               	 	o.color=float4(0,0,0,1);//黑
                #endif
                
                return o;
            }
            float4 frag(vertOut i):COLOR
            {
                return i.color;
            }
            ENDCG
		}//end pass
	}
	 CustomEditor "CustomMaterialEditor"  
}