# Guía completa — El Cuate en HTML, CSS y JavaScript

Esta es la versión definitiva: **9 pantallas**, cada una siguiendo el mismo
patrón que ya entendiste en `productos.html`. Si ese archivo te quedó claro,
este proyecto entero te va a quedar claro, porque todos usan la misma receta.

---

## 1. Por qué no te dejaba pasar del login (versión anterior)

En el zip de ayer te pedí que descargaras jQuery a mano, y probablemente esa
copia local nunca existió de verdad en tu carpeta — sólo dejé un aviso pidiendo
que la bajaras. Si además abriste el `index.html` con doble clic (en vez de un
servidor local), el navegador bloquea `localStorage` cuando la página se abre
como archivo (`file://`), y ahí todo se cae sin avisar si no tienes abierta la
consola (F12).

**En esta versión eso ya no debería pasar**, porque:
- el login en sí **no depende de jQuery** para la parte visual, sólo para el
  `$.ajax` cuando realmente conectes el backend;
- de todas formas, siempre abre el proyecto con **Live Server** de VS Code
  (clic derecho sobre `index.html` → *Open with Live Server*), nunca con
  doble clic. Es la causa más común de "no pasa nada y no hay error".

Si algo falla, **abre la consola del navegador (F12 → pestaña Console)**.
Ahí SIEMPRE aparece el motivo, aunque en la pantalla no se vea nada.

---

## 2. Estructura del proyecto

```
├── index.html              Login
├── inicio.html              Panel principal
├── mesas.html               Plano de mesas
├── cuenta.html               Detalle de una mesa (pedido + pago)
├── productos.html            Inventario (el que ya conoces)
├── usuarios.html             Usuarios (mismo patrón que productos)
├── historiales.html          Comprobantes pagados + compras
├── auditoria.html            Registro de actividades
├── reportes.html             Resumen de ventas
│
├── css/
│   └── estilos.css           Un solo archivo CSS para todo
│
└── js/
    ├── lib/
    │   └── jquery-3.7.1.min.js   ⚠ tienes que descargarlo (ver LEEME.txt)
    ├── core/
    │   ├── config.js          Las URLs de TODOS los Controllers
    │   └── sesion.js          Guardar/leer el token, proteger páginas
    └── modulos/
        ├── login.js
        ├── inicio.js
        ├── mesas.js
        ├── cuenta.js
        ├── productos.js
        ├── usuarios.js
        ├── historiales.js
        ├── auditoria.js
        └── reportes.js
```

**La regla es siempre la misma:** un `.html` por pantalla, con la tabla y el
formulario ya escritos, y un `.js` con el mismo nombre en `js/modulos/` que
hace los `$.ajax`. Nada de plantillas raras, nada de generar HTML desde cero:
sólo llenar huecos que ya existen en el HTML.

---

## 3. Los dos archivos que se repiten en todas las páginas

### `js/core/config.js`

Ahí están **todas** las URLs, una por Controller. Es el único archivo donde
se escribe una dirección del backend. Si necesitas cambiar el puerto, lo
cambias aquí y se actualiza en las nueve pantallas a la vez.

### `js/core/sesion.js`

Cuatro funciones cortas:

| Función | Qué hace |
|---|---|
| `guardarSesion(token)` | guarda el token al iniciar sesión |
| `obtenerToken()` | lo lee |
| `cerrarSesion()` | lo borra y vuelve al login |
| `protegerPagina()` | si no hay token, manda al login |

Por eso todas las páginas internas empiezan igual:

```js
$(document).ready(function () {
    protegerPagina();              // ¿hay sesión? si no, al login
    $("#botonSalir").click(cerrarSesion);
    // ...lo propio de esta pantalla
});
```

---

## 4. Cómo está armada cada pantalla

Todas siguen esta receta, sin excepción:

1. El HTML tiene la tabla vacía (`<tbody id="cuerpoTabla"></tbody>`) y, si
   aplica, un formulario con `id` en cada campo.
2. El JS hace `$(document).ready(...)` y ahí llama a la función que trae los
   datos (`listarProductos()`, `listarUsuarios()`, etc.).
3. Esa función hace `$.ajax` tipo `GET` y, en `success`, llama a
   `pintarTabla(...)`.
4. `pintarTabla(...)` recorre la lista con un `for` y arma las filas.
5. Si la pantalla tiene formulario, un botón llama a `guardarProducto()` /
   `guardarUsuario()` / etc., que hace `POST` (si es nuevo) o `PUT` (si se
   está editando).
6. Los botones "Editar" y "Eliminar" de cada fila llaman a funciones que
   hacen `GET` de uno solo o `DELETE`.

Es exactamente lo de `productos.js`, repetido con otros nombres.

---

## 5. Lo que hay que revisar antes de conectar de verdad

Cada archivo `.js` tiene comentarios con `⚠` marcando los puntos donde tuve
que **adivinar** el nombre de una propiedad porque no pude confirmar tu
backend real. Los más importantes:

- **`js/core/config.js`** — verifica el puerto y el nombre de cada
  Controller contra tu carpeta `Controllers/`.
- **`js/modulos/login.js`** — qué espera recibir el Autenticador
  (`email`/`password` u otros nombres) y qué devuelve (`token` o `Token`).
- **`js/modulos/mesas.js`** — el nombre del campo de estado (`estado`) y el
  texto exacto que usa para "ocupada" (`"Ocupada"`).
- **`js/modulos/cuenta.js`** — es la pantalla más incierta, porque junta
  Mesas + Pedidos + Comprobantes. Tiene un bloque de comentarios al
  principio explicando exactamente qué supuse y qué hay que confirmar.

Buscar `⚠` en cada archivo te lleva directo a esos puntos.

---

## 6. Prueba rápida sin esperar al backend

Cada archivo `.js` funciona igual esté o no el backend corriendo: si no
responde, sale el mensaje rojo de error (por ejemplo "No se pudieron cargar
los productos. Error 0"), y eso ya te confirma que el `$.ajax` está bien
armado, aunque el backend todavía no esté listo. El error 0 significa "no
hubo respuesta del servidor" — normal si el backend no está corriendo.

Para probar de verdad: levanta tu backend, abre la consola del navegador
(F12), y ve a la pestaña **Network**. Ahí ves cada petición que sale, a
dónde fue y qué respondió. Es la herramienta más útil que tienes para
depurar esto.

---

## 7. Orden sugerido para lo que queda de tiempo

Con la sustentación el 21 y estudio limitado el 19 y 20:

1. **Hoy (17):** conecta el login de verdad (`js/modulos/login.js`) y
   Productos (`js/modulos/productos.js`, ya lo entiendes). Es tu prioridad
   número uno: si el login no funciona, nada más importa.
2. **Mañana (18):** Usuarios y Mesas, que son el mismo patrón.
3. **19–20:** repasa en voz alta cómo explicarías `$.ajax` (está en la guía
   `GUIA-JAVASCRIPT.md` que te mandé ayer, sigue siendo válida palabra por
   palabra).
4. Si te sobra tiempo, Historiales y Reportes son GET puro, sin formularios:
   son las más rápidas de conectar.
5. Cuenta de mesa y Auditoría, si alcanza. Si no alcanza, se muestran con el
   mensaje de error visible y se explica que están pendientes de un ajuste
   de nombres — es honesto y defendible.

Tres o cuatro pantallas que puedas explicar de memoria valen más que nueve
que no puedas sustentar si te preguntan.
