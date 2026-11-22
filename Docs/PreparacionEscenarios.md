# Preparación de escenarios ACG — etapa 1

Las escenas de trabajo nuevas están en `Assets/Scenes/TronArena.unity`, `GamingRoom.unity` y `TouchGrass.unity`. Abrir cualquiera desde Project y seleccionar Game para ver su cámara de vista previa. Se conservan las tres escenas anteriores y las demostraciones de los proveedores: se crearon exactamente tres escenarios nuevos sin borrar trabajo previo.

## Escenarios

**TronArena.** Superficie plana de 110 × 140 m; ciudad perimetral con tres módulos de torre, retícula luminosa, portal de llegada, balizas y espacio central libre. Dos motos originales, cian y naranja, con raíz al nivel del suelo y frente hacia +Z. Sus hijos son editables; `RiderAnchor` y `EnergyWallAnchor` son referencias vacías para la siguiente etapa. Las dos paredes luminosas son piezas estáticas independientes de 10 m: no generan estelas ni tienen movimiento o lógica de choque.

**GamingRoom.** Habitación de 9 × 8 m, escritorio con dos monitores, teclado, mouse, PC con ventiladores RGB, silla gamer, sofá, librero, planta, taza y gamepad. Área central libre sobre una alfombra para la actuación. Puerta con pivote de bisagra y apertura visible. Visor independiente con `HeadAttachment`, `HandGrip_L` y `HandGrip_R`. El set es abierto por el lado de cámara y por arriba para permitir encuadres; las paredes están agrupadas y pueden ocultarse individualmente.

**TouchGrass.** Jardín de 32 × 30 m con camino continuo de 2.6 m de ancho, árbol principal, árboles de fondo, hierba, arbustos, rocas y cerca. El tronco principal y sus ramas tienen material `Wood_Slot`, separado de las hojas. El contacto está en `HeroTree/HandContact`, a 1.25 m de altura; `ActorStand` se encuentra frente al tronco. El camino y la aproximación permanecen libres de hierba y rocas.

Cada escena tiene cámara, luz principal, relleno ambiental y perfil de posprocesado URP independiente. Los materiales emisivos producen bloom; no se ha horneado iluminación. Las luces decorativas de relleno son luces reales, ya que la emisión por sí sola no ilumina objetos cercanos.

En `Assets/Prefabs/Characters` hay variantes estáticas Neutral, Cyan y Orange del modelo Zombunny original. Se añadieron referencias de escala en habitación y jardín (1.65 m incluyendo orejas); conservan la pose del modelo y tienen Animator desactivado. No son poses ni animaciones finales. Para la siguiente etapa pueden habilitar su Animator o usar el rig original. La escena de carrera conserva las motos sin pilotos posados para no realizar todavía trabajo de animación.

## Materiales sustituibles

Los temporales usan URP/Lit y se guardan en `Assets/Materials/ShaderPlaceholders/`. No son los cinco shaders requeridos por la materia y no deben presentarse como tales.

| Superficie | Material/objeto preparado | Integración posterior |
| --- | --- | --- |
| Pared energética cian/naranja | Tron/EnergyWall_*_Surface_Slot | Cambiar material del hijo Energy surface |
| Motos y ciudad | Tron/Neon_Cyan, Neon_Orange, Obsidian | Sustituir materiales compartidos o crear variantes |
| Suelo de arena | Tron/ArenaFloor_Slot | Material independiente de edificios y carrocerías |
| Pantallas | GamingRoom/Monitor_Display_Slot | Shader de pantalla acordado por el equipo |
| Tronco y ramas | Nature/Wood_Slot | Shader de madera |
| Césped de suelo | Nature/Grass_Slot | Shader de suelo/pasto |
| Matas de hierba | Nature/GrassTuft_Palette | Shader de vegetación; conserva UV de atlas |
| Hojas principales | Nature/Leaves | Sustitución independiente de madera |

La tabla identifica superficies disponibles, **no decide cuáles serán los cinco shaders finales**. Sus compañeros deben confirmar que sus implementaciones no repitan las vistas en clase. Los shaders heredados de Nicrom y los Shader Graph de Zombunny se conservaron sin modificarlos y no cuentan como trabajo nuevo.

## Cómo continuar

1. Abrir las escenas guardadas y trabajar directamente sobre sus objetos/prefabs. No hace falta ejecutar el constructor.
2. Acordar con los compañeros los cinco shaders y asignarlos a los materiales/superficies preparados. Documentar ubicación y variables personalizables.
3. Colocar/posar los Zombunnys originales y vincular el visor y los pilotos. Ajustar la altura de `HandContact` al rig/pose definitiva.
4. Preparar Timeline y las cámaras. Timeline ya está instalado; Cinemachine no está en el manifest y no se instaló en esta etapa.
5. Añadir movimiento de motos, estelas dinámicas, choque, GAME OVER, retirada del visor, salida, caminata y contacto con el árbol. Actualmente pulsar Play solo muestra el escenario estático.
6. Configurar las escenas finales en Build Settings y las transiciones cuando se implemente su reproducción. Se preservó la entrada original TronScene; no se creó un ejecutable en esta etapa.

## Relación con la rúbrica

Se leyó `C:/Users/david/Downloads/ACG_Final_Project_v2.pdf`, en particular Opción 2 (página 3) y rúbrica/entregables (página 5). Requiere al menos cinco shaders no vistos, variables personalizables para cada uno, movimiento en Unity al pulsar Play y un MP4 de al menos un minuto. El reparto es 60% implementación y 40% documentación/presentación. También solicita informe, presentación, recursos Unity y ejecutable. La aprobación de la propuesta por el profesor es un requisito académico que el equipo debe gestionar; no se ha supuesto que ya exista.

Esta etapa prepara escenarios y superficies. No completa movimiento, evaluación de los cinco shaders, video, informe académico final, presentación ni ejecutable. Entrega indicada: jueves 26 de noviembre de 2026, 23:53; presentación y demo: viernes 27, a la hora de clase.

Referencia visual: https://www.youtube.com/watch?v=SqSuRdkglxM. La página fue accesible pero no se pudo visualizar/reproducir el video mediante las herramientas disponibles. Las decisiones visuales proceden de la descripción del usuario y su captura adjunta; no se atribuyen al video detalles no observados.

## Herramientas y preservación

`Assets/Scripts/Editor/ACGStageBuilder.cs` contiene el constructor explícito del Editor. No se ejecuta al iniciar Unity ni durante Play. El menú `ACG/Prepare three stages (new scenes only)` rechaza sobrescribir una escena/prefab existente. Para modificar un escenario ya construido, usar la jerarquía o editar sus prefabs, no regenerarlo.

Las copias iniciales están en `Docs/Backups/OriginalScenes`. `ZombunnyHashes.json` y `SettingsHashes.json` registran la integridad original. El Zombunny, sus materiales, texturas y shaders permanecen en su ubicación. No se movieron ni reemplazaron los assets de los compañeros.

Las capturas reales del Editor se encuentran en `Docs/Previews`; el resultado de reapertura y revisión de mallas/materiales/scripts/cámaras se registra en `Docs/StageValidation.txt`. Consultar también `AssetsUtilizados.md` para procedencia y licencias.
