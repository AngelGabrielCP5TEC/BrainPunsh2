# 🎨 INSTRUCCIONES PASO A PASO - CONSTRUIR UI EN UNITY

## 📋 Índice

1. Crear Escena MainMenu
2. Construir Canvas y Panels
3. Crear Botones
4. Asignar Scripts
5. Construir UI en Ring
6. Build Settings
7. Testing

---

## ✅ PASO 1: Crear Escena MainMenu (2 minutos)

### 1.1 Nueva Escena
```
File → New Scene
```

### 1.2 Guardar
```
Ctrl+S
Carpeta: Assets/Scenes (crear si no existe)
Nombre: MainMenu
```

### 1.3 Resultado
```
✓ Tienes una escena vacía llamada MainMenu.unity
```

---

## ✅ PASO 2: Construir Canvas (2 minutos)

### 2.1 Crear Canvas
```
Right-click en Jerarquía
→ UI → Canvas
```

### 2.2 Configurar Canvas Scaler
```
Select Canvas
Inspector → Canvas Scaler
├─ Render Mode: Screen Space - Overlay
├─ UI Scale Mode: Scale With Screen Size
├─ Reference Resolution: 1920 x 1080
└─ Screen Match Mode: Match Width Or Height
```

### 2.3 Agregar GraphicRaycaster
```
Select Canvas
Add Component → Graphic Raycaster
```

### 2.4 Resultado
```
✓ Canvas configurado
✓ Se ajustará a cualquier resolución
```

---

## ✅ PASO 3: Crear MainPanel (3 minutos)

### 3.1 Crear Panel
```
Right-click Canvas en Jerarquía
→ UI → Panel
Renombra: MainPanel
```

### 3.2 Configurar Layout
```
Select MainPanel
Inspector → Rect Transform
├─ Anchor Presets: Shift+Alt+C (Stretch)
├─ Left: 0, Right: 0
├─ Top: 0, Bottom: 0
└─ Width & Height: 0, 0
```

### 3.3 Cambiar Color (Opcional)
```
Select MainPanel
Inspector → Image
├─ Color: RGB(50, 50, 50) o similar
├─ Transparency: 255 (completamente visible)
└─ Alpha: 1
```

### 3.4 Resultado
```
✓ MainPanel cubre toda la pantalla
✓ Color de fondo oscuro
```

---

## ✅ PASO 4: Crear Botones (5 minutos)

### 4.1 Primer Botón
```
Right-click MainPanel
→ UI → Button - TextMeshPro
Renombra: PlayButton
```

### 4.2 Configurar PlayButton
```
Select PlayButton en Jerarquía
Inspector → Rect Transform
├─ Anchor Presets: Top Center (Shift+Alt+T)
├─ Pivot: (0.5, 1)
├─ Anchored Position: X=0, Y=-100
├─ Width: 200, Height: 50
```

### 4.3 Texto del PlayButton
```
Select PlayButton → Text (TMP) hijo
Inspector → TextMeshProUGUI
├─ Text: "PLAY"
├─ Font Size: 40
├─ Alignment: Center
└─ Color: Blanco RGB(255,255,255)
```

### 4.4 Color del Botón
```
Select PlayButton
Inspector → Image
├─ Color: Azul RGB(51, 102, 255)
└─ Alpha: 1
```

### 4.5 Duplicar para SettingsButton
```
Right-click PlayButton
→ Duplicate (o Ctrl+D)
Renombra: SettingsButton

Select SettingsButton
Inspector → Rect Transform
├─ Anchored Position: X=0, Y=-160
└─ Text: "SETTINGS"
```

### 4.6 Duplicar para QuitButton
```
Right-click PlayButton
→ Duplicate
Renombra: QuitButton

Select QuitButton
Inspector → Rect Transform
├─ Anchored Position: X=0, Y=-220
└─ Text: "QUIT"
```

### 4.7 Resultado
```
✓ 3 botones verticales
✓ Centrados en pantalla
✓ Con textos claros
```

