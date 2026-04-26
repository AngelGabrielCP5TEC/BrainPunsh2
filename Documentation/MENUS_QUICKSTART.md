# ⚡ MENUS QUICKSTART - 10 MINUTOS

## 📦 Se Creó Todo - Solo Necesitas UI en Unity

```
✅ 9 Scripts compilados
✅ Sistema de estados listo
✅ Transiciones listas
✅ Pausa lista
✅ Win/Lose screens listos

Solo falta: Crear la UI en Unity
```

---

## 🚀 10 Pasos Rápidos (10 minutos)

### 1. Crear Escena MainMenu (1 min)
```
File → New Scene
Ctrl+S → Guardar como "MainMenu"
```

### 2. Canvas (30 seg)
```
Right-click Jerarquía → UI → Canvas
Canvas Scaler: Scale With Screen Size (1920x1080)
```

### 3. MainPanel (1 min)
```
Right-click Canvas → UI → Panel
Renombra: MainPanel
Color: Gris oscuro RGB(50,50,50)
Stretch (Shift+Alt+C)
```

### 4. Botones (2 min)
```
Right-click MainPanel → UI → Button
Renombra: PlayButton
Texto: "PLAY"

Duplica (Ctrl+D) para SettingsButton y QuitButton
Posiciones:
  - PlayButton: Y = 0
  - SettingsButton: Y = -80
  - QuitButton: Y = -160
```

### 5. SettingsPanel (1 min)
```
Right-click Canvas → UI → Panel
Renombra: SettingsPanel
Color: Gris oscuro
Stretch
Inicialmente Hidden (unchecked)

Adentro agregar:
  - VolumeSlider
  - BackButton
```

### 6. Asignar Script Main (30 seg)
```
Select Canvas
Add Component → MainMenuController

Asignar:
  - Play Button: PlayButton
  - Settings Button: SettingsButton
  - Quit Button: QuitButton
  - Main Panel: MainPanel
  - Settings Panel: SettingsPanel
```

### 7. Asignar Script Settings (30 seg)
```
Select SettingsPanel
Add Component → SettingsMenuController

Asignar:
  - Volume Slider: VolumeSlider
  - Back Button: BackButton
  - Settings Panel: SettingsPanel
  - Parent Panel: MainPanel
```

### 8. Build Settings (1 min)
```
File → Build Settings
Scene 0: MainMenu.unity
Scene 1: Ring.unity
```

### 9. UI en Ring - Pausa (2 min)
```
En tu Canvas de Ring agregar:

PausePanel (Image - Hidden)
SettingsPanel (Image - Hidden)
WinPanel (Image - Hidden)
LosePanel (Image - Hidden)

Con botones correspondientes
```

### 10. Asignar Scripts en Ring (1 min)
```
Canvas → Add Component → PauseMenuUI
Canvas → Add Component → WinMenuController
Canvas → Add Component → LoseMenuController

Asignar todas las referencias
```

---

## ✅ Verificar

```
▶️ Play en MainMenu
✓ ¿Ves botones?       → Funciona
✓ PLAY → Ring?        → ¡Transición!
✓ ESC → Pausa?        → ¡Pausa!
✓ Ganar/Perder?       → ¡Screens!

= LISTO ✨
```

---

## 📊 Timeline

```
0-1 min   → Escena MainMenu
1-2 min   → Canvas
2-3 min   → MainPanel
3-5 min   → Botones
5-6 min   → SettingsPanel
6-7 min   → Scripts principales
7-8 min   → Build Settings
8-10 min  → UI Ring + Scripts

TOTAL: 10 minutos
```

---

## 💡 Colores Recomendados

```
Botones Normal:  Azul RGB(51,102,255)
Botones Hover:   Azul RGB(76,127,255)
Background:      Gris RGB(50,50,50)
Texto:           Blanco RGB(255,255,255)
```

---

## 🎯 Resultado

```
Main Menu → PLAY → Ring (fade)
        ↓
During Game: ESC → Pause
        ↓
Ganar: Win Screen
Perder: Lose Screen
```

---

## ⏱️ TOTAL: 10 MINUTOS

¿Listo? **¡A crear UI!** 🚀

Ver detalles en: **COMPLETE_MENUS_GUIDE.md**
