# 🎯 REFERENCIA RÁPIDA - SISTEMA DE MENÚS

## 🚀 Lo Que Tienes

### 9 Scripts
```
✅ GameState.cs          → 9 estados sincronizados
✅ GameManager.cs        → Singleton + eventos
✅ SceneTransitionManager.cs → Fade transitions
✅ PauseHandler.cs       → ESC para pausar
✅ MainMenuController.cs → Menu principal
✅ SettingsMenuController.cs → Configuración
✅ PauseMenuUI.cs        → Pausa in-game
✅ WinMenuController.cs  → Pantalla victoria
✅ LoseMenuController.cs → Pantalla derrota
```

---

## 📊 Estados (9)

```
START
  ↓
MainMenu ←─────────────────┐
├─ PLAY ──→ [FADE] ──→ Ring ├─ RoundIntro
                      ├─ Fighting
                      │  ├─ ESC → Paused ──┐
                      │  │               ├─ Resume → Fighting
                      │  │               ├─ Settings
                      │  │               └─ Menu → [FADE] ──→ MainMenu
                      │  │
                      │  └─ MatchEnd ──┬─ Win  ──┬─ Next Round
                      │              │       └─ Menu → [FADE]
                      │              └─ Lose ──┬─ Retry
                      │                        └─ Menu → [FADE]
                      │
                      └─ RoundEnd → RoundIntro (loop)
```

---

## 🎬 Transiciones

```
Duración: 0.5s + 0.1s + 0.5s = 1.1s total

Escena 1     [FADE IN]    NEGRO    [CARGA]  [FADE OUT]   Escena 2
Normal   ────────→────── Negro ──────→── Cargando ────→── Normal
0.0s     0.0s-0.5s      0.5s   0.5s-0.6s              0.6s-1.1s
```

---

## 🎮 Cómo Funciona

```
Usuario presiona botón
        ↓
Callback Controller
        ↓
SceneTransitionManager.TransitionToScene()
   O
GameManager.ChangeState()
        ↓
OnStateChanged dispara
        ↓
Todos los UI actualizan
```

---

## 📱 UI Necesaria

### MainMenu.unity
```
Canvas
├─ MainPanel
│  ├─ PlayButton
│  ├─ SettingsButton
│  └─ QuitButton
│
└─ SettingsPanel (Hidden)
   ├─ VolumeSlider
   └─ BackButton
```

### Ring.unity (agregar)
```
Canvas
├─ PausePanel (Hidden)
│  ├─ ResumeButton
│  ├─ SettingsButton
│  └─ MainMenuButton
│
├─ SettingsPanel (Hidden)
│  ├─ VolumeSlider
│  └─ BackButton
│
├─ WinPanel (Hidden)
│  ├─ WinText
│  ├─ StatsText
│  ├─ NextRoundButton
│  └─ MainMenuButton
│
└─ LosePanel (Hidden)
   ├─ LoseText
   ├─ StatsText
   ├─ RetryButton
   └─ MainMenuButton
```

---

## 💻 Código Clave

### Cambiar escena con transición
```csharp
SceneTransitionManager.Instance.TransitionToScene("Ring");
```

### Cambiar estado
```csharp
GameManager.Instance.ChangeState(GameState.Paused);
```

### Escuchar cambios
```csharp
GameManager.Instance.OnStateChanged += OnStateChanged;

void OnStateChanged(GameState newState)
{
    if (newState == GameState.Paused)
        ShowPauseMenu();
}
```

### Personalizar transición
```csharp
SceneTransitionManager.Instance.SetFadeDuration(1f, 1f);
SceneTransitionManager.Instance.SetFadeColor(Color.red);
```

---

## ⏱️ Tiempo de Implementación

```
Crear MainMenu.unity:    5 min
Construir UI:           10 min
Asignar scripts:         5 min
Actualizar Ring:         5 min
Build Settings:          2 min
Testing:                 3 min

TOTAL: 30 minutos
```

---

## 🧪 Tests Esenciales

```
1. PLAY → Ring (con fade)
2. ESC pausa
3. RESUME continúa
4. Volume slider funciona
5. MAIN MENU → MainMenu (con fade)
6. Ganar muestra Win
7. Perder muestra Lose
8. NEXT ROUND/RETRY funcionan
```

---

## 🔧 Setup Botón

```
Canvas → UI → Button - TextMeshPro
├─ Nombre: MyButton
├─ Rect Transform:
│  ├─ Anchor: Center
│  ├─ Position: X=0, Y=0
│  └─ Size: 200x50
├─ Image: Color = RGB(51,102,255)
└─ Text:
   ├─ Text: "CLICK ME"
   ├─ FontSize: 40
   └─ Color: White
```

---

## 🎯 Setup Slider (Volumen)

```
Canvas → UI → Slider
├─ Nombre: VolumeSlider
├─ Rect Transform:
│  ├─ Size: 300x50
│  └─ Position: 0,0
├─ Slider:
│  ├─ Min Value: 0
│  ├─ Max Value: 1
│  └─ Value: 0.8
└─ Fill: Color = RGB(0,255,0)
```

---

## 🎨 Colores Recomendados

| Elemento | RGB |
|----------|-----|
| Botón Normal | (51, 102, 255) |
| Botón Hover | (76, 127, 255) |
| Botón Pressed | (25, 75, 200) |
| Background | (50, 50, 50) |
| Texto | (255, 255, 255) |
| Accent | (255, 200, 0) |

---

## 📋 Checklist Rápido

```
SCRIPTS:
☐ 9 scripts compilados
☐ Sin errores

SCENES:
☐ MainMenu.unity creado
☐ Ring.unity actualizado

UI MAINMENU:
☐ MainPanel + 3 botones
☐ SettingsPanel + Slider

UI RING:
☐ PausePanel + 3 botones
☐ SettingsPanel + Slider
☐ WinPanel + botones
☐ LosePanel + botones

SCRIPTS ASIGNADOS:
☐ MainMenuController
☐ SettingsMenuController (x2)
☐ PauseMenuUI
☐ WinMenuController
☐ LoseMenuController

BUILD:
☐ Build Settings configurados
☐ Scene 0: MainMenu
☐ Scene 1: Ring

TESTING:
☐ 8 tests completados
```

---

## 🚀 Comenzar Ahora

1. Lee **MENUS_QUICKSTART.md** (5 min)
2. Sigue **UI_STEP_BY_STEP.md** (30 min)
3. Test (5 min)

**Total: 40 minutos**

---

## 📞 Soporte Rápido

### "No compila"
→ Ver Console (Window → General → Console)

### "Botón no funciona"
→ Verificar que script está asignado

### "No aparece panel de pausa"
→ Verificar que PauseMenuUI está en Canvas

### "Transición muy rápida/lenta"
→ `SetFadeDuration(duracion, duracion)`

### "Escena no carga"
→ Build Settings → Verificar nombres

---

## 💡 Tips Pro

```
1. Duplicar botones para reutilizar
2. Usar Prefabs para UI repetido
3. Testear frecuentemente
4. Guardar después de cambios (Ctrl+S)
5. Debug en Console
```

---

## ✨ Resultado Final

```
╔═══════════════════════════╗
║   SISTEMA LISTO ✅       ║
║                           ║
║ • 9 scripts compilados    ║
║ • Transiciones suaves     ║
║ • Pausa funcional         ║
║ • Win/Lose screens       ║
║ • 30 min para UI          ║
║                           ║
║ "¡A crear UI!" 🚀       ║
╚═══════════════════════════╝
```

---

**Documentación rápida de referencia**

Para más detalles: ver otros archivos en Documentation/
