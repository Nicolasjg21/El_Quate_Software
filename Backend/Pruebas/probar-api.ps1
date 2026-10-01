<#
 Prueba rapida de la API (no crea ni borra datos).
 Uso (desde la carpeta Backend, con la API ya en ejecucion):
   powershell -ExecutionPolicy Bypass -File .\Pruebas\probar-api.ps1
 Las acciones restringidas se prueban con cuerpos VACIOS a proposito:
   - si el rol NO tiene permiso -> 403
   - si el rol SI tiene permiso -> 400 (datos invalidos, no se guarda nada)
#>

$base = Read-Host "URL base de la API (ej. https://localhost:7000)"
$base = $base.TrimEnd('/')

function Leer-Clave($texto) {
    $s = Read-Host $texto -AsSecureString
    $b = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($s)
    try { return [Runtime.InteropServices.Marshal]::PtrToStringAuto($b) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($b) }
}

$usuarios = @{}
foreach ($rol in 'Administrador', 'Cajero', 'Mesero') {
    $usuarios[$rol] = @{
        email = Read-Host "Correo del $rol"
        clave = Leer-Clave "Contrasena del $rol"
    }
}

if ($PSVersionTable.PSVersion.Major -ge 7) { $PSDefaultParameterValues['Invoke-WebRequest:SkipCertificateCheck'] = $true }

function Llamar($metodo, $ruta, $token, $cuerpo) {
    $h = @{}
    if ($token) { $h['Authorization'] = "Bearer $token" }
    $p = @{ Uri = "$base$ruta"; Method = $metodo; Headers = $h; UseBasicParsing = $true; TimeoutSec = 60 }
    if ($null -ne $cuerpo) { $p['Body'] = $cuerpo; $p['ContentType'] = 'application/json' }
    try {
        $r = Invoke-WebRequest @p
        return @{ codigo = [int]$r.StatusCode; cuerpo = $r.Content }
    } catch {
        $codigo = 0; $texto = $_.Exception.Message
        if ($_.Exception.Response) {
            $codigo = [int]$_.Exception.Response.StatusCode
            try {
                $sr = New-Object IO.StreamReader($_.Exception.Response.GetResponseStream())
                $texto = $sr.ReadToEnd()
            } catch { }
        }
        return @{ codigo = $codigo; cuerpo = $texto }
    }
}

function Entrar($rol) {
    $u = $usuarios[$rol]
    $c = @{ email = $u.email; password = $u.clave } | ConvertTo-Json
    $r = Llamar 'POST' '/api/Autenticador/Login' $null $c
    if ($r.codigo -eq 200) { return ($r.cuerpo | ConvertFrom-Json).token }
    Write-Host "  No se pudo iniciar sesion como $rol (codigo $($r.codigo))" -ForegroundColor Red
    return $null
}

$resultados = New-Object System.Collections.ArrayList
function Verificar($nombre, $obtenido, $esperado) {
    $ok = $esperado -contains $obtenido
    [void]$resultados.Add([pscustomobject]@{
        Prueba = $nombre; Obtenido = $obtenido; Esperado = ($esperado -join '/'); Resultado = $(if ($ok) { 'OK' } else { 'FALLA' }) })
    $color = if ($ok) { 'Green' } else { 'Red' }
    Write-Host ("[{0}] {1} -> {2} (esperado {3})" -f $(if ($ok) { 'OK ' } else { 'ERR' }), $nombre, $obtenido, ($esperado -join '/')) -ForegroundColor $color
}

Write-Host "`n== 0. Servidor ==" -ForegroundColor Cyan
Verificar 'GET /health' (Llamar 'GET' '/health' $null $null).codigo @(200)

Write-Host "`n== 1. Inicio de sesion ==" -ForegroundColor Cyan
$tokens = @{}
foreach ($rol in 'Administrador', 'Cajero', 'Mesero') {
    $tokens[$rol] = Entrar $rol
    Verificar "Login $rol" $(if ($tokens[$rol]) { 200 } else { 401 }) @(200)
}
$malo = @{ email = $usuarios['Administrador'].email; password = 'clave-incorrecta-xyz' } | ConvertTo-Json
Verificar 'Login con clave incorrecta' (Llamar 'POST' '/api/Autenticador/Login' $null $malo).codigo @(401)
Verificar 'GET sin token' (Llamar 'GET' '/api/Categorias/GetCategorias' $null $null).codigo @(401)