---

## ✅ PASO 5: Crear SettingsPanel (3 minutos)

### 5.1 Crear Panel
```
Right-click Canvas
→ UI → Panel
Renombra: SettingsPanel
```

### 5.2 Configurar Layout (igual a MainPanel)
```
Select SettingsPanel
Inspector → Rect Transform
├─ Anchor Presets: Stretch (Shift+Alt+C)
├─ Left: 0, Right: 0, Top: 0, Bottom: 0
└─ Width & Height: 0, 0
```

### 5.3 Color igual a MainPanel
```
Inspector → Image
├─ Color: RGB(50, 50, 50)
└─ Alpha: 1
```

### 5.4 Crear Título
```
Right-click SettingsPanel
→ UI → Text - TextMeshPro
Renombra: SettingsTitle

Inspector:
├─ Text: "SETTINGS"
├─ Font Size: 48
├─ Alignment: Center
├─ Color: Blanco

Rect Transform:
├─ Anchor: Top Center
├─ Anchored Position: Y=-50
├─ Width: 400, Height: 60
```

### 5.5 Crear Volume Slider
```
Right-click SettingsPanel
→ UI → Slider - TextMeshPro
Renombra: VolumeSlider

Select VolumeSlider
Inspector → Rect Transform
├─ Anchor: Center
├─ Anchored Position: Y=0, X=0
├─ Width: 300, Height: 50

Inspector → Slider
├─ Min Value: 0
├─ Max Value: 1
├─ Whole Numbers: OFF
├─ Interactable: ON
```

### 5.6 Crear BackButton
```
Right-click SettingsPanel
→ UI → Button - TextMeshPro
Renombra: BackButton

Inspector → Rect Transform
├─ Anchor: Bottom Center
├─ Anchored Position: Y=50, X=0
├─ Width: 150, Height: 50

Text:
├─ Text: "BACK"
├─ Font Size: 32
```

### 5.7 Ocultar SettingsPanel Inicialmente
```
Select SettingsPanel
Inspector → Unchecka "Active"
(Lo hace invisible inicialmente)
```

### 5.8 Resultado
```
✓ SettingsPanel con Slider y BackButton
✓ Inicialmente oculto
```

---

## ✅ PASO 6: Asignar Script MainMenuController (2 minutos)

### 6.1 Agregar Script
```
Select Canvas
Inspector → Add Component
Busca: MainMenuController
```

### 6.2 Asignar Referencias
```
En el Inspector, bajo MainMenuController:

Buttons:
├─ Play Button: Arrastra PlayButton aquí
├─ Settings Button: Arrastra SettingsButton aquí
└─ Quit Button: Arrastra QuitButton aquí

Panels:
├─ Main Panel: Arrastra MainPanel aquí
└─ Settings Panel: Arrastra SettingsPanel aquí
```

### 6.3 Resultado
```
✓ MainMenuController vinculado
✓ Todos los botones responden
```

---

## ✅ PASO 7: Asignar Script SettingsMenuController (1 minuto)

### 7.1 Agregar Script
```
Select SettingsPanel
Inspector → Add Component
Busca: SettingsMenuController
```

### 7.2 Asignar Referencias
```
Settings UI:
├─ Volume Slider: Arrastra VolumeSlider aquí
├─ Volume Text: (Dejar vacío por ahora)
├─ Back Button: Arrastra BackButton aquí
├─ Settings Panel: Arrastra SettingsPanel aquí
└─ Parent Panel: Arrastra MainPanel aquí
```

### 7.3 Resultado
```
✓ SettingsMenuController vinculado
✓ Slider funciona
```

---

## ✅ PASO 8: Build Settings (2 minutos)

### 8.1 Abrir Build Settings
```
File → Build Settings
```

### 8.2 Agregar Escenas
```
Scene 0: Arrastra MainMenu.unity aquí
Scene 1: Arrastra Ring.unity aquí

(Ring debe existir en Assets/Scenes/)
```

