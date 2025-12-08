Shader "Custom/PuzzlePieceShader"
{
    Properties
    {
        // Основная текстура для фрагмента (цвет берём из неё)
        _MainTex ("Texture", 2D) = "white" {}
        // Мультипликаторы каналов RGBA — позволяют тонко настраивать цвет/прозрачность
        _R ("Red", Range(0, 1)) = 1
        _G ("Green", Range(0, 1)) = 1
        _B ("Blue", Range(0, 1)) = 1
        _A ("Alpha", Range(0, 1)) = 1
        // Флаг инверсии RGB (0 — выкл, 1 — вкл)
        _Invert ("Invert RGB", Float) = 0
        // Яркость: добавляется к каждому каналу RGB (от -1 до 1)
        _Brightness ("Brightness", Range(-1, 1)) = 0

        // Границы обрезки (клиппинга) в системе координат родителя
        _MinX ("Min X", Float) = -0.5
        _MaxX ("Max X", Float) =  0.5
        _MinY ("Min Y", Float) = -0.5
        _MaxY ("Max Y", Float) =  0.5

        // Включение/выключение обрезки (0 — выкл, 1 — вкл)
        _EnableClip ("Enable Clip", Float) = 0
    }
    SubShader
    {
        // Теги рендера: непрозрачный материал, очередь Geometry
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            CGPROGRAM
            // Вершинный и фрагментный шейдеры
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // Входные данные вершины: позиция и UV
            struct appdata { float4 vertex: POSITION; float2 uv: TEXCOORD0; };
            // Данные, передаваемые во фрагментный шейдер:
            // - uv: координаты текстуры
            // - vertex: позиция в клип-пространстве
            // - worldPos: мировая позиция для вычислений клиппинга
            struct v2f { float2 uv: TEXCOORD0; float4 vertex: SV_POSITION; float3 worldPos: TEXCOORD1; };

            // Текстура и её параметры трансформации
            sampler2D _MainTex; float4 _MainTex_ST;
            // Параметры цвета и яркости
            float _R, _G, _B, _A, _Invert, _Brightness;

            // Параметры границ клиппинга и флаги
            float _MinX, _MaxX, _MinY, _MaxY;
            float _EnableClip;        // 0 = выкл, 1 = вкл
            // Матрица преобразования из мировых координат текущего объекта в локальные координаты родителя.
            // Заполняется через MaterialPropertyBlock из C# (например, матрицей inverse(parent.localToWorldMatrix) * object.localToWorldMatrix).
            float4x4 _WorldToParent;  // set via MaterialPropertyBlock

            v2f vert (appdata v)
            {
                v2f o;
                // Преобразуем вершину в клип-пространство (для рендера)
                o.vertex = UnityObjectToClipPos(v.vertex);
                // Трансформация UV с учётом _MainTex_ST (tile/offset)
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                // Сохраняем мировую позицию вершины для последующего клиппинга во фрагментном шейдере
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Обрезка пикселей по прямоугольной области в координатах родителя
                // Включается только при _EnableClip > 0.5 и при корректной подаче матрицы _WorldToParent
                if (_EnableClip > 0.5)
                {
                    // Перевод мировой позиции пикселя в локальные координаты родителя
                    float4 parentLocal = mul(_WorldToParent, float4(i.worldPos, 1));
                    // Проверка выхода за пределы по X/Y — если вышли, пиксель отбрасывается (discard)
                    if (parentLocal.x < _MinX || parentLocal.x > _MaxX ||
                        parentLocal.y < _MinY || parentLocal.y > _MaxY)
                    {
                        discard;
                    }
                }

                // Чтение цвета из текстуры
                fixed4 col = tex2D(_MainTex, i.uv);
                // Коррекция яркости: одинаково прибавляется ко всем компонентам RGB
                col.rgb += _Brightness;
                // Инвертирование цветов при включённом флаге
                if (_Invert > 0.5) col.rgb = 1 - col.rgb;
                // Мультипликаторы каналов для тонкой настройки цвета и альфы
                col.r *= _R; col.g *= _G; col.b *= _B; col.a *= _A;
                // Возвращаем итоговый цвет пикселя
                return col;
            }
            ENDCG
        }
    }
}