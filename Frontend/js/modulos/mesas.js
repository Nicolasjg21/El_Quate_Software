/* ==========================================================================
   MESAS
   --------------------------------------------------------------------------
   mesas(idMesa, numeroMesa:int, estado:string) — no hay "personas" ni "total":
   el consumo mostrado sale de la cuenta abierta y sus detalles de pedido.
   ========================================================================== */
(function (App) {
  "use strict";
  var ui = App.ui, cfg = App.config;
  var svc = App.api.entidad("mesas");

  var mesas = [], cuentas = [], pedidos = [], detallesPorCuenta = new Map(), usuarios = [], filtro = "todas";

  App.pagina(async function () {
    ui.html("estadoMesa", ui.opciones(
      [{ v: cfg.ESTADOS.MESA.LIBRE }, { v: cfg.ESTADOS.MESA.OCUPADA }], "v", "v", cfg.ESTADOS.MESA.LIBRE));

    document.getElementById("botonNuevaMesa").addEventListener("click", function () {
      ui.mostrar("formNuevaMesa", true);
      document.getElementById("numeroMesa").focus();
    });
    document.getElementById("botonCancelarMesa").addEventListener("click", function () { ui.mostrar("formNuevaMesa", false); });
    document.getElementById("botonRegistrar").addEventListener("click", registrar);
    document.getElementById("numeroMesa").addEventListener("keydown", function (e) { if (e.key === "Enter") registrar(); });

    ui.$$(".chip").forEach(function (chip) {
      chip.addEventListener("click", function () {
        ui.$$(".chip").forEach(function (c) { c.classList.remove("activo"); });
        chip.classList.add("activo");
        filtro = chip.dataset.filtro;
        pintar();
      });
    });

    ui.alHacerClic("rejillaMesas", "button[data-id]", function (b) {
      window.location.href = "cuenta.html?idMesa=" + encodeURIComponent(b.dataset.id);
    });
    ui.alHacerClic("rejillaMesas", "button[data-nueva]", function () { document.getElementById("botonNuevaMesa").click(); });

    await cargar();
  });

  async function cargar() {
    var d = await App.datos.cargar(["mesas", "cuentas", "pedidos", "detallePedidos", "usuarios"]);
    mesas = d.mesas.slice().sort(function (a, b) { return a.numeroMesa - b.numeroMesa; });
    cuentas = d.cuentas; pedidos = d.pedidos; usuarios = d.usuarios;
    detallesPorCuenta = App.datos.detallesPorCuenta(d.pedidos, d.detallePedidos);
    pintar();
  }

  function meseroDe(idCuenta) {
    var suyos = pedidos.filter(function (p) { return p.idCuenta === idCuenta; }).sort(function (a, b) { return a.idPedido - b.idPedido; });
    if (!suyos.length) return "";
    var u = usuarios.filter(function (x) { return x.idUsuario === suyos[0].idUsuario; })[0];
    return u ? String(u.nombres).split(" ")[0] : "";
  }

  function pintar() {
    var libres = mesas.filter(function (m) { return cfg.igual(m.estado, cfg.ESTADOS.MESA.LIBRE); }).length;
    var ocupadas = mesas.filter(function (m) { return cfg.igual(m.estado, cfg.ESTADOS.MESA.OCUPADA); }).length;
    var otras = mesas.length - libres - ocupadas;

    ui.html("resumenMesas",
      caja("roja", "🔴 Ocupadas", ocupadas, mesas.length) +
      caja("verde", "🟢 Libres", libres, mesas.length) +
      (otras ? caja("amarilla", "🟡 Otro estado", otras, mesas.length) : ""));

    if (!mesas.length) {
      ui.html("rejillaMesas", "<div style='grid-column:1/-1'>" + ui.vacio("🍽️", "Todavía no hay mesas registradas",
        "Registra las mesas de tu salón para poder abrir cuentas y tomar pedidos.",
        "<button data-nueva='1'>+ Registrar la primera mesa</button>") + "</div>");
      return;
    }

    var visibles = mesas.filter(function (m) {
      if (filtro === "todas") return true;
      return cfg.igual(m.estado, filtro === "libre" ? cfg.ESTADOS.MESA.LIBRE : cfg.ESTADOS.MESA.OCUPADA);
    });

    var html = visibles.map(function (m) {
      var cuenta = App.datos.cuentaAbierta(cuentas, m.idMesa);
      var esLibre = cfg.igual(m.estado, cfg.ESTADOS.MESA.LIBRE);
      var esOcupada = cfg.igual(m.estado, cfg.ESTADOS.MESA.OCUPADA);
      var clase = esOcupada ? "ocupada" : (esLibre ? "libre" : "otra");
      var punto = esOcupada ? "rojo" : (esLibre ? "verde" : "amarillo");

      var cuerpo;
      if (cuenta) {
        var lineas = detallesPorCuenta.get(cuenta.idCuenta) || [];
        var items = lineas.reduce(function (s, x) { return s + Number(x.cantidad); }, 0);
        var mesero = meseroDe(cuenta.idCuenta);
        cuerpo =
          "<div class='detalle'>🕐 " + ui.esc(ui.hace(cuenta.fechaApertura)) + "</div>" +
          "<div class='detalle'>🧾 " + items + (items === 1 ? " producto" : " productos") + (mesero ? " · " + ui.esc(mesero) : "") + "</div>" +
          "<div class='monto'>" + ui.moneda(App.datos.sumar(lineas)) + "<small>Consumo actual</small></div>";
      } else if (esLibre) {
        cuerpo = "<div class='detalle'>Disponible</div><div class='llamada'>Abrir cuenta</div>";
      } else {
        cuerpo = "<div class='detalle'>" + ui.esc(m.estado || "Sin estado") + "</div><div class='llamada'>Ver mesa</div>";
      }

      return "<button class='mesa-tarjeta " + clase + "' data-id='" + m.idMesa + "' title='Mesa " + ui.esc(m.numeroMesa) + " — " + ui.esc(m.estado) + "'>" +
        "<div class='cabecera'><span class='numero'>Mesa " + ui.esc(m.numeroMesa) + "</span><span class='punto " + punto + "'></span></div>" +
        cuerpo + "</button>";
    }).join("");

    ui.html("rejillaMesas", html || "<div style='grid-column:1/-1'>" + ui.vacio("🔍", "Ninguna mesa con ese estado", "Prueba con otro filtro.") + "</div>");
  }

  function caja(color, etiqueta, valor, total) {
    return "<div class='estado-caja " + color + "'><div class='etiqueta'>" + ui.esc(etiqueta) +
      "</div><div class='valor'>" + valor + " <small>/ " + total + "</small></div></div>";
  }

  async function registrar() {
    var r = ui.validar([{ id: "numeroMesa", etiqueta: "El número de mesa", requerido: true, tipo: "entero", min: 1 }]);
    if (!r.ok) { ui.reportarValidacion(r); return; }

    if (mesas.some(function (m) { return Number(m.numeroMesa) === r.valores.numeroMesa; })) {
      ui.aviso("Ya existe una mesa con el número " + r.valores.numeroMesa + ".", "error");
      return;
    }

    await ui.conBoton(document.getElementById("botonRegistrar"), async function () {
      var cuerpo = { numeroMesa: r.valores.numeroMesa, estado: ui.valor("estadoMesa") };
      try {
        await svc.crear(cuerpo);
        await App.api.auditar("mesas", "INSERT", null, cuerpo);
        ui.aviso("Mesa " + cuerpo.numeroMesa + " registrada correctamente.", "exito");
        ui.poner("numeroMesa", "");
        ui.mostrar("formNuevaMesa", false);
        await cargar();
      } catch (e) {
        if (e.status === 404 || e.status === 405) {
          ui.aviso("El backend no expone POST para mesas: MesasController no tiene ningún [HttpPost] " +
                   "(aunque MesasRepository.PostMesas sí existe). Hay que agregar el endpoint PostMesa; " +
                   "el parche está en CONECTAR-BACKEND.md (P3).", "error");
        } else {
          ui.aviso(App.api.explicar(e), "error");
        }
      }
    });
  }
})(window.App = window.App || {});