### 8.3 Resultado
```
✓ Build Settings configurados
✓ MainMenu es escena inicial
```

---

## ✅ PASO 9: Actualizar Ring - Agregar UI Pausa (5 minutos)

### 9.1 Abrir Ring.unity
```
En Project → Scenes → Ring.unity
Double-click para abrir
```

### 9.2 Encontrar Canvas
```
Jerarquía → Busca "Canvas"
(Debería existir del FightSceneBuilder)
```

### 9.3 Crear PausePanel
```
Right-click Canvas
→ UI → Panel
Renombra: PausePanel

Configurar (igual que SettingsPanel):
├─ Stretch (Shift+Alt+C)
├─ Color: RGB(0, 0, 0) con Alpha: 0.8 (semi-transparente)
```

### 9.4 Agregar Botones a PausePanel
```
Crear 3 botones igual que en MainMenu:
├─ ResumeButton → "RESUME"
├─ SettingsButton → "SETTINGS"
└─ MainMenuButton → "MAIN MENU"

Posiciones:
├─ ResumeButton: Y=-50
├─ SettingsButton: Y=-110
└─ MainMenuButton: Y=-170
```

### 9.5 Crear WinPanel
```
Right-click Canvas
→ UI → Panel
Renombra: WinPanel

Configurar:
├─ Stretch
├─ Color: RGB(0, 0, 0) + Alpha: 0.8

Agregar:
├─ WinText: "¡VICTORIA!" (FontSize: 64)
├─ StatsText: (FontSize: 32)
├─ NextRoundButton: "NEXT ROUND"
└─ MainMenuButton: "MAIN MENU"
```

### 9.6 Crear LosePanel
```
Right-click Canvas
→ UI → Panel
Renombra: LosePanel

Configurar igual a WinPanel

Agregar:
├─ LoseText: "¡DERROTA!" (FontSize: 64)
├─ StatsText: (FontSize: 32)
├─ RetryButton: "RETRY"
└─ MainMenuButton: "MAIN MENU"
```

### 9.7 Ocultar Todos los Panels
```
Select PausePanel → Unchecka Active
Select WinPanel → Unchecka Active
Select LosePanel → Unchecka Active

(Se mostrarán automáticamente cuando se necesiten)
```

### 9.8 Resultado
```
✓ 3 panels para Pausa, Win, Lose
✓ Todos ocultos inicialmente
```

---

## ✅ PASO 10: Asignar Scripts en Ring (3 minutos)

### 10.1 Agregar PauseMenuUI
```
Select Canvas
Inspector → Add Component
Busca: PauseMenuUI

Asignar:
├─ Pause Panel: PausePanel
├─ Resume Button: ResumeButton
├─ Settings Button: SettingsButton
├─ Main Menu Button: MainMenuButton
├─ Settings Panel: (crear si no existe)
└─ Settings Back Button: (si existe)
```

### 10.2 Agregar WinMenuController
```
Select Canvas
Inspector → Add Component
Busca: WinMenuController

Asignar:
├─ Win Panel: WinPanel
├─ Win Text: WinText
├─ Stats Text: StatsText
├─ Next Round Button: NextRoundButton
└─ Main Menu Button: MainMenuButton (del Win)
```

### 10.3 Agregar LoseMenuController
```
Select Canvas
Inspector → Add Component
Busca: LoseMenuController

Asignar:
├─ Lose Panel: LosePanel
├─ Lose Text: LoseText
├─ Stats Text: StatsText
├─ Retry Button: RetryButton
└─ Main Menu Button: MainMenuButton (del Lose)
```

### 10.4 Resultado
```
✓ Todos los scripts asignados
✓ Todas las referencias vinculadas
```

---

## ✅ PASO 11: Testing Final (5 minutos)

### 11.1 Play en MainMenu
```
Select MainMenu scene en Project
Click Play
Verifica:
├─ ¿Ves los 3 botones?
├─ ¿Puedes hacer hover?
└─ ✓ Si ves todo → Funciona
```

