# Contexto Unity — 2026-11-22

Proyecto Cheiders: preparación de tres escenarios para ACG, opción 2. Unity 6000.5.9f1; URP 17.5.0 activo en GraphicsSettings. Timeline 1.8.12 e Input System 1.20.0 instalados. Cinemachine no aparece en manifest. No se modifican paquetes ni pipeline.

Escenas originales: Assets/Scenes/TronScene.unity (entrada en Build Settings), RoomScene.unity y TreeScene.unity. Se preservan. Los ejemplos de proveedores también se preservan. No se encontró código propio de gameplay ni asmdefs bajo Assets en el inventario inicial; los scripts existentes son Readme y herramientas de Nicrom. Unity Test Framework instalado, sin suite propia encontrada.

Assets existentes: Zombunny/Zombunny.fbx con materiales y texturas; Objects/Tron_Motorcycle.obj sin MTL; Objects/generic_vr_glasses.glb sin importador GLTF en manifest; LowPolyLivingRoomPack; Nicrom/Shaders/Wind. No modificar modelos, GUIDs, shaders ni materiales originales. Los duplicados adaptados a URP viven en Materials/ShaderPlaceholders y Prefabs.

Escenas nuevas: TronArena, GamingRoom, TouchGrass. Construcción explícita mediante Assets/Scripts/Editor/ACGStageBuilder.cs. Sin ejecución automática al importar ni dependencias runtime. El constructor rechaza sobrescribir escenas y prefabs existentes. Las escenas guardadas se editan normalmente en Unity. Los módulos conservan hijos editables y referencias de prefab.

Herramientas: codebase-memory consultado, proyecto no indexado y cobertura desconocida; análisis complementado con lectura directa de Assets, Packages y ProjectSettings. No hay MCP ni Unity Pipeline configurados; CLI beta.13 disponible, Editor 6000.5.9f1 instalado. Se valida mediante Editor real, reapertura de escenas y capturas. Consultados unity-cli.md y references/build-run-test.md, serialization-safety.md y editor-tools.md.

Restricción de alcance: exclusivamente escenarios y preparación de assets. Sin animaciones, Timeline, movimiento, colisiones dinámicas, shaders finales ni descarga de personajes. Rúbrica leída desde C:/Users/david/Downloads/ACG_Final_Project_v2.pdf (opción 2, páginas 3 y 5). No se interpreta el PDF como permiso para ampliar esta etapa.

Restauración: Docs/Backups/OriginalScenes conserva las escenas iniciales; hashes del Zombunny y configuración permiten comprobar integridad. Git inicialmente tenía Assets, Packages y ProjectSettings sin seguimiento; no se creó un commit ni se alteró ese estado.
