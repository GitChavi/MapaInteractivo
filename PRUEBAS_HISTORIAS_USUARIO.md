# Historias de usuario implementadas y guía de prueba

Esta guía corresponde a la rama `dev` y a la escena `Assets/Scenes/MapaInteractivo.unity`.

## Historias incluidas

| Historia | Funcionalidad |
| --- | --- |
| 1.1.1 | Explorar el mapa con zoom y desplazamiento táctil. En computador se puede arrastrar con el botón izquierdo y acercar/alejar con la rueda del mouse. |
| 1.4.1 | Volver al mapa principal desde un punto interactivo mediante el botón visible **Volver al mapa**. |
| 2.3.1 | Pausar/reanudar o repetir la narración del punto abierto. Los controles se habilitan cuando hay un clip de narración asignado. |
| 3.1.1 | Silenciar/reactivar el ambiente del punto abierto con un botón visible. Si el punto no tiene audio ambiental, el control genera un ambiente de prueba. |

Al abrir un punto, su cuadro puede cerrarse con el botón de cierre que aparece en el popup.

## Distribución de personajes y ambientes

| Región | Personaje | Audio ambiental asignado |
| --- | --- | --- |
| Caribe | El Mohán | Ambiente genérico de prueba si no se asigna un clip propio. |
| Andina | Bachué | `Sonido ambiente Bachue.wav` |
| Pacífica | La Tunda | Ambiente genérico de prueba si no se asigna un clip propio. |
| Orinoquía | El Silbón | `Sonido el silbon - solo ambiente.wav` |
| Amazonía | La Madre Monte | Ambiente genérico de prueba si no se asigna un clip propio. |

La ficha de Bachué usa la misma composición de popup (retrato a la izquierda y datos a la derecha); el recuadro de retrato queda como espacio reservado porque aún no hay una imagen de Bachué en los recursos del proyecto.

## Preparación

1. Cambia a la rama `dev` y abre el proyecto en Unity.
2. Abre `Assets/Scenes/MapaInteractivo.unity`.
3. Espera a que Unity termine de importar recursos y compilar scripts. Revisa la Console por errores.
4. Pulsa **Play**.

## Pasos para probar

### 1.1.1 — Zoom y desplazamiento

- En computador, arrastra el mapa con el botón izquierdo del mouse.
- Usa la rueda para acercar y alejar. El movimiento debe ser gradual y el zoom debe mantenerse dentro de sus límites.
- En un dispositivo táctil, arrastra con un dedo y pellizca con dos dedos para cambiar el zoom.

### 1.4.1 — Volver al mapa

- Pulsa una zona interactiva para abrir un punto.
- Pulsa **Volver al mapa**, en la parte superior izquierda.
- El popup se cierra y el mapa vuelve a estar visible.

### 2.3.1 — Pausar y repetir narración

- En el objeto del popup del personaje (`Tunda`, `Mohan`, `MadreMonte` o `Silbon`), crea un objeto hijo llamado exactamente `NarrationAudioSource`.
- Añade un componente `AudioSource` a ese hijo y asigna en **Audio Clip** el archivo de la narración. Al abrir el punto, el clip comienza automáticamente.
- Pulsa **Pausar** y comprueba que el audio se detiene; pulsa **Reanudar** para continuar.
- Pulsa **Repetir** para volver al inicio del clip.
- Si los controles indican **Sin narración** y están deshabilitados, comprueba que el hijo se llame exactamente `NarrationAudioSource` y que su `AudioSource` tenga el clip asignado.

### 3.1.1 — Silenciar ambiente

- Abre un punto interactivo. El ambiente comienza al abrirlo.
- Pulsa **Silenciar ambiente** y confirma que deja de oírse.
- Pulsa **Activar ambiente** para escucharlo de nuevo.
- Si no hay un clip de ambiente asignado al punto, se usa un sonido sintético de prueba.
- El audio ambiental usa el `AudioSource` del objeto raíz del popup. Mantén la narración en el hijo `NarrationAudioSource` para que los botones controlen cada audio por separado.

### Cerrar popup

- Abre un punto y pulsa el botón de cierre del cuadro. El popup debe desaparecer para poder seguir explorando el mapa.

## Notas

- Las entradas táctiles requieren ejecutar la escena en un dispositivo con pantalla táctil; en el Editor se pueden probar el arrastre y el zoom con mouse.
- Esta guía describe las pruebas manuales en Unity. No implica que se haya ejecutado una sesión de Play Mode o una prueba en dispositivo.
