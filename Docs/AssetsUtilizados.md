# Assets utilizados y procedencia

Fecha de consulta: 22 de noviembre de 2026. No se descargaron nuevos paquetes/modelos externos ni se compraron recursos. Se usaron assets ya presentes y geometría propia construida para este proyecto.

| Recurso | Procedencia y uso | Licencia / estado |
| --- | --- | --- |
| Torres, retícula, portal, balizas, motos cian/naranja y paredes de energía | Geometría original construida mediante ACGStageBuilder con primitivas Unity y malla de aro propia | Sin modelo externo descargado; el código y composición quedan en el proyecto. No se afirma que los componentes del motor sean CC0 |
| Habitación, escritorio, monitores, teclado, PC, silla y visor VR | Geometría original del constructor | Sin recurso de terceros descargado |
| Árbol principal, ramas, raíces, arbustos, camino y cerca | Geometría original del constructor; tronco/hojas separados | Sin recurso de terceros descargado |
| Librero, sofá, planta, taza y gamepad | Assets/LowPolyLivingRoomPack; paquete identificado como Low Poly Living Room Pack (71 Models), The PolynestLab | Página oficial: FREE, Standard Unity Asset Store EULA; **no CC0**. Reutilizados localmente; no se verificó el recibo de adquisición de la cuenta |
| Árboles secundarios, matas de hierba y rocas | Assets/Nicrom/Shaders/Wind; Low Poly Wind, Nicrom | Página oficial: FREE, Standard Unity Asset Store EULA; **no CC0**. Se reutilizan mallas y atlas locales con nuevos materiales URP/Lit; no se modifica ni usa como shader final el shader de viento |
| Zombunny | Assets/Zombunny/Zombunny.fbx y texturas, proporcionados previamente por el equipo; instancias estáticas y variantes de material nuevas en Prefabs/Characters | Origen externo/licencia no documentados en los archivos inspeccionados. Original conservado; confirmar atribución con el equipo para la entrega |
| URP/Lit y efectos Bloom/Tonemapping/Vignette | Unity/URP 17.5.0 ya instalado | Componentes del motor y del paquete; materiales temporales, no shaders originales del equipo |

Fuentes oficiales consultadas:

- [Low Poly Living Room Pack — Unity Asset Store](https://assetstore.unity.com/packages/3d/props/interior/low-poly-living-room-pack-71-models-cozy-interior-furniture-prop-320366)
- [Low Poly Wind — Unity Asset Store](https://assetstore.unity.com/packages/vfx/shaders/low-poly-wind-182586)
- [Unity Asset Store Terms / EULA](https://unity.com/legal/as-terms)

Estas páginas identifican la licencia ofrecida por el distribuidor, pero no prueban la licencia particular de archivos obtenidos antes de esta sesión. El equipo debe conservar sus comprobantes de adquisición. No etiquetar estos paquetes como CC0 ni redistribuirlos como paquetes de assets independientes.

## Archivos existentes evaluados pero no usados en los escenarios nuevos

- `Assets/Objects/Tron_Motorcycle.obj`: referencia a `Tron_Motorcycle.mtl`, ausente. No contiene aviso de licencia/origen verificable. Se conserva; las motos nuevas tienen geometría propia editable.
- `Assets/Objects/generic_vr_glasses.glb`: existe, pero no hay importador GLTF en el manifest ni licencia local. No se afirmó haberlo importado como modelo usable. Se conserva; se creó un visor propio con anclajes.
- Escenas demo de proveedores, escenas originales y Shader Graph de Zombunny: conservados. No se migró el proyecto ni se reescribieron materiales originales para corregir su aspecto globalmente.

No hay descargas manuales pendientes para abrir los tres escenarios. Solo queda confirmar la procedencia del personaje existente para completar los créditos del proyecto académico.