Write-Host "`n== 2. Consultas (GET) con administrador ==" -ForegroundColor Cyan
$gets = 'Auditorias/GetAuditorias','Categorias/GetCategorias','Compras/GetCompras','Comprobantes/GetComprobantes',
  'Cuentas/GetCuentas','DetalleCompras/GetDetalleCompras','DetallePedidos/GetDetallePedidos','Kardex/GetKardex',
  'Mesas/GetMesas','MetodosPago/GetMetodosPago','Pedidos/GetPedidos','Permisos/GetPermisos','Productos/GetProductos',
  'Proveedores/GetProveedores','Roles/GetRoles','RolesPermisos/GetRolesPermisos','TipoDocumento/GetTipoDocumento',
  'Usuarios/GetUsuarios','Analiticas/Ventas'
foreach ($g in $gets) {
    # 404 es valido: varios endpoints responden 404 cuando la lista esta vacia
    Verificar "GET /api/$g" (Llamar 'GET' "/api/$g" $tokens['Administrador'] $null).codigo @(200, 404)
}

Write-Host "`n== 3. Permisos por rol (cuerpo vacio: 403 = sin permiso, 400 = con permiso) ==" -ForegroundColor Cyan
# ruta, metodo, rol -> esperado
$matriz = @(
  @('POST','/api/Pedidos/PostPedidos',       @{Mesero=400;Cajero=400;Administrador=400}),
  @('POST','/api/Comprobantes/PostComprobante', @{Mesero=403;Cajero=400;Administrador=400}),
  @('POST','/api/Productos/PostProductos',   @{Mesero=403;Cajero=403;Administrador=400}),
  @('POST','/api/Usuarios/PostUsuarios',     @{Mesero=403;Cajero=403;Administrador=400}),
  @('POST','/api/Roles/PostRoles',           @{Mesero=403;Cajero=403;Administrador=400}),
  @('PUT', '/api/Kardex/PutKardex',          @{Mesero=403;Cajero=403;Administrador=400}),
  @('GET', '/api/Auditorias/GetAuditorias',  @{Mesero=403;Cajero=403;Administrador=200})
)
foreach ($fila in $matriz) {
    foreach ($rol in 'Mesero', 'Cajero', 'Administrador') {
        if (-not $tokens[$rol]) { continue }
        $cuerpo = if ($fila[0] -eq 'GET') { $null } else { '{}' }
        $r = Llamar $fila[0] $fila[1] $tokens[$rol] $cuerpo
        $esperado = @($fila[2][$rol])
        if ($rol -eq 'Administrador' -and $fila[0] -eq 'GET') { $esperado = @(200, 404) }
        Verificar "$rol $($fila[0]) $($fila[1])" $r.codigo $esperado
    }
}

Write-Host "`n== 4. Cierre de sesion ==" -ForegroundColor Cyan
$t = Entrar 'Administrador'
if ($t) {
    Verificar 'Antes del logout: GET' (Llamar 'GET' '/api/Categorias/GetCategorias' $t $null).codigo @(200, 404)
    Verificar 'Logout' (Llamar 'POST' '/api/Autenticador/Logout' $t $null).codigo @(200)
    Verificar 'Despues del logout: mismo token' (Llamar 'GET' '/api/Categorias/GetCategorias' $t $null).codigo @(401)
}

Write-Host "`n== 5. Bloqueo por intentos (espera 65 s para no chocar con el limite por minuto) ==" -ForegroundColor Cyan
Start-Sleep -Seconds 65
$falso = @{ email = 'bloqueo.prueba@example.com'; password = 'incorrecta-123' } | ConvertTo-Json
1..5 | ForEach-Object { $null = Llamar 'POST' '/api/Autenticador/Login' $null $falso }
Verificar 'Intento 6 con el mismo correo' (Llamar 'POST' '/api/Autenticador/Login' $null $falso).codigo @(429)

Write-Host "`n================ RESUMEN ================" -ForegroundColor Cyan
$resultados | Format-Table -AutoSize
$fallas = @($resultados | Where-Object { $_.Resultado -eq 'FALLA' })
Write-Host ("Total: {0}  OK: {1}  FALLAS: {2}" -f $resultados.Count, ($resultados.Count - $fallas.Count), $fallas.Count) -ForegroundColor $(if ($fallas.Count) { 'Red' } else { 'Green' })
if ($fallas.Count) { Write-Host "`nCopia y pega aqui la tabla de arriba (solo las filas FALLA si son muchas)." }