### 11.2 Click PLAY
```
Click PLAY button
Verifica:
├─ ¿Transición con fade?
├─ ¿Se carga Ring?
└─ ✓ Si funciona → Listo
```

### 11.3 Presiona ESC
```
Durante juego, presiona ESC
Verifica:
├─ ¿Aparece panel de pausa?
├─ ¿El juego se detiene?
└─ ✓ Si funciona → Listo
```

### 11.4 Click RESUME
```
Click RESUME
Verifica:
├─ ¿Panel desaparece?
├─ ¿Juego continúa?
└─ ✓ Si funciona → Listo
```

### 11.5 Ciclo Completo
```
Juega hasta ganar/perder
Verifica:
├─ ¿Aparece Win o Lose screen?
├─ ¿Botones funcionan?
└─ ✓ Si funciona → LISTO ✅
```

---

## 📊 Checklist de Implementación

```
MainMenu.unity:
☐ Canvas creado
☐ MainPanel creado
☐ PlayButton creado
☐ SettingsButton creado
☐ QuitButton creado
☐ SettingsPanel creado
☐ VolumeSlider creado
☐ BackButton (Settings) creado
☐ MainMenuController asignado
☐ SettingsMenuController asignado

Ring.unity:
☐ PausePanel creado
☐ ResumeButton creado
☐ SettingsButton (Pausa) creado
☐ MainMenuButton (Pausa) creado
☐ WinPanel creado
☐ NextRoundButton creado
☐ LosePanel creado
☐ RetryButton creado
☐ PauseMenuUI asignado
☐ WinMenuController asignado
☐ LoseMenuController asignado

Build Settings:
☐ MainMenu en Scene 0
☐ Ring en Scene 1

Testing:
☐ MainMenu funciona
☐ Play → Ring funciona
☐ ESC pausa funciona
☐ Resume funciona
☐ Settings funciona
☐ Win/Lose funcionan
☐ Ciclo completo funciona

TOTAL: 33 items
```

---

## ⏱️ Tiempo Total

```
Step 1:  2 min  - Escena MainMenu
Step 2:  2 min  - Canvas
Step 3:  3 min  - MainPanel
Step 4:  5 min  - Botones
Step 5:  3 min  - SettingsPanel
Step 6:  2 min  - MainMenuController
Step 7:  1 min  - SettingsMenuController
Step 8:  2 min  - Build Settings
Step 9:  5 min  - UI en Ring
Step 10: 3 min  - Scripts en Ring
Step 11: 5 min  - Testing

TOTAL: 33 minutos
```

---

## 🎨 Colores Recomendados

```
Botones Normal:    RGB(51, 102, 255)   Azul
Botones Hover:     RGB(76, 127, 255)   Azul claro
Botones Pressed:   RGB(25, 75, 200)    Azul oscuro
Background:        RGB(50, 50, 50)     Gris oscuro
Texto Normal:      RGB(255, 255, 255)  Blanco
Texto Hover:       RGB(200, 200, 200)  Gris claro
```

---

## 💡 Tips

```
1. Guardar escenas frecuentemente (Ctrl+S)
2. Usar Prefabs para botones (reutilizar)
3. Tested incrementalmente (no esperar al final)
4. Ver Console para errores (Window → General → Console)
5. Usar Play button para testear en tiempo real
```

---

## 🚀 Resultado Final

```
╔════════════════════════════════════════╗
║                                        ║
║  ✅ MAINMENU COMPLETAMENTE FUNCIONAL ║
║  ✅ PAUSA TOTALMENTE OPERATIVA       ║
║  ✅ WIN/LOSE SCREENS LISTOS          ║
║  ✅ TRANSICIONES SUAVES              ║
║  ✅ SISTEMA 100% INTEGRADO           ║
║                                        ║
║  "¡Tu juego está listo!" 🎮         ║
║                                        ║
╚════════════════════════════════════════╝
```

---

**Tiempo total: 33 minutos**

¡A disfrutar tu juego completamente funcional! 🚀✨
